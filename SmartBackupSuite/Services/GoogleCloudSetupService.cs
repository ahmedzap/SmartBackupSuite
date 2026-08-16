//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Drive.v3;
//using Google.Apis.Services;
//using Google.Apis.Util.Store;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace SmartBackupSuite.Services
//{
//    /// <summary>
//    /// خدمة إعداد وإدارة Google Drive مع دعم حسابات متعددة
//    /// </summary>
//    public static class GoogleCloudSetupService
//    {
//        #region 🔑 الثوابت

//        // ✅ Client ID و Secret مدمجان (مسجلين مسبقاً من المطور)
//        private const string GOOGLE_CLIENT_ID = "870684669281-4s2d7i87q8l2vdks8sb70078e4imofju.apps.googleusercontent.com";
//        private const string GOOGLE_CLIENT_SECRET = "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";

//        private static readonly string CredentialsPath = Path.Combine(
//            AppDomain.CurrentDomain.BaseDirectory,
//            "credentials.json"
//        );

//        // ✅ مجلد رئيسي للتوكينات
//        private static readonly string TokenRootFolder = Path.Combine(
//            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//            "SmartBackupSuite",
//            "GoogleDriveTokens"
//        );

//        private static readonly string[] Scopes = new[]
//        {
//            DriveService.Scope.Drive
//        };

//        #endregion

//        #region 🚀 الإعداد التلقائي

//        /// <summary>
//        /// إعداد Google Drive تلقائياً (يحاول إنشاء credentials.json)
//        /// </summary>
//        public static async Task<bool> SetupGoogleDriveAutomatically()
//        {
//            try
//            {
//                LogService.WriteLog("GoogleCloud", "Info", "🔄 جاري إعداد Google Drive تلقائياً...");

//                // ✅ 1. التحقق من وجود credentials.json
//                if (File.Exists(CredentialsPath))
//                {
//                    LogService.WriteLog("GoogleCloud", "Info", "✅ ملف credentials.json موجود");

//                    // ✅ إشعار يختفي تلقائياً
//                    ShowAutoDismissNotification(
//                        "✅ Google Drive - تم التفعيل!",
//                        "تم حفظ بيانات المصادقة.\nسيتم رفع الملفات تلقائياً إلى Google Drive.",
//                        ToolTipIcon.Info
//                    );
//                    return true;
//                }

//                // ✅ 2. إنشاء مجلد التوكين
//                if (!Directory.Exists(TokenRootFolder))
//                {
//                    Directory.CreateDirectory(TokenRootFolder);
//                }

//                // ✅ 3. التحقق من Client ID
//                if (GOOGLE_CLIENT_ID == "YOUR_CLIENT_ID")
//                {
//                    ShowGoogleSetupInstructions();
//                    return false;
//                }

//                // ✅ 4. المصادقة (تفتح المتصفح)
//                LogService.WriteLog("GoogleCloud", "Info", "🔐 جاري فتح المتصفح للمصادقة مع Google...");

//                var clientSecrets = new ClientSecrets
//                {
//                    ClientId = GOOGLE_CLIENT_ID,
//                    ClientSecret = GOOGLE_CLIENT_SECRET
//                };

//                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
//                    clientSecrets,
//                    Scopes,
//                    "user",
//                    CancellationToken.None,
//                    new FileDataStore(TokenRootFolder, true)
//                );

//                // ✅ 5. إنشاء ملف credentials.json
//                await CreateCredentialsFile(clientSecrets);

//                LogService.WriteLog("GoogleCloud", "Success", "✅ تم إعداد Google Drive تلقائياً بنجاح");

//                // ✅ ✅ ✅ إشعار نجاح يختفي تلقائياً (بدون موافقة المستخدم)
//                ShowAutoDismissNotification(
//                    "✅ Google Drive - تم التفعيل!",
//                    "تم حفظ بيانات المصادقة.\nسيتم رفع الملفات تلقائياً إلى Google Drive.\n\n💡 هذه العملية مطلوبة مرة واحدة فقط!",
//                    ToolTipIcon.Info,
//                    5000 // ✅ يظهر لمدة 5 ثواني
//                );

//                return true;
//            }
//            catch (Exception ex)
//            {
//                LogService.WriteLog("GoogleCloud", "Error", $"❌ فشل إعداد Google Drive: {ex.Message}");

