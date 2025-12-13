using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XuchFramework.Core.Utils
{
    public static class FileHelper
    {
        public static long GetFileSize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }

            return new FileInfo(path).Length;
        }

        public static string ReadAllTextSafe(string path)
        {
            return ReadAllTextSafe(path, Encoding.UTF8);
        }

        public static string ReadAllTextSafe(string path, Encoding encoding)
        {
            try
            {
                ValidateFile(path);

                string content;
                // Android 平台需要使用 WebRequest 读取
                if (Application.platform == RuntimePlatform.Android)
                {
                    var result = WebRequestHelper.WebGetBufferAsync(path).GetAwaiter().GetResult();
                    content = result.DownloadBuffer.Text;
                }
                else
                {
                    content = File.ReadAllText(path, encoding);
                }

                return content;
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to read file '{path}': {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static async UniTask<string> ReadAllTextSafeAsync(string path, CancellationToken cancellationToken = default)
        {
            return await ReadAllTextSafeAsync(path, Encoding.UTF8, cancellationToken);
        }

        public static async UniTask<string> ReadAllTextSafeAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateFile(path);

                string content;
                // Android 平台需要使用 WebRequest 读取
                if (Application.platform == RuntimePlatform.Android)
                {
                    var result = await WebRequestHelper.WebGetBufferAsync(path);
                    if (result.Status == WebRequestStatus.Success)
                    {
                        content = result.DownloadBuffer.Text;
                    }
                    else
                    {
                        Log.Error($"[FileHelper] Failed to read file on Android by web request: {result.Error}");
                        return null;
                    }
                }
                else
                {
                    content = await File.ReadAllTextAsync(path, encoding, cancellationToken).AsUniTask();
                }

                return content;
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to read file '{path}': {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static byte[] ReadAllBytesSafe(string path)
        {
            try
            {
                ValidateFile(path);
                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to ReadAllBytesSafe for {path}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static async UniTask<byte[]> ReadAllBytesSafeAsync(string path)
        {
            try
            {
                ValidateFile(path);
                return await File.ReadAllBytesAsync(path).AsUniTask();
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to ReadAllBytesSafeAsync for {path}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static void WriteAllTextSafe(string path, string content)
        {
            WriteAllTextSafe(path, content, Encoding.UTF8);
        }

        public static void WriteAllTextSafe(string path, string content, Encoding encoding)
        {
            try
            {
                CreateDirectoryForFileIfNotExist(path);
                File.WriteAllText(path, content, encoding);
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to write file on {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async UniTask WriteAllTextAsync(string path, string content)
        {
            await WriteAllTextAsync(path, content, Encoding.UTF8);
        }

        public static async UniTask WriteAllTextAsync(string path, string content, Encoding encoding)
        {
            try
            {
                CreateDirectoryForFileIfNotExist(path);
                byte[] bytes = encoding.GetBytes(content);
                await File.WriteAllBytesAsync(path, bytes).AsUniTask();
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to write file on {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void WriteAllBytesSafe(string path, byte[] bytes)
        {
            try
            {
                CreateDirectoryForFileIfNotExist(path);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to WriteAllBytesSafe for {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async UniTask WriteAllBytesSafeAsync(string path, byte[] bytes)
        {
            try
            {
                CreateDirectoryForFileIfNotExist(path);
                await File.WriteAllBytesAsync(path, bytes).AsUniTask();
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to WriteAllBytesSafeAsync for {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 如果文件所在的目录不存在，则创建目录
        /// </summary>
        /// <param name="path">文件路径</param>
        public static void CreateDirectoryForFileIfNotExist(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "[FileHelper] File path cannot be null or empty.");

            string directory = Path.GetDirectoryName(path);
            CreateDirectoryIfNotExist(directory);
        }

        /// <summary>
        /// 如果目录不存在，则创建目录
        /// </summary>
        /// <param name="directory">目录路径</param>
        public static void CreateDirectoryIfNotExist(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory), "[FileHelper] File path cannot be null or empty.");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void ValidateFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path), "[FileHelper] File path is null or empty.");
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException(nameof(path), $"[FileHelper] File '{path}' not found.");
            }
        }
    }
}