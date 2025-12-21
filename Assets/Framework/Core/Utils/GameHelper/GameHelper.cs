using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace XuchFramework.Core.Utils
{
    public static class GameHelper
    {
        #region Type

        /// <summary>
        /// Get type by full name and assembly name
        /// </summary>
        public static Type GetType(string typeFullName, string assemblyName = null)
        {
            if (!string.IsNullOrEmpty(assemblyName))
            {
                var type = Type.GetType($"{typeFullName}, {assemblyName}");
                if (type != null)
                    return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var currentAssemblyName = assembly.GetName().Name;
                if (!string.IsNullOrEmpty(assemblyName) && currentAssemblyName != assemblyName)
                    continue;

                foreach (var t in assembly.GetTypes())
                {
                    if (t.FullName == typeFullName)
                        return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Get all subclass names of the specified base class
        /// </summary>
        public static string[] GetDerivedTypeNames(Type baseType)
        {
            return GetDerivedTypeNamesInternal(baseType, AppDomain.CurrentDomain.GetAssemblies());
        }

        /// <summary>
        /// Get all subclass names of the specified base class from the specified assemblies
        /// </summary>
        public static string[] GetDerivedTypeNames(Type baseType, params string[] assemblies)
        {
            return GetDerivedTypeNamesInternal(baseType, assemblies);
        }

        private static string[] GetDerivedTypeNamesInternal(Type baseType, string[] assemblyNames)
        {
            var assemblies = new List<Assembly>();
            foreach (string assemblyName in assemblyNames)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch (Exception)
                {
                    Log.Error($"[TypeHelper] Failed to load assembly {assemblyName}.");
                    continue;
                }

                if (assembly == null)
                    continue;

                assemblies.Add(assembly);
            }

            return GetDerivedTypeNamesInternal(baseType, assemblies.ToArray());
        }

        private static string[] GetDerivedTypeNamesInternal(Type baseType, Assembly[] assemblies)
        {
            var result = new List<string>();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                foreach (Type t in assembly.GetTypes())
                {
                    if (t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
                    {
                        result.Add(t.FullName);
                    }
                }
            }

            result.Sort();
            return result.ToArray();
        }

        #endregion

        #region Float

        public static bool FloatEquals(float a, float b, float epsilon = 0.001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }

        #endregion

        #region String

        /// <summary>
        /// Convert '-' or '_' separated string to PascalCase
        /// </summary>
        public static string ToPascalCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            str = str.Replace("_", " ").Replace("-", " ");
            var words = str.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return string.Join("", words);
        }

        /// <summary>
        /// Convert string array to string, split by space
        /// </summary>
        public static string ConvertArrayToStr(string[] array)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                if (i != array.Length - 1)
                    sb.Append(' ');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert string (slit by space) to string array
        /// </summary>
        public static string[] ConvertStrToArray(string str)
        {
            return str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion

        #region Format Time

        public static string SecondsToTimeStr(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 1)
                return span.ToString(@"hh\:mm\:ss");
            else if (span.Minutes > 1)
                return span.ToString(@"mm\:ss");
            else
                return span.ToString(@"ss");
        }

        public static string SecondsToTimeStr_hms(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 1)
                return span.ToString(@"hh\hmm\mss\s");
            else if (span.Minutes > 1)
                return span.ToString(@"mm\mss\s");
            else
                return span.ToString(@"ss\s");
        }

        public static string SecondsToTimeStr_HMS(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours > 1)
                return span.ToString(@"hh\Hmm\Mss\S");
            else if (span.Minutes > 1)
                return span.ToString(@"mm\Mss\S");
            else
                return span.ToString(@"ss\S");
        }

        #endregion

        #region Path

        public static string GetRegularPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = path.Replace(@"\\", "/");
            path = path.Replace(@"\", "/");
            path = path.Replace("//", "/");
            return path;
        }

        public static string RemoveExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                return path;
            }
            else
            {
                return path.Substring(0, lastDotIndex);
            }
        }

        public static string AddExtension(string path, string extension)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(extension))
                return path;

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            return path + extension;
        }

        /// <summary>
        /// Convert to path with prefix 'file://'
        /// </summary>
        public static string ConvertToWWWFilePath(string path)
        {
            string prefix = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => "file:///",
                RuntimePlatform.Android => "jar:file://",
                RuntimePlatform.IPhonePlayer or RuntimePlatform.WindowsPlayer or RuntimePlatform.OSXPlayer => "file://",
                _ => throw new NotImplementedException()
            };

            return ConvertToWWWPathInternal(path, prefix);
        }

        /// <summary>
        /// Convert to path with 'http://'
        /// </summary>
        public static string ConvertToHttpPath(string path)
        {
            return ConvertToWWWPathInternal(path, "http://");
        }

        /// <summary>
        /// Convert to path with 'https://'
        /// </summary>
        public static string ConvertToHttpsPath(string path)
        {
            return ConvertToWWWPathInternal(path, "https://");
        }

        private static string ConvertToWWWPathInternal(string path, string prefix)
        {
            string regularPath = GetRegularPath(path);
            if (regularPath == null)
            {
                return null;
            }

            if (regularPath.StartsWith(prefix))
            {
                return regularPath;
            }
            else
            {
                string fullPath = prefix + regularPath;
                // Remove duplicate slashes
                return fullPath.Replace(prefix + "/", prefix);
            }
        }

        #endregion

        #region File

        public static long GetFileSize(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            return new FileInfo(path).Length;
        }

        public static string ReadAllTextSafe(string path)
        {
            return ReadAllTextSafe(path, Encoding.UTF8);
        }

        public static string ReadAllTextSafe(string path, Encoding encoding)
        {
            if (!ValidateFile(path))
                return null;

            try
            {
                string content;
                // Android platform should read by WebRequest
                if (Application.platform == RuntimePlatform.Android)
                {
                    var result = GameHelper.WebGetBufferAsync(path).GetAwaiter().GetResult();
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
            if (!ValidateFile(path))
                return null;

            try
            {
                string content;
                // Android platform should read by WebRequest
                if (Application.platform == RuntimePlatform.Android)
                {
                    var result = await GameHelper.WebGetBufferAsync(path);
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
            if (!ValidateFile(path))
                return null;

            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to read all bytes for {path}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public static async UniTask<byte[]> ReadAllBytesSafeAsync(string path)
        {
            if (!ValidateFile(path))
                return null;

            try
            {
                return await File.ReadAllBytesAsync(path).AsUniTask();
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to read all bytes for {path}: {ex.Message}\n{ex.StackTrace}");
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
                CreateFileDirectoryIfNotExist(path);
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
            if (!CreateFileDirectoryIfNotExist(path))
                return;

            try
            {
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
            if (!CreateFileDirectoryIfNotExist(path))
                return;
            try
            {
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to write all bytes for {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async UniTask WriteAllBytesSafeAsync(string path, byte[] bytes)
        {
            if (!CreateFileDirectoryIfNotExist(path))
                return;

            try
            {
                await File.WriteAllBytesAsync(path, bytes).AsUniTask();
            }
            catch (Exception ex)
            {
                Log.Error($"[FileHelper] Failed to write all bytes for {path}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static bool CreateFileDirectoryIfNotExist(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Log.Error("[FileHelper] File path cannot be null or empty.");
                return false;
            }

            string directory = Path.GetDirectoryName(filePath);
            return CreateDirectoryIfNotExist(directory);
        }

        public static bool CreateDirectoryIfNotExist(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                Log.Error("[FileHelper] Directory cannot be null or empty.");
                return false;
            }

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return true;
        }

        private static bool ValidateFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Log.Error("[FileHelper] File path is null or empty.");
                return false;
            }

            if (!File.Exists(filePath))
            {
                Log.Error($"[FileHelper] File '{filePath}' not found.");
                return false;
            }

            return true;
        }

        #endregion

        #region Encryption

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

        #endregion

        #region Hash

        public static string HashBytesToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static string StringSHA1(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return BytesSHA1(bytes);
        }

        public static string FileSHA1(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return StreamSHA1(fs);
            }
        }

        public static string BytesSHA1(byte[] bytes)
        {
            var sha1 = HashAlgorithm.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }

        public static string StreamSHA1(Stream stream)
        {
            var sha1 = HashAlgorithm.Create();
            byte[] hashBytes = sha1.ComputeHash(stream);
            return HashBytesToString(hashBytes);
        }

        public static string StringMD5(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return BytesMD5(bytes);
        }

        public static string FileMD5(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return StreamMD5(fs);
            }
        }

        public static string BytesMD5(byte[] bytes)
        {
            var md5Provider = new MD5CryptoServiceProvider();
            byte[] hashBytes = md5Provider.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }

        public static string StreamMD5(Stream stream)
        {
            var md5Provider = new MD5CryptoServiceProvider();
            byte[] hashBytes = md5Provider.ComputeHash(stream);
            return HashBytesToString(hashBytes);
        }

        #endregion

        #region Unity

        /// <summary>
        /// Get layer mask that can collide with the specified layer
        /// </summary>
        public static LayerMask GetPhysicsLayerMask(int currentLayer)
        {
            int finalMask = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(currentLayer, i))
                    finalMask = finalMask | (1 << i);
            }
            return finalMask;
        }

        public static async UniTask<WebRequestResult> WebGetBufferAsync(string uri, float timeout = 60f)
        {
            UnityWebRequest www = UnityWebRequest.Get(uri);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.disposeDownloadHandlerOnDispose = true;

            return await WebRequestInternal(www, timeout);
        }

        public static async UniTask<WebRequestResult> WebGetFileAsync(string uri, string savePath, float timeout = 60f)
        {
            UnityWebRequest www = UnityWebRequest.Get(uri);
            www.downloadHandler = new DownloadHandlerFile(savePath) { removeFileOnAbort = true };
            www.disposeDownloadHandlerOnDispose = true;

            return await WebRequestInternal(www, timeout);
        }

        private static async UniTask<WebRequestResult> WebRequestInternal(UnityWebRequest www, float timeout, bool autoDispose = true)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(timeout));
            var (isCanceled, _) = await www.SendWebRequest().WithCancellation(cts.Token).SuppressCancellationThrow();

            WebRequestResult result;
            if (isCanceled)
            {
                result = new WebRequestResult(
                    WebRequestStatus.TimeoutError,
                    $"Request for {www.uri} failed. {WebRequestStatus.TimeoutError}: Time out.",
                    default);
            }
            else if (www.result == UnityWebRequest.Result.Success)
            {
                result = new WebRequestResult(
                    WebRequestStatus.Success,
                    null,
                    new WebDownloadBuffer(www.downloadHandler.data, www.downloadHandler.text));
            }
            else
            {
                var status = www.result switch
                {
                    UnityWebRequest.Result.ConnectionError => WebRequestStatus.ConnectionError,
                    UnityWebRequest.Result.DataProcessingError => WebRequestStatus.DataProcessingError,
                    UnityWebRequest.Result.ProtocolError => WebRequestStatus.ProtocolError,
                    _ => WebRequestStatus.UnknownError,
                };

                result = new WebRequestResult(status, $"Request for {www.uri} failed. {status}: {www.error}", default);
            }

            if (autoDispose)
            {
                www.Dispose();
            }

            return result;
        }

        #endregion
    }
}