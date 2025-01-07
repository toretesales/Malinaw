using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Malinaw
{

    //To log whatever is happening to a text string
    class Logger
    {
        private static readonly string logFilePath;

        /// <summary>
        /// Initializes the logger and creates a session-specific log file.
        /// </summary>
        static Logger()
        {
            // Define log directory
            string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");

            // Ensure the Logs directory exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Create a new log file for this session with a timestamp
            logFilePath = Path.Combine(logDirectory, $"Malinaw-logs-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            // Write the session start header to the log file
            File.AppendAllText(logFilePath, $"Session started: {DateTime.Now}\n-----------------------------------\n");
        }

        /// <summary>
        /// Writes a log message to the session's log file.
        /// </summary>
        /// <param name="message">The log message.</param>
        public static void Log(string message)
        {
            try
            {
                // Prefix the message with a timestamp
                string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";

                // Append the message to the log file
                File.AppendAllText(logFilePath, timestampedMessage);
            }
            catch (Exception ex)
            {
                // If logging fails, fallback to Debug output
                Logger.Log($"Failed to write log: {ex.Message}");
            }
        }
    }
}
