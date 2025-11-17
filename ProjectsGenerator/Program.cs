using Microsoft.Data.Sqlite;

class Program
{
    const int minInserts = 99;
    const int maxInserts = 2000;

    const string tableCreationStmt =
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

    const string tableInsertionStmt =
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

    const string contacts = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    readonly static string[] checkTypes =
    [
        "напряжения",
        "сопротивления",
        "тока",
    ];

    readonly static string[] objects =
    [
        "ЛОГ",
        "МОД",
        "ПРОД",
        "МИД",
        "МУР"
    ];

    readonly static Dictionary<string, string[]> modifications = new()
    {
        {"ЛОГ", ["ЛОГ0", "ЛОГ4", "ЛОГ5", "ЛОГ7", "ЛОГ9"]},
        {"МОД", ["МОД1", "МОД2", "МОД4", "МОД6", "МОД7"]},
        {"ПРОД", ["ПРОД2", "ПРОД3", "ПРОД6", "ПРОД8", "ПРОД11"]},
        {"МИД", ["МИД1", "МИД2", "МИД3", "МИД6", "МИД9"]},
        {"МУР", ["МУР1", "МУР3", "МУР8", "МУР9", "МУР11", "МУР12"]}
    };

    static void InitDB(SqliteConnection conn)
    {
        conn.Open();

        // Создание таблицы
        using var create = new SqliteCommand(tableCreationStmt, conn);
        create.ExecuteNonQuery();

        var inserts = Random.Shared.Next(minInserts, maxInserts);

        for (int i = 0; i < inserts; i++)
        {
            // Подготовка к вставке
            var contactsArr = contacts.ToArray();
            Random.Shared.Shuffle(contactsArr);

            var contact1 = contactsArr[0];
            var contact2 = contactsArr[1];
            var checkType = checkTypes[Random.Shared.Next(0, checkTypes.Length)];
            var checkedObject = objects[Random.Shared.Next(0, objects.Length)];

            var objMods = modifications[checkedObject];
            var modification = objMods[Random.Shared.Next(1, objMods.Length)];

            // Вставка
            using var insert = new SqliteCommand(tableInsertionStmt, conn);
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
        if (args.Length == 0)
        {
            Console.WriteLine("no args");
        }

        try
        {
            var connectionString = "Data Source=usersdata.db";
            using var conn = new SqliteConnection(connectionString);
            InitDB(conn);
        }
        catch (Exception e)
        {
            Console.WriteLine("error: " + e.ToString());
        }

        return 0;
    }
}