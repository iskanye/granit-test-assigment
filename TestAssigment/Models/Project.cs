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

    public void Dispose()
    {
        Database.Close();
        ChecksResultDb?.Close();
        Database.Dispose();
        ChecksResultDb?.Dispose();

        GC.SuppressFinalize(this);
    }
}