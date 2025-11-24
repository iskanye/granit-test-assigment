using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using FastReport;
using FastReport.Data;
using FastReport.Export.Html;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using Microsoft.Data.Sqlite;

namespace TestAssigment.Models;

public class Project : IDisposable
{
    private class JsonProject
    {
        public string Name { get; set; } = "";
        public string Equipment { get; set; } = "";
    }

    public string Name { get; }

    public SqliteConnection Database { get; }

    private const string LoadObjectsStmt =
        """
        SELECT name FROM objects;
        """;

    private const string LoadModificationsStmt =
        """
        SELECT name 
        FROM modifications
        WHERE obj_id = (SELECT obj_id FROM objects WHERE name = @obj_name);
        """;


    private const string LoadCheckNumsStmt =
        """
        SELECT DISTINCT check_num 
        FROM checks
        WHERE 
            obj_id = (SELECT obj_id FROM objects WHERE name = @obj_name) AND
            modifications LIKE @modification
        ;
        """;

    private const string LoadCheckStmt =
        """
        SELECT c.contact1, l1.port, c.contact2, l2.port, c.check_type, c.modifications 
        FROM checks c
        JOIN layout l1 ON c.obj_id = l1.obj_id AND c.contact1 = l1.contact
        JOIN layout l2 ON c.obj_id = l2.obj_id AND c.contact2 = l2.contact
        WHERE 
            c.obj_id = (SELECT o.obj_id FROM objects o WHERE o.name = @obj_name) AND
            c.modifications LIKE @modification AND
            c.check_num = @check_num
        ;
        """;

    private const string ReportCreateStmt =
        """
        DROP TABLE IF EXISTS report;
        CREATE TABLE IF NOT EXISTS report 
        (
            n INTEGER NOT NULL,
            contact1 TEXT NOT NULL,
            port1 TEXT NOT NULL,
            contact2 TEXT NOT NULL,
            port2 TEXT NOT NULL,
            check_type TEXT NOT NULL,
            object TEXT NOT NULL,
            modifications TEXT NOT NULL,
            result INTEGER NOT NULL
        );
        """;

    private const string ReportInsertStmt =
        """
        INSERT INTO report (n, contact1, port1, contact2, port2, check_type, object, modifications, result)
        VALUES (@n, @contact1, @port1, @contact2, @port2, @check_type, @object, @modifications, @result);
        """;

    private Uri _uri;

    public Project(Uri uri)
    {
        _uri = uri;
        var files = (new DirectoryInfo(uri.LocalPath)).GetFiles();

        // Проверяем есть ли в данной папке файл проекта
        if (files.Any(f => f.Name == "project.json"))
        {
            var jsonProject = File.ReadAllText(files.First(f => f.Name == "project.json").FullName);
            var project = JsonSerializer.Deserialize<JsonProject>(jsonProject)!;

            Database = new SqliteConnection($"Filename={uri.LocalPath + '\\' + project.Name}.db");

            Name = project.Name;
        }
        else
        {
            throw new FileNotFoundException("Не найден файл проекта", uri.LocalPath);
        }

        Database.Open();
    }

