using System;
using UnityEngine;

namespace XuchFramework.Core.Utils
{
    public static class PathHelper
    {
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
        /// 获取 WWW 文件格式的路径（'file://' 前缀）
        /// </summary>
        public static string ConvertToWWWFilePath(string path)
        {
            string prefix;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    prefix = "file:///";
                    break;
                case RuntimePlatform.Android:
                    prefix = "jar:file://";
                    break;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                    prefix = "file://";
                    break;
                default:
                    throw new NotImplementedException();
            }

            return ConvertToWWWPathInternal(path, prefix);
        }

        /// <summary>
        /// 获取 HTTP 格式的路径（'http://' 前缀）
        /// </summary>
        public static string ConvertToHttpPath(string path)
        {
            return ConvertToWWWPathInternal(path, "http://");
        }

        /// <summary>
        /// 获取 HTTPS 格式的路径（'https://' 前缀）
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
                // 去掉重复的斜杠
                return fullPath.Replace(prefix + "/", prefix);
            }
        }
    }
}