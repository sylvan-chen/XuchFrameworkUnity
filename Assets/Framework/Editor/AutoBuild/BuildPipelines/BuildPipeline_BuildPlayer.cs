using System;
using System.IO;
using System.Linq;
using DigiEden.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    public static class BuildPipeline_BuildPlayer
    {
        public static void Run(BuildConfig buildConfig)
        {
            var scenes = EditorBuildSettings.scenes.Where((scene) => scene.enabled).Select(scene => scene.path).ToArray();

            if (Directory.Exists(buildConfig.OutputDirectory))
            {
                Directory.Delete(buildConfig.OutputDirectory, true);
            }
            Directory.CreateDirectory(buildConfig.OutputDirectory);

            var outputPath = GetBuildOutputPath(buildConfig);

            var buildOptions = GetBuildOptions(buildConfig);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = buildConfig.BuildTarget,
                options = buildOptions
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Log.Info($"[AutoBuilder] BuildPlayer - 构建成功！输出路径: {outputPath}");
                Log.Info($"[AutoBuilder] 构建大小: {report.summary.totalSize / (1024f * 1024f):F2} MB");
            }
            else
            {
                throw new Exception($"[AutoBuilder] BuildPlayer - 构建失败: {report.summary.result}");
            }
        }

        private static string GetBuildOutputPath(BuildConfig buildConfig)
        {
            var outputPath = Path.Combine(buildConfig.OutputDirectory, buildConfig.BuildName);

            switch (buildConfig.BuildTarget)
            {
                case BuildTarget.Android:
                    if (!outputPath.EndsWith(".apk") && !outputPath.EndsWith(".aab"))
                    {
                        outputPath += buildConfig.BuildAppBundle ? ".aab" : ".apk";
                    }
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    if (!outputPath.EndsWith(".exe"))
                    {
                        outputPath += ".exe";
                    }
                    break;
                case BuildTarget.StandaloneOSX:
                    if (!outputPath.EndsWith(".app"))
                    {
                        outputPath += ".app";
                    }
                    break;
                case BuildTarget.iOS:
                    // iOS 构建输出文件夹
                    break;
                case BuildTarget.StandaloneLinux64:
                    // Linux 不需要扩展名
                    break;
            }

            return outputPath;
        }

        private static BuildOptions GetBuildOptions(BuildConfig buildConfig)
        {
            var buildOptions = BuildOptions.None;

            // 开发构建
            if (buildConfig.DevelopmentBuild)
            {
                buildOptions |= BuildOptions.Development;

                if (buildConfig.AutoconnectProfiler)
                {
                    buildOptions |= BuildOptions.ConnectWithProfiler;
                }
                if (buildConfig.DeepProfilingSurpport)
                {
                    buildOptions |= BuildOptions.EnableDeepProfilingSupport;
                }
                if (buildConfig.ScriptDebugging)
                {
                    buildOptions |= BuildOptions.AllowDebugging;
                }
            }

            // 压缩选项
            switch (buildConfig.PlayerCompression)
            {
                case PlayerCompressionType.LZ4:
                    buildOptions |= BuildOptions.CompressWithLz4;
                    break;
                case PlayerCompressionType.LZ4HC:
                    buildOptions |= BuildOptions.CompressWithLz4HC;
                    break;
                default:
                    Debug.LogError($"[BuildPipeline_BuildPlayer] 未知的 PlayerCompressionType: {buildConfig.PlayerCompression}");
                    break;
            }

            return buildOptions;
        }
    }
}