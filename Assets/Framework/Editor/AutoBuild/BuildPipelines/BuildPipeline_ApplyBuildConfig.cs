using UnityEditor;
using UnityEditor.Build;

namespace Xuch.Editor
{
    public static class BuildPipeline_ApplyBuildConfig
    {
        public static void Run(BuildConfig buildConfig)
        {
            BuildUtils.ShowProcessBar("应用构建配置", "正在应用构建配置...", 0.1f);

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildConfig.BuildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            // 如果平台不一致，切换平台
            if (EditorUserBuildSettings.activeBuildTarget != buildConfig.BuildTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildConfig.BuildTarget);
            }

            // 设置宏定义
            if (string.IsNullOrEmpty(buildConfig.MacroDefinitions))
            {
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, buildConfig.MacroDefinitions);
            }

            // 设置 App Identifier
            if (!string.IsNullOrEmpty(buildConfig.AppIdentifier))
            {
                PlayerSettings.SetApplicationIdentifier(namedBuildTarget, buildConfig.AppIdentifier);
            }

            // 设置公司名称、产品名称
            if (!string.IsNullOrEmpty(buildConfig.CompanyName))
            {
                PlayerSettings.companyName = buildConfig.CompanyName;
            }

            if (!string.IsNullOrEmpty(buildConfig.ProductName))
            {
                PlayerSettings.productName = buildConfig.ProductName;
            }

            // 设置版本号
            if (!string.IsNullOrEmpty(buildConfig.AppVersion))
            {
                PlayerSettings.bundleVersion = buildConfig.AppVersion;
            }

            // 设置 BundleCode
            if (buildConfig.BundleVersionCode > 0)
            {
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
                        // 其他平台不需要设置 BundleCode
                        break;
                }
            }

            BuildUtils.ShowProcessBar("应用构建配置", "写入资源脏数据...", 0.5f);

            AssetDatabase.SaveAssets();

            BuildUtils.ShowProcessBar("应用构建配置", "完成！", 1f);
        }
    }
}