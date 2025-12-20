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

            BuildUtils.ShowProcessBar("Build Addressable", "Get settings...", 0f);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Log.Error("[BuildPipeline_BuildAddressables] AddressableAssetSettings not found, skip Addressables process");
                return;
            }

            if (!string.IsNullOrEmpty(buildConfig.AddressablesActiveProfile))
            {
                BuildUtils.ShowProcessBar("Build Addressable", "Set active profile...", 0.2f);

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
                BuildUtils.ShowProcessBar("Build Addressable", "Clean old content...", 0.3f);

                AddressableAssetSettings.CleanPlayerContent();
                BuildCache.PurgeCache(false);
            }

            AssetDatabase.SaveAssets();

            BuildUtils.ShowProcessBar("Build Addressable", "Clean old content...", 0.5f);
            AddressableAssetSettings.BuildPlayerContent();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildUtils.ShowProcessBar("Build Addressable", "Done!", 1f);
        }
    }
}