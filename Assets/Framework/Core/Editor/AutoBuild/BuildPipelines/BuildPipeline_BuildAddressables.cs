using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace XuchFramework.Editor
{
    public static class BuildPipeline_BuildAddressables
    {
        public static void Run(BuildConfig buildConfig)
        {
            if (!buildConfig.BuildAddressables)
            {
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[BuildPipline_BuildAddressables] Addressable Asset Settings 未找到，跳过 Addressables 构建");
                return;
            }

            // 设置 Addressables Active Profile
            if (!string.IsNullOrEmpty(buildConfig.AddressablesActiveProfile))
            {
                var profileId = settings.profileSettings.GetProfileId(buildConfig.AddressablesActiveProfile);
                if (!string.IsNullOrEmpty(profileId))
                {
                    settings.activeProfileId = profileId;
                    EditorUtility.SetDirty(settings);
                }
                else
                {
                    Debug.LogWarning(
                        $"[BuildPipline_BuildAddressables] 未找到 Addressables Profile: {buildConfig.AddressablesActiveProfile}，使用当前 Profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}");
                }
            }

            // Clean Build
            if (buildConfig.AddressablesCleanBuild)
            {
                AddressableAssetSettings.CleanPlayerContent();
                BuildCache.PurgeCache(false);
            }

            AssetDatabase.SaveAssets();

            AddressableAssetSettings.BuildPlayerContent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}