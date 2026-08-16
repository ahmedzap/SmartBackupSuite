using SmartBackupSuite.Forms;
using SmartBackupSuite.Helpers;
using SmartBackupSuite.Services;
using System;
using System.IO;
using System.Windows.Forms;

namespace SmartBackupSuite
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ✅ إنشاء المجلدات المطلوبة
            CreateRequiredDirectories();

            try
            {
                // ✅ التحقق من الترخيص
                var savedLicense = LicenseHelper.LoadLicenseFromFile();

                if (savedLicense == null || !LicenseHelper.ValidateLicenseKey(savedLicense.LicenseKey))
                {
                    using (var activationForm = new ActivationForm())
                    {
                        if (activationForm.ShowDialog() != DialogResult.OK)
                        {
                            return;
                        }
                    }
                }

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في بدء التشغيل:\n{ex.Message}",
                    "خطأ فادح", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CreateRequiredDirectories()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartBackupSuite"
                );

                string[] directories = {
                    appDataPath,
                    Path.Combine(appDataPath, "Logs"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempBackups"),
                    Path.Combine(appDataPath, "GoogleDriveTokens"),
                    Path.Combine(appDataPath, "DropboxTokens"),
                    Path.Combine(appDataPath, "OneDriveTokens")
                };

                foreach (string dir in directories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
            catch { }
        }
    }
}