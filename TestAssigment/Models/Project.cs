using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    public void SaveToDB(string obj, string modification, int checkNum, List<Check> checks)
    {
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