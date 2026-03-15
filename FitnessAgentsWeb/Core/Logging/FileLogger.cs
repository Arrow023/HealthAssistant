using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace FitnessAgentsWeb.Core.Logging
{
    internal class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static readonly object _lock = new object();

        public FileLogger(string filePath)
        {
            _filePath = filePath;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            var logRecord = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logLevel}] {message}{Environment.NewLine}{(exception != null ? exception.ToString() : string.Empty)}";

            lock (_lock)
            {
                var dir = Path.GetDirectoryName(_filePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(_filePath, logRecord);
            }
        }
    }
}
