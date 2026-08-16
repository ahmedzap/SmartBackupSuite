using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmartBackupSuite.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly byte[] Salt = new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Salt, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Salt, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }

        // تشفير الملفات (AES)
        public static void EncryptFile(string inputFile, string outputFile, string password)
        {
            using (Aes aes = Aes.Create())
            {
                using (var passwordBytes = new Rfc2898DeriveBytes(password, new byte[] { 0x73, 0x61, 0x6c, 0x74 }, 10000))
                {
                    aes.Key = passwordBytes.GetBytes(32);
                    aes.IV = passwordBytes.GetBytes(16);
                }

                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open))
                using (FileStream fsOutput = new FileStream(outputFile, FileMode.Create))
                using (CryptoStream cs = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cs.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }

        public static void DecryptFile(string inputFile, string outputFile, string password)
        {
            using (Aes aes = Aes.Create())
            {
                using (var passwordBytes = new Rfc2898DeriveBytes(password, new byte[] { 0x73, 0x61, 0x6c, 0x74 }, 10000))
                {
                    aes.Key = passwordBytes.GetBytes(32);
                    aes.IV = passwordBytes.GetBytes(16);
                }

                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open))
                using (FileStream fsOutput = new FileStream(outputFile, FileMode.Create))
                using (CryptoStream cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    byte[] buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fsOutput.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}