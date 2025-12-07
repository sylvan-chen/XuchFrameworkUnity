using System.Diagnostics;
using UnityEngine;

namespace DigiEden.Framework.Utils
{
    /// <summary>
    /// 日志工具类
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 打印 Verbose 级别日志
        /// </summary>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_LOG_DEBUG")]
        public static void Verbose(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// 打印 Debug 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_LOG_DEBUG")]
        public static void Debug(string message)
        {
            UnityEngine.Debug.Log($"<color=#2ecc71>{message}</color>");
        }

        /// <summary>
        /// 打印 Info 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Info(string message)
        {
            UnityEngine.Debug.Log($"<color=#3498db>{message}</color>");
        }

        /// <summary>
        /// 打印 Warning 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning($"<color=#f1c40f>{message}</color>");
        }

        /// <summary>
        /// 打印 Error 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"<color=#e74c3c>{message}</color>");
        }

        /// <summary>
        /// 打印 Fatal 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Fatal(string message)
        {
            UnityEngine.Debug.LogError($"<color=#e74c3c><b>[FATAL] </b>{message}</color>");
        }
    }
}