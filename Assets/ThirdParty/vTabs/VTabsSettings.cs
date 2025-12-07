#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VTabs
{
    public class VTabsSettings : ScriptableObject
    {
        private static VTabsSettings _instance;
        private const string SETTINGS_PATH = "Assets/ThirdParty/VTabs/vTabs Settings.asset";
        private const string SETTINGS_FOLDER = "Assets/ThirdParty/VTabs";

        public static VTabsSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<VTabsSettings>(SETTINGS_PATH);

                    if (_instance == null)
                    {
                        _instance = CreateInstance<VTabsSettings>();

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
        public bool dragndropEnabled = true;
        public bool addTabButtonEnabled = true;
        public bool closeTabButtonEnabled = true;
        public bool dividersEnabled = true;
        public bool hideLockButtonEnabled = false;
        public int tabStyle = 0;
        public int backgroundStyle = 0;

        [Header("Shortcuts")]
        public bool switchTabShortcutEnabled = true;
        public bool addTabShortcutEnabled = true;
        public bool closeTabShortcutEnabled = true;
        public bool reopenTabShortcutEnabled = true;

        [Header("OSX Specific")]
        public bool slideScrollEnabled = true;
        public float slideScrollSensitivity = 1.0f;
        public bool reverseScrollDirectionEnabled = false;

        [Header("Other")]
        public bool pluginDisabled = false;

        public static bool DragndropEnabled
        {
            get => Instance.dragndropEnabled;
            set
            {
                Instance.dragndropEnabled = value;
                SaveSettings();
            }
        }

        public static bool AddTabButtonEnabled
        {
            get => Instance.addTabButtonEnabled;
            set
            {
                Instance.addTabButtonEnabled = value;
                SaveSettings();
            }
        }

        public static bool CloseTabButtonEnabled
        {
            get => Instance.closeTabButtonEnabled;
            set
            {
                Instance.closeTabButtonEnabled = value;
                SaveSettings();
            }
        }

        public static bool DividersEnabled
        {
            get => Instance.dividersEnabled;
            set
            {
                Instance.dividersEnabled = value;
                SaveSettings();
            }
        }

        public static bool HideLockButtonEnabled
        {
            get => Instance.hideLockButtonEnabled;
            set
            {
                Instance.hideLockButtonEnabled = value;
                SaveSettings();
            }
        }

        public static int TabStyle
        {
            get => Instance.tabStyle;
            set
            {
                Instance.tabStyle = value;
                SaveSettings();
            }
        }

        public static int BackgroundStyle
        {
            get => Instance.backgroundStyle;
            set
            {
                Instance.backgroundStyle = value;
                SaveSettings();
            }
        }

        public static bool SwitchTabShortcutEnabled
        {
            get => Instance.switchTabShortcutEnabled;
            set
            {
                Instance.switchTabShortcutEnabled = value;
                SaveSettings();
            }
        }

        public static bool AddTabShortcutEnabled
        {
            get => Instance.addTabShortcutEnabled;
            set
            {
                Instance.addTabShortcutEnabled = value;
                SaveSettings();
            }
        }

        public static bool CloseTabShortcutEnabled
        {
            get => Instance.closeTabShortcutEnabled;
            set
            {
                Instance.closeTabShortcutEnabled = value;
                SaveSettings();
            }
        }

        public static bool ReopenTabShortcutEnabled
        {
            get => Instance.reopenTabShortcutEnabled;
            set
            {
                Instance.reopenTabShortcutEnabled = value;
                SaveSettings();
            }
        }

        public static bool SlideScrollEnabled
        {
            get => Instance.slideScrollEnabled;
            set
            {
                Instance.slideScrollEnabled = value;
                SaveSettings();
            }
        }

        public static float SlideScrollSensitivity
        {
            get => Instance.slideScrollSensitivity;
            set
            {
                Instance.slideScrollSensitivity = value;
                SaveSettings();
            }
        }

        public static bool ReverseScrollDirectionEnabled
        {
            get => Instance.reverseScrollDirectionEnabled;
            set
            {
                Instance.reverseScrollDirectionEnabled = value;
                SaveSettings();
            }
        }

        public static bool PluginDisabled
        {
            get => Instance.pluginDisabled;
            set
            {
                Instance.pluginDisabled = value;
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