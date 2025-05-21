using Newtonsoft.Json;
using PlayerApp;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

public static class DatabaseHelper
{
    private static string databaseFile = "playlists.db";
    private static string connectionString = $"Data Source={databaseFile};Version=3;";

    public static void InitializeDatabase()
    {
        if (!File.Exists(databaseFile))
        {
            SQLiteConnection.CreateFile(databaseFile);
        }

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            // Создаём таблицу Categories, если её нет
            string createCategories = @"
            CREATE TABLE IF NOT EXISTS Categories (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE
            )";

            new SQLiteCommand(createCategories, connection).ExecuteNonQuery();

            // Создаём таблицу Playlists с внешним ключом
            string createPlaylists = @"
            CREATE TABLE IF NOT EXISTS Playlists (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            CategoryId INTEGER NOT NULL,
            PlaylistData TEXT NOT NULL,
            FOREIGN KEY(CategoryId) REFERENCES Categories(Id)
            )";

            new SQLiteCommand(createPlaylists, connection).ExecuteNonQuery();
        }
    }

    public static void SavePlaylist(string name, int categoryId, List<string> tracks)
    {
        string json = JsonConvert.SerializeObject(tracks);

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string sql = @"INSERT INTO Playlists (Name, CategoryId, PlaylistData) 
                      VALUES (@name, @categoryId, @data)";
            var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@categoryId", categoryId);
            command.Parameters.AddWithValue("@data", json);
            command.ExecuteNonQuery();
        }
    }

    public static List<Playlist> GetPlaylistsByCategory(int categoryId)
    {
        var playlists = new List<Playlist>();

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string sql = @"SELECT Id, Name, PlaylistData FROM Playlists 
                      WHERE CategoryId = @categoryId ORDER BY Name";
            var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@categoryId", categoryId);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    playlists.Add(new Playlist
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        CategoryId = categoryId,
                        Tracks = JsonConvert.DeserializeObject<List<string>>(reader.GetString(2))
                    });
                }
            }
        }

        return playlists;
    }

    public static List<Category> GetAllCategories()
    {
        var categories = new List<Category>();

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string sql = "SELECT Id, Name FROM Categories ORDER BY Name";
            var command = new SQLiteCommand(sql, connection);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    categories.Add(new Category
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1)
                    });
                }
            }
        }

        return categories;
    }

    public static int AddCategory(string name)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            string sql = "INSERT INTO Categories (Name) VALUES (@name); SELECT last_insert_rowid();";
            var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@name", name);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    public static void DeletePlaylist(int id)
    {
        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string sql = "DELETE FROM Playlists WHERE Id = @id";
            var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
    }
}