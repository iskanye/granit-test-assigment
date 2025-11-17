using Microsoft.Data.Sqlite;

const string tableCreationStmt = """
create table if not exists checks
(
    № integer primary key autoincrement unique,
    Контакт_1 text not null,
    Контакт_2 text not null,
    Тип_проверки text not null,
    Объект text no null,
    Модификация text not null,
    Результат_проверки integer 
)
""";

if (args.Length == 0)
{
    Console.WriteLine("no args");
    return 1;
}

var path = args[0];

try
{
    var connectionString = "Data Source=usersdata.db";
    using var conn = new SqliteConnection(connectionString);
    conn.Open();

    var cmd = new SqliteCommand(tableCreationStmt, conn);

    cmd.ExecuteNonQuery();
}
catch (Exception e)
{
    Console.WriteLine("error: " + e.Message);
}

return 0;
