#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VInspector
{
    public class VInspectorSettings : ScriptableObject
    {
        private static VInspectorSettings _instance;
        private const string SETTINGS_PATH = "Assets/ThirdParty/vInspector/vInspector Settings.asset";
        private const string SETTINGS_FOLDER = "Assets/ThirdParty/vInspector";

        public static VInspectorSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<VInspectorSettings>(SETTINGS_PATH);

                    if (_instance == null)
                    {
                        _instance = CreateInstance<VInspectorSettings>();

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
        [SerializeField]
        private bool _navigationBarEnabled = true;
        [SerializeField]
        private bool _copyPasteButtonsEnabled = true;
        [SerializeField]
        private bool _playmodeSaveButtonEnabled = true;
        [SerializeField]
        private bool _componentWindowsEnabled = true;
        [SerializeField]
        private bool _componentAnimationsEnabled = true;
        [SerializeField]
        private bool _minimalModeEnabled = false;
        [SerializeField]
        private bool _resettableVariablesEnabled = false;
        [SerializeField]
        private bool _hideScriptFieldEnabled = false;
        [SerializeField]
        private bool _hideHelpButtonEnabled = false;
        [SerializeField]
        private bool _hidePresetsButtonEnabled = false;

        [Header("Shortcuts")]
        [SerializeField]
        private bool _toggleActiveEnabled = true;
        [SerializeField]
        private bool _deleteEnabled = true;
        [SerializeField]
        private bool _toggleExpandedEnabled = true;
        [SerializeField]
        private bool _collapseEverythingElseEnabled = true;
        [SerializeField]
        private bool _collapseEverythingEnabled = true;

        [Header("Other")]
        [SerializeField]
        private bool _pluginDisabled = false;

        public static bool navigationBarEnabled
        {
            get => Instance._navigationBarEnabled;
            set
            {
                Instance._navigationBarEnabled = value;
                SaveSettings();
            }
        }

        public static bool copyPasteButtonsEnabled
        {
            get => Instance._copyPasteButtonsEnabled;
            set
            {
                Instance._copyPasteButtonsEnabled = value;
                SaveSettings();
            }
        }

        public static bool playmodeSaveButtonEnabled
        {
            get => Instance._playmodeSaveButtonEnabled;
            set
            {
                Instance._playmodeSaveButtonEnabled = value;
                SaveSettings();
            }
        }

        public static bool componentWindowsEnabled
        {
            get => Instance._componentWindowsEnabled;
            set
            {
                Instance._componentWindowsEnabled = value;
                SaveSettings();
            }
        }

        public static bool componentAnimationsEnabled
        {
            get => Instance._componentAnimationsEnabled;
            set
            {
                Instance._componentAnimationsEnabled = value;
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

        public static bool resettableVariablesEnabled
        {
            get => Instance._resettableVariablesEnabled;
            set
            {
                Instance._resettableVariablesEnabled = value;
                SaveSettings();
            }
        }

        public static bool hideScriptFieldEnabled
        {
            get => Instance._hideScriptFieldEnabled;
            set
            {
                Instance._hideScriptFieldEnabled = value;
                SaveSettings();
            }
        }

        public static bool hideHelpButtonEnabled
        {
            get => Instance._hideHelpButtonEnabled;
            set
            {
                Instance._hideHelpButtonEnabled = value;
                SaveSettings();
            }
        }

        public static bool hidePresetsButtonEnabled
        {
            get => Instance._hidePresetsButtonEnabled;
            set
            {
                Instance._hidePresetsButtonEnabled = value;
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

        public static bool collapseEverythingElseEnabled
        {
            get => Instance._collapseEverythingElseEnabled;
            set
            {
                Instance._collapseEverythingElseEnabled = value;
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