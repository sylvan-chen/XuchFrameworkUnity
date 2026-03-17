using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using Framework.Core;

namespace Framework.Editor
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

            // 获取 Settings
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Log.Error(
                    "[BuildPipeline_BuildAddressables] AddressableAssetSettings not found, skip Addressables process"
                );
                return;
            }

            // ===== 刷新 Addressable 资源组 =====

            // 先清空原来的 groups（除了默认组）
            if (settings.groups.Count > 0)
            {
                var clearingGroups = new List<AddressableAssetGroup>();
                foreach (var group in settings.groups)
                {
                    if (!group.IsDefaultGroup() && group.name != "protos")
                    {
                        clearingGroups.Add(group);
                    }
                }

                foreach (var group in clearingGroups)
                {
                    settings.RemoveGroup(group);
                }
            }

            // 再通过 AddressableImporter 重新导入
            var assetsPaths = new string[] { "Assets/Res" };
            // AddressableImporter.FolderImporter.ReimportFolders(assetsPaths, false);

            // ===== 设置激活的 profile =====

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
                        $"[BuildPipeline_BuildAddressables] No active Addressables profile: {buildConfig.AddressablesActiveProfile}, use current profile: {settings.profileSettings.GetProfileName(settings.activeProfileId)}"
                    );
                }
            }

            if (buildConfig.AddressablesCleanBuild)
            {
                BuildUtils.ShowProcessBar("Build Addressable", "Clean old content...", 0.3f);

                AddressableAssetSettings.CleanPlayerContent();
                BuildCache.PurgeCache(false);
            }

            AssetDatabase.SaveAssets();

            BuildUtils.ShowProcessBar("Build Addressable", "Build player content...", 0.5f);
            try
            {
                AddressableAssetSettings.BuildPlayerContent();
                BuildUtils.ShowProcessBar("Build Addressable", "Build addressable finished.", 1f);
            }
            catch (Exception e)
            {
                Log.Error($"[BuildPipeline_BuildAddressables] Build Addressables failed: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}