using System;

namespace SmartBackupSuite.Models
{
    /// <summary>
    /// معلومات ملف مخزن في السحابة
    /// </summary>
    public class CloudFileInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreationTime { get; set; }
        public long Size { get; set; }
    }
}