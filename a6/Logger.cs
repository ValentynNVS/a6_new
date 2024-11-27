using System;
using System.IO;

namespace a6_win
{
    public static class Logger
    {
        public static void Log(string message)
        {
            try
            {
                // Read the log file path from app.config
                string logFilePath = System.Configuration.ConfigurationManager.AppSettings["LogFilePath"];

                // Ensure the directory exists
                string directory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create or append to the log file
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    // Write log message with a timestamp
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur while logging
                Console.WriteLine($"Error logging message: {ex.Message}");
            }
        }
    }
}
