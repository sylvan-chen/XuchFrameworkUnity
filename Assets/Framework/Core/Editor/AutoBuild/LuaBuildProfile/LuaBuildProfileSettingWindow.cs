using System.IO;
using UnityEditor;
using UnityEngine;
using Framework.Core;
using Framework.Utils;

namespace Framework.Editor
{
    public class LuaBuildProfileSettingWindow : EditorWindow
    {
        private const string DEFAULT_LUA_SCRIPTS_DIRECTORY = "../Lua";
        private const string DEFAULT_ENCRYPTED_LUA_SCRIPTS_OUTPUT_DIRECTORY = "./BuildGenerated/EncryptedLuaScripts";
        private readonly string[] DEFAULT_IGNORED_DIRECTORIES = { "type_hints" };
        private const string DEFAULT_ADDRESSABLE_GROUP_NAME = "luascripts";
        private const string DEFAULT_ADDRESSABLE_LABEL = "luascript";

        private LuaBuildProfile _luaBuildProfile;
        private string _ignoredDirectoriesStr = string.Empty;

        private void OnEnable()
        {
            this.minSize = new Vector2(600, 600);

            _luaBuildProfile = Resources.Load<LuaBuildProfile>("LuaBuildProfile");
            if (_luaBuildProfile == null)
            {
                _luaBuildProfile = CreateInstance<LuaBuildProfile>();
                const string assetPath = "Assets/Resources/LuaBuildProfile.asset";
                AssetDatabase.CreateAsset(_luaBuildProfile, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log.Info($"[LuaProfileSettingWindow] LuaProfile asset not found. A new one has been created at {assetPath}");
            }

            if (_luaBuildProfile.IgnoredDirectoryNames != null)
                _ignoredDirectoriesStr = GameUtils.ConvertArrayToStr(_luaBuildProfile.IgnoredDirectoryNames);
        }

        [MenuItem("Build/Lua Build Profile", priority = 50)]
        public static void ShowWindow()
        {
            GetWindow<LuaBuildProfileSettingWindow>("Lua Build Profile");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Source Lua Directory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Root directory where Lua scripts are stored");
            using (new EditorGUILayout.HorizontalScope())
            {
                _luaBuildProfile.LuaScriptsDirectory = GetFullRegularPath(_luaBuildProfile.LuaScriptsDirectory);

                GUI.enabled = false;
                _luaBuildProfile.LuaScriptsDirectory = EditorGUILayout.TextField(_luaBuildProfile.LuaScriptsDirectory, GUILayout.MinWidth(200));
                GUI.enabled = true;

                if (GUILayout.Button("...", GUILayout.Width(50)))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    try
                    {
                        var selectedPath = EditorUtility.OpenFolderPanel("Choose Lua Directory", Path.GetDirectoryName(Application.dataPath), "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            _luaBuildProfile.LuaScriptsDirectory = GetFullRegularPath(selectedPath);
                        }
                    }
                    finally
                    {
                        Directory.SetCurrentDirectory(currentDir);
                    }
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Output Directory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Lua scripts will be encrypted and output to this directory during build process");
            EditorGUILayout.LabelField("- Must be inside the 'Assets/' folder to be included in Addressable group");
            using (new EditorGUILayout.HorizontalScope())
            {
                _luaBuildProfile.EncryptedLuaScriptsOutputDirectory = GetFullRegularPath(_luaBuildProfile.EncryptedLuaScriptsOutputDirectory);

                _luaBuildProfile.EncryptedLuaScriptsOutputDirectory = EditorGUILayout.TextField(
                    _luaBuildProfile.EncryptedLuaScriptsOutputDirectory,
                    GUILayout.MinWidth(200));

                if (GUILayout.Button("...", GUILayout.Width(50)))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    try
                    {
                        var selectedPath = EditorUtility.OpenFolderPanel("Choose Output Directory", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            _luaBuildProfile.EncryptedLuaScriptsOutputDirectory = GetFullRegularPath(selectedPath);
                        }
                    }
                    finally
                    {
                        Directory.SetCurrentDirectory(currentDir);
                    }
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Ignore Directory Names", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Split directory names with spaces");
            EditorGUILayout.LabelField("- eg. type_hints test temp");
            _ignoredDirectoriesStr = EditorGUILayout.TextField(_ignoredDirectoriesStr, GUILayout.MinWidth(200));
            _luaBuildProfile.IgnoredDirectoryNames = GameUtils.ConvertStrToArray(_ignoredDirectoriesStr);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Addressable Group", EditorStyles.boldLabel);
            _luaBuildProfile.AddressableGroupName = EditorGUILayout.TextField(_luaBuildProfile.AddressableGroupName);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Addressable Label", EditorStyles.boldLabel);
            _luaBuildProfile.AddressableLabel = EditorGUILayout.TextField(_luaBuildProfile.AddressableLabel);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Save", GUILayout.Height(40)))
            {
                SaveCurrentProfile();
            }

            if (GUILayout.Button("Reset", GUILayout.Height(30)))
            {
                _luaBuildProfile.LuaScriptsDirectory = DEFAULT_LUA_SCRIPTS_DIRECTORY;
                _luaBuildProfile.EncryptedLuaScriptsOutputDirectory = DEFAULT_ENCRYPTED_LUA_SCRIPTS_OUTPUT_DIRECTORY;
                _luaBuildProfile.IgnoredDirectoryNames = DEFAULT_IGNORED_DIRECTORIES;

                _ignoredDirectoriesStr = GameUtils.ConvertArrayToStr(_luaBuildProfile.IgnoredDirectoryNames);

                _luaBuildProfile.AddressableGroupName = DEFAULT_ADDRESSABLE_GROUP_NAME;
                _luaBuildProfile.AddressableLabel = DEFAULT_ADDRESSABLE_LABEL;

                SaveCurrentProfile();
            }

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // hotControl == 0 means clicked on empty space
                if (GUIUtility.hotControl == 0)
                {
                    GUI.FocusControl(null);
                    SaveCurrentProfile();
                    Repaint();
                }
            }
        }

        private string GetFullRegularPath(string path)
        {
            var fullPath = Path.GetFullPath(path, Application.dataPath);
            return GameUtils.GetRegularPath(fullPath);
        }

        private void SaveCurrentProfile()
        {
            EditorUtility.SetDirty(_luaBuildProfile);
            AssetDatabase.SaveAssets();
            GUI.FocusControl(null);
        }
    }
}