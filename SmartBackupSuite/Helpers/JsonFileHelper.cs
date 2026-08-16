using Newtonsoft.Json;
using SmartBackupSuite.Models;
using System;
using System.IO;

namespace SmartBackupSuite.Helpers
{
    public static class JsonFileHelper
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "settings.json"
        );

        private static readonly object _lock = new object();

        static JsonFileHelper()
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"خطأ في إنشاء المجلد: {ex.Message}");
                }
            }
        }

        public static AppSettings ReadSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(SettingsFilePath))
                    {
                        var defaultSettings = GetDefaultSettings();
                        SaveSettings(defaultSettings);
                        return defaultSettings;
                    }

                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);

                    if (settings == null)
                        return GetDefaultSettings();

                    if (settings.ServerConnections == null)
                        settings.ServerConnections = new System.Collections.Generic.List<ServerConnection>();
                    if (settings.BackupJobs == null)
                        settings.BackupJobs = new System.Collections.Generic.List<BackupJob>();
                    if (settings.Schedules == null)
                        settings.Schedules = new System.Collections.Generic.List<Schedule>();
                    if (settings.Destinations == null)
                        settings.Destinations = new System.Collections.Generic.List<Destination>();
                    if (settings.GeneralSettings == null)
                        settings.GeneralSettings = new GeneralSettings();

                    return settings;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"خطأ في قراءة الإعدادات: {ex.Message}");
                    return GetDefaultSettings();
                }
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            lock (_lock)
            {
                try
                {
                    var directory = Path.GetDirectoryName(SettingsFilePath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    if (File.Exists(SettingsFilePath))
                    {
                        string backupPath = SettingsFilePath + ".bak";
                        try { File.Copy(SettingsFilePath, backupPath, true); }
                        catch { }
                    }

                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    File.WriteAllText(SettingsFilePath, json);
                }
                catch (Exception ex)
                {
                    throw new Exception($"خطأ في حفظ الإعدادات: {ex.Message}");
                }
            }
        }

        private static AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                ServerConnections = new System.Collections.Generic.List<ServerConnection>(),
                BackupJobs = new System.Collections.Generic.List<BackupJob>(),
                Schedules = new System.Collections.Generic.List<Schedule>(),
                Destinations = new System.Collections.Generic.List<Destination>(),
                GeneralSettings = new GeneralSettings
                {
                    AutoStartWithWindows = false,
                    ShowNotifications = true,
                    LogRetentionDays = 30,
                    EmailNotifications = "",
                    SmtpServer = "",
                    SmtpPort = 587,
                    SmtpUsername = "",
                    SmtpPassword = "",
                    EnableEmailNotifications = false
                }
            };
        }

        public static string GetSettingsFilePath()
        {
            return SettingsFilePath;
        }
    }
}