using Microsoft.Data.Sqlite;

namespace ReaderNote.Services;

public sealed class StorageService
{
    public void EnsureDatabase(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS cards (
              id TEXT PRIMARY KEY,
              title TEXT,
              content TEXT,
              x REAL NOT NULL,
              y REAL NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
