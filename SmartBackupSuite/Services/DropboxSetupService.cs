using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartBackupSuite.Services
{
    /// <summary>
    /// خدمة إعداد Dropbox تلقائياً
    /// </summary>
    public static class DropboxSetupService
    {
        #region 🔑 الثوابت

        // ✅ App Key و Secret مدمجان (مسجلين مسبقاً من المطور)
        private const string DROPBOX_APP_KEY = "your-dropbox-app-key";
        private const string DROPBOX_APP_SECRET = "your-dropbox-app-secret";

        private static readonly string TokenFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "DropboxTokens"
        );

        #endregion

        #region 🚀 الإعداد التلقائي

        /// <summary>
        /// إعداد Dropbox تلقائياً
        /// </summary>
        public static async Task<bool> SetupDropboxAutomatically()
        {
            try
            {
                LogService.WriteLog("Dropbox", "Info", "🔄 جاري إعداد Dropbox...");

                // ✅ 1. إنشاء مجلد التوكين
                if (!Directory.Exists(TokenFolderPath))
                {
                    Directory.CreateDirectory(TokenFolderPath);
                }

                // ✅ 2. التحقق من وجود App Key
                if (DROPBOX_APP_KEY == "your-dropbox-app-key")
                {
                    ShowDropboxSetupInstructions();
                    return false;
                }

                // ✅ 3. التحقق من وجود Refresh Token محفوظ
                string refreshToken = LoadRefreshToken();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    LogService.WriteLog("Dropbox", "Info", "✅ Refresh Token موجود");
                    return true;
                }

                // ✅ 4. المصادقة (تفتح المتصفح)
                LogService.WriteLog("Dropbox", "Info", "🔐 جاري فتح المتصفح للمصادقة مع Dropbox...");

                bool result = await StartDropboxAuthentication();

                if (result)
                {
                    MessageBox.Show(
                        "✅ تم تفعيل Dropbox بنجاح!\n\n" +
                        "🔐 تم حفظ Refresh Token.\n" +
                        "📤 سيتم رفع الملفات تلقائياً إلى Dropbox.\n\n" +
                        "💡 هذه العملية مطلوبة مرة واحدة فقط!",
                        "Dropbox - تم الإعداد",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dropbox", "Error", $"❌ فشل إعداد Dropbox: {ex.Message}");
                ShowDropboxSetupInstructions();
                return false;
            }
        }

        /// <summary>
        /// التحقق من إعداد Dropbox
        /// </summary>
        public static bool IsDropboxConfigured()
        {
            return !string.IsNullOrEmpty(LoadRefreshToken());
        }

        #endregion

        #region 🔐 المصادقة

        private static async Task<bool> StartDropboxAuthentication()
        {
            try
            {
                string redirectUri = "http://localhost";
                string authUrl = $"https://www.dropbox.com/oauth2/authorize?" +
                    $"client_id={DROPBOX_APP_KEY}&" +
                    $"response_type=code&" +
                    $"token_access_type=offline&" +
                    $"redirect_uri={redirectUri}";

                // ✅ فتح المتصفح
                var browser = new System.Diagnostics.Process();
                browser.StartInfo.UseShellExecute = true;
                browser.StartInfo.FileName = authUrl;
                browser.Start();

                // ✅ طلب رمز المصادقة
                string authCode = await GetAuthCodeFromUser();

                if (string.IsNullOrEmpty(authCode))
                {
                    LogService.WriteLog("Dropbox", "Error", "❌ لم يتم إدخال رمز المصادقة");
                    return false;
                }

                // ✅ تبادل رمز المصادقة
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
                            string refreshToken = refreshElement.GetString();
                            if (!string.IsNullOrEmpty(refreshToken))
                            {
                                SaveRefreshToken(refreshToken);
                                LogService.WriteLog("Dropbox", "Success", "✅ تم حفظ Refresh Token");
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogService.WriteLog("Dropbox", "Error", $"❌ فشل المصادقة: {ex.Message}");
                return false;
            }
        }

        private static string LoadRefreshToken()
        {
            try
            {
                string tokenFilePath = Path.Combine(TokenFolderPath, "refresh_token.txt");
                if (File.Exists(tokenFilePath))
                    return File.ReadAllText(tokenFilePath).Trim();
            }
            catch { }
            return null;
        }

        private static void SaveRefreshToken(string token)
        {
            try
            {
                string tokenFilePath = Path.Combine(TokenFolderPath, "refresh_token.txt");
                File.WriteAllText(tokenFilePath, token);
            }
            catch { }
        }

        private static async Task<string> GetAuthCodeFromUser()
        {
            return await Task.Run(() =>
            {
                using (var inputForm = new Form())
                {
                    inputForm.Text = "🔑 إدخال رمز المصادقة - Dropbox";
                    inputForm.Size = new System.Drawing.Size(550, 200);
                    inputForm.StartPosition = FormStartPosition.CenterScreen;
                    inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputForm.MaximizeBox = false;
                    inputForm.MinimizeBox = false;
                    inputForm.BackColor = System.Drawing.Color.FromArgb(224, 247, 250);
                    inputForm.RightToLeft = RightToLeft.Yes;

                    var label = new Label()
                    {
                        Text = "📋 بعد تسجيل الدخول إلى Dropbox، انسخ رمز المصادقة من شريط العنوان:\n" +
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

        #endregion

        #region 🛠️ دوال مساعدة

        private static void ShowDropboxSetupInstructions()
        {
            string message =
                "🔑 إعداد Dropbox:\n\n" +
                "📌 الخطوات:\n\n" +
                "1️⃣ اذهب إلى: https://www.dropbox.com/developers/apps\n" +
                "2️⃣ Create app → Scoped access → App folder\n" +
                "3️⃣ انسخ App Key و App Secret\n" +
                "4️⃣ أضفهما في الكود (DROPBOX_APP_KEY و DROPBOX_APP_SECRET)\n\n" +
                "💡 هذا الإجراء مطلوب مرة واحدة فقط!";

            MessageBox.Show(message, "Dropbox - الإعداد",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // ✅ فتح Dropbox Developers في المتصفح
            System.Diagnostics.Process.Start("https://www.dropbox.com/developers/apps");
        }

        #endregion
    }
}