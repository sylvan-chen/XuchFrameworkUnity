using System;
using System.Security.Cryptography;
using System.Text;

namespace Xuch.Framework.Utils
{
    public static class EncryptionHelper
    {
        private static readonly byte[] _encrptionKey = Encoding.UTF8.GetBytes("DigiEden@202304*QWERTYU-Mnbvcxz#");

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="text">待加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var textBytes = Encoding.UTF8.GetBytes(text);
            RijndaelManaged rm = new()
            {
                Key = _encrptionKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            using (var cryptoTransform = rm.CreateEncryptor())
            {
                var resultBytes = cryptoTransform.TransformFinalBlock(textBytes, 0, textBytes.Length);
                return Convert.ToBase64String(resultBytes, 0, resultBytes.Length);
            }
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="text">待解密的字符串</param>
        /// <returns>解密结果</returns>
        public static byte[] Decrypt(string text)
        {
            var textBytes = Convert.FromBase64String(text);
            RijndaelManaged rm = new()
            {
                Key = _encrptionKey,
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