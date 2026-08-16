using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartBackupSuite.Helpers
{
    public static class LicenseHelper
    {
        #region 🔑 الثوابت

        private static readonly string SecretKey = "SmartBackup2024SecureKey!@#$";
        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "license.dat"
        );

        private static readonly string[] DeveloperKeys = new[]
        {
            "DEVEL-OPER-KEY20-24FUL-LVERS",
            "DEVELOPERKEY2024FULLVERS",
            "DEVKEY2024FULLACCESS",
            "SMARTBACKUPDEV2024"
        };

        // ✅ تخزين المعرف في ملف ثابت لمنع تغيره
        private static string _cachedMachineId = null;
        private static readonly string MachineIdCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartBackupSuite",
            "machine.id"
        );

        #endregion

        #region 📋 هيكل بيانات الترخيص

        public class LicenseData
        {
            public string LicenseKey { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string MachineId { get; set; } = "";
            public string LicenseType { get; set; } = "Trial";
            public DateTime ActivationDate { get; set; } = DateTime.Now;
            public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);
            public string BoundMachineId { get; set; } = "";
            public string Signature { get; set; } = "";
            public bool IsActive { get; set; } = true;
            public bool IsDeveloper { get; set; } = false;
        }

        #endregion

        #region 🖥️ الحصول على معرف ثابت للجهاز (محسن)

        /// <summary>
        /// الحصول على معرف فريد وثابت للجهاز
        /// يعتمد على مكونات ثابتة ولا يتغير مع إعادة التشغيل
        /// </summary>
        public static string GetMachineId()
        {
            // ✅ 1. التحقق من التخزين المؤقت
            if (!string.IsNullOrEmpty(_cachedMachineId))
            {
                return _cachedMachineId;
            }

            // ✅ 2. محاولة قراءة المعرف من ملف مخزن
            try
            {
                if (File.Exists(MachineIdCachePath))
                {
                    string storedId = File.ReadAllText(MachineIdCachePath).Trim();
                    if (!string.IsNullOrEmpty(storedId) && storedId.Length >= 8)
                    {
                        _cachedMachineId = storedId;
                        return _cachedMachineId;
                    }
                }
            }
            catch { }

            // ✅ 3. إنشاء معرف جديد من مكونات ثابتة
            string newMachineId = GenerateStableMachineId();

            // ✅ 4. حفظ المعرف في ملف لضمان الثبات
            try
            {
                string directory = Path.GetDirectoryName(MachineIdCachePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(MachineIdCachePath, newMachineId);
            }
            catch { }

            _cachedMachineId = newMachineId;
            return _cachedMachineId;
        }

        /// <summary>
        /// إنشاء معرف ثابت من مكونات الجهاز
        /// </summary>
        private static string GenerateStableMachineId()
        {
            List<string> idParts = new List<string>();

            // ✅ 1. Serial Number من القرص C: (الأكثر ثباتاً)
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_LogicalDisk WHERE DeviceID='C:'"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(serial) && serial.Length >= 4)
                        {
                            idParts.Add(serial.Trim());
                            break;
                        }
                    }
                }
            }
            catch { }

            // ✅ 2. Processor ID (ثابت)
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string processorId = obj["ProcessorId"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(processorId) && processorId.Length >= 4)
                        {
                            idParts.Add(processorId.Trim());
                            break;
                        }
                    }
                }
            }
            catch { }

            // ✅ 3. Motherboard Serial Number (ثابت)
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(serial) && serial.Length >= 4)
                        {
                            idParts.Add(serial.Trim());
                            break;
                        }
                    }
                }
            }
            catch { }

            // ✅ 4. Volume Serial Number (ثابت)
            try
            {
                string volumeSerial = GetVolumeSerialNumber();
                if (!string.IsNullOrEmpty(volumeSerial))
                {
                    idParts.Add(volumeSerial);
                }
            }
            catch { }

            // ✅ 5. إذا توفرت معلومات كافية
            if (idParts.Count >= 2)
            {
                string combined = string.Join("|", idParts);
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    string machineId = BitConverter.ToString(hash).Replace("-", "").Substring(0, 12);
                    return machineId.ToUpper();
                }
            }

            // ✅ 6. استخدام اسم الجهاز + معرف المستخدم كحل احتياطي
            string fallback = $"{Environment.MachineName}|{Environment.UserName}|{Environment.UserDomainName}";
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                string machineId = BitConverter.ToString(hash).Replace("-", "").Substring(0, 12);
                return machineId.ToUpper();
            }
        }

        /// <summary>
        /// الحصول على Volume Serial Number
        /// </summary>
        private static string GetVolumeSerialNumber()
        {
            try
            {
                string drive = "C:\\";
                if (!Directory.Exists(drive))
                {
                    drive = Path.GetPathRoot(Environment.SystemDirectory);
                }

                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT SerialNumber FROM Win32_LogicalDisk WHERE DeviceID='{drive.Replace("\\", "")}'"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(serial))
                        {
                            return serial;
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// إعادة تعيين المعرف المخزن (للتجربة فقط)
        /// </summary>
        public static void ResetCachedMachineId()
        {
            _cachedMachineId = null;
            try
            {
                if (File.Exists(MachineIdCachePath))
                    File.Delete(MachineIdCachePath);
            }
            catch { }
        }

        #endregion

        // ... باقي الكود (GenerateLicenseKey, ValidateLicenseKey, SaveLicenseToFile, etc.)
        // يبقى كما هو دون تغيير

        #region 🔑 التحقق من مفتاح المطور

        public static bool IsDeveloperKey(string licenseKey)
        {
            if (string.IsNullOrEmpty(licenseKey)) return false;
            string cleanKey = licenseKey.Replace("-", "").ToUpper();

            foreach (string devKey in DeveloperKeys)
            {
                string cleanDevKey = devKey.Replace("-", "").ToUpper();
                if (cleanKey == cleanDevKey) return true;
            }
            return false;
        }

        public static LicenseData GetDeveloperLicenseData()
        {
            return new LicenseData
            {
                LicenseKey = DeveloperKeys[0].Replace("-", ""),
                CustomerName = "المطور",
                MachineId = "Developer",
                LicenseType = "Lifetime",
                ActivationDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(100),
                BoundMachineId = GetMachineId(),
                IsActive = true,
                IsDeveloper = true,
                Signature = "DEV_KEY_SIGNATURE"
            };
        }

        #endregion

        #region 🛠️ إنشاء مفتاح الترخيص

        public static string GenerateLicenseKey(string customerName, string machineId, string licenseType, int durationDays = 30)
        {
            try
            {
                string guid = Guid.NewGuid().ToString().ToUpper().Replace("-", "").Replace("{", "").Replace("}", "");

                if (guid.Length < 25)
                {
                    guid = guid.PadRight(25, 'X');
                }
                else if (guid.Length > 25)
                {
                    guid = guid.Substring(0, 25);
                }

                DateTime expiryDate = licenseType switch
                {
                    "Trial" => DateTime.Now.AddDays(durationDays),
                    "Monthly" => DateTime.Now.AddMonths(1),
                    "Yearly" => DateTime.Now.AddYears(1),
                    "Lifetime" => DateTime.Now.AddYears(100),
                    _ => DateTime.Now.AddDays(30)
                };

                var licenseData = new LicenseData
                {
                    LicenseKey = guid,
                    CustomerName = customerName,
                    MachineId = machineId,
                    LicenseType = licenseType,
                    ActivationDate = DateTime.Now,
                    ExpiryDate = expiryDate,
                    BoundMachineId = string.IsNullOrEmpty(machineId) ? "" : machineId,
                    IsActive = true,
                    IsDeveloper = false
                };

                string dataToSign = $"{licenseData.LicenseKey}|{licenseData.CustomerName}|{licenseData.MachineId}|{licenseData.ExpiryDate:yyyy-MM-dd}";
                licenseData.Signature = GenerateSignature(dataToSign);

                SaveLicenseForAdmin(licenseData);

                return FormatLicenseKey(guid);
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل إنشاء مفتاح الترخيص: {ex.Message}");
            }
        }

        private static string FormatLicenseKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            string cleanKey = key.Replace("-", "").Replace(" ", "").ToUpper();

            if (cleanKey.Length < 25)
            {
                cleanKey = cleanKey.PadRight(25, 'X');
            }

            if (cleanKey.Length > 25)
            {
                cleanKey = cleanKey.Substring(0, 25);
            }

            string formatted = "";
            for (int i = 0; i < cleanKey.Length; i += 5)
            {
                if (i > 0) formatted += "-";
                formatted += cleanKey.Substring(i, Math.Min(5, cleanKey.Length - i));
            }

            return formatted;
        }

        private static string GenerateSignature(string data)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash).Substring(0, 10);
            }
        }

        #endregion

        #region ✅ التحقق من صحة الترخيص

        public static bool ValidateLicenseKey(string licenseKey)
        {
            try
            {
                if (string.IsNullOrEmpty(licenseKey)) return false;
                if (IsDeveloperKey(licenseKey)) return true;

                string cleanKey = licenseKey.Replace("-", "").ToUpper();
                if (cleanKey.Length != 25) return false;

                var savedLicense = LoadLicenseFromFile();
                if (savedLicense == null) return false;
                if (savedLicense.LicenseKey != cleanKey) return false;

                string dataToVerify = $"{savedLicense.LicenseKey}|{savedLicense.CustomerName}|{savedLicense.MachineId}|{savedLicense.ExpiryDate:yyyy-MM-dd}";
                string expectedSignature = GenerateSignature(dataToVerify);
                if (savedLicense.Signature != expectedSignature) return false;

                if (DateTime.Now > savedLicense.ExpiryDate) return false;
                if (!savedLicense.IsActive) return false;

                if (!string.IsNullOrEmpty(savedLicense.BoundMachineId) && !savedLicense.IsDeveloper)
                {
                    string currentMachineId = GetMachineId();
                    if (savedLicense.BoundMachineId != currentMachineId) return false;
                }

                return true;
            }
            catch { return false; }
        }

        public static LicenseValidationResult ValidateLicenseWithDetails(string licenseKey)
        {
            var result = new LicenseValidationResult();

            try
            {
                if (string.IsNullOrEmpty(licenseKey))
                {
                    result.IsValid = false;
                    result.Message = "الرجاء إدخال مفتاح التفعيل";
                    return result;
                }

                if (IsDeveloperKey(licenseKey))
                {
                    result.IsValid = true;
                    result.Message = "✅ مفتاح المطور - ترخيص دائم";
                    result.IsExpired = false;
                    result.DaysRemaining = 36500;
                    result.ExpiryDate = DateTime.Now.AddYears(100);
                    result.LicenseData = GetDeveloperLicenseData();
                    result.IsDeveloper = true;
                    return result;
                }

                string cleanKey = licenseKey.Replace("-", "").ToUpper();
                if (cleanKey.Length != 25)
                {
                    result.IsValid = false;
                    result.Message = "مفتاح التفعيل يجب أن يكون 25 حرفاً";
                    return result;
                }

                var savedLicense = LoadLicenseFromFile();

                if (savedLicense == null)
                {
                    var newLicense = new LicenseData
                    {
                        LicenseKey = cleanKey,
                        CustomerName = "مستخدم جديد",
                        MachineId = "",
                        LicenseType = "Lifetime",
                        ActivationDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddYears(100),
                        BoundMachineId = "",
                        IsActive = true,
                        IsDeveloper = false,
                        Signature = GenerateSignature($"{cleanKey}|مستخدم جديد||{DateTime.Now.AddYears(100):yyyy-MM-dd}")
                    };

                    result.IsValid = true;
                    result.Message = "✅ تم التفعيل بنجاح!";
                    result.LicenseData = newLicense;
                    result.IsExpired = false;
                    result.DaysRemaining = 36500;
                    result.ExpiryDate = DateTime.Now.AddYears(100);
                    return result;
                }

                if (savedLicense.LicenseKey != cleanKey)
                {
                    result.IsValid = false;
                    result.Message = "المفتاح غير متطابق مع الترخيص المحفوظ";
                    return result;
                }

                string dataToVerify = $"{savedLicense.LicenseKey}|{savedLicense.CustomerName}|{savedLicense.MachineId}|{savedLicense.ExpiryDate:yyyy-MM-dd}";
                string expectedSignature = GenerateSignature(dataToVerify);
                if (savedLicense.Signature != expectedSignature)
                {
                    result.IsValid = false;
                    result.Message = "توقيع الترخيص غير صحيح";
                    return result;
                }

                result.ExpiryDate = savedLicense.ExpiryDate;
                result.DaysRemaining = (int)(savedLicense.ExpiryDate - DateTime.Now).TotalDays;

                if (DateTime.Now > savedLicense.ExpiryDate)
                {
                    result.IsValid = false;
                    result.Message = $"انتهت صلاحية الترخيص في {savedLicense.ExpiryDate:yyyy-MM-dd}";
                    result.IsExpired = true;
                    return result;
                }

                if (!savedLicense.IsActive)
                {
                    result.IsValid = false;
                    result.Message = "الترخيص غير نشط";
                    return result;
                }

                if (!string.IsNullOrEmpty(savedLicense.BoundMachineId) && !savedLicense.IsDeveloper)
                {
                    string currentMachineId = GetMachineId();
                    if (savedLicense.BoundMachineId != currentMachineId)
                    {
                        result.IsValid = false;
                        result.Message = "هذا المفتاح مقيد بجهاز آخر";
                        return result;
                    }
                }

                result.IsValid = true;
                result.Message = "✅ الترخيص صالح";
                result.LicenseData = savedLicense;
                result.IsExpired = false;
                result.IsDeveloper = false;

                if (result.DaysRemaining <= 7)
                {
                    result.Message += $" (تنتهي خلال {result.DaysRemaining} يوم)";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"خطأ في التحقق: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region 💾 حفظ وقراءة الترخيص

        public static void SaveLicenseForAdmin(LicenseData license)
        {
            try
            {
                string adminPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Licenses",
                    $"{license.LicenseKey}.json"
                );

                string directory = Path.GetDirectoryName(adminPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(adminPath, json);
            }
            catch { }
        }

        public static void SaveLicenseToFile(LicenseData license)
        {
            try
            {
                string directory = Path.GetDirectoryName(LicenseFilePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                if (string.IsNullOrEmpty(license.BoundMachineId))
                {
                    license.BoundMachineId = GetMachineId();
                }

                string json = JsonSerializer.Serialize(license);
                string encryptedJson = EncryptString(json);
                File.WriteAllText(LicenseFilePath, encryptedJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل حفظ الترخيص: {ex.Message}");
            }
        }

        public static LicenseData LoadLicenseFromFile()
        {
            try
            {
                if (!File.Exists(LicenseFilePath)) return null;

                string encryptedJson = File.ReadAllText(LicenseFilePath);
                string json = DecryptString(encryptedJson);
                return JsonSerializer.Deserialize<LicenseData>(json);
            }
            catch { return null; }
        }

        public static void DeleteLicenseFile()
        {
            try { if (File.Exists(LicenseFilePath)) File.Delete(LicenseFilePath); }
            catch { }
        }

        #endregion

        #region 🔐 التشفير وفك التشفير

        private static string EncryptString(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] salt = new byte[] { 0x73, 0x61, 0x6c, 0x74 };
                using (var password = new Rfc2898DeriveBytes(SecretKey, salt, 10000))
                {
                    aes.Key = password.GetBytes(32);
                    aes.IV = password.GetBytes(16);
                }

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static string DecryptString(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] salt = new byte[] { 0x73, 0x61, 0x6c, 0x74 };
                using (var password = new Rfc2898DeriveBytes(SecretKey, salt, 10000))
                {
                    aes.Key = password.GetBytes(32);
                    aes.IV = password.GetBytes(16);
                }

                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (var ms = new MemoryStream(cipherBytes))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #endregion
    }

    #region 📋 نتيجة التحقق من الترخيص

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = "";
        public bool IsExpired { get; set; }
        public int DaysRemaining { get; set; }
        public DateTime ExpiryDate { get; set; }
        public LicenseHelper.LicenseData LicenseData { get; set; }
        public bool IsDeveloper { get; set; } = false;
    }

    #endregion
}