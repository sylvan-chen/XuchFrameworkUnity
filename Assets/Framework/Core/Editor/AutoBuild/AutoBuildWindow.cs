using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace XuchFramework.Editor
{
    public class AutoBuildWindow : EditorWindow
    {
        public enum AndroidDebugSymbols
        {
            None = 0,
            SymbolTable = 1,
            Full = 2,
        }

        private readonly List<BuildConfig> _buildConfigs = new();
        private int _selectedConfigIndex = 0;
        private BuildConfig _currentConfig;
        private Vector2 _scrollPosition;
        private Vector2 _macroScrollPosition;
        private SerializedObject _serializedConfig;
        private static GUIStyle _wordWrapStyle;

        public GUIStyle WordWrapStyle
        {
            get
            {
                _wordWrapStyle ??= new GUIStyle(EditorStyles.textArea)
                {
                    // 启用自动换行
                    wordWrap = true
                };
                return _wordWrapStyle;
            }
        }

        [MenuItem("Build/AutoBuilder", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoBuildWindow>("Auto Builder");
            window.minSize = new Vector2(1200, 800);
            window.Show();
        }

        private void OnEnable()
        {
            LoadBuildConfigs();
        }

        private void LoadBuildConfigs()
        {
            _buildConfigs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:BuildConfig");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                BuildConfig config = AssetDatabase.LoadAssetAtPath<BuildConfig>(assetPath);
                if (config != null)
                {
                    _buildConfigs.Add(config);
                }
            }

            if (_buildConfigs.Count > 0)
            {
                _selectedConfigIndex = Mathf.Clamp(_selectedConfigIndex, 0, _buildConfigs.Count - 1);
                _currentConfig = _buildConfigs[_selectedConfigIndex];
                UpdateSerializedObject();
            }
            else
            {
                _currentConfig = null;
                _serializedConfig = null;
            }
        }

        private void UpdateSerializedObject()
        {
            if (_currentConfig != null)
            {
                _serializedConfig?.Dispose();
                _serializedConfig = new SerializedObject(_currentConfig);
            }
            else
            {
                _serializedConfig = null;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("自动构建工具", titleStyle);

            EditorGUILayout.Space(10);
            DrawHorizontalLine();
            EditorGUILayout.Space(10);

            // 工具栏：配置选择和操作按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Config:", GUILayout.Width(50));

                if (_buildConfigs.Count > 0)
                {
                    string[] configNames = _buildConfigs.Select(c => c.name).ToArray();
                    int newIndex = EditorGUILayout.Popup(_selectedConfigIndex, configNames);
                    if (newIndex != _selectedConfigIndex)
                    {
                        SaveCurrentConfig();
                        _selectedConfigIndex = newIndex;
                        _currentConfig = _buildConfigs[_selectedConfigIndex];
                        UpdateSerializedObject();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No config available", EditorStyles.helpBox);
                }

                // 创建按钮
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    CreateNewBuildConfig();
                }
                GUI.backgroundColor = Color.white;

                using (new EditorGUI.DisabledGroupScope(_buildConfigs.Count == 0))
                {
                    // 删除按钮
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        DeleteCurrentConfig();
                    }
                    GUI.backgroundColor = Color.white;

                    // 刷新按钮
                    if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                    {
                        SaveCurrentConfig();
                        LoadBuildConfigs();
                    }
                }
            }

            EditorGUILayout.Space(5);
            DrawHorizontalLine();
            EditorGUILayout.Space(5);

            if (_buildConfigs.Count == 0)
            {
                EditorGUILayout.HelpBox("No BuildConfig exists. Click 'Create' to create a new one.", MessageType.Info);
                return;
            }

            // 显示配置详情
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                if (_currentConfig != null && _serializedConfig != null)
                {
                    DrawConfigEditor();
                }
            }

            EditorGUILayout.Space(10);
            DrawHorizontalLine();
            EditorGUILayout.Space(10);

            // 构建按钮
            using (new EditorGUI.DisabledGroupScope(_currentConfig == null))
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Build", GUILayout.Height(40)))
                {
                    if (_currentConfig != null)
                    {
                        SaveCurrentConfig();
                        if (EditorUtility.DisplayDialog(
                                "Confirm Build",
                                $"Start building with config '{_currentConfig.name}'\nTarget Platform: {_currentConfig.BuildTarget}",
                                "Build",
                                "Cancel"))
                        {
                            AutoBuilder.StartBuild(_currentConfig);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(5);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // 如果没有控件被激活(hotControl == 0),说明点击了空白区域
                if (GUIUtility.hotControl == 0)
                {
                    GUI.FocusControl(null);
                    SaveCurrentConfig();
                    Repaint();
                }
            }
        }

        private void CreateNewBuildConfig()
        {
            string configName = "BuildConfig_New";
            int counter = 1;

            // 确保 Resources 文件夹存在
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                string parentFolder = "Assets";
                AssetDatabase.CreateFolder(parentFolder, "Resources");
            }

            // 生成唯一名称
            string assetPath = $"{resourcesPath}/{configName}.asset";
            while (File.Exists(assetPath))
            {
                configName = $"BuildConfig_New{counter}";
                assetPath = $"{resourcesPath}/{configName}.asset";
                counter++;
            }

            // 创建新配置
            BuildConfig newConfig = CreateInstance<BuildConfig>();
            newConfig.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var projRoot = Path.GetDirectoryName(Application.dataPath) ?? "Assets/..";
            newConfig.OutputDirectory = Path.Combine(projRoot, "BuildTargets", newConfig.BuildTarget.ToString());
            newConfig.BuildName = "Build";
            newConfig.CompanyName = Application.companyName;
            newConfig.ProductName = Application.productName;
            newConfig.AppIdentifier = Application.identifier;
            newConfig.AppVersion = Application.version;
            newConfig.AddressablesActiveProfile = "Default";
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(newConfig.BuildTarget);
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            newConfig.MacroDefinitions = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            AssetDatabase.CreateAsset(newConfig, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadBuildConfigs();

            // 选择新创建的配置
            _selectedConfigIndex = _buildConfigs.IndexOf(newConfig);
            if (_selectedConfigIndex >= 0)
            {
                _currentConfig = newConfig;
                UpdateSerializedObject();
            }

            EditorGUIUtility.PingObject(newConfig);
        }

        private void DeleteCurrentConfig()
        {
            if (_currentConfig == null)
                return;

            if (EditorUtility.DisplayDialog(
                    "Delete Build Config",
                    $"Are you sure you want to delete '{_currentConfig.name}'?\nThis action cannot be undone.",
                    "Delete",
                    "Cancel"))
            {
                string assetPath = AssetDatabase.GetAssetPath(_currentConfig);
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LoadBuildConfigs();
            }
        }

        private void SaveCurrentConfig()
        {
            if (_serializedConfig != null)
            {
                _serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(_currentConfig);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawConfigEditor()
        {
            _serializedConfig.Update();

            // 基本信息
            DrawSection(
                "Basic Information",
                () =>
                {
                    EditorGUILayout.ObjectField("Build Config", _currentConfig, typeof(BuildConfig), false);
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BuildTarget)));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var outputDirProperty = _serializedConfig.FindProperty(nameof(BuildConfig.OutputDirectory));
                        EditorGUILayout.PropertyField(outputDirProperty);

                        if (GUILayout.Button("...", GUILayout.Width(50)))
                        {
                            var currentDir = Directory.GetCurrentDirectory();
                            try
                            {
                                var selectedPath = EditorUtility.OpenFolderPanel("选择构建输出目录", Path.GetDirectoryName(Application.dataPath), "");
                                if (!string.IsNullOrEmpty(selectedPath))
                                {
                                    outputDirProperty.stringValue = selectedPath;
                                }
                            }
                            finally
                            {
                                Directory.SetCurrentDirectory(currentDir);
                            }
                        }
                    }
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BuildName)));
                });

            // 应用信息
            DrawSection(
                "Application Information",
                () =>
                {
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.CompanyName)));
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.ProductName)));
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.AppIdentifier)));
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.AppVersion)));

                    switch (_currentConfig.BuildTarget)
                    {
                        // 平台特定版本信息
                        case BuildTarget.Android:
                            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BundleVersionCode)));
                            break;
                        case BuildTarget.iOS:
                            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BundleNumber)));
                            break;
                    }
                });

            // 资源构建选项
            DrawSection(
                "Resource Options",
                () =>
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BuildLua)));

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(20);
                            if (GUILayout.Button("Lua Build Profile...", GUILayout.Width(125)))
                            {
                                LuaBuildProfileSettingWindow.ShowWindow();
                            }
                        }
                    }

                    EditorGUILayout.Space(5);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BuildProto)));

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(20);
                            if (GUILayout.Button("Proto Build Profile...", GUILayout.Width(125)))
                            {
                                ProtoBuildProfileSettingWindow.ShowWindow();
                            }
                        }
                    }

                    EditorGUILayout.Space(5);

                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.BuildAddressables)));
                    if (_currentConfig.BuildAddressables)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(
                            _serializedConfig.FindProperty(nameof(BuildConfig.AddressablesCleanBuild)),
                            new GUIContent("Clean Build"));
                        EditorGUILayout.PropertyField(
                            _serializedConfig.FindProperty(nameof(BuildConfig.AddressablesActiveProfile)),
                            new GUIContent("Active Profile"));
                        EditorGUI.indentLevel--;
                    }
                });

            // 宏定义
            DrawSection(
                "Define Symbols",
                () =>
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(_macroScrollPosition, GUILayout.Height(50)))
                    {
                        _macroScrollPosition = scroll.scrollPosition;

                        var macroDefinitionsProperty = _serializedConfig.FindProperty(nameof(BuildConfig.MacroDefinitions));
                        macroDefinitionsProperty.stringValue = EditorGUILayout.TextArea(
                            macroDefinitionsProperty.stringValue,
                            WordWrapStyle,
                            GUILayout.ExpandHeight(true));
                    }
                },
                () =>
                {
                    GUILayout.Space(-105);
                    if (GUILayout.Button("↻", GUILayout.Width(50)))
                    {
                        SyncMacroDefinitions();
                    }
                    GUILayout.FlexibleSpace();
                });

            // 调试选项
            DrawSection(
                "Debug Options",
                () =>
                {
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.DevelopmentBuild)));
                    using (new EditorGUI.DisabledGroupScope(!_currentConfig.DevelopmentBuild))
                    {
                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.AutoconnectProfiler)));
                        EditorGUILayout.PropertyField(
                            _serializedConfig.FindProperty(nameof(BuildConfig.DeepProfilingSurpport)),
                            new GUIContent("Deep Profiling"));
                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.ScriptDebugging)));
                    }
                });

            // 压缩选项
            DrawSection(
                "Compression Options",
                () =>
                {
                    EditorGUILayout.PropertyField(
                        _serializedConfig.FindProperty(nameof(BuildConfig.PlayerCompression)),
                        new GUIContent("Compression Method"));
                });

            // Android 特定设置
            if (_currentConfig.BuildTarget == BuildTarget.Android)
            {
                DrawSection(
                    "Android Settings",
                    () =>
                    {
                        var currentSymbolProperty = _serializedConfig.FindProperty(nameof(BuildConfig.DebugSymbols));
                        var currentSymbolValue = (AndroidDebugSymbols)currentSymbolProperty.intValue;
                        var newSymbolValue = (AndroidDebugSymbols)EditorGUILayout.EnumPopup("Debug Symbols", currentSymbolValue);
                        if (newSymbolValue != currentSymbolValue)
                        {
                            currentSymbolProperty.intValue = (int)newSymbolValue;
                        }

                        EditorGUILayout.LabelField("Minify");
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.MinifyRelease)), new GUIContent("Release"));
                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.MinifyDebug)), new GUIContent("Debug"));
                        EditorGUI.indentLevel--;

                        var buildAppBundleProperty = _serializedConfig.FindProperty(nameof(BuildConfig.BuildAppBundle));
                        EditorGUILayout.PropertyField(buildAppBundleProperty, new GUIContent("Build AppBundle (AAB)"));

                        var splitAppBinaryProperty = _serializedConfig.FindProperty(nameof(BuildConfig.SplitApplicationBinary));
                        if (buildAppBundleProperty.boolValue)
                        {
                            splitAppBinaryProperty.boolValue = true;
                        }

                        using (new EditorGUI.DisabledGroupScope(buildAppBundleProperty.boolValue))
                        {
                            EditorGUILayout.PropertyField(splitAppBinaryProperty, new GUIContent("Split Application Binary"));
                        }

                        EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.UseCustomKeystore)));

                        if (_currentConfig.UseCustomKeystore)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.KeystoreName)));
                            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.KeystorePass)));
                            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.KeyaliasName)));
                            EditorGUILayout.PropertyField(_serializedConfig.FindProperty(nameof(BuildConfig.KeyaliasPass)));
                            EditorGUI.indentLevel--;
                        }
                    });
            }

            _serializedConfig.ApplyModifiedProperties();
        }

        private void SyncMacroDefinitions()
        {
            if (_currentConfig == null)
                return;

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(_currentConfig.BuildTarget);
            string currentMacros = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup));

            var macroProperty = _serializedConfig.FindProperty(nameof(BuildConfig.MacroDefinitions));
            macroProperty.stringValue = currentMacros;
            _serializedConfig.ApplyModifiedProperties();

            EditorUtility.SetDirty(_currentConfig);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DrawSection(string sectionTitle, System.Action drawContent, System.Action titleExtends = null)
        {
            EditorGUILayout.Space(5);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            if (titleExtends != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(sectionTitle, headerStyle);
                    titleExtends.Invoke();
                }
            }
            else
            {
                EditorGUILayout.LabelField(sectionTitle, headerStyle);
            }

            EditorGUI.indentLevel++;
            drawContent();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void OnDisable()
        {
            SaveCurrentConfig();
        }
    }
}