using System.IO;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    public class LuaBuildProfileSettingWindow : EditorWindow
    {
        private const string DEFAULT_LUA_SCRIPTS_DIRECTORY = "../Lua";
        private const string DEFAULT_ENCRYPTED_LUA_SCRIPTS_OUTPUT_DIRECTORY = "./BuildGenerated/EncryptedLuaScripts";
        private readonly string[] DEFAULT_IGNORED_DIRECTORIES = { "type_hints" };
        private const string DEFAULT_ADDRESSABLE_GROUP_NAME = "LuaScripts";
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
                _ignoredDirectoriesStr = StringHelper.ConvertArrayToStr(_luaBuildProfile.IgnoredDirectoryNames);
        }

        [MenuItem("Build/Lua 构建配置", priority = 50)]
        public static void ShowWindow()
        {
            GetWindow<LuaBuildProfileSettingWindow>("Lua 构建配置");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Lua 脚本目录", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Lua 脚本存放的根目录");
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
                        var selectedPath = EditorUtility.OpenFolderPanel("选择 lua 脚本目录", Path.GetDirectoryName(Application.dataPath), "");
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

            EditorGUILayout.LabelField("加密 lua 脚本输出目录", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- 资源构建时会自动加密 lua 脚本并存放到该目录中");
            EditorGUILayout.LabelField("- 必须在 Assets/ 之内，以便加入 Addressable group");
            using (new EditorGUILayout.HorizontalScope())
            {
                _luaBuildProfile.EncryptedLuaSciptsOutputDirectory = GetFullRegularPath(_luaBuildProfile.EncryptedLuaSciptsOutputDirectory);

                _luaBuildProfile.EncryptedLuaSciptsOutputDirectory = EditorGUILayout.TextField(
                    _luaBuildProfile.EncryptedLuaSciptsOutputDirectory,
                    GUILayout.MinWidth(200));

                if (GUILayout.Button("...", GUILayout.Width(50)))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    try
                    {
                        var selectedPath = EditorUtility.OpenFolderPanel("选择加密 lua 脚本输出目录", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            _luaBuildProfile.EncryptedLuaSciptsOutputDirectory = GetFullRegularPath(selectedPath);
                        }
                    }
                    finally
                    {
                        Directory.SetCurrentDirectory(currentDir);
                    }
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("构建时要忽略的 Lua 脚本目录名", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- 多个目录名用空格分隔");
            EditorGUILayout.LabelField("- 例如: type_hints test temp");
            _ignoredDirectoriesStr = EditorGUILayout.TextField(_ignoredDirectoriesStr, GUILayout.MinWidth(200));
            _luaBuildProfile.IgnoredDirectoryNames = StringHelper.ConvertStrToArray(_ignoredDirectoriesStr);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("构建时要加入的 Addressable group 名字", EditorStyles.boldLabel);
            _luaBuildProfile.AddressableGroupName = EditorGUILayout.TextField(_luaBuildProfile.AddressableGroupName);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("构建时要添加的 Addressable label", EditorStyles.boldLabel);
            _luaBuildProfile.AddressableLabel = EditorGUILayout.TextField(_luaBuildProfile.AddressableLabel);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("保存设置", GUILayout.Height(40)))
            {
                SaveCurrentProfile();
            }

            if (GUILayout.Button("恢复默认设置", GUILayout.Height(30)))
            {
                _luaBuildProfile.LuaScriptsDirectory = DEFAULT_LUA_SCRIPTS_DIRECTORY;
                _luaBuildProfile.EncryptedLuaSciptsOutputDirectory = DEFAULT_ENCRYPTED_LUA_SCRIPTS_OUTPUT_DIRECTORY;
                _luaBuildProfile.IgnoredDirectoryNames = DEFAULT_IGNORED_DIRECTORIES;

                _ignoredDirectoriesStr = StringHelper.ConvertArrayToStr(_luaBuildProfile.IgnoredDirectoryNames);

                _luaBuildProfile.AddressableGroupName = DEFAULT_ADDRESSABLE_GROUP_NAME;
                _luaBuildProfile.AddressableLabel = DEFAULT_ADDRESSABLE_LABEL;

                SaveCurrentProfile();
            }
        }

        private string GetFullRegularPath(string path)
        {
            var fullPath = Path.GetFullPath(path, Application.dataPath);
            return Core.Utils.PathHelper.GetRegularPath(fullPath);
        }

        private void SaveCurrentProfile()
        {
            EditorUtility.SetDirty(_luaBuildProfile);
            AssetDatabase.SaveAssets();
            GUI.FocusControl(null);
        }
    }
}