//                // ✅ إشعار خطأ يختفي تلقائياً
//                ShowAutoDismissNotification(
//                    "❌ Google Drive - فشل التفعيل",
//                    $"حدث خطأ أثناء الإعداد: {ex.Message}",
//                    ToolTipIcon.Error
//                );

//                ShowGoogleSetupInstructions();
//                return false;
//            }
//        }

//        /// <summary>
//        /// التحقق من إعداد Google Drive
//        /// </summary>
//        public static bool IsGoogleDriveConfigured()
//        {
//            return File.Exists(CredentialsPath) || Directory.Exists(TokenRootFolder);
//        }

//        #endregion

//        #region 🔐 المصادقة مع حسابات متعددة

//        /// <summary>
//        /// الحصول على مجلد مخصص لكل حساب
//        /// </summary>
//        private static string GetUserTokenFolder(string userEmail)
//        {
//            if (string.IsNullOrEmpty(userEmail))
//                return Path.Combine(TokenRootFolder, "default");

//            string safeName = userEmail.Replace("@", "_at_").Replace(".", "_dot_");
//            return Path.Combine(TokenRootFolder, safeName);
//        }

//        /// <summary>
//        /// المصادقة مع Google Drive (ترجع التوكن والبريد الإلكتروني)
//        /// </summary>
//        public static async Task<(UserCredential Credential, string Email)> AuthenticateAsync(string customUserEmail = null)
//        {
//            try
//            {
//                if (!File.Exists(CredentialsPath))
//                {
//                    await SetupGoogleDriveAutomatically();
//                }

//                if (!File.Exists(CredentialsPath))
//                {
//                    throw new Exception("❌ ملف credentials.json غير موجود!");
//                }

//                string userEmail = customUserEmail ?? "user";
//                string tokenFolder = GetUserTokenFolder(userEmail);

//                if (!Directory.Exists(tokenFolder))
//                {
//                    Directory.CreateDirectory(tokenFolder);
//                }

//                UserCredential credential;
//                using (var stream = new FileStream(CredentialsPath, FileMode.Open, FileAccess.Read))
//                {
//                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
//                        GoogleClientSecrets.FromStream(stream).Secrets,
//                        Scopes,
//                        userEmail,
//                        CancellationToken.None,
//                        new FileDataStore(tokenFolder, true)
//                    );
//                }

//                string email = await GetCurrentUserEmailAsync(credential);

//                LogService.WriteLog("GoogleDrive", "Success", $"✅ تمت المصادقة بنجاح مع Google Drive: {email}");

//                ShowAutoDismissNotification(
//                    "✅ Google Drive",
//                    $"تم تسجيل الدخول بنجاح: {email}",
//                    ToolTipIcon.Info
//                );

//                return (credential, email);
//            }
//            catch (Exception ex)
//            {
//                LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل المصادقة: {ex.Message}");
//                throw;
//            }
//        }

//        /// <summary>
//        /// الحصول على البريد الإلكتروني للمستخدم الحالي
//        /// </summary>
//        public static async Task<string> GetCurrentUserEmailAsync(UserCredential credential = null)
//        {
//            try
//            {
//                if (credential == null)
//                {
//                    var (cred, email) = await AuthenticateAsync();
//                    return email;
//                }

//                var service = new DriveService(new BaseClientService.Initializer()
//                {
//                    HttpClientInitializer = credential,
//                    ApplicationName = "Smart Backup Suite"
//                });

//                var about = await service.About.Get().ExecuteAsync();
//                return about.User?.EmailAddress ?? "غير معروف";
//            }
//            catch
//            {
//                return "غير معروف";
//            }
//        }

//        #endregion

//        #region 🗑️ حذف التوكينات

//        /// <summary>
//        /// حذف توكين حساب معين فقط
//        /// </summary>
//        public static void ClearTokenForAccount(string userEmail)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(userEmail))
//                {
//                    ShowAutoDismissNotification("❌ خطأ", "لم يتم تحديد البريد الإلكتروني للحذف.", ToolTipIcon.Error);
//                    return;
//                }

//                string tokenFolder = GetUserTokenFolder(userEmail);

