using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using SmartBackupSuite.Models;

namespace SmartBackupSuite.Services
{
    public static class EmailService
    {
        public static async Task<bool> SendEmailAsync(string toEmail, string subject, string body, GeneralSettings settings)
        {
            try
            {
                if (settings == null || !settings.EnableEmailNotifications)
                {
                    LogService.WriteLog("EmailService", "Warning", "⚠️ إشعارات البريد الإلكتروني غير مفعلة");
                    return false;
                }

                if (string.IsNullOrEmpty(settings.SmtpServer) || string.IsNullOrEmpty(settings.SmtpUsername))
                {
                    LogService.WriteLog("EmailService", "Error", "❌ إعدادات SMTP غير مكتملة");
                    return false;
                }

                return await Task.Run(() =>
                {
                    using (var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort))
                    {
                        client.EnableSsl = true;
                        client.UseDefaultCredentials = false;
                        client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);
                        client.Timeout = 15000;

                        using (var message = new MailMessage())
                        {
                            message.From = new MailAddress(settings.SmtpUsername);
                            message.To.Add(toEmail);
                            message.Subject = subject;
                            message.Body = body;
                            message.IsBodyHtml = true;

                            client.Send(message);
                        }
                    }

                    LogService.WriteLog("EmailService", "Success", $"✅ تم إرسال البريد إلى {toEmail}");
                    return true;
                });
            }
            catch (Exception ex)
            {
                LogService.WriteLog("EmailService", "Error", $"❌ فشل إرسال البريد: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> SendJobNotificationAsync(string jobName, string status, string details, GeneralSettings settings, string toEmail = null)
        {
            try
            {
                string email = toEmail ?? settings.EmailNotifications;
                if (string.IsNullOrEmpty(email))
                {
                    LogService.WriteLog("EmailService", "Warning", "⚠️ لا يوجد بريد إلكتروني مستلم");
                    return false;
                }

                string subject = $"📋 تقرير النسخ الاحتياطي - {jobName}";
                string body = $@"
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; direction: rtl; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background: #f8f9fa; border-radius: 10px; }}
        .header {{ background: {(status == "Success" ? "#16a34a" : "#dc2626")}; padding: 15px; color: white; text-align: center; border-radius: 8px; }}
        .content {{ background: white; padding: 20px; margin-top: 15px; border-radius: 8px; }}
        .status {{ font-size: 18px; font-weight: bold; color: {(status == "Success" ? "#16a34a" : "#dc2626")}; }}
        .details {{ margin-top: 10px; padding: 10px; background: #f1f5f9; border-radius: 5px; }}
        .footer {{ margin-top: 20px; text-align: center; color: #94a3b8; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📋 تقرير النسخ الاحتياطي</h2>
        </div>
        <div class='content'>
            <h3>📌 {jobName}</h3>
            <p class='status'>الحالة: {(status == "Success" ? "✅ نجح" : "❌ فشل")}</p>
            <div class='details'>
                <p>📅 التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                <p>📋 التفاصيل: {details}</p>
            </div>
        </div>
        <div class='footer'>
            Smart Backup Suite - نظام النسخ الاحتياطي الذكي
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(email, subject, body, settings);
            }
            catch (Exception ex)
            {
                LogService.WriteLog("EmailService", "Error", $"❌ فشل إرسال إشعار المهمة: {ex.Message}");
                return false;
            }
        }
    }
}