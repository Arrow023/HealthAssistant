using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;

namespace FitnessAgentsWeb.Core.Logging
{
    internal class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logsFolder;

        public FileLoggerProvider(string logsFolder)
        {
            _logsFolder = logsFolder;
            if (!Directory.Exists(_logsFolder)) Directory.CreateDirectory(_logsFolder);
            PruneOldLogs();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var filePath = GetCurrentWeekFilePath();
            return new FileLogger(filePath);
        }

        public void Dispose() { }

        private string GetCurrentWeekFilePath()
        {
            // Use ISO week year and week number to create a weekly filename
            var now = DateTime.UtcNow;
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var fileName = $"log-{now:yyyy}-w{week}.txt";
            return Path.Combine(_logsFolder, fileName);
        }

        private void PruneOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(_logsFolder, "log-*.txt");
                var cutoff = DateTime.UtcNow.AddDays(-7);
                foreach (var f in files)
                {
                    var info = new FileInfo(f);
                    if (info.CreationTimeUtc < cutoff)
                    {
                        File.Delete(f);
                    }
                }
            }
            catch { /* swallow */ }
        }
    }
}
