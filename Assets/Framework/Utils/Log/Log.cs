using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ZLogger;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// using ZLogger;
// using ZLogger.Unity;
// using Microsoft.Extensions.Logging;
// using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Framework.Core
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
#if UNITY_EDITOR
            // Logger.ZLogDebug($"<color=#2ecc71>{message}</color>");
#else
            Logger.ZLogDebug(message);
#endif
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Info(string message)
        {
#if UNITY_EDITOR
            // Logger.ZLogInformation($"<color=#3498db>{message}</color>");
#else
            Logger.ZLogInformation(message);
#endif
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Warning(string message)
        {
#if UNITY_EDITOR
            // Logger.ZLogWarning($"<color=#f1c40f>{message}</color>");
#else
            Logger.ZLogWarning(message);
#endif
        }

        [HideInCallstack]
        [Conditional("ENABLE_LOG")]
        public static void Error(string message)
        {
#if UNITY_EDITOR
            // Logger.ZLogError($"<color=#e74c3c>{message}</color>");
#else
            Logger.ZLogError(message);
#endif
        }

        // private static ILoggerFactory _loggerFactory;
        //
        // public static ILogger Logger { get; private set; }
        //
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // public static void Initialize()
        // {
        //     try
        //     {
        //         string logDirectory = Path.Combine(Application.persistentDataPath, "UnityLogs");
        //         if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
        //
        //         // 文件名包含日期，方便区分
        //         var date = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
        //         string fileName = $"log_{date}.log";
        //         string filePath = Path.Combine(logDirectory, fileName);
        //
        //         // 2. 配置 LoggerFactory
        //         _loggerFactory = LoggerFactory.Create(
        //             logging =>
        //             {
        //                 logging.SetMinimumLevel(LogLevel.Trace);
        //                 logging.AddZLoggerUnityDebug();
        //
        //                 // 添加 ZLogger 文件输出
        //                 logging.AddZLoggerFile(
        //                     filePath,
        //                     options =>
        //                     {
        //                         // 格式化：[时间] [级别] 内容
        //                         options.UsePlainTextFormatter(
        //                             (formatter =>
        //                             {
        //                                 formatter.SetPrefixFormatter(
        //                                     $"{0}|{1}|",
        //                                     (in MessageTemplate template, in LogInfo info) =>
        //                                         template.Format(info.Timestamp, info.LogLevel)
        //                                 );
        //                                 formatter.SetSuffixFormatter(
        //                                     $" ({0})",
        //                                     (in MessageTemplate template, in LogInfo info) =>
        //                                         template.Format(info.Category)
        //                                 );
        //                                 formatter.SetExceptionFormatter(
        //                                     (writer, ex) =>
        //                                         Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}")
        //                                 );
        //                             })
        //                         );
        //                     }
        //                 );
        //             }
        //         );
        //
        //         Logger = _loggerFactory.CreateLogger("GlobalLog");
        //
        //         // 3. 注册退出事件，确保缓冲区内容刷入磁盘
        //         Application.quitting += OnQuitting;
        //
        //         Logger.ZLogInformation($"=== 静态日志系统初始化成功 ===");
        //         Logger.ZLogInformation($"日志路径: {0}", filePath);
        //     }
        //     catch (Exception e)
        //     {
        //         UnityEngine.Debug.LogError($"初始化日志系统失败，{e.Message}");
        //     }
        // }
        //
        // private static void OnQuitting()
        // {
        //     Logger.ZLogInformation($"=== 程序准备退出，保存日志 ===");
        //     // 释放工厂会强制执行所有挂起的写入操作
        //     _loggerFactory?.Dispose();
        // }
    }
}