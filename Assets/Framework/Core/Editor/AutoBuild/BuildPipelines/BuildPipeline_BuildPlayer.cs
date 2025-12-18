using System;
using System.IO;
using System.Linq;
using UnityEditor;
using XuchFramework.Core;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_BuildPlayer
    {
        public static void Run(BuildConfig buildConfig)
        {
            var scenes = EditorBuildSettings.scenes.Where((scene) => scene.enabled).Select(scene => scene.path).ToArray();

            var outputDir = GetBuildOutputDirectory(buildConfig);

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            var buildOptions = GetBuildOptions(buildConfig);
            var outputPath = GetBuildOutputPath(buildConfig);

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
                Log.Debug($"[AutoBuilder] BuildPlayer - Build Succeed! Output path: {outputPath}");
                Log.Debug($"[AutoBuilder] Build size: {report.summary.totalSize / (1024f * 1024f):F2} MB");
            }
            else
            {
                throw new Exception($"[AutoBuilder] BuildPlayer - Build Failed ({report.summary.totalErrors} Errors): {report.SummarizeErrors()}");
            }
        }

        private static string GetBuildOutputDirectory(BuildConfig buildConfig)
        {
            var outputDir = Path.Combine(
                buildConfig.OutputDirectory,
                buildConfig.AppVersion,
                $"{DateTime.Now.Year}_{DateTime.Now.Month:D2}_{DateTime.Now.Day:D2}_{DateTime.Now.Hour:D2}_{DateTime.Now.Minute:D2}_{DateTime.Now.Second:D2}");

            return outputDir;
        }

        private static string GetBuildOutputPath(BuildConfig buildConfig)
        {
            var outputPath = Path.Combine(GetBuildOutputDirectory(buildConfig), buildConfig.BuildName);

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
                case BuildTarget.iOS:               // iOS Output is a directory, no extension needed
                case BuildTarget.StandaloneLinux64: // Linux executable, no extension needed
                default:
                    // No special handling needed
                    break;
            }

            return outputPath;
        }

        private static BuildOptions GetBuildOptions(BuildConfig buildConfig)
        {
            var buildOptions = BuildOptions.None;

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

            switch (buildConfig.PlayerCompression)
            {
                case PlayerCompressionType.LZ4:
                    buildOptions |= BuildOptions.CompressWithLz4;
                    break;
                case PlayerCompressionType.LZ4HC:
                    buildOptions |= BuildOptions.CompressWithLz4HC;
                    break;
                default:
                    Log.Error($"[BuildPipeline_BuildPlayer] Unknow PlayerCompression Type: {buildConfig.PlayerCompression}");
                    break;
            }

            return buildOptions;
        }
    }
}