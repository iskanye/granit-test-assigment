using Microsoft.Data.Sqlite;

namespace ProjectsGenerator;

public static class Program
{
    private const int MinInserts = 99;
    private const int MaxInserts = 2000;

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
        )
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
        )
        """;

    private const string Contacts = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private static readonly string[] CheckTypes =
    [
        "напряжения",
        "сопротивления",
        "тока",
    ];

    private static readonly string[] Objects =
    [
        "ЛОГ",
        "МОД",
        "ПРОД",
        "МИД",
        "МУР"
    ];

    private static readonly Dictionary<string, string[]> Modifications = new()
    {
        { "ЛОГ", ["ЛОГ0", "ЛОГ4", "ЛОГ5", "ЛОГ7", "ЛОГ9"] },
        { "МОД", ["МОД1", "МОД2", "МОД4", "МОД6", "МОД7"] },
        { "ПРОД", ["ПРОД2", "ПРОД3", "ПРОД6", "ПРОД8", "ПРОД11"] },
        { "МИД", ["МИД1", "МИД2", "МИД3", "МИД6", "МИД9"] },
        { "МУР", ["МУР1", "МУР3", "МУР8", "МУР9", "МУР11", "МУР12"] }
    };

    private static void InitDatabase(SqliteConnection conn)
    {
        conn.Open();

        // Создание таблицы
        using var create = new SqliteCommand(TableCreationStmt, conn);
        create.ExecuteNonQuery();

        var inserts = Random.Shared.Next(MinInserts, MaxInserts);

        for (int i = 0; i < inserts; i++)
        {
            // Подготовка к вставке
            var contactsArr = Contacts.ToArray();
            Random.Shared.Shuffle(contactsArr);

            var contact1 = contactsArr[0];
            var contact2 = contactsArr[1];
            var checkType = CheckTypes[Random.Shared.Next(0, CheckTypes.Length)];
            var checkedObject = Objects[Random.Shared.Next(0, Objects.Length)];

            var objMods = Modifications[checkedObject];
            var modification = objMods[Random.Shared.Next(1, objMods.Length)];

            // Вставка
            using var insert = new SqliteCommand(TableInsertionStmt, conn);
            insert.Parameters.Add("@contact1", SqliteType.Text).Value = "Контакт " + contact1;
            insert.Parameters.Add("@contact2", SqliteType.Text).Value = "Контакт " + contact2;
            insert.Parameters.Add("@checkType", SqliteType.Text).Value = "измерение " + checkType;
            insert.Parameters.Add("@object", SqliteType.Text).Value = "Объект " + checkedObject;
            insert.Parameters.Add("@modification", SqliteType.Text).Value = modification;
            insert.Parameters.Add("@checkResult", SqliteType.Real).Value = Random.Shared.NextDouble() * 1000;

            insert.ExecuteNonQuery();
        }
    }

    public static int Main(string[] args)
    {
        try
        {
            var arguments = new Arguments(args);

            var connectionString = "Data Source=usersdata.db";
            using var conn = new SqliteConnection(connectionString);
            InitDatabase(conn);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 1;
        }

        return 0;
    }
}