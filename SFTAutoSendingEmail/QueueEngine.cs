using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace AutoEmailer
{
    public class PendingEmail
    {
        public long Id { get; set; }
        public string FilePath { get; set; } = "";
        public string ProfileId { get; set; } = "";
        public string EmailAddress { get; set; } = "";
    }

    public class QueueEngine
    {
        private readonly string _connectionString;

        public QueueEngine()
        {
            _connectionString = "Data Source=hipodoc.db";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS PendingEmails (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FilePath TEXT NOT NULL,
                        ProfileId TEXT NOT NULL,
                        EmailAddress TEXT NOT NULL
                    )";
                command.ExecuteNonQuery();
            }
        }

        public void AddPendingEmail(string filePath, string profileId, string emailAddress)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO PendingEmails (FilePath, ProfileId, EmailAddress)
                    VALUES ($filePath, $profileId, $emailAddress)";
                command.Parameters.AddWithValue("$filePath", filePath);
                command.Parameters.AddWithValue("$profileId", profileId);
                command.Parameters.AddWithValue("$emailAddress", emailAddress);
                command.ExecuteNonQuery();
            }
        }

        public List<PendingEmail> GetPendingEmails()
        {
            var results = new List<PendingEmail>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, FilePath, ProfileId, EmailAddress FROM PendingEmails";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new PendingEmail
                        {
                            Id = reader.GetInt64(0),
                            FilePath = reader.GetString(1),
                            ProfileId = reader.GetString(2),
                            EmailAddress = reader.GetString(3)
                        });
                    }
                }
            }
            return results;
        }

        public void RemoveRecord(long id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM PendingEmails WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}