//                if (Directory.Exists(tokenFolder))
//                {
//                    Directory.Delete(tokenFolder, true);
//                    LogService.WriteLog("GoogleDrive", "Info", $"✅ تم حذف توكين Google Drive للحساب: {userEmail}");

//                    ShowAutoDismissNotification(
//                        "✅ Google Drive",
//                        $"تم تسجيل الخروج من الحساب: {userEmail}",
//                        ToolTipIcon.Info
//                    );
//                }
//                else
//                {
//                    ShowAutoDismissNotification(
//                        "ℹ️ معلومات",
//                        $"لا يوجد توكين محفوظ للحساب: {userEmail}",
//                        ToolTipIcon.Info
//                    );
//                }
//            }
//            catch (Exception ex)
//            {
//                ShowAutoDismissNotification(
//                    "❌ خطأ",
//                    $"فشل حذف التوكين: {ex.Message}",
//                    ToolTipIcon.Error
//                );
//            }
//        }

//        /// <summary>
//        /// حذف جميع توكينات Google Drive
//        /// </summary>
//        public static void ClearAllTokens()
//        {
//            try
//            {
//                if (Directory.Exists(TokenRootFolder))
//                {
//                    Directory.Delete(TokenRootFolder, true);
//                    LogService.WriteLog("GoogleDrive", "Info", "✅ تم حذف جميع توكينات Google Drive");

//                    ShowAutoDismissNotification(
//                        "✅ Google Drive",
//                        "تم حذف جميع توكينات المصادقة بنجاح.",
//                        ToolTipIcon.Info
//                    );
//                }
//                else
//                {
//                    ShowAutoDismissNotification(
//                        "ℹ️ معلومات",
//                        "لا توجد توكينات محفوظة للحذف.",
//                        ToolTipIcon.Info
//                    );
//                }
//            }
//            catch (Exception ex)
//            {
//                ShowAutoDismissNotification(
//                    "❌ خطأ",
//                    $"فشل حذف التوكينات: {ex.Message}",
//                    ToolTipIcon.Error
//                );
//            }
//        }

//        /// <summary>
//        /// الحصول على قائمة الحسابات المخزنة
//        /// </summary>
//        public static List<string> GetStoredAccounts()
//        {
//            var accounts = new List<string>();
//            try
//            {
//                if (Directory.Exists(TokenRootFolder))
//                {
//                    var folders = Directory.GetDirectories(TokenRootFolder);
//                    foreach (var folder in folders)
//                    {
//                        string name = Path.GetFileName(folder);
//                        if (name != "default")
//                        {
//                            string email = name.Replace("_at_", "@").Replace("_dot_", ".");
//                            accounts.Add(email);
//                        }
//                    }
//                }
//            }
//            catch { }
//            return accounts;
//        }

//        #endregion

//        #region 🛠️ دوال مساعدة

//        private static async Task CreateCredentialsFile(ClientSecrets secrets)
//        {
//            try
//            {
//                string jsonContent = $@"{{
//  ""installed"": {{
//    ""client_id"": ""{secrets.ClientId}"",
//    ""project_id"": ""smartbackup-project"",
//    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
//    ""token_uri"": ""https://oauth2.googleapis.com/token"",
//    ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
//    ""client_secret"": ""{secrets.ClientSecret}"",
//    ""redirect_uris"": [""http://localhost""]
//  }}
//}}";

//                await File.WriteAllTextAsync(CredentialsPath, jsonContent);
//                LogService.WriteLog("GoogleCloud", "Info", "✅ تم إنشاء ملف credentials.json");
//            }
//            catch (Exception ex)
//            {
//                LogService.WriteLog("GoogleCloud", "Error", $"❌ فشل إنشاء credentials.json: {ex.Message}");
//                throw;
//            }
//        }

//        private static void ShowGoogleSetupInstructions()
//        {
//            string message =
//                "🔑 إعداد Google Drive:\n\n" +
//                "📌 الخطوات:\n\n" +
//                "1️⃣ اذهب إلى: https://console.cloud.google.com/\n" +
//                "2️⃣ APIs & Services → Credentials\n" +
//                "3️⃣ Create Credentials → OAuth client ID\n" +
//                "4️⃣ اختر Desktop application\n" +
//                "5️⃣ انسخ Client ID و Client Secret\n" +
//                "6️⃣ أضفهما في الكود (GOOGLE_CLIENT_ID و GOOGLE_CLIENT_SECRET)\n\n" +
//                "💡 هذا الإجراء مطلوب مرة واحدة فقط!";

