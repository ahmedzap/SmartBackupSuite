using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartBackupSuite.Services
{
    public static class LogService
    {
        // ✅ استخدام مجلد Logs
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "Logs"
        );

        static LogService()
        {
            if (!Directory.Exists(LogFolder))
                Directory.CreateDirectory(LogFolder);
        }

        public static void WriteLog(string source, string level, string message, string details = "")
        {
            try
            {
                string logFile = Path.Combine(LogFolder, $"log_{DateTime.Now:yyyy-MM-dd}.log");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level.ToUpper()}] [{source}] {message}";
                if (!string.IsNullOrEmpty(details))
                    logEntry += $" | {details}";

                File.AppendAllText(logFile, logEntry + Environment.NewLine);
                Console.WriteLine(logEntry);
            }
            catch { }
        }

        public static void CleanOldLogs(int retentionDays)
        {
            try
            {
                if (!Directory.Exists(LogFolder)) return;

                var files = Directory.GetFiles(LogFolder, "*.log");
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime < cutoffDate)
                            fileInfo.Delete();
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static List<string> GetLogFiles()
        {
            try
            {
                if (!Directory.Exists(LogFolder)) return new List<string>();
                return Directory.GetFiles(LogFolder, "*.log")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public static string GetLogs(int maxLines = 100)
        {
            try
            {
                if (!Directory.Exists(LogFolder)) return "لا توجد سجلات.";

                var logFiles = Directory.GetFiles(LogFolder, "*.log")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();

                if (logFiles.Count == 0) return "لا توجد سجلات.";

                var allLogLines = new List<string>();  // ✅ تغيير الاسم من 'lines' إلى 'allLogLines'

                foreach (var file in logFiles)
                {
                    var fileLines = File.ReadAllLines(file);  // ✅ تغيير الاسم من 'lines' إلى 'fileLines'
                    allLogLines.AddRange(fileLines);
                    if (allLogLines.Count >= maxLines) break;
                }

                var lastLines = allLogLines.Count > maxLines
                    ? allLogLines.Skip(allLogLines.Count - maxLines).ToArray()
                    : allLogLines.ToArray();

                return string.Join(Environment.NewLine, lastLines);
            }
            catch
            {
                return "خطأ في قراءة السجلات.";
            }
        }

        public static List<string> ReadLogFile(string filePath, int maxLines = 1000)
        {
            try
            {
                if (!File.Exists(filePath)) return new List<string>();
                var fileContent = File.ReadAllLines(filePath).ToList();  // ✅ تغيير الاسم من 'lines' إلى 'fileContent'
                if (fileContent.Count > maxLines)
                    return fileContent.Skip(fileContent.Count - maxLines).ToList();
                return fileContent;
            }
            catch
            {
                return new List<string>();
            }
        }

        // ✅ ✅ ✅ دوال إضافية للتشغيل مع الملفات
        public static string GetLogsByDate(DateTime date, int maxLines = 100)
        {
            try
            {
                string logFile = Path.Combine(LogFolder, $"log_{date:yyyy-MM-dd}.log");
                if (!File.Exists(logFile)) return $"لا توجد سجلات لتاريخ {date:yyyy-MM-dd}.";

                var fileLines = File.ReadAllLines(logFile);
                var lastLines = fileLines.Length > maxLines
                    ? fileLines.Skip(fileLines.Length - maxLines).ToArray()
                    : fileLines;

                return string.Join(Environment.NewLine, lastLines);
            }
            catch
            {
                return "خطأ في قراءة السجلات.";
            }
        }

        public static void ClearLogs()
        {
            try
            {
                if (!Directory.Exists(LogFolder)) return;

                var files = Directory.GetFiles(LogFolder, "*.log");
                foreach (var file in files)
                {
                    try { File.Delete(file); }
                    catch { }
                }
            }
            catch { }
        }

        public static long GetLogsSize()
        {
            try
            {
                if (!Directory.Exists(LogFolder)) return 0;

                var files = Directory.GetFiles(LogFolder, "*.log");
                long totalSize = 0;
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                    catch { }
                }
                return totalSize;
            }
            catch
            {
                return 0;
            }
        }
    }
}