    public List<string> LoadObjects()
    {
        using var loadObjects = new SqliteCommand(LoadObjectsStmt, Database);
        var reader = loadObjects.ExecuteReader();

        var result = new List<string>();

        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public List<string> LoadModification(string obj)
    {
        using var loadModifications = new SqliteCommand(LoadModificationsStmt, Database);
        loadModifications.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;

        var reader = loadModifications.ExecuteReader();
        var result = new List<string>();
        // Добавить возможность выбрать все модификации
        result.Add(" ");

        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public List<int> LoadCheckNums(string obj, string modification)
    {
        using var loadCheckNums = new SqliteCommand(LoadCheckNumsStmt, Database);
        loadCheckNums.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;
        loadCheckNums.Parameters.Add("@modification", SqliteType.Text).Value = $"%{modification}%";

        var reader = loadCheckNums.ExecuteReader();
        var result = new List<int>();

        while (reader.Read())
        {
            result.Add(reader.GetInt32(0));
        }

        return result;
    }

    public List<Check> LoadChecks(string obj, string modification, int checkNum)
    {
        using var loadChechs = new SqliteCommand(LoadCheckStmt, Database);
        loadChechs.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;
        loadChechs.Parameters.Add("@modification", SqliteType.Text).Value = $"%{modification}%";
        loadChechs.Parameters.Add("@check_num", SqliteType.Integer).Value = checkNum;

        var reader = loadChechs.ExecuteReader();
        var result = new List<Check>();

        ulong i = 1;
        while (reader.Read())
        {
            var check = new Check(
                i++,
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                obj,
                reader.GetString(5),
                null);
            result.Add(check);
        }

        return result;
    }

    public void SaveToDb(string obj, string modification, int checkNum, IEnumerable<Check> checks)
    {
        using var conn = new SqliteConnection($"Filename={_uri.LocalPath}\\{obj}_{modification}_{checkNum}.db");
        conn.Open();

        using var create = new SqliteCommand(ReportCreateStmt, conn);
        create.ExecuteNonQuery();

        var i = 1;
        foreach (var check in checks)
        {
            using var insert = new SqliteCommand(ReportInsertStmt, conn);
            insert.Parameters.Add("@n", SqliteType.Integer).Value = i++;
            insert.Parameters.Add("@object", SqliteType.Text).Value = obj;
            insert.Parameters.Add("@contact1", SqliteType.Text).Value = check.Contact1;
            insert.Parameters.Add("@port1", SqliteType.Text).Value = check.Port1;
            insert.Parameters.Add("@contact2", SqliteType.Text).Value = check.Contact2;
            insert.Parameters.Add("@port2", SqliteType.Text).Value = check.Port2;
            insert.Parameters.Add("@modifications", SqliteType.Text).Value = check.Modifications;
            insert.Parameters.Add("@check_type", SqliteType.Text).Value = check.CheckType;
            insert.Parameters.Add("@result", SqliteType.Integer).Value = check.CheckResult;

            insert.ExecuteNonQuery();
        }

        conn.Close();
    }

    public void SaveReport(string obj, string modification, int checkNum, IEnumerable<Check> checks)
    {
        using var conn = new SQLiteConnection($"Data Source={_uri.LocalPath}\\{obj}_{modification}_{checkNum}.db");
        conn.Open();

        using var select = new SQLiteDataAdapter("SELECT * FROM report ORDER BY n;", conn);

        var dataSet = new DataSet();
        select.Fill(dataSet, "report");

        var report = new Report();
        report.RegisterData(dataSet.Tables["report"], "Checks");
        report.GetDataSource("Checks").Enabled = true;

        ReportPage page = new ReportPage();
        page.Name = "Page";
        report.Pages.Add(page);

// create ReportTitle band
        page.ReportTitle = new ReportTitleBand();
        page.ReportTitle.Name = "ReportTitle";

// set its height to 1.5cm
        page.ReportTitle.Height = Units.Centimeters * 1.5f;

// create group header
        GroupHeaderBand group1 = new GroupHeaderBand();
        group1.Name = "GroupHeader";
        group1.Height = Units.Centimeters * 1;

// set group condition
        group1.Condition = "[Checks.object]";

// add group to the page.Bands collection
        page.Bands.Add(group1);

// create group footer
        group1.GroupFooter = new GroupFooterBand();
        group1.GroupFooter.Name = "GroupFooter";
        group1.GroupFooter.Height = Units.Centimeters * 1;

// create DataBand
        DataBand data = new DataBand();
        data.Name = "Data";
        data.Height = Units.Centimeters * 0.5f;

// set data source
        data.DataSource = report.GetDataSource("Checks");

// connect databand to a group
        group1.Data = data;
        // create "Text" objects
// report title
        TextObject text1 = new TextObject();
        text1.Name = "Header";

// set bounds
        text1.Bounds = new RectangleF(0, 0, Units.Centimeters * 19, Units.Centimeters * 1);

// set text
        text1.Text = "РЕЗУЛЬТАТЫ ПРОВЕРОК";

// set appearance
        text1.HorzAlign = HorzAlign.Center;
        text1.Font = new Font("Tahoma", 14, FontStyle.Bold);

// add it to ReportTitle
        page.ReportTitle.Objects.Add(text1);

        TextObject text3 = new TextObject();
        text3.Name = "Contact1";
        text3.Bounds = new RectangleF(Units.Centimeters * 1, 0, Units.Centimeters * 2, Units.Centimeters * 0.5f);
        text3.Text = "[Checks.contact1]";
        text3.Font = new Font("Tahoma", 8);

        data.Objects.Add(text3);

        TextObject text4 = new TextObject();
        text4.Name = "Port1";
        text4.Bounds = new RectangleF(Units.Centimeters * 3, 0, Units.Centimeters * 2, Units.Centimeters * 0.5f);
        text4.Text = "[Checks.port1]";
        text4.Font = new Font("Tahoma", 8);

        data.Objects.Add(text4);

        TextObject text5 = new TextObject();
        text5.Name = "Contact2";
        text5.Bounds = new RectangleF(Units.Centimeters * 5, 0, Units.Centimeters * 2, Units.Centimeters * 0.5f);
        text5.Text = "[Checks.contact2]";
        text5.Font = new Font("Tahoma", 8);

        data.Objects.Add(text5);

        TextObject text6 = new TextObject();
        text6.Name = "Port2";
        text6.Bounds = new RectangleF(Units.Centimeters * 7, 0, Units.Centimeters * 2, Units.Centimeters * 0.5f);
        text6.Text = "[Checks.port2]";
        text6.Font = new Font("Tahoma", 8);

        data.Objects.Add(text6);

        TextObject text7 = new TextObject();
        text7.Name = "CheckType";
        text7.Bounds = new RectangleF(Units.Centimeters * 9, 0, Units.Centimeters * 5, Units.Centimeters * 0.5f);
        text7.Text = "[Checks.check_type]";
        text7.Font = new Font("Tahoma", 8);

        data.Objects.Add(text7);

        TextObject text8 = new TextObject();
        text8.Name = "Object";
        text8.Bounds = new RectangleF(Units.Centimeters * 14, 0, Units.Centimeters * 2, Units.Centimeters * 0.5f);
        text8.Text = "[Checks.object]";
        text8.Font = new Font("Tahoma", 8);

        data.Objects.Add(text8);

        TextObject text9 = new TextObject();
        text9.Name = "Modifications";
        text9.Bounds = new RectangleF(Units.Centimeters * 16, 0, Units.Centimeters * 6, Units.Centimeters * 0.5f);
        text9.Text = "[Checks.modifications]";
        text9.Font = new Font("Tahoma", 8);

        data.Objects.Add(text9);

        TextObject text10 = new TextObject();
        text10.Name = "CheckResults";
        text10.Bounds = new RectangleF(Units.Centimeters * 22, 0, Units.Centimeters * 5, Units.Centimeters * 0.5f);
        text10.Text = "[Checks.result]";
        text10.Font = new Font("Tahoma", 8);

        data.Objects.Add(text10);

        report.Prepare();

        var export = new HTMLExport();
        report.Export(export, $"{_uri.AbsolutePath}\\{obj}_{modification}_{checkNum}.html");
    }

    public void Dispose()
    {
        Database.Close();
        Database.Dispose();

        GC.SuppressFinalize(this);
    }
}