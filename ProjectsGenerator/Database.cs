using Microsoft.Data.Sqlite;

namespace ProjectsGenerator;

public class Database(string path) : IDisposable
{
    private const int MaxChecksPerObject = 9;
    private const int MaxStepsPerCheck = 200;
    private const int MaxModifications = 5;
    private const int MaxPortId = 10;

    // Команды для создания таблиц в базе данных
    private const string TableCreationStmt =
        """
        CREATE TABLE IF NOT EXISTS objects
        (
            obj_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
            name TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS modifications
        (
            obj_id INTEGER NOT NULL,
            name TEXT NOT NULL,
            FOREIGN KEY (obj_id) REFERENCES objects(obj_id)
        );
        CREATE TABLE IF NOT EXISTS layout
        (
            obj_id INTEGER NOT NULL,
            contact TEXT NOT NULL,
            port TEXT NOT NULL,
            FOREIGN KEY (obj_id) REFERENCES objects(obj_id)
        );
        CREATE TABLE IF NOT EXISTS checks
        (
            obj_id INTEGER NOT NULL,
            check_num INTEGER NOT NULL,
            contact1 TEXT NOT NULL,
            contact2 TEXT NOT NULL,
            check_type TEXT NOT NULL,
            modifications TEXT NOT NULL UNIQUE,
            FOREIGN KEY (obj_id) REFERENCES objects(obj_id)
        );
        """;

    // Команды для вставок в таблицы
    private const string ObjectsInsertStmt =
        """
        INSERT INTO objects (name)
        VALUES (@name);
        """;

    private const string ModificationsInsertStmt =
        """
        INSERT INTO modifications (obj_id, name)
        VALUES 
        (
            (SELECT obj_id FROM objects WHERE name = @obj_name),
            @name
        );
        """;

    private const string LayoutInsertStmt =
        """
        INSERT INTO layout (obj_id, contact, port)
        VALUES 
        (
            (SELECT obj_id FROM objects WHERE name = @obj_name),
            @contact, @port
        );
        """;

    private const string ChecksInsertStmt =
        """
        INSERT INTO checks (obj_id, check_num, contact1, contact2, check_type, modifications)
        VALUES 
        (
            @obj_id,
            @check_num,
            @contact1,
            @contact2,
            @check_type,
            @modifications
        );
        """;

    // Получения id объекта
    private const string GetObjectIdStmt =
        "SELECT obj_id FROM objects WHERE name = @obj_name;";

    // Получение контакта объекта
    private const string GetContactsStmt =
        "SELECT contact FROM layout WHERE obj_id = @obj_id ORDER BY RANDOM() LIMIT 2;";

    private readonly SqliteConnection _connection = new($"Data Source={path}");

    public void Init()
    {
        _connection.Open();

        // Создание таблицы
        using var create = new SqliteCommand(TableCreationStmt, _connection);
        create.ExecuteNonQuery();

        // Вставка объектов
        foreach (var obj in ChecksConstants.Objects)
        {
            using var insertObj = new SqliteCommand(ObjectsInsertStmt, _connection);

            insertObj.Parameters.Add("@name", SqliteType.Text).Value = obj;
            insertObj.ExecuteNonQuery();
        }

        // Вставка модификаций
        foreach (var (obj, mods) in ChecksConstants.Modifications)
        {
            foreach (var mod in mods)
            {
                using var insertMods = new SqliteCommand(ModificationsInsertStmt, _connection);

                insertMods.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;
                insertMods.Parameters.Add("@name", SqliteType.Text).Value = mod;

                insertMods.ExecuteNonQuery();
            }
        }

        // Вставка контактов
        foreach (var obj in ChecksConstants.Objects)
        {
            // Составление списка контактов
            var contacts = new char[ChecksConstants.Contacts.Length];
            ChecksConstants.Contacts.CopyTo(contacts);
            Random.Shared.Shuffle(contacts);

            for (int i = 0; i < Random.Shared.Next(2, contacts.Length + 1); i++)
            {
                var contact = "Контакт " + contacts[i];
                var port = "Порт " + Random.Shared.Next(1, MaxPortId);

                using var insertMods = new SqliteCommand(LayoutInsertStmt, _connection);

                insertMods.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;
                insertMods.Parameters.Add("@contact", SqliteType.Text).Value = contact;
                insertMods.Parameters.Add("@port", SqliteType.Text).Value = port;

                insertMods.ExecuteNonQuery();
            }
        }

        // Вставка проверок
        foreach (var obj in ChecksConstants.Objects)
        {
            var checks = Random.Shared.Next(1, MaxChecksPerObject + 1);
            for (int check = 0; check < checks; check++)
            {
                var steps = Random.Shared.Next(1, MaxStepsPerCheck + 1);
                for (int j = 0; j < steps; j++)
                {
                    var checkType = "Измерение " + ChecksConstants.RandomElement(ChecksConstants.CheckTypes);

                    // Ограничиваем количество модификаций их максимальным числом
                    var modsCount = Math.Min(Random.Shared.Next(1, MaxModifications),
                        ChecksConstants.Modifications[obj].Length);

                    // Составление списка модификаций
                    var modsList = new string[ChecksConstants.Modifications[obj].Length];
                    ChecksConstants.Modifications[obj].CopyTo(modsList, 0);
                    Random.Shared.Shuffle(modsList);

                    var mods = new string[modsCount];
                    for (int k = 0; k < modsCount; k++)
                    {
                        mods[k] = modsList[k];
                    }

                    // Поиск id объекта
                    using var getId = new SqliteCommand(GetObjectIdStmt, _connection);
                    getId.Parameters.Add("@obj_name", SqliteType.Text).Value = obj;

                    var objId = (long)(getId.ExecuteScalar() ?? 0);

                    // Получение контактов
                    using var getContacts = new SqliteCommand(GetContactsStmt, _connection);
                    getContacts.Parameters.Add("@obj_id", SqliteType.Integer).Value = objId;

                    var contacts = new string[2];
                    using var reader = getContacts.ExecuteReader();

                    // Запись списка контактов в массив
                    reader.Read();
                    contacts[0] = reader.GetString(0);
                    reader.Read();
                    contacts[1] = reader.GetString(0);

                    // Запуск вставки
                    using var insertCheck = new SqliteCommand(ChecksInsertStmt, _connection);
                    insertCheck.Parameters.Add("@obj_id", SqliteType.Integer).Value = objId;
                    insertCheck.Parameters.Add("@check_num", SqliteType.Integer).Value = check + 1;
                    insertCheck.Parameters.Add("@contact1", SqliteType.Text).Value = contacts[0];
                    insertCheck.Parameters.Add("@contact2", SqliteType.Text).Value = contacts[1];
                    insertCheck.Parameters.Add("@check_type", SqliteType.Text).Value = checkType;
                    insertCheck.Parameters.Add("@modifications", SqliteType.Text).Value = string.Join(", ", mods);

                    try
                    {
                        insertCheck.ExecuteNonQuery();
                    }
                    catch (SqliteException ex)
                    {
                        if (ex.SqliteErrorCode == 19)
                            continue;
                        break;
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}