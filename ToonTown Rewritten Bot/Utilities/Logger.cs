using System;
using System.IO;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot.Utilities
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            string levelTag = Level switch
            {
                LogLevel.Debug => "DBG",
                LogLevel.Info => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                _ => "???"
            };
            return $"[{Timestamp:HH:mm:ss.fff}] [{levelTag}] [{Category}] {Message}";
        }
    }

    public class Logger
    {
        private static Logger _instance;
        private static readonly object _lock = new object();

        private readonly object _fileLock = new object();
        private string _currentLogDate;
        private StreamWriter _writer;
        private readonly string _logDirectory;

        public LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        public event Action<LogEntry> LogEntryWritten;

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Logger();
                        }
                    }
                }
                return _instance;
            }
        }

        private Logger()
        {
            _logDirectory = Path.Combine(AppPaths.ExeDirectory, "Logs");
            Directory.CreateDirectory(_logDirectory);
        }

        public static void Debug(string category, string message)
            => Instance.Log(LogLevel.Debug, category, message);

        public static void Info(string category, string message)
            => Instance.Log(LogLevel.Info, category, message);

        public static void Warning(string category, string message)
            => Instance.Log(LogLevel.Warning, category, message);

        public static void Error(string category, string message)
            => Instance.Log(LogLevel.Error, category, message);

        public void Log(LogLevel level, string category, string message)
        {
            if (level < MinimumLevel)
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category,
                Message = message
            };

            // Fire event synchronously for UI subscribers
            LogEntryWritten?.Invoke(entry);

            // Write to file on a background thread to avoid blocking
            Task.Run(() => WriteToFile(entry));
        }

        private void WriteToFile(LogEntry entry)
        {
            lock (_fileLock)
            {
                try
                {
                    string today = DateTime.Now.ToString("yyyy-MM-dd");

                    if (_currentLogDate != today)
                    {
                        _writer?.Flush();
                        _writer?.Dispose();
                        _writer = null;
                        _currentLogDate = today;

                        // Clean up old logs on date rollover
                        CleanupOldLogs();
                    }

                    if (_writer == null)
                    {
                        string filePath = Path.Combine(_logDirectory, $"ttrbot_{_currentLogDate}.log");
                        _writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
                    }

                    _writer.WriteLine(entry.ToString());
                }
                catch
                {
                    // Logging should never crash the app
                }
            }
        }

        private void CleanupOldLogs()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-7);
                foreach (var file in Directory.GetFiles(_logDirectory, "ttrbot_*.log"))
                {
                    if (File.GetLastWriteTime(file) < cutoff)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Cleanup failure is non-critical
            }
        }

        public void Flush()
        {
            lock (_fileLock)
            {
                try
                {
                    _writer?.Flush();
                }
                catch { }
            }
        }

        public void Shutdown()
        {
            lock (_fileLock)
            {
                try
                {
                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch { }
            }
        }

        public string GetCurrentLogFilePath()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(_logDirectory, $"ttrbot_{today}.log");
        }

        public string LogDirectory => _logDirectory;
    }
}
