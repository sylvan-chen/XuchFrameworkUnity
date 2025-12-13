using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace XuchFramework.Editor
{
    public static class BuildUtils
    {
        public static void ShowProcessBar(string title, string info, float progress)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar(title, info, progress);
#endif
            Debug.Log($"{title}: {info} ({progress * 100f}%)");
        }

        public static void AddToAddressableGroup(string directory, string groupName, string label)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new InvalidOperationException("[LuaBuilder] Addressable Asset Settings not found!");
            }

            var existingGroup = settings.FindGroup(groupName);
            if (existingGroup != null)
            {
                settings.RemoveGroup(existingGroup);
            }

            var group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema));

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema != null)
            {
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            }

            string[] assetGuids = AssetDatabase.FindAssets("", new[] { directory });

            foreach (var guid in assetGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.HasExtension(assetPath))
                {
                    assetPath = Path.GetFileNameWithoutExtension(assetPath);
                }

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = Path.GetFileNameWithoutExtension(assetPath);
                settings.AddLabel(label, false);
                entry.SetLabel(label, true, false, false);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}