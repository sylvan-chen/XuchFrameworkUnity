using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using XuchFramework.Core;

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
                Log.Error("[BuildPipeline_BuildAddressables] AddressableAssetSettings not found, skip Addressables process");
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
                    Log.Warning(
                        $"[BuildPipeline_BuildAddressables] No active Addressables profile: {buildConfig.AddressablesActiveProfile}, use current profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}");
                }
            }

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