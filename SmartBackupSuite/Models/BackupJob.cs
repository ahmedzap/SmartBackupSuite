using System;
using System.Collections.Generic;

namespace SmartBackupSuite.Models
{
    public class BackupJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobName { get; set; } = "";
        public string ConnectionId { get; set; } = "";
        public List<string> DatabaseNames { get; set; } = new List<string>();
        public string BackupType { get; set; } = "Full"; // Full, Differential, Log
        public bool Compression { get; set; } = true;
        public bool Encryption { get; set; } = false;
        public int RetentionCount { get; set; } = 7;
        public bool IsActive { get; set; } = true;
        public bool KeepLocalCopy { get; set; } = true;
        public DateTime LastRunTime { get; set; } = DateTime.MinValue;
        public string LastRunStatus { get; set; } = "";

        // ✅ خصائص التنفيذ الفائت
        public bool ExecuteMissedJobs { get; set; } = true;
        public int MissedCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}