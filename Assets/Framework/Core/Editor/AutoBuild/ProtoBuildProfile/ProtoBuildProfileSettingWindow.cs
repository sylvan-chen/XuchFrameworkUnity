using System.IO;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    public class ProtoBuildProfileSettingWindow : EditorWindow
    {
        private const string DEFAULT_PROTOS_DIRECTORY = "../Lua/csproto";
        private const string DEFAULT_ENCRYPTED_PROTO_OUTPUT_DIRECTORY = "./BuildGenerated/EncryptedProtos";
        private readonly string[] DEFAULT_IGNORED_DIRECTORIES = { };
        private const string DEFAULT_ADDRESSABLE_GROUP_NAME = "protos";
        private const string DEFAULT_ADDRESSABLE_LABEL = "proto";

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
                _ignoredDirectoriesStr = GameHelper.ConvertArrayToStr(_protoBuildProfile.IgnoredDirectoryNames);
        }

        [MenuItem("Build/Proto Build Profile", priority = 51)]
        public static void ShowWindow()
        {
            GetWindow<ProtoBuildProfileSettingWindow>("Proto Build Profile");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Source Proto Directory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Root directory where proto files are stored");
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
                        var selectedPath = EditorUtility.OpenFolderPanel("Choos Proto Directory", Path.GetDirectoryName(Application.dataPath), "");
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

            EditorGUILayout.LabelField("Output Directory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Proto files will be encrypted and output to this directory during build process");
            EditorGUILayout.LabelField("- Must be inside the 'Assets/' folder to be included in Addressable group");
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
                        var selectedPath = EditorUtility.OpenFolderPanel("Choose Output Directory", Application.dataPath, "");
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

            EditorGUILayout.LabelField("Ignore Directory Names", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Split directory names with space");
            EditorGUILayout.LabelField("- eg. test temp");
            _ignoredDirectoriesStr = EditorGUILayout.TextField(_ignoredDirectoriesStr, GUILayout.MinWidth(200));
            _protoBuildProfile.IgnoredDirectoryNames = GameHelper.ConvertStrToArray(_ignoredDirectoriesStr);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Addressable Group", EditorStyles.boldLabel);
            _protoBuildProfile.AddressableGroupName = EditorGUILayout.TextField(_protoBuildProfile.AddressableGroupName);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Addressable Label", EditorStyles.boldLabel);
            _protoBuildProfile.AddressableLabel = EditorGUILayout.TextField(_protoBuildProfile.AddressableLabel);

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Save", GUILayout.Height(40)))
            {
                SaveCurrentProfile();
            }

            if (GUILayout.Button("Reset", GUILayout.Height(30)))
            {
                _protoBuildProfile.ProtosDirectory = DEFAULT_PROTOS_DIRECTORY;
                _protoBuildProfile.EncryptedProtoOutputDirectory = DEFAULT_ENCRYPTED_PROTO_OUTPUT_DIRECTORY;
                _protoBuildProfile.IgnoredDirectoryNames = DEFAULT_IGNORED_DIRECTORIES;

                _ignoredDirectoriesStr = GameHelper.ConvertArrayToStr(_protoBuildProfile.IgnoredDirectoryNames);

                _protoBuildProfile.AddressableGroupName = DEFAULT_ADDRESSABLE_GROUP_NAME;
                _protoBuildProfile.AddressableLabel = DEFAULT_ADDRESSABLE_LABEL;

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
            return GameHelper.GetRegularPath(fullPath);
        }

        private void SaveCurrentProfile()
        {
            EditorUtility.SetDirty(_protoBuildProfile);
            AssetDatabase.SaveAssets();
            GUI.FocusControl(null);
        }
    }
}