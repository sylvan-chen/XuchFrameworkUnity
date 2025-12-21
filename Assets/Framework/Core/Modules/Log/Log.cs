using System.Diagnostics;
using UnityEngine;

namespace XuchFramework.Core
{
    public static class Log
    {
        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_LOG_DEBUG")]
        public static void Verbose(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        [Conditional("ENABLE_LOG_DEBUG")]
        public static void Debug(string message)
        {
            UnityEngine.Debug.Log($"<color=#2ecc71>{message}</color>");
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Info(string message)
        {
            UnityEngine.Debug.Log($"<color=#3498db>{message}</color>");
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning($"<color=#f1c40f>{message}</color>");
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"<color=#e74c3c>{message}</color>");
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Fatal(string message)
        {
            UnityEngine.Debug.LogError($"<color=#e74c3c><b>[FATAL] </b>{message}</color>");
        }
    }
}