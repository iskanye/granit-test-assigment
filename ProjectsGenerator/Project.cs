using System.Text.Json;

namespace ProjectsGenerator;

public class Project(string path, string name, string equipment)
{
    public string Name { get; } = name;
    public string Equipment { get; } = equipment;

    public async Task Save()
    {
        var dirPath = path + $"\\{Name}";

        Directory.CreateDirectory(dirPath);
        await using var stream = File.CreateText(dirPath + $"\\project.json");
        var json = JsonSerializer.SerializeAsync(stream.BaseStream, this, JsonSerializerOptions.Default);

        using var db = new Database(dirPath + $"\\{Name}.db");
        db.Init();

        await json;
    }
}