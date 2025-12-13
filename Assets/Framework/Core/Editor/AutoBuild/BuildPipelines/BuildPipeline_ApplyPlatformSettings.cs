using Unity.Android.Types;
using UnityEditor;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_ApplyPlatformSettings
    {
        public static void Run(BuildConfig buildConfig)
        {
            BuildUtils.ShowProcessBar("应用平台设置", "正在应用平台特定设置...", 0.2f);

            if (buildConfig.BuildTarget == BuildTarget.Android)
            {
                // 设置 Android Debug Symbols
                int symbols = 1 << buildConfig.DebugSymbols;
                UnityEditor.Android.UserBuildSettings.DebugSymbols.level = (DebugSymbolLevel)symbols;

                // 设置 Keystore 信息
                PlayerSettings.Android.useCustomKeystore = buildConfig.UseCustomKeystore;
                if (buildConfig.UseCustomKeystore)
                {
                    PlayerSettings.Android.keystoreName = buildConfig.KeystoreName;
                    PlayerSettings.Android.keystorePass = buildConfig.KeystorePass;
                    PlayerSettings.Android.keyaliasName = buildConfig.KeyaliasName;
                    PlayerSettings.Android.keyaliasPass = buildConfig.KeyaliasPass;
                }

                // 混淆设置
                PlayerSettings.Android.minifyRelease = buildConfig.MinifyRelease;
                PlayerSettings.Android.minifyDebug = buildConfig.MinifyDebug;

                // 是否分包
                PlayerSettings.Android.splitApplicationBinary = buildConfig.SplitApplicationBinary;

                // 构建格式：AAB 或 APK
                EditorUserBuildSettings.buildAppBundle = buildConfig.BuildAppBundle;
            }

            BuildUtils.ShowProcessBar("应用平台设置", "写入资源脏数据...", 0.5f);

            AssetDatabase.SaveAssets();

            BuildUtils.ShowProcessBar("应用平台设置", "完成！", 1f);
        }
    }
}