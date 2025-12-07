#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VHierarchy
{
    /// <summary>
    /// ScriptableObject-based settings for vHierarchy
    /// </summary>
    public class VHierarchySettings : ScriptableObject
    {
        private static VHierarchySettings _instance;
        private const string SETTINGS_PATH = "Assets/ThirdParty/vHierarchy/vHierarchy Settings.asset";
        private const string SETTINGS_FOLDER = "Assets/ThirdParty/vHierarchy";

        public static VHierarchySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<VHierarchySettings>(SETTINGS_PATH);

                    if (_instance == null)
                    {
                        _instance = CreateInstance<VHierarchySettings>();

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

        // Features
        [Header("Features")]
        [SerializeField]
        private bool _navigationBarEnabled = true;
        [SerializeField]
        private bool _sceneSelectorEnabled = true;
        [SerializeField]
        private bool _componentMinimapEnabled = true;
        [SerializeField]
        private bool _activationToggleEnabled = true;
        [SerializeField]
        private bool _hierarchyLinesEnabled = true;
        [SerializeField]
        private bool _zebraStripingEnabled = true;
        [SerializeField]
        private bool _minimalModeEnabled = false;

        // Shortcuts
        [Header("Shortcuts")]
        [SerializeField]
        private bool _toggleActiveEnabled = true;
        [SerializeField]
        private bool _focusEnabled = true;
        [SerializeField]
        private bool _deleteEnabled = true;
        [SerializeField]
        private bool _toggleExpandedEnabled = true;
        [SerializeField]
        private bool _isolateEnabled = true;
        [SerializeField]
        private bool _collapseEverythingEnabled = true;
        [SerializeField]
        private bool _setDefaultParentEnabled = true;

        // Plugin state
        [Header("Plugin State")]
        [SerializeField]
        private bool _pluginDisabled = false;

        // Properties
        public static bool navigationBarEnabled
        {
            get => Instance._navigationBarEnabled;
            set
            {
                Instance._navigationBarEnabled = value;
                SaveSettings();
            }
        }

        public static bool sceneSelectorEnabled
        {
            get => Instance._sceneSelectorEnabled;
            set
            {
                Instance._sceneSelectorEnabled = value;
                SaveSettings();
            }
        }

        public static bool componentMinimapEnabled
        {
            get => Instance._componentMinimapEnabled;
            set
            {
                Instance._componentMinimapEnabled = value;
                SaveSettings();
            }
        }

        public static bool activationToggleEnabled
        {
            get => Instance._activationToggleEnabled;
            set
            {
                Instance._activationToggleEnabled = value;
                SaveSettings();
            }
        }

        public static bool hierarchyLinesEnabled
        {
            get => Instance._hierarchyLinesEnabled;
            set
            {
                Instance._hierarchyLinesEnabled = value;
                SaveSettings();
            }
        }

        public static bool minimalModeEnabled
        {
            get => Instance._minimalModeEnabled;
            set
            {
                Instance._minimalModeEnabled = value;
                SaveSettings();
            }
        }

        public static bool zebraStripingEnabled
        {
            get => Instance._zebraStripingEnabled;
            set
            {
                Instance._zebraStripingEnabled = value;
                SaveSettings();
            }
        }

        public static bool toggleActiveEnabled
        {
            get => Instance._toggleActiveEnabled;
            set
            {
                Instance._toggleActiveEnabled = value;
                SaveSettings();
            }
        }

        public static bool focusEnabled
        {
            get => Instance._focusEnabled;
            set
            {
                Instance._focusEnabled = value;
                SaveSettings();
            }
        }

        public static bool deleteEnabled
        {
            get => Instance._deleteEnabled;
            set
            {
                Instance._deleteEnabled = value;
                SaveSettings();
            }
        }

        public static bool toggleExpandedEnabled
        {
            get => Instance._toggleExpandedEnabled;
            set
            {
                Instance._toggleExpandedEnabled = value;
                SaveSettings();
            }
        }

        public static bool isolateEnabled
        {
            get => Instance._isolateEnabled;
            set
            {
                Instance._isolateEnabled = value;
                SaveSettings();
            }
        }

        public static bool collapseEverythingEnabled
        {
            get => Instance._collapseEverythingEnabled;
            set
            {
                Instance._collapseEverythingEnabled = value;
                SaveSettings();
            }
        }

        public static bool setDefaultParentEnabled
        {
            get => Instance._setDefaultParentEnabled;
            set
            {
                Instance._setDefaultParentEnabled = value;
                SaveSettings();
            }
        }

        public static bool pluginDisabled
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