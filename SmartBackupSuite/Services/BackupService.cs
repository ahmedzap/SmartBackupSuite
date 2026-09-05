using Microsoft.Data.SqlClient;
using SmartBackupSuite.Helpers;
using SmartBackupSuite.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartBackupSuite.Services
{
    public class BackupService
    {
        private static readonly string DefaultBackupPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Backups"
        );

        private static readonly string TempBackupPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TempBackups"
        );

        public async Task<bool> ExecuteBackup(BackupJob job, ServerConnection connection, Destination destination)
        {
            bool allBackupsSucceeded = true;
            string firstErrorMessage = "";
            string successDetails = "";
            string failedDetails = "";

            try
            {
                LogService.WriteLog(job.JobName, "Starting", $"بدء النسخ الاحتياطي للقاعدة البيانات: {string.Join(", ", job.DatabaseNames)}");

                string backupPath = GetBackupPath(job, destination);

                if (!Directory.Exists(backupPath))
                {
                    try
                    {
                        Directory.CreateDirectory(backupPath);
                        LogService.WriteLog(job.JobName, "Info", $"✅ تم إنشاء المجلد: {backupPath}");
                        SetFullPermissions(backupPath);
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteLog(job.JobName, "Error", $"❌ لا يمكن إنشاء المجلد: {backupPath}\nالسبب: {ex.Message}");
                        MessageBox.Show(
                            $"❌ لا يمكن الوصول إلى المسار:\n\n{backupPath}\n\n" +
                            "الرجاء التحقق من:\n" +
                            "1️⃣ صلاحيات الكتابة في المجلد\n" +
                            "2️⃣ صحة المسار\n\n" +
                            "📌 سيتم استخدام المسار الافتراضي: " + DefaultBackupPath,
                            "خطأ في الصلاحيات",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        backupPath = DefaultBackupPath;
                        if (!Directory.Exists(backupPath))
                        {
                            Directory.CreateDirectory(backupPath);
                            SetFullPermissions(backupPath);
                        }
                        LogService.WriteLog(job.JobName, "Info", $"✅ تم التبديل إلى المسار الافتراضي: {backupPath}");
                    }
                }

                string connectionString = BuildConnectionString(connection);
                bool useCompression = await IsCompressionSupportedAsync(connectionString);
                List<string> createdFiles = new List<string>();
                List<string> successfulDatabases = new List<string>();
                List<string> failedDatabases = new List<string>();

                foreach (string dbName in job.DatabaseNames)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string backupFileName = $"{dbName}_{timestamp}.bak";
                    string fullBackupPath = Path.Combine(backupPath, backupFileName);

                    LogService.WriteLog(job.JobName, "Info", $"جاري عمل نسخ لقاعدة البيانات: {dbName}");

                    string backupType = job.BackupType;

                    if (backupType == "Log")
                    {
                        bool canDoLogBackup = await CanDoLogBackup(connectionString, dbName);
                        if (!canDoLogBackup)
                        {
                            LogService.WriteLog(job.JobName, "Warning", $"⚠️ قاعدة البيانات {dbName} لا تدعم BACKUP LOG. سيتم استخدام Full Backup بدلاً من ذلك.");
                            backupType = "Full";
                        }
                    }

                    try
                    {
                        string backupQuery = await BuildBackupQueryAsync(dbName, fullBackupPath, backupType, connectionString);

                        using (SqlConnection conn = new SqlConnection(connectionString))
                        {
                            await conn.OpenAsync();
                            using (SqlCommand cmd = new SqlCommand(backupQuery, conn))
                            {
                                cmd.CommandTimeout = 0;
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        LogService.WriteLog(job.JobName, "Info", $"بانتظار تحرير الملف: {backupFileName}");
                        await WaitForFileRelease(fullBackupPath, TimeSpan.FromSeconds(30));

                        if (!File.Exists(fullBackupPath))
                        {
                            throw new Exception($"الملف {fullBackupPath} لم يتم إنشاؤه");
                        }

                        long fileSize = new FileInfo(fullBackupPath).Length;
                        if (fileSize == 0)
                        {
                            LogService.WriteLog(job.JobName, "Warning", $"⚠️ الملف فارغ (حجمه 0): {backupFileName}");
                        }

                        LogService.WriteLog(job.JobName, "Info", $"حجم الملف: {fileSize / 1024 / 1024} MB");

                        string currentFilePath = fullBackupPath;
                        createdFiles.Add(currentFilePath);

                        // ضغط الملف
                        if (job.Compression && fileSize > 0)
                        {
                            LogService.WriteLog(job.JobName, "Info", $"جاري ضغط الملف: {backupFileName}");
                            string compressedFile = fullBackupPath + ".zip";

                            if (File.Exists(compressedFile))
                                File.Delete(compressedFile);

                            await CompressFileAsync(fullBackupPath, compressedFile);

                            if (File.Exists(compressedFile) && new FileInfo(compressedFile).Length > 0)
                            {
                                File.Delete(fullBackupPath);
                                createdFiles.Remove(currentFilePath);
                                currentFilePath = compressedFile;
                                createdFiles.Add(currentFilePath);
                                LogService.WriteLog(job.JobName, "Info", $"تم الضغط بنجاح: {Path.GetFileName(compressedFile)}");
                            }
                            else
                            {
                                LogService.WriteLog(job.JobName, "Warning", "فشل الضغط، سيتم استخدام الملف غير المضغوط");
                                currentFilePath = fullBackupPath;
                            }
                        }

                        // تشفير الملف
                        if (job.Encryption)
                        {
                            LogService.WriteLog(job.JobName, "Info", "جاري تشفير الملف");
                            string encryptedFile = currentFilePath + ".enc";
                            string password = "BackupPassword2024!";

                            await Task.Run(() => EncryptionHelper.EncryptFile(currentFilePath, encryptedFile, password));

                            if (File.Exists(encryptedFile) && new FileInfo(encryptedFile).Length > 0)
                            {
                                File.Delete(currentFilePath);
                                createdFiles.Remove(currentFilePath);
                                currentFilePath = encryptedFile;
                                createdFiles.Add(currentFilePath);
                                LogService.WriteLog(job.JobName, "Info", "تم التشفير بنجاح");
                            }
                        }

                        string finalFilePath = currentFilePath;

                        // رفع إلى السحابة
                        if (destination.DestinationType != "Local" && destination.DestinationType != "Network")
                        {
                            try
                            {
                                LogService.WriteLog(job.JobName, "Info", $"جاري الرفع إلى {destination.DestinationType}");
                                await UploadToCloud(finalFilePath, destination, dbName);
                                LogService.WriteLog(job.JobName, "Success", $"✅ تم رفع الملف بنجاح إلى {destination.DestinationType}");

                                // ✅ تطبيق سياسة الاحتفاظ على السحابة بعد الرفع الناجح
                                await ApplyCloudRetentionPolicy(job, destination, dbName);
                            }
                            catch (Exception ex)
                            {
                                string errorMsg = ex.Message;
                                LogService.WriteLog(job.JobName, "Error", $"❌ فشل الرفع إلى {destination.DestinationType}: {errorMsg}");

                                allBackupsSucceeded = false;
                                failedDatabases.Add(dbName);
                                if (string.IsNullOrEmpty(firstErrorMessage))
                                    firstErrorMessage = $"فشل الرفع إلى {destination.DestinationType}: {errorMsg}";

                                string userMessage = $"⚠️ **تنبيه:**\n\n" +
                                                     $"تم إنشاء النسخ الاحتياطي المحلي بنجاح، ولكن فشل رفعه إلى {destination.DestinationType}.\n\n" +
                                                     $"السبب: {errorMsg}\n\n" +
                                                     $"الملف المحلي موجود في:\n{finalFilePath}\n\n" +
                                                     $"الرجاء التحقق من إعدادات {destination.DestinationType} وحاول مرة أخرى.";

                                MessageBox.Show(userMessage, $"فشل الرفع إلى {destination.DestinationType}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                continue;
                            }

                            if (!job.KeepLocalCopy)
                            {
                                if (File.Exists(finalFilePath))
                                {
                                    File.Delete(finalFilePath);
                                    createdFiles.Remove(finalFilePath);
                                    LogService.WriteLog(job.JobName, "Info", $"🗑️ تم حذف الملف المحلي: {Path.GetFileName(finalFilePath)}");
                                }
                            }
                            else
                            {
                                LogService.WriteLog(job.JobName, "Info", $"💾 تم الاحتفاظ بنسخة محلية: {finalFilePath}");
                            }
                        }

                        successfulDatabases.Add(dbName);
                        LogService.WriteLog(job.JobName, "Success", $"نجح النسخ لقاعدة البيانات: {dbName}", finalFilePath);
                    }
                    catch (Exception ex)
                    {
                        allBackupsSucceeded = false;
                        failedDatabases.Add(dbName);
                        string errorMsg = ex.Message;
                        LogService.WriteLog(job.JobName, "Error", $"❌ فشل نسخ قاعدة البيانات {dbName}: {errorMsg}");
                        if (string.IsNullOrEmpty(firstErrorMessage))
                            firstErrorMessage = $"فشل نسخ قاعدة البيانات {dbName}: {errorMsg}";
                    }
                }

                // تنظيف المجلد المؤقت
                if (backupPath == TempBackupPath)
                {
                    try
                    {
                        if (Directory.Exists(backupPath) && Directory.GetFiles(backupPath).Length == 0)
                        {
                            Directory.Delete(backupPath);
                            LogService.WriteLog(job.JobName, "Info", $"🗑️ تم حذف المجلد المؤقت: {backupPath}");
                        }
                    }
                    catch { }
                }

                // تطبيق سياسة الاحتفاظ المحلية
                if (job.KeepLocalCopy && Directory.Exists(backupPath) && backupPath != TempBackupPath)
                {
                    LogService.WriteLog(job.JobName, "Info", "جاري تطبيق سياسة الاحتفاظ المحلية");
                    ApplyRetentionPolicy(job, destination, backupPath);
                }

                // ✅ إرسال إشعار البريد الإلكتروني
                await SendEmailNotification(job, allBackupsSucceeded, successfulDatabases, failedDatabases, firstErrorMessage);

                if (allBackupsSucceeded)
                {
                    LogService.WriteLog(job.JobName, "Success", "✅ اكتمل النسخ الاحتياطي لجميع قواعد البيانات بنجاح");
                    return true;
                }
                else
                {
                    LogService.WriteLog(job.JobName, "Failed", $"❌ اكتمل النسخ المحلي ولكن فشل الرفع السحابي: {firstErrorMessage}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog(job.JobName, "Error", $"❌ فشل النسخ الاحتياطي: {ex.Message}", ex.StackTrace);

                // ✅ إرسال إشعار فشل
                await SendEmailNotification(job, false, new List<string>(), new List<string>(), ex.Message);

                return false;
            }
        }

        #region 📧 إرسال إشعارات البريد الإلكتروني

        /// <summary>
        /// إرسال إشعار البريد الإلكتروني بعد تنفيذ النسخ الاحتياطي
        /// </summary>
        private async Task SendEmailNotification(BackupJob job, bool isSuccess, List<string> successfulDatabases, List<string> failedDatabases, string errorMessage)
        {
            try
            {
                // ✅ قراءة الإعدادات
                var settings = JsonFileHelper.ReadSettings();
                if (settings?.GeneralSettings == null || !settings.GeneralSettings.EnableEmailNotifications)
                {
                    LogService.WriteLog(job.JobName, "Info", "ℹ️ إشعارات البريد الإلكتروني غير مفعلة");
                    return;
                }

                if (string.IsNullOrEmpty(settings.GeneralSettings.EmailNotifications))
                {
                    LogService.WriteLog(job.JobName, "Warning", "⚠️ لا يوجد بريد إلكتروني مستلم للإشعارات");
                    return;
                }

                // ✅ بناء محتوى البريد الإلكتروني
                string subject = isSuccess
                    ? $"✅ نجاح النسخ الاحتياطي - {job.JobName}"
                    : $"❌ فشل النسخ الاحتياطي - {job.JobName}";

                string body = BuildEmailBody(job, isSuccess, successfulDatabases, failedDatabases, errorMessage);

                // ✅ إرسال البريد
                bool emailSent = await EmailService.SendEmailAsync(
                    settings.GeneralSettings.EmailNotifications,
                    subject,
                    body,
                    settings.GeneralSettings
                );

                if (emailSent)
                {
                    LogService.WriteLog(job.JobName, "Success", $"✅ تم إرسال إشعار البريد الإلكتروني إلى {settings.GeneralSettings.EmailNotifications}");
                }
                else
                {
                    LogService.WriteLog(job.JobName, "Warning", $"⚠️ فشل إرسال إشعار البريد الإلكتروني إلى {settings.GeneralSettings.EmailNotifications}");
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog(job.JobName, "Error", $"❌ خطأ في إرسال إشعار البريد الإلكتروني: {ex.Message}");
            }
        }

        /// <summary>
        /// بناء نص البريد الإلكتروني
        /// </summary>
        private string BuildEmailBody(BackupJob job, bool isSuccess, List<string> successfulDatabases, List<string> failedDatabases, string errorMessage)
        {
            string statusIcon = isSuccess ? "✅" : "❌";
            string statusText = isSuccess ? "نجاح" : "فشل";
            string statusColor = isSuccess ? "#16a34a" : "#dc2626";

            string databasesList = "";
            if (successfulDatabases.Count > 0)
            {
                databasesList += $"<li style='color: #16a34a;'>✅ {string.Join(", ", successfulDatabases)}</li>";
            }
            if (failedDatabases.Count > 0)
            {
                databasesList += $"<li style='color: #dc2626;'>❌ {string.Join(", ", failedDatabases)}</li>";
            }

            string body = $@"
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; direction: rtl; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background: #f8f9fa; border-radius: 12px; }}
        .header {{ background: {statusColor}; padding: 20px; color: white; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ background: white; padding: 25px; margin-top: 0; border-radius: 0 0 8px 8px; }}
        .status {{ font-size: 18px; font-weight: bold; color: {statusColor}; text-align: center; }}
        .details {{ margin-top: 15px; padding: 15px; background: #f1f5f9; border-radius: 8px; }}
        .details table {{ width: 100%; border-collapse: collapse; }}
        .details td {{ padding: 8px 12px; border-bottom: 1px solid #e2e8f0; }}
        .details td:first-child {{ font-weight: bold; color: #475569; width: 40%; }}
        .details td:last-child {{ color: #1e293b; }}
        .databases {{ margin-top: 15px; padding: 15px; background: #f8fafc; border-radius: 8px; }}
        .databases ul {{ list-style: none; padding: 0; margin: 0; }}
        .databases li {{ padding: 4px 0; }}
        .error {{ margin-top: 15px; padding: 15px; background: #fee2e2; border-radius: 8px; color: #dc2626; }}
        .footer {{ margin-top: 20px; text-align: center; color: #94a3b8; font-size: 12px; }}
        .summary {{ text-align: center; margin: 10px 0; }}
        .badge {{ display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 14px; font-weight: bold; }}
        .badge-success {{ background: #dcfce7; color: #16a34a; }}
        .badge-failed {{ background: #fee2e2; color: #dc2626; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{statusIcon} {statusText} النسخ الاحتياطي</h1>
            <p style='margin: 5px 0 0; opacity: 0.9;'>تقرير تنفيذ المهمة</p>
        </div>
        <div class='content'>
            <div class='summary'>
                <span class='badge {(isSuccess ? "badge-success" : "badge-failed")}'>
                    {statusText}
                </span>
            </div>

            <div class='details'>
                <table>
                    <tr><td>📌 اسم المهمة</td><td>{job.JobName}</td></tr>
                    <tr><td>📅 تاريخ التنفيذ</td><td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td></tr>
                    <tr><td>💻 الجهاز</td><td>{Environment.MachineName}</td></tr>
                    <tr><td>📊 نوع النسخ</td><td>{job.BackupType}</td></tr>
                    <tr><td>📁 عدد قواعد البيانات</td><td>{job.DatabaseNames.Count}</td></tr>
                    {(isSuccess ? "" : $"<tr><td>❌ سبب الفشل</td><td style='color: #dc2626;'>{errorMessage}</td></tr>")}
                </table>
            </div>

            <div class='databases'>
                <strong>📋 قواعد البيانات:</strong>
                <ul>
                    {databasesList}
                </ul>
            </div>

            {(isSuccess ? $@"
            <div style='margin-top: 15px; text-align: center; color: #16a34a;'>
                ✅ تم نسخ جميع قواعد البيانات بنجاح!
            </div>
            " : $@"
            <div class='error'>
                <strong>❌ فشل تنفيذ بعض قواعد البيانات</strong>
                <p style='margin-top: 8px;'>{errorMessage}</p>
            </div>
            ")}

            <div style='margin-top: 15px; padding: 10px; background: #f1f5f9; border-radius: 8px; text-align: center; font-size: 13px; color: #475569;'>
                💾 مسار الحفظ: {DefaultBackupPath}
            </div>
        </div>
        <div class='footer'>
            Smart Backup Suite - نظام النسخ الاحتياطي الذكي<br>
            © {DateTime.Now.Year} جميع الحقوق محفوظة
        </div>
    </div>
</body>
</html>";

            return body;
        }

        #endregion

        // ==================== دوال سياسة الاحتفاظ ====================

        /// <summary>
        /// تطبيق سياسة الاحتفاظ على السحابة بعد الرفع الناجح
        /// </summary>
        private async Task ApplyCloudRetentionPolicy(BackupJob job, Destination destination, string dbName)
        {
            try
            {
                if (job.RetentionCount <= 0) return;

                LogService.WriteLog(job.JobName, "Info", $"☁️ جاري تطبيق سياسة الاحتفاظ على السحابة للقاعدة: {dbName}");

                switch (destination.DestinationType)
                {
                    case "GoogleDrive":
                        await CloudUploadService.ApplyRetentionPolicyGoogleDrive(
                            destination.GoogleDriveFolderId, dbName, job.RetentionCount);
                        break;

                    case "Dropbox":
                        await CloudUploadService.ApplyRetentionPolicyDropbox(
                            destination.DropboxFolderPath ?? "/Backups", dbName, job.RetentionCount);
                        break;

                    case "OneDrive":
                        await CloudUploadService.ApplyRetentionPolicyOneDrive(
                            destination.OneDriveFolderPath ?? "/Backups", dbName, job.RetentionCount);
                        break;

                    case "S3":
                        await CloudUploadService.ApplyRetentionPolicyS3(
                            destination.S3BucketName, dbName, job.RetentionCount,
                            destination.S3AccessKey, destination.S3SecretKey, destination.S3Region);
                        break;

                    case "Azure":
                        await CloudUploadService.ApplyRetentionPolicyAzure(
                            destination.AzureContainerName, dbName, job.RetentionCount,
                            destination.AzureConnectionString);
                        break;
                }

                LogService.WriteLog(job.JobName, "Info", $"✅ تم تطبيق سياسة الاحتفاظ على السحابة للقاعدة: {dbName}");
            }
            catch (Exception ex)
            {
                LogService.WriteLog(job.JobName, "Warning", $"⚠️ فشل تطبيق سياسة الاحتفاظ على السحابة: {ex.Message}");
            }
        }

        // ==================== دوال مساعدة (بدون تغيير) ====================

        private string GetBackupPath(BackupJob job, Destination destination)
        {
            if (destination.DestinationType != "Local" && destination.DestinationType != "Network" && !job.KeepLocalCopy)
            {
                if (!Directory.Exists(TempBackupPath))
                {
                    Directory.CreateDirectory(TempBackupPath);
                    SetFullPermissions(TempBackupPath);
                }
                LogService.WriteLog(job.JobName, "Info", $"📁 استخدام مجلد مؤقت: {TempBackupPath}");
                return TempBackupPath;
            }

            if (destination == null || string.IsNullOrEmpty(destination.LocalPath))
            {
                if (!Directory.Exists(DefaultBackupPath))
                {
                    Directory.CreateDirectory(DefaultBackupPath);
                    SetFullPermissions(DefaultBackupPath);
                }
                return DefaultBackupPath;
            }

            try
            {
                string path = destination.LocalPath;
                if (!Path.IsPathRooted(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    SetFullPermissions(path);
                }
                return path;
            }
            catch
            {
                if (!Directory.Exists(DefaultBackupPath))
                {
                    Directory.CreateDirectory(DefaultBackupPath);
                    SetFullPermissions(DefaultBackupPath);
                }
                return DefaultBackupPath;
            }
        }

        private void SetFullPermissions(string path)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                var security = directoryInfo.GetAccessControl();

                string currentUser = Environment.UserName;
                string domain = Environment.UserDomainName;
                string fullUserName = $"{domain}\\{currentUser}";

                var identity = new System.Security.Principal.NTAccount(fullUserName);

                var rule = new System.Security.AccessControl.FileSystemAccessRule(
                    identity,
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.InheritanceFlags.ObjectInherit |
                    System.Security.AccessControl.InheritanceFlags.ContainerInherit,
                    System.Security.AccessControl.PropagationFlags.None,
                    System.Security.AccessControl.AccessControlType.Allow
                );
                security.AddAccessRule(rule);
                directoryInfo.SetAccessControl(security);

                LogService.WriteLog("System", "Info", $"✅ تم منح صلاحيات كاملة للمجلد: {path}");
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Warning", $"⚠️ لا يمكن منح صلاحيات كاملة للمجلد {path}: {ex.Message}");
            }
        }

        private async Task<bool> CanDoLogBackup(string connectionString, string databaseName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"
                        SELECT recovery_model_desc 
                        FROM sys.databases 
                        WHERE name = @dbName";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@dbName", databaseName);
                        var result = await cmd.ExecuteScalarAsync();
                        string recoveryModel = result?.ToString() ?? "";

                        LogService.WriteLog("System", "Info", $"ℹ️ قاعدة البيانات {databaseName} - نموذج الاسترداد: {recoveryModel}");

                        return recoveryModel == "FULL" || recoveryModel == "BULK_LOGGED";
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Warning", $"⚠️ لا يمكن التحقق من نموذج الاسترداد: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> IsCompressionSupportedAsync(string connectionString)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT SERVERPROPERTY('Edition')", conn))
                    {
                        string edition = (await cmd.ExecuteScalarAsync())?.ToString() ?? "";
                        if (edition.Contains("Express") || edition.Contains("Express Edition"))
                        {
                            LogService.WriteLog("System", "Info", "ℹ️ تم اكتشاف SQL Server Express - سيتم تعطيل ضغط SQL Server");
                            return false;
                        }
                        LogService.WriteLog("System", "Info", $"✅ SQL Server Edition: {edition} - يدعم الضغط");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("System", "Warning", $"⚠️ لا يمكن التحقق من دعم الضغط: {ex.Message} - سيتم استخدام الوضع الآمن");
                return false;
            }
        }

        private async Task<string> BuildBackupQueryAsync(string databaseName, string backupPath, string backupType, string connectionString)
        {
            bool useCompression = await IsCompressionSupportedAsync(connectionString);
            string compression = useCompression ? ", COMPRESSION" : "";

            switch (backupType)
            {
                case "Full":
                    return $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupPath}' WITH INIT{compression}, CHECKSUM;";
                case "Differential":
                    return $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupPath}' WITH DIFFERENTIAL, INIT{compression}, CHECKSUM;";
                case "Log":
                    return $"BACKUP LOG [{databaseName}] TO DISK = '{backupPath}' WITH INIT{compression}, CHECKSUM;";
                default:
                    return $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupPath}' WITH INIT{compression}, CHECKSUM;";
            }
        }

        private string BuildConnectionString(ServerConnection connection)
        {
            if (connection.AuthType == "Windows")
            {
                return $"Server={connection.ServerName};Integrated Security=True;TrustServerCertificate=True;Connection Timeout=30;";
            }
            else
            {
                string password = EncryptionHelper.Decrypt(connection.EncryptedPassword);
                return $"Server={connection.ServerName};User Id={connection.Username};Password={password};TrustServerCertificate=True;Connection Timeout=30;";
            }
        }

        private async Task WaitForFileRelease(string filePath, TimeSpan timeout)
        {
            DateTime startTime = DateTime.Now;
            bool fileReleased = false;

            while (!fileReleased && (DateTime.Now - startTime) < timeout)
            {
                try
                {
                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fileReleased = true;
                        LogService.WriteLog("System", "Info", $"تم تحرير الملف: {Path.GetFileName(filePath)}");
                    }
                }
                catch (IOException)
                {
                    await Task.Delay(500);
                }
                catch (Exception)
                {
                    await Task.Delay(500);
                }
            }

            if (!fileReleased)
            {
                throw new Exception($"لم يتم تحرير الملف بعد {timeout.TotalSeconds} ثانية: {filePath}");
            }
        }

        private async Task CompressFileAsync(string inputFile, string outputFile)
        {
            try
            {
                await Task.Run(() =>
                {
                    string directory = Path.GetDirectoryName(outputFile);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                        SetFullPermissions(directory);
                    }

                    using (FileStream zipStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                        {
                            string entryName = Path.GetFileName(inputFile);
                            archive.CreateEntryFromFile(inputFile, entryName, CompressionLevel.Optimal);
                        }
                    }

                    if (!File.Exists(outputFile) || new FileInfo(outputFile).Length == 0)
                    {
                        throw new Exception("فشل الضغط - الملف الناتج فارغ أو غير موجود");
                    }
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل الضغط: {ex.Message}");
            }
        }

        private async Task UploadToCloud(string filePath, Destination destination, string dbName)
        {
            try
            {
                switch (destination.DestinationType)
                {
                    case "GoogleDrive":
                        LogService.WriteLog("GoogleDrive", "Info", $"📤 جاري رفع الملف إلى Google Drive");
                        string folderId = string.IsNullOrEmpty(destination.GoogleDriveFolderId) ? null : destination.GoogleDriveFolderId;
                        await CloudUploadService.UploadToGoogleDrive(filePath, folderId);
                        LogService.WriteLog("GoogleDrive", "Success", $"✅ تم رفع الملف بنجاح إلى Google Drive");
                        break;

                    case "Dropbox":
                        LogService.WriteLog("Dropbox", "Info", $"📤 جاري رفع الملف إلى Dropbox");
                        await CloudUploadService.UploadToDropbox(filePath, destination.DropboxFolderPath ?? "/Backups");
                        LogService.WriteLog("Dropbox", "Success", $"✅ تم رفع الملف بنجاح إلى Dropbox");
                        break;

                    case "OneDrive":
                        LogService.WriteLog("OneDrive", "Info", $"📤 جاري رفع الملف إلى OneDrive");
                        await CloudUploadService.UploadToOneDrive(filePath, destination.OneDriveFolderPath ?? "/Backups");
                        LogService.WriteLog("OneDrive", "Success", $"✅ تم رفع الملف بنجاح إلى OneDrive");
                        break;

                    case "S3":
                        LogService.WriteLog("S3", "Info", $"📤 جاري رفع الملف إلى S3");
                        await CloudUploadService.UploadToS3(filePath, destination.S3BucketName, destination.S3AccessKey, destination.S3SecretKey, destination.S3Region);
                        LogService.WriteLog("S3", "Success", $"✅ تم رفع الملف بنجاح إلى S3");
                        break;

                    case "Azure":
                        LogService.WriteLog("Azure", "Info", $"📤 جاري رفع الملف إلى Azure");
                        await CloudUploadService.UploadToAzure(filePath, destination.AzureContainerName, destination.AzureConnectionString);
                        LogService.WriteLog("Azure", "Success", $"✅ تم رفع الملف بنجاح إلى Azure");
                        break;

                    default:
                        throw new Exception($"نوع الوجهة غير مدعوم: {destination.DestinationType}");
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog(destination.DestinationType, "Error", $"❌ فشل رفع الملف: {ex.Message}");
                throw;
            }
        }

        private void ApplyRetentionPolicy(BackupJob job, Destination destination, string backupPath)
        {
            try
            {
                string path = backupPath;
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    return;

                var files = new List<string>();
                files.AddRange(Directory.GetFiles(path, "*.bak"));
                files.AddRange(Directory.GetFiles(path, "*.zip"));
                files.AddRange(Directory.GetFiles(path, "*.enc"));

                if (files.Count <= job.RetentionCount)
                    return;

                files.Sort((f1, f2) => File.GetCreationTime(f1).CompareTo(File.GetCreationTime(f2)));

                int filesToDelete = files.Count - job.RetentionCount;
                int deletedCount = 0;

                for (int i = 0; i < filesToDelete; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteLog(job.JobName, "Warning", $"فشل حذف الملف القديم {files[i]}: {ex.Message}");
                    }
                }

                if (deletedCount > 0)
                {
                    LogService.WriteLog(job.JobName, "Info", $"تم حذف {deletedCount} ملفات قديمة حسب سياسة الاحتفاظ");
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog(job.JobName, "Warning", $"فشل تطبيق سياسة الاحتفاظ: {ex.Message}");
            }
        }
    }
}