using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace TestAssigment.Models;

public class Project : IDisposable
{
    private class JsonProject
    {
        public string Name { get; }
        public string Equipment { get; }
    }

    public SqliteConnection Database { get; }
    public SqliteConnection? ChecksResultDB { get; }

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
                Database = new SqliteConnection($"Filename={uri.LocalPath + "\\checkResults.db"}.db");
            }
        }
        else
        {
            throw new FileNotFoundException("Не найден файл проекта", uri.LocalPath);
        }
    }

    public void Dispose()
    {
        Database.Dispose();
        ChecksResultDB?.Dispose();

        GC.SuppressFinalize(this);
    }
}