using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FastReport;
using Microsoft.Data.Sqlite;

namespace TestAssigment.Models;

public class Project : IDisposable
{
    private class JsonProject
    {
        public string Name { get; set; }
        public string Equipment { get; set; }
    }

    public string Name { get; }

    public SqliteConnection Database { get; }
    public SqliteConnection? ChecksResultDb { get; }

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

            // Проверяем есть ли файл проверок
            if (files.Any(f => f.Name == "checkResults.db"))
            {
                ChecksResultDb = new SqliteConnection($"Filename={uri.LocalPath + "\\checkResults.db"}.db");
            }

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

    public void SaveToDB(string obj, string modification, int checkNum, IEnumerable<Check> checks)
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
        var report = new Report();
    }

    public void Dispose()
    {
        Database.Close();
        ChecksResultDb?.Close();
        Database.Dispose();
        ChecksResultDb?.Dispose();

        GC.SuppressFinalize(this);
    }
}