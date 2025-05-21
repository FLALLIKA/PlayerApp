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

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string sql = @"CREATE TABLE IF NOT EXISTS Playlists (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Name TEXT NOT NULL,
                                PlaylistData TEXT NOT NULL
                              );";

                var command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }
    }

    public static void SavePlaylist(string name, List<string> tracks)
    {
        string json = JsonConvert.SerializeObject(tracks);

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string sql = "INSERT INTO Playlists (Name, PlaylistData) VALUES (@name, @data)";
            var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@data", json);
            command.ExecuteNonQuery();
        }
    }

    public static List<Playlist> LoadAllPlaylists()
    {
        var playlists = new List<Playlist>();

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            string sql = "SELECT Id, Name, PlaylistData FROM Playlists";
            var command = new SQLiteCommand(sql, connection);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var playlist = new Playlist
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Tracks = JsonConvert.DeserializeObject<List<string>>(reader.GetString(2))
                    };
                    playlists.Add(playlist);
                }
            }
        }

        return playlists;
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