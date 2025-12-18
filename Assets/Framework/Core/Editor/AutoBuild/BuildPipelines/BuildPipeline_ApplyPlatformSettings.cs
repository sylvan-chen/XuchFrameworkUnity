using Unity.Android.Types;
using UnityEditor;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_ApplyPlatformSettings
    {
        public static void Run(BuildConfig buildConfig)
        {
            BuildUtils.ShowProcessBar("Apply platform settings", "Applying...", 0.2f);

            if (buildConfig.BuildTarget == BuildTarget.Android)
            {
                // Set Android Debug Symbols
                int symbols = 1 << buildConfig.DebugSymbols;
                UnityEditor.Android.UserBuildSettings.DebugSymbols.level = (DebugSymbolLevel)symbols;

                // Set Keystore
                PlayerSettings.Android.useCustomKeystore = buildConfig.UseCustomKeystore;
                if (buildConfig.UseCustomKeystore)
                {
                    PlayerSettings.Android.keystoreName = buildConfig.KeystoreName;
                    PlayerSettings.Android.keystorePass = buildConfig.KeystorePass;
                    PlayerSettings.Android.keyaliasName = buildConfig.KeyaliasName;
                    PlayerSettings.Android.keyaliasPass = buildConfig.KeyaliasPass;
                }

                // Set minify options
                PlayerSettings.Android.minifyRelease = buildConfig.MinifyRelease;
                PlayerSettings.Android.minifyDebug = buildConfig.MinifyDebug;

                // Should split APK
                PlayerSettings.Android.splitApplicationBinary = buildConfig.SplitApplicationBinary;

                // Should build app bundle
                EditorUserBuildSettings.buildAppBundle = buildConfig.BuildAppBundle;
            }

            BuildUtils.ShowProcessBar("Apply platform settings", "Save assets...", 0.5f);

            AssetDatabase.SaveAssets();

            BuildUtils.ShowProcessBar("Apply platform settings", "Done!", 1f);
        }
    }
}