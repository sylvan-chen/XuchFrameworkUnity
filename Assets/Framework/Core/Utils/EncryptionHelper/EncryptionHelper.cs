using System;
using System.Security.Cryptography;
using System.Text;

namespace XuchFramework.Core.Utils
{
    public static class EncryptionHelper
    {
        private static readonly byte[] _encryptionKey = Encoding.UTF8.GetBytes("XuchFramework@202512*QWERTYU-Mnbvcxz#");

        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var textBytes = Encoding.UTF8.GetBytes(text);
            RijndaelManaged rm = new()
            {
                Key = _encryptionKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            using (var cryptoTransform = rm.CreateEncryptor())
            {
                var resultBytes = cryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);
                return Convert.ToBase64String(resultBytes, 0, resultBytes.Length);
            }
        }

        public static byte[] Decrypt(string text)
        {
            var textBytes = Convert.FromBase64String(text);
            RijndaelManaged rm = new()
            {
                Key = _encryptionKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            using (var cryptoTransform = rm.CreateDecryptor())
            {
                return cryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);
            }
        }
    }
}