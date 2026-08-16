using System;

namespace SmartBackupSuite.Models
{
    public class Destination
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobId { get; set; } = "";
        public string DestinationType { get; set; } = "Local"; // Local, Network, GoogleDrive, Dropbox, OneDrive, Azure, S3

        // ✅ المسار المحلي
        private string _localPath = "Backups";
        public string LocalPath
        {
            get => string.IsNullOrEmpty(_localPath) ? "Backups" : _localPath;
            set => _localPath = string.IsNullOrEmpty(value) ? "Backups" : value;
        }

        public string NetworkPath { get; set; } = "";

        // ✅ Google Drive - فقط معرف المجلد (الباقي تلقائي)
        public string GoogleDriveFolderId { get; set; } = "";

        // ✅ Dropbox - مسار المجلد (الباقي تلقائي)
        public string DropboxFolderPath { get; set; } = "/Backups";

        // ✅ OneDrive - مسار المجلد (الباقي تلقائي)
        public string OneDriveFolderPath { get; set; } = "/Backups";

        // ✅ AWS S3 - اسم Bucket والمفاتيح (يجب إدخالها من المستخدم)
        public string S3BucketName { get; set; } = "";
        public string S3AccessKey { get; set; } = "";
        public string S3SecretKey { get; set; } = "";
        public string S3Region { get; set; } = "us-east-1";

        // ✅ Azure Blob Storage
        public string AzureContainerName { get; set; } = "";
        public string AzureConnectionString { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public bool KeepLocalCopy { get; set; } = false;

        public string DisplayName
        {
            get
            {
                switch (DestinationType)
                {
                    case "Local": return $"محلي: {LocalPath}";
                    case "Network": return $"شبكي: {NetworkPath}";
                    case "GoogleDrive": return "Google Drive (تلقائي)";
                    case "Dropbox": return "Dropbox (تلقائي)";
                    case "OneDrive": return "OneDrive (تلقائي)";
                    case "Azure": return $"Azure: {AzureContainerName}";
                    case "S3": return $"S3: {S3BucketName}";
                    default: return DestinationType;
                }
            }
        }
    }
}