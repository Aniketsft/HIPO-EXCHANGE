using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;

namespace AutoEmailer
{
    public class FailedEmailRecord
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string ProfileType { get; set; }
        public DateTime AddedDate { get; set; }
    }

    public static class RetryEngine
    {
        private static string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hipodoc.db");
        private static string ConnectionString => $"Data Source={DbPath}";
        
        static RetryEngine()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS FailedEmails (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FilePath TEXT NOT NULL,
                        ProfileType TEXT NOT NULL,
                        AddedDate DATETIME NOT NULL
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        public static void AddFailed(string filePath, string profileType)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO FailedEmails (FilePath, ProfileType, AddedDate)
                    VALUES ($filePath, $profileType, $addedDate)
                ";
                command.Parameters.AddWithValue("$filePath", filePath);
                command.Parameters.AddWithValue("$profileType", profileType);
                command.Parameters.AddWithValue("$addedDate", DateTime.UtcNow);
                command.ExecuteNonQuery();
            }
        }

        public static List<FailedEmailRecord> GetPendingRetries()
        {
            var records = new List<FailedEmailRecord>();
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, FilePath, ProfileType, AddedDate FROM FailedEmails";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(new FailedEmailRecord
                        {
                            Id = reader.GetInt32(0),
                            FilePath = reader.GetString(1),
                            ProfileType = reader.GetString(2),
                            AddedDate = reader.GetDateTime(3)
                        });
                    }
                }
            }
            return records;
        }

        public static void RemoveRecord(int id)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM FailedEmails WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}
