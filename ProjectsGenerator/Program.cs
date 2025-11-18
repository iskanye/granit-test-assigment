using Microsoft.Data.Sqlite;

namespace ProjectsGenerator;

public static class Program
{
    private const string TableCreationStmt =
        """
        CREATE TABLE IF NOT EXISTS checks
        (
            № INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE,
            Контакт_1 TEXT NOT NULL,
            Контакт_2 TEXT NOT NULL,
            Тип_проверки TEXT NOT NULL,
            Объект TEXT NOT NULL,
            Модификация TEXT NOT NULL,
            Результат_проверки REAL 
        );
        """;

    private const string TableInsertionStmt =
        """
        INSERT INTO checks
        (
            Контакт_1,
            Контакт_2,
            Тип_проверки,
            Объект,
            Модификация,
            Результат_проверки 
        ) 
        VALUES
        (
            @contact1,
            @contact2,
            @checkType,
            @object,
            @modification,
            @checkResult
        );
        """;

    private static Arguments _args;

    public static int Main(string[] args)
    {
        try
        {
            _args = new Arguments(args);

            using var db = new Database(_args.Path);
            db.Init();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return 1;
        }

        return 0;
    }
}