//            MessageBox.Show(message, "Google Drive - الإعداد",
//                MessageBoxButtons.OK, MessageBoxIcon.Warning);

//            System.Diagnostics.Process.Start("https://console.cloud.google.com/apis/credentials");
//        }

//        #endregion

//        #region 💬 إشعارات تختفي تلقائياً

//        /// <summary>
//        /// عرض إشعار يختفي تلقائياً بعد 3 ثواني
//        /// </summary>
//        private static void ShowAutoDismissNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int duration = 3000)
//        {
//            try
//            {
//                var notifyIcon = new NotifyIcon()
//                {
//                    Icon = SystemIcons.Application,
//                    Visible = true,
//                    BalloonTipTitle = title,
//                    BalloonTipText = message,
//                    BalloonTipIcon = icon
//                };

//                notifyIcon.ShowBalloonTip(duration);

//                // ✅ تحرير الموارد بعد الإشعار
//                Task.Delay(duration + 500).ContinueWith(_ =>
//                {
//                    notifyIcon.Visible = false;
//                    notifyIcon.Dispose();
//                });
//            }
//            catch
//            {
//                // ✅ في حالة الفشل، استخدم MessageBox (ولكن نادراً ما يحدث)
//                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//        }

//        #endregion

//        #region 📤 رفع الملفات

//        /// <summary>
//        /// رفع ملف إلى Google Drive باستخدام المصادقة المخزنة
//        /// </summary>
//        public static async Task<bool> UploadFileToDrive(string filePath, string folderId, string userEmail = null)
//        {
//            try
//            {
//                if (!File.Exists(filePath))
//                {
//                    ShowAutoDismissNotification("❌ خطأ", $"الملف غير موجود: {filePath}", ToolTipIcon.Error);
//                    return false;
//                }

//                if (string.IsNullOrEmpty(folderId))
//                {
//                    ShowAutoDismissNotification("❌ خطأ", "لم يتم تحديد معرف المجلد", ToolTipIcon.Error);
//                    return false;
//                }

//                var (credential, email) = await AuthenticateAsync(userEmail);

//                var service = new DriveService(new BaseClientService.Initializer()
//                {
//                    HttpClientInitializer = credential,
//                    ApplicationName = "Smart Backup Suite",
//                    HttpClientTimeout = TimeSpan.FromMinutes(30)
//                });

//                try
//                {
//                    var checkRequest = service.Files.Get(folderId);
//                    checkRequest.Fields = "id, name";
//                    var folder = await checkRequest.ExecuteAsync();
//                    LogService.WriteLog("GoogleDrive", "Info", $"✅ تم العثور على المجلد: {folder.Name}");
//                }
//                catch (Exception ex)
//                {
//                    ShowAutoDismissNotification("❌ خطأ", $"المجلد غير موجود: {ex.Message}", ToolTipIcon.Error);
//                    return false;
//                }

//                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
//                {
//                    Name = Path.GetFileName(filePath),
//                    Parents = new[] { folderId }
//                };

//                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
//                {
//                    var request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
//                    request.Fields = "id, name, webViewLink";

//                    var response = await request.UploadAsync();

//                    if (response.Status == Google.Apis.Upload.UploadStatus.Completed)
//                    {
//                        LogService.WriteLog("GoogleDrive", "Success", $"✅ تم رفع الملف: {Path.GetFileName(filePath)}");

//                        ShowAutoDismissNotification(
//                            "✅ Google Drive",
//                            $"تم رفع الملف بنجاح: {Path.GetFileName(filePath)}",
//                            ToolTipIcon.Info
//                        );
//                        return true;
//                    }
//                    else
//                    {
//                        ShowAutoDismissNotification(
//                            "❌ خطأ",
//                            $"فشل رفع الملف: {response.Exception?.Message ?? "خطأ غير معروف"}",
//                            ToolTipIcon.Error
//                        );
//                        return false;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                ShowAutoDismissNotification(
//                    "❌ خطأ",
//                    $"فشل رفع الملف: {ex.Message}",
//                    ToolTipIcon.Error
//                );
//                return false;
//            }
//        }

//        #endregion
//    }
//}