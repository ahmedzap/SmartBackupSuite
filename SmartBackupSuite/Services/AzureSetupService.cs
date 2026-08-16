using Azure.Identity;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartBackupSuite.Services
{
    /// <summary>
    /// خدمة إعداد OneDrive تلقائياً
    /// </summary>
    public static class AzureSetupService
    {
        #region 🔑 الثوابت

        // ✅ Client ID مدمج (مسجل مسبقاً من المطور)
        // يمكنك استخدام هذا الـ Client ID للتجربة (من Azure CLI)
        private const string AZURE_CLIENT_ID = "1950a258-227b-4e31-a9cf-717495945fc2"; // Azure CLI Client ID
        private const string TENANT_ID = "common";

        private static readonly string ClientIdFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "onedrive_clientid.txt"
        );

        private static readonly string TokenFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "OneDriveTokens"
        );

        #endregion

        #region 🚀 الإعداد التلقائي

        /// <summary>
        /// إعداد OneDrive تلقائياً (إنشاء تطبيق Azure AD)
        /// </summary>
        public static async Task<string> SetupOneDriveAutomatically()
        {
            try
            {
                LogService.WriteLog("Azure", "Info", "🔄 جاري إعداد OneDrive تلقائياً...");

                // ✅ 1. التحقق من وجود Client ID محفوظ
                if (File.Exists(ClientIdFilePath))
                {
                    string savedClientId = File.ReadAllText(ClientIdFilePath).Trim();
                    if (!string.IsNullOrEmpty(savedClientId))
                    {
                        LogService.WriteLog("Azure", "Info", $"✅ Client ID محفوظ: {savedClientId}");
                        return savedClientId;
                    }
                }

                // ✅ 2. إنشاء مجلد التوكين
                if (!Directory.Exists(TokenFolderPath))
                {
                    Directory.CreateDirectory(TokenFolderPath);
                }

                // ✅ 3. المصادقة (تفتح المتصفح)
                LogService.WriteLog("Azure", "Info", "🔐 جاري فتح المتصفح للمصادقة مع Microsoft...");

                var credential = new InteractiveBrowserCredential(
                    new InteractiveBrowserCredentialOptions
                    {
                        ClientId = AZURE_CLIENT_ID,
                        TenantId = TENANT_ID,
                        RedirectUri = new Uri("http://localhost"),
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                    }
                );

                var graphClient = new GraphServiceClient(credential, new[] { "Application.ReadWrite.All" });

                // ✅ 4. التحقق من الاتصال
                try
                {
                    var user = await graphClient.Me.GetAsync();
                    LogService.WriteLog("Azure", "Info", $"✅ تم الاتصال بحساب: {user?.UserPrincipalName}");
                }
                catch (Exception ex)
                {
                    LogService.WriteLog("Azure", "Error", $"❌ فشل الاتصال: {ex.Message}");
                    ShowAzureSetupInstructions();
                    return null;
                }

                // ✅ 5. إنشاء تطبيق جديد
                LogService.WriteLog("Azure", "Info", "📝 جاري إنشاء تطبيق Azure AD...");

                // ✅ إصلاح CS0029: تحويل string إلى Guid
                Guid resourceAccessId = Guid.Parse("e1fe6dd8-ba31-4d61-89e7-88639da4683d");

                var app = new Microsoft.Graph.Models.Application
                {
                    DisplayName = $"SmartBackupSuite-{DateTime.Now:yyyyMMdd}",
                    SignInAudience = "AzureADMyOrg",
                    Web = new Microsoft.Graph.Models.WebApplication
                    {
                        RedirectUris = new List<string> { "http://localhost" },
                        ImplicitGrantSettings = new Microsoft.Graph.Models.ImplicitGrantSettings
                        {
                            EnableAccessTokenIssuance = true,
                            EnableIdTokenIssuance = true
                        }
                    },
                    RequiredResourceAccess = new List<Microsoft.Graph.Models.RequiredResourceAccess>
                    {
                        new Microsoft.Graph.Models.RequiredResourceAccess
                        {
                            ResourceAppId = "00000003-0000-0000-c000-000000000000",
                            ResourceAccess = new List<Microsoft.Graph.Models.ResourceAccess>
                            {
                                new Microsoft.Graph.Models.ResourceAccess
                                {
                                    Id = resourceAccessId, // ✅ الآن هو من نوع Guid
                                    Type = "Scope"
                                }
                            }
                        }
                    }
                };

                var createdApp = await graphClient.Applications.PostAsync(app);

                if (createdApp == null)
                {
                    throw new Exception("فشل إنشاء التطبيق في Azure AD - لم يتم إرجاع أي بيانات");
                }

                // ✅ الحصول على Client ID (AppId)
                string clientId = createdApp.AppId;

                if (string.IsNullOrEmpty(clientId))
                {
                    throw new Exception("فشل الحصول على Client ID من التطبيق المُنشأ");
                }

                LogService.WriteLog("Azure", "Info", $"✅ تم إنشاء التطبيق: {createdApp.DisplayName}");

                // ✅ 6. حفظ Client ID
                File.WriteAllText(ClientIdFilePath, clientId);

                LogService.WriteLog("Azure", "Success", $"✅ تم إنشاء تطبيق Azure: {clientId}");

                MessageBox.Show(
                    $"✅ تم إنشاء تطبيق OneDrive بنجاح!\n\n" +
                    $"📋 Client ID: {clientId}\n\n" +
                    "🔐 تم حفظ Client ID تلقائياً.\n" +
                    "📤 يمكنك الآن رفع الملفات إلى OneDrive.\n\n" +
                    "💡 هذه العملية مطلوبة مرة واحدة فقط!",
                    "OneDrive - تم الإعداد",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return clientId;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Azure", "Error", $"❌ فشل إعداد OneDrive: {ex.Message}");
                ShowAzureSetupInstructions();
                return null;
            }
        }

        /// <summary>
        /// التحقق من إعداد OneDrive
        /// </summary>
        public static bool IsOneDriveConfigured()
        {
            return File.Exists(ClientIdFilePath);
        }

        /// <summary>
        /// الحصول على Client ID المحفوظ
        /// </summary>
        public static string LoadClientId()
        {
            try
            {
                if (File.Exists(ClientIdFilePath))
                {
                    return File.ReadAllText(ClientIdFilePath).Trim();
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region 🛠️ دوال مساعدة

        private static void ShowAzureSetupInstructions()
        {
            string message =
                "🔑 إعداد OneDrive:\n\n" +
                "📌 الخطوات:\n\n" +
                "1️⃣ اذهب إلى: https://portal.azure.com\n" +
                "2️⃣ Azure Active Directory → App registrations\n" +
                "3️⃣ New registration → سمه: SmartBackupSuite\n" +
                "4️⃣ انسخ Application (client) ID\n" +
                "5️⃣ أضفه في الكود (AZURE_CLIENT_ID)\n\n" +
                "💡 هذا الإجراء مطلوب مرة واحدة فقط!";

            MessageBox.Show(message, "OneDrive - الإعداد",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // ✅ فتح Azure Portal في المتصفح
            System.Diagnostics.Process.Start("https://portal.azure.com");
        }

        #endregion
    }
}