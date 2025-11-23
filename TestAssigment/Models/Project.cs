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
        SELECT contact1, contact2, check_type, modifications 
        FROM checks
        WHERE 
            obj_id = (SELECT obj_id FROM objects WHERE name = @obj_name) AND
            modifications LIKE @modification AND
            check_num = @check_num
        ;
        """;

    public Project(Uri uri)
    {
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

    public List<Check> LoadChecks(string obj, string modification, int check_num)
    {
        using var loadChechs = new SqliteCommand(LoadCheckStmt, Database);
        loadChechs.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;
        loadChechs.Parameters.Add("@modification", SqliteType.Text).Value = $"%{modification}%";
        loadChechs.Parameters.Add("@check_num", SqliteType.Integer).Value = check_num;

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
                obj,
                reader.GetString(3),
                null);
            result.Add(check);
        }

        return result;
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