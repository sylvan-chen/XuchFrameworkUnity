using UnityEditor;
using UnityEditor.Build;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_ApplyBuildConfig
    {
        public static void Run(BuildConfig buildConfig)
        {
            BuildUtils.ShowProcessBar("Apply build config", "Applying...", 0.1f);

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildConfig.BuildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            // Switch build target to the one specified in build config
            if (EditorUserBuildSettings.activeBuildTarget != buildConfig.BuildTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildConfig.BuildTarget);
            }

            if (string.IsNullOrEmpty(buildConfig.MacroDefinitions))
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, buildConfig.MacroDefinitions);
            }

            if (!string.IsNullOrEmpty(buildConfig.AppIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(namedBuildTarget, buildConfig.AppIdentifier);
            }

            if (!string.IsNullOrEmpty(buildConfig.CompanyName))
            {
                PlayerSettings.companyName = buildConfig.CompanyName;
            }

            if (!string.IsNullOrEmpty(buildConfig.ProductName))
            {
                PlayerSettings.productName = buildConfig.ProductName;
            }

            if (!string.IsNullOrEmpty(buildConfig.AppVersion))
            {
                PlayerSettings.bundleVersion = buildConfig.AppVersion;
            }

            switch (buildConfig.BuildTarget)
            {
                case BuildTarget.Android:
                    PlayerSettings.Android.bundleVersionCode = buildConfig.BundleVersionCode;
                    break;
                case BuildTarget.iOS:
                    PlayerSettings.iOS.buildNumber = buildConfig.BundleNumber;
                    break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                default:
                    // No platform-specific settings to apply
                    break;
            }

            BuildUtils.ShowProcessBar("Apply build config", "Save assets...", 0.5f);

            AssetDatabase.SaveAssets();

            BuildUtils.ShowProcessBar("Apply build config", "Done!", 1f);
        }
    }
}