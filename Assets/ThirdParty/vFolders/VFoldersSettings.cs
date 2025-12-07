#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VFolders
{
    public class VFoldersSettings : ScriptableObject
    {
        private static VFoldersSettings _instance;
        private const string SETTINGS_PATH = "Assets/ThirdParty/VFolders/vFolders Settings.asset";
        private const string SETTINGS_FOLDER = "Assets/ThirdParty/VFolders";

        public static VFoldersSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<VFoldersSettings>(SETTINGS_PATH);

                    if (_instance == null)
                    {
                        _instance = CreateInstance<VFoldersSettings>();

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(SETTINGS_FOLDER))
                        {
                            Directory.CreateDirectory(SETTINGS_FOLDER);
                        }

                        AssetDatabase.CreateAsset(_instance, SETTINGS_PATH);
                        AssetDatabase.SaveAssets();
                    }
                }

                return _instance;
            }
        }

        [Header("Features")]
        public bool navigationBarEnabled = true;
        public bool twoLineNamesEnabled = true;
        public bool autoIconsEnabled = true;
        public bool hierarchyLinesEnabled = true;
        public bool zebraStripingEnabled = true;
        public bool contentMinimapEnabled = true;
        public bool backgroundColorsEnabled = false;
        public bool minimalModeEnabled = false;
        public bool foldersFirstEnabled = false;
        public bool toggleExpandedEnabled = true;
        public bool collapseEverythingElseEnabled = true;
        public bool collapseEverythingEnabled = true;

        [Header("Other")]
        public bool _pluginDisabled = false;

        public static bool NavigationBarEnabled
        {
            get => Instance.navigationBarEnabled;
            set
            {
                Instance.navigationBarEnabled = value;
                SaveSettings();
            }
        }

        public static bool TwoLineNamesEnabled
        {
            get => Instance.twoLineNamesEnabled;
            set
            {
                Instance.twoLineNamesEnabled = value;
                SaveSettings();
            }
        }

        public static bool AutoIconsEnabled
        {
            get => Instance.autoIconsEnabled;
            set
            {
                Instance.autoIconsEnabled = value;
                SaveSettings();
            }
        }

        public static bool HierarchyLinesEnabled
        {
            get => Instance.hierarchyLinesEnabled;
            set
            {
                Instance.hierarchyLinesEnabled = value;
                SaveSettings();
            }
        }

        public static bool ZebraStripingEnabled
        {
            get => Instance.zebraStripingEnabled;
            set
            {
                Instance.zebraStripingEnabled = value;
                SaveSettings();
            }
        }

        public static bool ContentMinimapEnabled
        {
            get => Instance.contentMinimapEnabled;
            set
            {
                Instance.contentMinimapEnabled = value;
                SaveSettings();
            }
        }

        public static bool BackgroundColorsEnabled
        {
            get => Instance.backgroundColorsEnabled;
            set
            {
                Instance.backgroundColorsEnabled = value;
                SaveSettings();
            }
        }

        public static bool MinimalModeEnabled
        {
            get => Instance.minimalModeEnabled;
            set
            {
                Instance.minimalModeEnabled = value;
                SaveSettings();
            }
        }

        public static bool FoldersFirstEnabled
        {
            get => Instance.foldersFirstEnabled;
            set
            {
                Instance.foldersFirstEnabled = value;
                SaveSettings();
            }
        }

        public static bool ToggleExpandedEnabled
        {
            get => Instance.toggleExpandedEnabled;
            set
            {
                Instance.toggleExpandedEnabled = value;
                SaveSettings();
            }
        }

        public static bool CollapseEverythingElseEnabled
        {
            get => Instance.collapseEverythingElseEnabled;
            set
            {
                Instance.collapseEverythingElseEnabled = value;
                SaveSettings();
            }
        }

        public static bool CollapseEverythingEnabled
        {
            get => Instance.collapseEverythingEnabled;
            set
            {
                Instance.collapseEverythingEnabled = value;
                SaveSettings();
            }
        }

        public static bool PluginDisabled
        {
            get => Instance._pluginDisabled;
            set
            {
                Instance._pluginDisabled = value;
                SaveSettings();
            }
        }

        private static void SaveSettings()
        {
            if (_instance != null)
            {
                EditorUtility.SetDirty(_instance);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif