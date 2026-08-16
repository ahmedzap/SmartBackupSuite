


using Dropbox.Api;
using Dropbox.Api.Files;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartBackupSuite.Services
{
    public static class CloudUploadService
    {
        // ==================== إعدادات Google Drive ====================
        private static string GOOGLE_CLIENT_ID = "";
        private static string GOOGLE_CLIENT_SECRET = "";
        private static readonly string CredentialsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "credentials.json"
        );
        private static readonly string TokenFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "GoogleDriveTokens"
        );
        private static readonly string[] GoogleScopes = new[] { DriveService.Scope.Drive };
        private static UserCredential _googleCredential = null;
        private static bool _googleAuthorized = false;
        private static bool _credentialsLoaded = false;
        private static string _cachedFolderId = null;
        private const string DEFAULT_FOLDER_NAME = "SmartBackup";

        // ==================== إعدادات Dropbox ====================
        private const string DROPBOX_APP_KEY = "qse09oaja9jvc9s";
        private const string DROPBOX_APP_SECRET = "d7w2et4zf0a89s0";
        private static readonly string DropboxTokenFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "DropboxTokens"
        );
        private static string _dropboxRefreshToken = null;
        private static string _dropboxAccessToken = null;
        private static bool _dropboxAuthorized = false;

        // ==================== إعدادات OneDrive ====================
        private const string ONEDRIVE_CLIENT_ID = "your-client-id-here";
        private static readonly string OneDriveTokenFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "OneDriveTokens"
        );
        private static bool _oneDriveAuthorized = false;

        // ==================== دوال مساعدة للإشعارات ====================
        private static void ShowAutoDismissNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int duration = 3000)
        {
            try
            {
                var notifyIcon = new NotifyIcon()
                {
                    Icon = SystemIcons.Application,
                    Visible = true,
                    BalloonTipTitle = title,
                    BalloonTipText = message,
                    BalloonTipIcon = icon
                };
                notifyIcon.ShowBalloonTip(duration);
                Task.Delay(duration + 500).ContinueWith(_ =>
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                });
            }
            catch
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ==================== Google Drive ====================
        private static string FindCredentialsFile()
        {
            if (File.Exists(CredentialsPath))
            {
                LogService.WriteLog("GoogleDrive", "Info", $"✅ تم العثور على credentials.json في: {CredentialsPath}");
                return CredentialsPath;
            }

            try
            {
                string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "credentials.json", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    LogService.WriteLog("GoogleDrive", "Info", $"✅ تم العثور على credentials.json في: {files[0]}");
                    return files[0];
                }
            }
            catch { }
            return null;
        }

        private static bool LoadCredentialsFromFile()
        {
            try
            {
                if (_credentialsLoaded && !string.IsNullOrEmpty(GOOGLE_CLIENT_ID)) return true;

                string filePath = FindCredentialsFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    LogService.WriteLog("GoogleDrive", "Warning", "⚠️ ملف credentials.json غير موجود");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                using (var document = JsonDocument.Parse(json))
                {
                    var root = document.RootElement;
                    if (root.TryGetProperty("installed", out var installed))
                    {
                        if (installed.TryGetProperty("client_id", out var clientId))
                            GOOGLE_CLIENT_ID = clientId.GetString() ?? "";
                        if (installed.TryGetProperty("client_secret", out var clientSecret))
                            GOOGLE_CLIENT_SECRET = clientSecret.GetString() ?? "";
                    }
                }

                if (string.IsNullOrEmpty(GOOGLE_CLIENT_ID) || string.IsNullOrEmpty(GOOGLE_CLIENT_SECRET))
                {
                    LogService.WriteLog("GoogleDrive", "Error", "❌ فشل قراءة Client ID أو Client Secret من الملف");
                    return false;
                }

                _credentialsLoaded = true;
                LogService.WriteLog("GoogleDrive", "Success", $"✅ تم تحميل Client ID: {GOOGLE_CLIENT_ID}");
                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل تحميل credentials.json: {ex.Message}");
                return false;
            }
        }

        private static async Task<string> FindFolder(DriveService service, string folderName)
        {
            try
            {
                var searchQuery = $"name='{folderName}' and mimeType='application/vnd.google-apps.folder' and trashed=false";
                var request = service.Files.List();
                request.Q = searchQuery;
                request.Fields = "files(id, name, parents)";
                request.PageSize = 10;

                var result = await request.ExecuteAsync();

                if (result.Files != null && result.Files.Count > 0)
                {
                    foreach (var file in result.Files)
                    {
                        if (file.Parents != null && file.Parents.Contains("root"))
                            return file.Id;
                    }
                    return result.Files[0].Id;
                }
                return null;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("GoogleDrive", "Warning", $"⚠️ فشل البحث عن المجلد: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> CreateFolder(DriveService service, string folderName)
        {
            try
            {
                LogService.WriteLog("GoogleDrive", "Info", $"📁 جاري إنشاء مجلد جديد: {folderName}");
                var folderMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder"
                };
                var createRequest = service.Files.Create(folderMetadata);
                createRequest.Fields = "id";
                var folder = await createRequest.ExecuteAsync();
                LogService.WriteLog("GoogleDrive", "Success", $"✅ تم إنشاء المجلد: {folderName} (ID: {folder.Id})");
                return folder.Id;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل إنشاء المجلد: {ex.Message}");
                throw;
            }
        }

        private static async Task<string> GetOrCreateFolder(DriveService service, string folderId = null, string folderName = DEFAULT_FOLDER_NAME)
        {
            if (!string.IsNullOrEmpty(_cachedFolderId))
            {
                try
                {
                    var checkRequest = service.Files.Get(_cachedFolderId);
                    checkRequest.Fields = "id, name";
                    var folder = await checkRequest.ExecuteAsync();
                    LogService.WriteLog("GoogleDrive", "Info", $"✅ استخدام المجلد المخزن: {folder.Name} (ID: {folder.Id})");
                    return _cachedFolderId;
                }
                catch
                {
                    _cachedFolderId = null;
                    LogService.WriteLog("GoogleDrive", "Warning", "⚠️ المجلد المخزن غير صالح، جاري البحث مرة أخرى...");
                }
            }

            if (!string.IsNullOrEmpty(folderId))
            {
                try
                {
                    var checkRequest = service.Files.Get(folderId);
                    checkRequest.Fields = "id, name";
                    var folder = await checkRequest.ExecuteAsync();
                    LogService.WriteLog("GoogleDrive", "Info", $"✅ تم العثور على المجلد: {folder.Name} (ID: {folder.Id})");
                    _cachedFolderId = folder.Id;
                    return folder.Id;
                }
                catch
                {
                    LogService.WriteLog("GoogleDrive", "Warning", $"⚠️ المجلد {folderId} غير موجود. سيتم البحث عن المجلد '{folderName}'...");
                }
            }

            string existingFolderId = await FindFolder(service, folderName);
            if (!string.IsNullOrEmpty(existingFolderId))
            {
                LogService.WriteLog("GoogleDrive", "Info", $"✅ تم العثور على المجلد: {folderName} (ID: {existingFolderId})");
                _cachedFolderId = existingFolderId;
                return existingFolderId;
            }

            string newFolderId = await CreateFolder(service, folderName);
            _cachedFolderId = newFolderId;
            return newFolderId;
        }

        public static void ResetGoogleDriveCache()
        {
            _cachedFolderId = null;
            LogService.WriteLog("GoogleDrive", "Info", "🔄 تم إعادة تعيين التخزين المؤقت للمجلد");
        }

        public static async Task<bool> UploadToGoogleDrive(string filePath, string folderId = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogService.WriteLog("GoogleDrive", "Error", $"❌ الملف غير موجود: {filePath}");
                    return false;
                }

                if (!_credentialsLoaded)
                {
                    if (!LoadCredentialsFromFile())
                    {
                        ShowGoogleDriveSetupInstructions();
                        return false;
                    }
                }

                if (!_googleAuthorized)
                {
                    if (!await AuthenticateGoogleDrive())
                        return false;
                    _googleAuthorized = true;
                }

                LogService.WriteLog("GoogleDrive", "Info", $"📤 جاري رفع الملف: {Path.GetFileName(filePath)}");
                LogService.WriteLog("GoogleDrive", "Info", $"📊 حجم الملف: {new FileInfo(filePath).Length / 1024 / 1024} MB");

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _googleCredential,
                    ApplicationName = "Smart Backup Suite",
                    HttpClientTimeout = TimeSpan.FromMinutes(30)
                });

                string targetFolderId = await GetOrCreateFolder(service, folderId, DEFAULT_FOLDER_NAME);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new[] { targetFolderId }
                };

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                    request.Fields = "id, name, webViewLink";

                    request.ProgressChanged += (progress) =>
                    {
                        if (progress.Status == UploadStatus.Uploading)
                        {
                            double percentage = (double)progress.BytesSent / stream.Length * 100;
                            if (percentage % 10 < 1)
                                LogService.WriteLog("GoogleDrive", "Info", $"📤 جاري الرفع: {percentage:F0}%");
                        }
                        else if (progress.Status == UploadStatus.Completed)
                        {
                            LogService.WriteLog("GoogleDrive", "Success", $"✅ تم رفع الملف بنجاح");
                        }
                        else if (progress.Status == UploadStatus.Failed)
                        {
                            LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل الرفع: {progress.Exception?.Message}");
                        }
                    };

                    var response = await request.UploadAsync();

                    if (response.Status == UploadStatus.Completed)
                    {
                        LogService.WriteLog("GoogleDrive", "Success", $"✅ تم رفع الملف إلى Google Drive: {Path.GetFileName(filePath)}");
                        if (request.ResponseBody != null && !string.IsNullOrEmpty(request.ResponseBody.WebViewLink))
                        {
                            LogService.WriteLog("GoogleDrive", "Success", $"🔗 الرابط: {request.ResponseBody.WebViewLink}");
                        }
                        ShowAutoDismissNotification("✅ Google Drive", $"تم رفع الملف بنجاح: {Path.GetFileName(filePath)}", ToolTipIcon.Info);
                        return true;
                    }
                    else
                    {
                        LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل الرفع: {response.Exception?.Message ?? "خطأ غير معروف"}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل الرفع: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> AuthenticateGoogleDrive()
        {
            try
            {
                if (!Directory.Exists(TokenFolderPath))
                    Directory.CreateDirectory(TokenFolderPath);

                if (string.IsNullOrEmpty(GOOGLE_CLIENT_ID))
                {
                    if (!LoadCredentialsFromFile())
                    {
                        ShowGoogleDriveSetupInstructions();
                        return false;
                    }
                }

                LogService.WriteLog("GoogleDrive", "Info", "🔐 جاري فتح المتصفح للمصادقة مع Google...");

                var clientSecrets = new ClientSecrets
                {
                    ClientId = GOOGLE_CLIENT_ID,
                    ClientSecret = GOOGLE_CLIENT_SECRET
                };

                _googleCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    GoogleScopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(TokenFolderPath, true)
                );

                LogService.WriteLog("GoogleDrive", "Success", "✅ تمت المصادقة مع Google Drive بنجاح");
                ResetGoogleDriveCache();

                ShowAutoDismissNotification("✅ Google Drive - تم التفعيل!",
                    "تم حفظ بيانات المصادقة.\nسيتم رفع الملفات تلقائياً إلى Google Drive.\n\n💡 هذه العملية مطلوبة مرة واحدة فقط!",
                    ToolTipIcon.Info, 5000);

                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("GoogleDrive", "Error", $"❌ فشل المصادقة: {ex.Message}");
                ShowAutoDismissNotification("❌ Google Drive", $"فشل المصادقة: {ex.Message}", ToolTipIcon.Error);
                return false;
            }
        }

        private static void ShowGoogleDriveSetupInstructions()
        {
            string message =
                "🔑 إعداد Google Drive:\n\n" +
                "📌 الخطوات:\n\n" +
                "1️⃣ اذهب إلى: https://console.cloud.google.com/\n" +
                "2️⃣ APIs & Services → Credentials\n" +
                "3️⃣ Create Credentials → OAuth client ID\n" +
                "4️⃣ اختر Desktop application\n" +
                "5️⃣ حمّل ملف JSON\n" +
                $"6️⃣ ضعه في مجلد التطبيق باسم: credentials.json\n\n" +
                "📁 موقع التطبيق:\n" + AppDomain.CurrentDomain.BaseDirectory;

            MessageBox.Show(message, "Google Drive - الإعداد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            System.Diagnostics.Process.Start("https://console.cloud.google.com/apis/credentials");
        }

        public static void ClearGoogleDriveToken()
        {
            try
            {
                if (Directory.Exists(TokenFolderPath))
                {
                    Directory.Delete(TokenFolderPath, true);
                    _googleAuthorized = false;
                    _googleCredential = null;
                    ResetGoogleDriveCache();
                    LogService.WriteLog("GoogleDrive", "Info", "✅ تم حذف توكين Google Drive");
                    ShowAutoDismissNotification("✅ Google Drive", "تم حذف توكين Google Drive بنجاح", ToolTipIcon.Info);
                }
                else
                {
                    ShowAutoDismissNotification("ℹ️ معلومات", "لا يوجد توكين Google Drive محفوظ للحذف", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                ShowAutoDismissNotification("❌ خطأ", $"فشل حذف التوكين: {ex.Message}", ToolTipIcon.Error);
            }
        }

        // ==================== Dropbox ====================
        public static async Task<bool> UploadToDropbox(string filePath, string folderPath = "/Backups")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogService.WriteLog("Dropbox", "Error", $"❌ الملف غير موجود: {filePath}");
                    return false;
                }

                if (!_dropboxAuthorized)
                {
                    if (!await AuthenticateDropbox())
                        return false;
                    _dropboxAuthorized = true;
                }

                LogService.WriteLog("Dropbox", "Info", $"📤 جاري رفع الملف: {Path.GetFileName(filePath)}");
                LogService.WriteLog("Dropbox", "Info", $"📊 حجم الملف: {new FileInfo(filePath).Length / 1024 / 1024} MB");

                string accessToken = await GetDropboxAccessToken();
                if (string.IsNullOrEmpty(accessToken))
                {
                    LogService.WriteLog("Dropbox", "Error", "❌ فشل الحصول على Access Token");
                    return false;
                }

                using (var dbx = new DropboxClient(accessToken))
                {
                    var fileName = Path.GetFileName(filePath);
                    string dropboxPath = $"{folderPath}/{fileName}".Replace("//", "/");

                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var uploadArg = new UploadArg(dropboxPath, WriteMode.Overwrite.Instance);
                        await dbx.Files.UploadAsync(uploadArg, stream);

                        LogService.WriteLog("Dropbox", "Success", $"✅ تم رفع الملف إلى Dropbox: {fileName}");
                        ShowAutoDismissNotification("✅ Dropbox", $"تم رفع الملف بنجاح: {fileName}", ToolTipIcon.Info);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dropbox", "Error", $"❌ فشل الرفع: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> AuthenticateDropbox()
        {
            try
            {
                if (!Directory.Exists(DropboxTokenFolder))
                    Directory.CreateDirectory(DropboxTokenFolder);

                if (DROPBOX_APP_KEY == "your-dropbox-app-key")
                {
                    MessageBox.Show(
                        "⚠️ لم يتم إعداد Dropbox App Key.\n\n" +
                        "يرجى:\n" +
                        "1. الذهاب إلى https://www.dropbox.com/developers/apps\n" +
                        "2. Create app → Scoped access → App folder\n" +
                        "3. نسخ App Key و App Secret\n" +
                        "4. إضافتهما في الكود (DROPBOX_APP_KEY و DROPBOX_APP_SECRET)",
                        "Dropbox - الإعداد",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return false;
                }

                _dropboxRefreshToken = LoadDropboxRefreshToken();

                if (!string.IsNullOrEmpty(_dropboxRefreshToken))
                {
                    LogService.WriteLog("Dropbox", "Info", "🔄 استخدام Refresh Token Dropbox محفوظ");
                    _dropboxAccessToken = await GetDropboxAccessTokenFromRefresh();
                    if (!string.IsNullOrEmpty(_dropboxAccessToken))
                        return true;
                }

                LogService.WriteLog("Dropbox", "Info", "🔐 جاري فتح المتصفح للمصادقة مع Dropbox...");

                string redirectUri = "http://localhost";
                string authUrl = $"https://www.dropbox.com/oauth2/authorize?" +
                    $"client_id={DROPBOX_APP_KEY}&" +
                    $"response_type=code&" +
                    $"token_access_type=offline&" +
                    $"redirect_uri={redirectUri}";

                var browser = new System.Diagnostics.Process();
                browser.StartInfo.UseShellExecute = true;
                browser.StartInfo.FileName = authUrl;
                browser.Start();

                string authCode = await GetAuthCodeFromUser("Dropbox");
                if (string.IsNullOrEmpty(authCode))
                {
                    LogService.WriteLog("Dropbox", "Error", "❌ لم يتم إدخال رمز المصادقة");
                    return false;
                }

                using (var httpClient = new HttpClient())
                {
                    var requestBody = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("code", authCode),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("client_id", DROPBOX_APP_KEY),
                        new KeyValuePair<string, string>("client_secret", DROPBOX_APP_SECRET),
                        new KeyValuePair<string, string>("redirect_uri", redirectUri)
                    });

                    var response = await httpClient.PostAsync(
                        "https://api.dropbox.com/oauth2/token",
                        requestBody
                    );

                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        LogService.WriteLog("Dropbox", "Error", $"❌ فشل الحصول على Token: {responseContent}");
                        return false;
                    }

                    using (var document = JsonDocument.Parse(responseContent))
                    {
                        var root = document.RootElement;

                        if (root.TryGetProperty("refresh_token", out JsonElement refreshElement))
                        {
                            string refreshToken = refreshElement.GetString() ?? "";
                            if (!string.IsNullOrEmpty(refreshToken))
                            {
                                SaveDropboxRefreshToken(refreshToken);
                                _dropboxRefreshToken = refreshToken;
                                LogService.WriteLog("Dropbox", "Success", "✅ تم حفظ Refresh Token");
                            }
                        }

                        if (root.TryGetProperty("access_token", out JsonElement tokenElement))
                        {
                            _dropboxAccessToken = tokenElement.GetString() ?? "";
                        }
                    }
                }

                ShowAutoDismissNotification("✅ Dropbox - تم التفعيل!",
                    "تم حفظ Refresh Token.\nسيتم رفع الملفات تلقائياً إلى Dropbox.\n\n💡 هذه العملية مطلوبة مرة واحدة فقط!",
                    ToolTipIcon.Info, 5000);

                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dropbox", "Error", $"❌ فشل المصادقة: {ex.Message}");
                ShowAutoDismissNotification("❌ Dropbox", $"فشل المصادقة: {ex.Message}", ToolTipIcon.Error);
                return false;
            }
        }

        private static async Task<string> GetDropboxAccessTokenFromRefresh()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var requestBody = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", _dropboxRefreshToken),
                        new KeyValuePair<string, string>("client_id", DROPBOX_APP_KEY),
                        new KeyValuePair<string, string>("client_secret", DROPBOX_APP_SECRET)
                    });

                    var response = await httpClient.PostAsync(
                        "https://api.dropbox.com/oauth2/token",
                        requestBody
                    );

                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        LogService.WriteLog("Dropbox", "Error", $"❌ فشل تجديد Access Token: {responseContent}");
                        return null;
                    }

                    using (var document = JsonDocument.Parse(responseContent))
                    {
                        var root = document.RootElement;
                        if (root.TryGetProperty("access_token", out JsonElement tokenElement))
                            return tokenElement.GetString() ?? "";
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dropbox", "Error", $"❌ خطأ في تجديد Access Token: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> GetDropboxAccessToken()
        {
            try
            {
                if (!string.IsNullOrEmpty(_dropboxAccessToken))
                    return _dropboxAccessToken;

                if (!string.IsNullOrEmpty(_dropboxRefreshToken))
                {
                    _dropboxAccessToken = await GetDropboxAccessTokenFromRefresh();
                    if (!string.IsNullOrEmpty(_dropboxAccessToken))
                        return _dropboxAccessToken;
                }
                return null;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dropbox", "Error", $"❌ خطأ في الحصول على Access Token: {ex.Message}");
                return null;
            }
        }

        private static string LoadDropboxRefreshToken()
        {
            try
            {
                string tokenFilePath = Path.Combine(DropboxTokenFolder, "refresh_token.txt");
                if (File.Exists(tokenFilePath))
                    return File.ReadAllText(tokenFilePath).Trim();
            }
            catch { }
            return null;
        }

        private static void SaveDropboxRefreshToken(string token)
        {
            try
            {
                string tokenFilePath = Path.Combine(DropboxTokenFolder, "refresh_token.txt");
                File.WriteAllText(tokenFilePath, token);
            }
            catch { }
        }

        public static void ClearDropboxToken()
        {
            try
            {
                if (Directory.Exists(DropboxTokenFolder))
                {
                    Directory.Delete(DropboxTokenFolder, true);
                    _dropboxAuthorized = false;
                    _dropboxRefreshToken = null;
                    _dropboxAccessToken = null;
                    LogService.WriteLog("Dropbox", "Info", "✅ تم حذف توكين Dropbox");
                    ShowAutoDismissNotification("✅ Dropbox", "تم حذف توكين Dropbox بنجاح", ToolTipIcon.Info);
                }
                else
                {
                    ShowAutoDismissNotification("ℹ️ معلومات", "لا يوجد توكين Dropbox محفوظ للحذف", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                ShowAutoDismissNotification("❌ خطأ", $"فشل حذف التوكين: {ex.Message}", ToolTipIcon.Error);
            }
        }

        // ==================== OneDrive ====================
        public static async Task<bool> UploadToOneDrive(string filePath, string folderPath = "/Backups")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogService.WriteLog("OneDrive", "Error", $"❌ الملف غير موجود: {filePath}");
                    return false;
                }

                if (!_oneDriveAuthorized)
                {
                    if (!await AuthenticateOneDrive())
                        return false;
                    _oneDriveAuthorized = true;
                }

                LogService.WriteLog("OneDrive", "Info", $"📤 جاري رفع الملف: {Path.GetFileName(filePath)}");
                LogService.WriteLog("OneDrive", "Info", $"📊 حجم الملف: {new FileInfo(filePath).Length / 1024 / 1024} MB");

                if (!Directory.Exists(OneDriveTokenFolder))
                    Directory.CreateDirectory(OneDriveTokenFolder);

                var credential = new Azure.Identity.InteractiveBrowserCredential(
                    new Azure.Identity.InteractiveBrowserCredentialOptions
                    {
                        ClientId = ONEDRIVE_CLIENT_ID,
                        TenantId = "common",
                        RedirectUri = new Uri("http://localhost"),
                        AuthorityHost = Azure.Identity.AzureAuthorityHosts.AzurePublicCloud
                    }
                );

                var graphClient = new GraphServiceClient(credential, new[] { "Files.ReadWrite" });

                try
                {
                    var user = await graphClient.Me.GetAsync();
                    LogService.WriteLog("OneDrive", "Info", $"✅ تم الاتصال بحساب: {user?.UserPrincipalName}");
                }
                catch (Exception ex)
                {
                    LogService.WriteLog("OneDrive", "Error", $"❌ فشل الاتصال: {ex.Message}");
                    return false;
                }

                var drive = await graphClient.Me.Drive.GetAsync();
                string driveId = drive?.Id ?? "";

                if (string.IsNullOrEmpty(driveId))
                {
                    LogService.WriteLog("OneDrive", "Error", "❌ لا يمكن العثور على Drive ID");
                    return false;
                }

                var fileName = Path.GetFileName(filePath);
                string uploadPath = $"{folderPath}/{fileName}".Replace("//", "/").TrimStart('/');

                using (var fileStream = File.OpenRead(filePath))
                {
                    await graphClient
                        .Drives[driveId]
                        .Root
                        .ItemWithPath(uploadPath)
                        .Content
                        .PutAsync(fileStream);

                    LogService.WriteLog("OneDrive", "Success", $"✅ تم رفع الملف إلى OneDrive: {fileName}");
                    ShowAutoDismissNotification("✅ OneDrive", $"تم رفع الملف بنجاح: {fileName}", ToolTipIcon.Info);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.WriteLog("OneDrive", "Error", $"❌ فشل الرفع: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> AuthenticateOneDrive()
        {
            try
            {
                if (!Directory.Exists(OneDriveTokenFolder))
                    Directory.CreateDirectory(OneDriveTokenFolder);

                if (ONEDRIVE_CLIENT_ID == "your-client-id-here")
                {
                    MessageBox.Show(
                        "⚠️ لم يتم إعداد OneDrive Client ID.\n\n" +
                        "يرجى:\n" +
                        "1. الذهاب إلى https://portal.azure.com\n" +
                        "2. Azure Active Directory → App registrations\n" +
                        "3. New registration → سمه: SmartBackupSuite\n" +
                        "4. نسخ Application (client) ID\n" +
                        "5. إضافته في الكود (ONEDRIVE_CLIENT_ID)",
                        "OneDrive - الإعداد",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return false;
                }

                LogService.WriteLog("OneDrive", "Info", "🔐 جاري فتح المتصفح للمصادقة مع OneDrive...");

                var credential = new Azure.Identity.InteractiveBrowserCredential(
                    new Azure.Identity.InteractiveBrowserCredentialOptions
                    {
                        ClientId = ONEDRIVE_CLIENT_ID,
                        TenantId = "common",
                        RedirectUri = new Uri("http://localhost"),
                        AuthorityHost = Azure.Identity.AzureAuthorityHosts.AzurePublicCloud
                    }
                );

                var graphClient = new GraphServiceClient(credential, new[] { "Files.ReadWrite" });

                try
                {
                    var user = await graphClient.Me.GetAsync();
                    LogService.WriteLog("OneDrive", "Success", $"✅ تمت المصادقة مع OneDrive بنجاح");
                }
                catch (Exception ex)
                {
                    LogService.WriteLog("OneDrive", "Error", $"❌ فشل المصادقة: {ex.Message}");
                    return false;
                }

                ShowAutoDismissNotification("✅ OneDrive - تم التفعيل!",
                    "تم حفظ بيانات المصادقة.\nسيتم رفع الملفات تلقائياً إلى OneDrive.\n\n💡 هذه العملية مطلوبة مرة واحدة فقط!",
                    ToolTipIcon.Info, 5000);

                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("OneDrive", "Error", $"❌ فشل المصادقة: {ex.Message}");
                ShowAutoDismissNotification("❌ OneDrive", $"فشل المصادقة: {ex.Message}", ToolTipIcon.Error);
                return false;
            }
        }

        public static void ClearOneDriveToken()
        {
            try
            {
                if (Directory.Exists(OneDriveTokenFolder))
                {
                    Directory.Delete(OneDriveTokenFolder, true);
                    _oneDriveAuthorized = false;
                    LogService.WriteLog("OneDrive", "Info", "✅ تم حذف توكين OneDrive");
                    ShowAutoDismissNotification("✅ OneDrive", "تم حذف توكين OneDrive بنجاح", ToolTipIcon.Info);
                }
                else
                {
                    ShowAutoDismissNotification("ℹ️ معلومات", "لا يوجد توكين OneDrive محفوظ للحذف", ToolTipIcon.Info);
                }
            }
            catch (Exception ex)
            {
                ShowAutoDismissNotification("❌ خطأ", $"فشل حذف التوكين: {ex.Message}", ToolTipIcon.Error);
            }
        }

        // ==================== AWS S3 ====================
        public static async Task<bool> UploadToS3(string filePath, string bucketName, string accessKey, string secretKey, string region = "us-east-1")
        {
            try
            {
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
                {
                    LogService.WriteLog("S3", "Error", "❌ لم يتم إدخال مفاتيح AWS S3");
                    return false;
                }

                if (string.IsNullOrEmpty(bucketName))
                {
                    LogService.WriteLog("S3", "Error", "❌ لم يتم إدخال اسم الـ Bucket");
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    LogService.WriteLog("S3", "Error", $"❌ الملف غير موجود: {filePath}");
                    return false;
                }

                LogService.WriteLog("S3", "Info", $"📤 جاري رفع الملف: {Path.GetFileName(filePath)}");
                LogService.WriteLog("S3", "Info", $"📊 حجم الملف: {new FileInfo(filePath).Length / 1024 / 1024} MB");

                // في الإصدار الحقيقي، استخدم AWSSDK.S3
                LogService.WriteLog("S3", "Success", $"✅ تم رفع الملف إلى S3 (تجريبي)");
                ShowAutoDismissNotification("✅ S3", $"تم رفع الملف بنجاح (تجريبي): {Path.GetFileName(filePath)}", ToolTipIcon.Info);
                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("S3", "Error", $"❌ فشل الرفع: {ex.Message}");
                return false;
            }
        }

        // ==================== Azure Blob ====================
        public static async Task<bool> UploadToAzure(string filePath, string containerName, string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    LogService.WriteLog("Azure", "Error", "❌ لم يتم إدخال سلسلة الاتصال لـ Azure");
                    return false;
                }

                if (string.IsNullOrEmpty(containerName))
                {
                    LogService.WriteLog("Azure", "Error", "❌ لم يتم إدخال اسم الحاوية (Container) لـ Azure");
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    LogService.WriteLog("Azure", "Error", $"❌ الملف غير موجود: {filePath}");
                    return false;
                }

                LogService.WriteLog("Azure", "Info", $"📤 جاري رفع الملف إلى Azure: {Path.GetFileName(filePath)}");
                LogService.WriteLog("Azure", "Info", $"📊 حجم الملف: {new FileInfo(filePath).Length / 1024 / 1024} MB");

                // في الإصدار الحقيقي، استخدم Azure.Storage.Blobs
                LogService.WriteLog("Azure", "Success", $"✅ تم رفع الملف إلى Azure (تجريبي)");
                ShowAutoDismissNotification("✅ Azure", $"تم رفع الملف بنجاح (تجريبي): {Path.GetFileName(filePath)}", ToolTipIcon.Info);
                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Azure", "Error", $"❌ فشل الرفع: {ex.Message}");
                return false;
            }
        }

        // ==================== أدوات مساعدة للتوكنات ====================
        private static async Task<string> GetAuthCodeFromUser(string serviceName)
        {
            return await Task.Run(() =>
            {
                using (var inputForm = new Form())
                {
                    inputForm.Text = $"🔑 إدخال رمز المصادقة - {serviceName}";
                    inputForm.Size = new System.Drawing.Size(550, 200);
                    inputForm.StartPosition = FormStartPosition.CenterScreen;
                    inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputForm.MaximizeBox = false;
                    inputForm.MinimizeBox = false;
                    inputForm.BackColor = System.Drawing.Color.FromArgb(224, 247, 250);
                    inputForm.RightToLeft = RightToLeft.Yes;

                    var label = new Label()
                    {
                        Text = $"📋 بعد تسجيل الدخول إلى {serviceName}، انسخ رمز المصادقة من شريط العنوان:\n" +
                               "مثال: http://localhost/?code=YOUR_CODE_HERE",
                        Location = new System.Drawing.Point(20, 20),
                        Size = new System.Drawing.Size(500, 50),
                        Font = new System.Drawing.Font("Segoe UI", 9F),
                        TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                    };

                    var textBox = new TextBox()
                    {
                        Location = new System.Drawing.Point(20, 85),
                        Size = new System.Drawing.Size(490, 25),
                        Font = new System.Drawing.Font("Segoe UI", 10F),
                        TextAlign = HorizontalAlignment.Center
                    };

                    var button = new Button()
                    {
                        Text = "✅ تأكيد",
                        Location = new System.Drawing.Point(200, 125),
                        Size = new System.Drawing.Size(120, 30),
                        Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold),
                        BackColor = System.Drawing.Color.FromArgb(46, 204, 113),
                        ForeColor = System.Drawing.Color.White,
                        FlatStyle = FlatStyle.Flat,
                        DialogResult = DialogResult.OK
                    };

                    inputForm.Controls.Add(label);
                    inputForm.Controls.Add(textBox);
                    inputForm.Controls.Add(button);

                    string result = "";
                    button.Click += (s, e) =>
                    {
                        result = textBox.Text.Trim();
                        inputForm.DialogResult = DialogResult.OK;
                        inputForm.Close();
                    };

                    inputForm.KeyPreview = true;
                    inputForm.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Escape)
                        {
                            inputForm.DialogResult = DialogResult.Cancel;
                            inputForm.Close();
                        }
                    };

                    if (inputForm.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(result))
                        return result;
                    return null;
                }
            });
        }

        // ==================== إدارة التوكنات العامة ====================
        public static bool HasToken(string serviceName)
        {
            string key = serviceName switch
            {
                "Google Drive" => "GoogleDrive",
                "Dropbox" => "Dropbox",
                "OneDrive" => "OneDrive",
                _ => serviceName
            };

            string tokenPath = GetTokenPath(key);
            return File.Exists(tokenPath);
        }

        public static bool HasAnyToken()
        {
            return HasToken("GoogleDrive") || HasToken("Dropbox") || HasToken("OneDrive");
        }

        public static void ClearToken(string service)
        {
            string key = service switch
            {
                "GoogleDrive" => "GoogleDrive",
                "Dropbox" => "Dropbox",
                "OneDrive" => "OneDrive",
                _ => service
            };

            string tokenPath = GetTokenPath(key);
            if (File.Exists(tokenPath))
            {
                try { File.Delete(tokenPath); }
                catch { }
            }
        }

        private static string GetTokenPath(string service)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            return service switch
            {
                "GoogleDrive" => Path.Combine(appPath, "token_google.json"),
                "Dropbox" => Path.Combine(appPath, "token_dropbox.json"),
                "OneDrive" => Path.Combine(appPath, "token_onedrive.json"),
                _ => Path.Combine(appPath, $"token_{service.ToLower()}.json")
            };
        }

        public static void ClearAllTokens()
        {
            ClearToken("GoogleDrive");
            ClearToken("Dropbox");
            ClearToken("OneDrive");
            ShowAutoDismissNotification("✅ تم الحذف",
                "تم حذف جميع التوكينات المحفوظة.\nسيُطلب منك تسجيل الدخول مرة أخرى عند استخدام أي خدمة سحابية.",
                ToolTipIcon.Info, 4000);
        }
    }
}