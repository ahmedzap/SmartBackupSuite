using System.Collections.Generic;

namespace SmartBackupSuite.Models
{
    public class AppSettings
    {
        public List<ServerConnection> ServerConnections { get; set; } = new List<ServerConnection>();
        public List<BackupJob> BackupJobs { get; set; } = new List<BackupJob>();
        public List<Schedule> Schedules { get; set; } = new List<Schedule>();
        public List<Destination> Destinations { get; set; } = new List<Destination>();

        // إعدادات عامة
        public GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
    }

    public class GeneralSettings
    {
        public bool AutoStartWithWindows { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public int LogRetentionDays { get; set; } = 30;
        public string EmailNotifications { get; set; } = "";
        public string SmtpServer { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public bool EnableEmailNotifications { get; set; } = false;
    }

    //public class ServerConnection
    //{
    //    public string Id { get; set; } = Guid.NewGuid().ToString();
    //    public string DisplayName { get; set; } = "";
    //    public string ServerName { get; set; } = "";
    //    public string AuthType { get; set; } = "Windows"; // Windows, SQL
    //    public string Username { get; set; } = "";
    //    public string EncryptedPassword { get; set; } = "";
    //    public bool IsDefault { get; set; } = false;
    //    public int TimeoutSeconds { get; set; } = 30;
    //}
}