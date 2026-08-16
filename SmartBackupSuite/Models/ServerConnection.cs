using System;

namespace SmartBackupSuite.Models
{
    public class ServerConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ServerName { get; set; } = "";
        public string AuthType { get; set; } = "Windows"; // Windows, SQL
        public string Username { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public bool IsActive { get; set; } = true;

        // ✅ خاصية عرض اسم الاتصال
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(ServerName))
                    return "اتصال غير مسمى";

                if (AuthType == "Windows")
                    return $"{ServerName} (Windows)";
                else
                    return $"{ServerName} (SQL)";
            }
        }

        // ✅ خاصية لتحديد ما إذا كان الاتصال يستخدم مصادقة SQL
        public bool IsSQLAuth => AuthType == "SQL";
    }
}