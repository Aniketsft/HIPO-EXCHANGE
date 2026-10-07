using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace AutoEmailer
{
    public class SystemState
    {
        public int Id { get; set; }
        public string LastHeartbeat { get; set; } = string.Empty;
        public string EngineCommand { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
    }

    public static class SystemStateManager
    {
        private static string GetConnectionString()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hipodoc.db");
            return $"Data Source={dbPath}";
        }

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SystemState (
                        Id INTEGER PRIMARY KEY,
                        LastHeartbeat TEXT,
                        EngineCommand TEXT,
                        CurrentStatus TEXT
                    );
                    INSERT OR IGNORE INTO SystemState (Id, LastHeartbeat, EngineCommand, CurrentStatus)
                    VALUES (1, '', 'STOP', 'STOPPED');
                ";
                command.ExecuteNonQuery();
            }
        }

        public static SystemState GetState()
        {
            InitializeDatabase();
            var state = new SystemState { Id = 1 };
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT LastHeartbeat, EngineCommand, CurrentStatus FROM SystemState WHERE Id = 1";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        state.LastHeartbeat = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        state.EngineCommand = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        state.CurrentStatus = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    }
                }
            }
            return state;
        }

        public static void SetCommand(string commandStr)
        {
            InitializeDatabase();
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE SystemState SET EngineCommand = @cmd WHERE Id = 1";
                command.Parameters.AddWithValue("@cmd", commandStr);
                command.ExecuteNonQuery();
            }
        }

        public static void SetHeartbeatAndStatus(string heartbeat, string status)
        {
            InitializeDatabase();
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE SystemState SET LastHeartbeat = @hb, CurrentStatus = @st WHERE Id = 1";
                command.Parameters.AddWithValue("@hb", heartbeat);
                command.Parameters.AddWithValue("@st", status);
                command.ExecuteNonQuery();
            }
        }
    }
}
