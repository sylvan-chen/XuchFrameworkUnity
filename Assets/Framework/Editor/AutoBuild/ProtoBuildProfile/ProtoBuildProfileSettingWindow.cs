using System.IO;
using DigiEden.Framework.Utils;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    public class ProtoBuildProfileSettingWindow : EditorWindow
    {
        private const string DEFAULT_PROTOS_DIRECTORY = "../Lua/csproto";
        private const string DEFAULT_ENCRYPTED_PROTO_OUTPUT_DIRECTORY = "./BuildGenerated/EncryptedProtos";
        private readonly string[] DEFAULT_IGNORED_DIRECTORIES = { };
        private const string DEFAULT_ADDRESSABLE_GROUP_NAME = "Protos";
        private const string DEFAULT_ADDRESSABLE_LABEL = "luaproto";

        private ProtoBuildProfile _protoBuildProfile;
        private string _ignoredDirectoriesStr = string.Empty;

        private void OnEnable()
        {
            this.minSize = new Vector2(600, 600);

            _protoBuildProfile = Resources.Load<ProtoBuildProfile>("ProtoBuildProfile");
            if (_protoBuildProfile == null)
            {
                _protoBuildProfile = CreateInstance<ProtoBuildProfile>();
                const string assetPath = "Assets/Resources/ProtoBuildProfile.asset";
                AssetDatabase.CreateAsset(_protoBuildProfile, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Log.Info($"[LuaProfileSettingWindow] ProtoBuildProfile asset not found. A new one has been created at {assetPath}");
            }

            if (_protoBuildProfile.IgnoredDirectoryNames != null)
                _ignoredDirectoriesStr = StringHelper.ConvertArrayToStr(_protoBuildProfile.IgnoredDirectoryNames);
        }

        [MenuItem("Build/Proto 构建配置", priority = 51)]
        public static void ShowWindow()
        {
            GetWindow<ProtoBuildProfileSettingWindow>("Proto 构建配置");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Proto 文件目录", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Proto 文件存放的根目录");
            using (new EditorGUILayout.HorizontalScope())
            {
                _protoBuildProfile.ProtosDirectory = GetFullRegularPath(_protoBuildProfile.ProtosDirectory);

                GUI.enabled = false;
                _protoBuildProfile.ProtosDirectory = EditorGUILayout.TextField(_protoBuildProfile.ProtosDirectory, GUILayout.MinWidth(200));
                GUI.enabled = true;

                if (GUILayout.Button("...", GUILayout.Width(50)))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    try
                    {
                        var selectedPath = EditorUtility.OpenFolderPanel("选择 proto 文件目录", Path.GetDirectoryName(Application.dataPath), "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            _protoBuildProfile.ProtosDirectory = GetFullRegularPath(selectedPath);
                        }
                    }
                    finally
                    {
                        Directory.SetCurrentDirectory(currentDir);
                    }
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("加密 proto 文件输出目录", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- 资源构建时会自动加密 proto 文件并存放到该目录中");
            EditorGUILayout.LabelField("- 必须在 Assets/ 之内，以便加入 Addressable group");
            using (new EditorGUILayout.HorizontalScope())
            {
                _protoBuildProfile.EncryptedProtoOutputDirectory = GetFullRegularPath(_protoBuildProfile.EncryptedProtoOutputDirectory);

                _protoBuildProfile.EncryptedProtoOutputDirectory = EditorGUILayout.TextField(
                    _protoBuildProfile.EncryptedProtoOutputDirectory,
                    GUILayout.MinWidth(200));

                if (GUILayout.Button("...", GUILayout.Width(50)))
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    try
                    {
                        var selectedPath = EditorUtility.OpenFolderPanel("选择加密 proto 文件输出目录", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            _protoBuildProfile.EncryptedProtoOutputDirectory = GetFullRegularPath(selectedPath);
                        }
                    }
                    finally
                    {
                        Directory.SetCurrentDirectory(currentDir);
                    }
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("构建时要忽略的 proto 文件目录名", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- 多个目录名用空格分隔");
            EditorGUILayout.LabelField("- 例如: test temp");
            _ignoredDirectoriesStr = EditorGUILayout.TextField(_ignoredDirectoriesStr, GUILayout.MinWidth(200));
            _protoBuildProfile.IgnoredDirectoryNames = StringHelper.ConvertStrToArray(_ignoredDirectoriesStr);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("构建时要加入的 Addressable group 名字", EditorStyles.boldLabel);
            _protoBuildProfile.AddressableGroupName = EditorGUILayout.TextField(_protoBuildProfile.AddressableGroupName);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("构建时要添加的 Addressable label", EditorStyles.boldLabel);
            _protoBuildProfile.AddressableLabel = EditorGUILayout.TextField(_protoBuildProfile.AddressableLabel);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("保存设置", GUILayout.Height(40)))
            {
                SaveCurrentProfile();
            }

            if (GUILayout.Button("恢复默认设置", GUILayout.Height(30)))
            {
                _protoBuildProfile.ProtosDirectory = DEFAULT_PROTOS_DIRECTORY;
                _protoBuildProfile.EncryptedProtoOutputDirectory = DEFAULT_ENCRYPTED_PROTO_OUTPUT_DIRECTORY;
                _protoBuildProfile.IgnoredDirectoryNames = DEFAULT_IGNORED_DIRECTORIES;

                _ignoredDirectoriesStr = StringHelper.ConvertArrayToStr(_protoBuildProfile.IgnoredDirectoryNames);

                _protoBuildProfile.AddressableGroupName = DEFAULT_ADDRESSABLE_GROUP_NAME;
                _protoBuildProfile.AddressableLabel = DEFAULT_ADDRESSABLE_LABEL;

                SaveCurrentProfile();
            }
        }

        private string GetFullRegularPath(string path)
        {
            var fullPath = Path.GetFullPath(path, Application.dataPath);
            return Framework.Utils.PathHelper.GetRegularPath(fullPath);
        }

        private void SaveCurrentProfile()
        {
            EditorUtility.SetDirty(_protoBuildProfile);
            AssetDatabase.SaveAssets();
            GUI.FocusControl(null);
        }
    }
}