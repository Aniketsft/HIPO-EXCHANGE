using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoEmailer
{
    public class LogEntry
    {
        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Profile { get; set; }
        public string Message { get; set; }
    }

    public static class HipodocLogger
    {
        private static readonly object _lock = new object();
        private static string LogDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        public static void Log(string level, string profile, string message)
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
                    string logFile = Path.Combine(LogDirectory, $"log_{dateStr}.json");

                    List<LogEntry> logs = new List<LogEntry>();
                    if (File.Exists(logFile))
                    {
                        string existingContent = File.ReadAllText(logFile);
                        if (!string.IsNullOrWhiteSpace(existingContent))
                        {
                            try
                            {
                                logs = JsonSerializer.Deserialize<List<LogEntry>>(existingContent) ?? new List<LogEntry>();
                            }
                            catch
                            {
                                // Handle bad json format gracefully
                            }
                        }
                    }

                    logs.Add(new LogEntry
                    {
                        Timestamp = DateTime.Now.ToString("o"),
                        Level = level,
                        Profile = profile,
                        Message = message
                    });

                    string newContent = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(logFile, newContent);
                }
            }
            catch
            {
                // In a true logger, handle this
            }
        }

        public static void Info(string profile, string message) => Log("INFO", profile, message);
        public static void Error(string profile, string message) => Log("ERROR", profile, message);
        public static void Warn(string profile, string message) => Log("WARN", profile, message);
    }
}
