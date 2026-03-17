using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Framework.Utils;

namespace Framework.Editor
{
    public class TableClassGenerator : EditorWindow
    {
        private const string DEFAULT_JSON_DIR = "DEFAULT:./Res/tables";
        private const string DEFAULT_OUTPUT_DIR = "DEFAULT:./TableConfigs";
        private const string DEFAULT_NAMESPACE = "EdenFramework.Table";

        private const string JSON_DIR_KEY = "ConfigGenerator_JsonDir";
        private const string OUTPUT_DIR_KEY = "ConfigGenerator_OutputDir";
        private const string NAMESPACE_KEY = "ConfigGenerator_Namespace";
        private const string SEARCH_OPTION_KEY = "ConfigGenerator_SearchOption";

        private SearchOption _searchOption = SearchOption.AllDirectories;
        private string _jsonDirectory = DEFAULT_JSON_DIR;
        private string _outputDirectory = DEFAULT_OUTPUT_DIR;
        private string _namespaceName = DEFAULT_NAMESPACE;
        private Vector2 _jsonFilesScroll;

        [MenuItem("Tools/策划工具/配置表 C# 结构生成器", priority = 10100)]
        private static void ShowWindow()
        {
            var window = GetWindow<TableClassGenerator>();
            window.titleContent = new GUIContent("Table Class Generator");
            window.minSize = new Vector2(800, 800);
            window.Show();
        }

        private void OnEnable()
        {
            _jsonDirectory = EditorPrefs.GetString(JSON_DIR_KEY, DEFAULT_JSON_DIR);
            _outputDirectory = EditorPrefs.GetString(OUTPUT_DIR_KEY, DEFAULT_OUTPUT_DIR);
            _namespaceName = EditorPrefs.GetString(NAMESPACE_KEY, DEFAULT_NAMESPACE);
            _searchOption = (SearchOption)EditorPrefs.GetInt(SEARCH_OPTION_KEY, (int)SearchOption.AllDirectories);
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(JSON_DIR_KEY, _jsonDirectory);
            EditorPrefs.SetString(OUTPUT_DIR_KEY, _outputDirectory);
            EditorPrefs.SetString(NAMESPACE_KEY, _namespaceName);
            EditorPrefs.SetInt(SEARCH_OPTION_KEY, (int)_searchOption);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16, alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Table Class Generator", titleStyle);

            EditorGUILayout.Space(10);
            DrawHorizontalLine();
            EditorGUILayout.Space(10);

            GUILayout.Label("Base", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                _jsonDirectory = EditorGUILayout.TextField("JSON Directory:", _jsonDirectory);
                if (GUILayout.Button("...", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("Choose JSON Directory", _jsonDirectory, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        _jsonDirectory = selectedPath;
                    }
                }

                _jsonDirectory = NormalizePath(_jsonDirectory);
            }

            EditorGUILayout.Space(5);

            _searchOption = (SearchOption)EditorGUILayout.EnumPopup("Searching Options:", _searchOption);

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                _outputDirectory = EditorGUILayout.TextField("Output Directory:", _outputDirectory);
                if (GUILayout.Button("...", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel(
                        "Choose Output Directory",
                        _outputDirectory,
                        ""
                    );
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        _outputDirectory = selectedPath;
                    }
                }

                _outputDirectory = NormalizePath(_outputDirectory);
            }

            EditorGUILayout.Space(5);

            _namespaceName = EditorGUILayout.TextField("Target Namespace:", _namespaceName);

            EditorGUILayout.Space(10);

            bool jsonDirExists = Directory.Exists(_jsonDirectory);
            if (!jsonDirExists)
            {
                EditorGUILayout.HelpBox($"JSON directory not exists: {_jsonDirectory}", MessageType.Warning);
            }
            else
            {
                string[] jsonFiles = Directory.GetFiles(_jsonDirectory, "*.json", _searchOption);
                string searchInfo = _searchOption == SearchOption.TopDirectoryOnly ? "(Top only)" : "(Recursive)";
                EditorGUILayout.HelpBox($"Found {jsonFiles.Length} JSON files {searchInfo}", MessageType.Info);
                EditorGUILayout.Space(3);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("JSON Files", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    float listMaxHeight = Mathf.Min(
                        220f,
                        (jsonFiles.Length * (EditorGUIUtility.singleLineHeight + 2)) + 8f
                    );
                    using (var scroll = new EditorGUILayout.ScrollViewScope(
                               _jsonFilesScroll,
                               GUILayout.Height(listMaxHeight)
                           ))
                    {
                        _jsonFilesScroll = scroll.scrollPosition;
                        foreach (var f in jsonFiles)
                        {
                            GUILayout.Label(Path.GetFileName(f));
                            EditorGUILayout.Space(3);
                        }
                    }
                }
            }

            bool namespaceNameValid = !string.IsNullOrWhiteSpace(_namespaceName);
            if (!namespaceNameValid)
            {
                EditorGUILayout.HelpBox("Classname cannot be null or white space", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            GUI.enabled = jsonDirExists && namespaceNameValid;
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("Generate", GUILayout.Height(35)))
            {
                Debug.Log(
                    $"[TableClassGenerator] Generated: JSON Directory = {_jsonDirectory}, Output Directory = {_outputDirectory}, Namespace = {_namespaceName}"
                );
                GenerateClasses();
            }
            GUI.backgroundColor = Color.white;

            GUI.enabled = true;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reset"))
            {
                _jsonDirectory = DEFAULT_JSON_DIR;
                _outputDirectory = DEFAULT_OUTPUT_DIR;
                _namespaceName = DEFAULT_NAMESPACE;
                _searchOption = SearchOption.AllDirectories;
            }
        }

        private void GenerateClasses()
        {
            if (!Directory.Exists(_jsonDirectory))
            {
                Debug.LogError($"[TableClassGenerator] JSON Directory not exists: {_jsonDirectory}");
                return;
            }

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            string[] files = Directory.GetFiles(_jsonDirectory, "*.json", _searchOption);

            if (files.Length == 0)
            {
                Debug.LogWarning(
                    $"[TableClassGenerator] No JSON files found in {_jsonDirectory} (Searching Options: {_searchOption})"
                );
                return;
            }

            int successCount = 0;
            float progressStep = 1.0f / files.Length;

            EditorUtility.DisplayProgressBar("Generating", "Start generating...", 0f);

            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    float progress = i * progressStep;
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    EditorUtility.DisplayProgressBar(
                        "Generating",
                        $"Handle files: {fileName} ({i + 1}/{files.Length})",
                        progress
                    );

                    string jsonContent = File.ReadAllText(file);
                    try
                    {
                        var rootArr = JToken.Parse(jsonContent) as JArray;

                        if (rootArr == null)
                        {
                            Debug.LogError(
                                $"[TableClassGenerator] Failed to parse JSON, root element must be array: {file}"
                            );
                            continue;
                        }

                        if (rootArr.Count == 0 || rootArr[0].Type != JTokenType.Object)
                        {
                            Debug.LogError(
                                $"[TableClassGenerator] Failed to parse JSON, no element in root array: {file}"
                            );
                            continue;
                        }

                        var sampleObj = rootArr[0] as JObject;
                        if (sampleObj == null)
                        {
                            Debug.LogError($"[TableClassGenerator] Failed to parse JSON, format error: {file}");
                            continue;
                        }

                        string className = GameUtils.ToPascalCase(fileName);
                        string code = GenerateClassCode(file, className, sampleObj);

                        string outputPath = Path.Combine(_outputDirectory, $"Config{className}.cs");
                        File.WriteAllText(outputPath, code, Encoding.UTF8);

                        Debug.Log($"[TableClassGenerator] Generated: {className} -> {outputPath}");
                        successCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[TableClassGenerator] Failed to parse JSON: {file}\n{ex}");
                    }
                }

                EditorUtility.DisplayProgressBar("Generating", "Refresh AssetDatabase...", 1.0f);
                AssetDatabase.Refresh();

                Debug.Log($"[TableClassGenerator] Done. Generated {successCount}/{files.Length} class files.");
                EditorUtility.DisplayDialog(
                    "Generate Done",
                    $"Generated {successCount}/{files.Length} class files\nOutput directory: {_outputDirectory}",
                    "OK"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TableClassGenerator] Failed to generate table class: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log("[TableClassGenerator] Generate done.");
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // DEFAULT will be converted to Application.dataPath
            if (path.StartsWith("DEFAULT:"))
                path = Path.Combine(Application.dataPath, path.Substring("DEFAULT:".Length));

            path = path.Replace('\\', '/');

            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var stack = new Stack<string>();

            foreach (var seg in segments)
            {
                if (seg == ".") continue; // Remove "/."
                if (seg == "..")          // Remove "/.." and the segment before it
                {
                    if (stack.Count > 0) stack.Pop();
                    continue;
                }

                stack.Push(seg);
            }

            var arr = stack.Reverse().ToArray();
            string prefix = path.StartsWith("/") ? "/" : "";
            return prefix + string.Join("/", arr);
        }

        private string GenerateClassCode(string file, string className, JObject jsonObj)
        {
            var propertyDefs = new List<string>();

            foreach (var property in jsonObj.Properties())
            {
                string propertyType = InferTypeName(property.Value);
                string propertyName = GameUtils.ToPascalCase(property.Name);

                propertyDefs.Add($"[JsonProperty(\"{property.Name}\")]");
                propertyDefs.Add($"public {propertyType} {propertyName} {{ get; set; }}");
                propertyDefs.Add(string.Empty);
            }

            propertyDefs.RemoveAt(propertyDefs.Count - 1);

            var sb = new StringBuilder();

            sb.AppendLine("/// ------------------------------------------------------------------------------");
            sb.AppendLine("/// <auto-generated>");
            sb.AppendLine("/// This file is generated by TableClassGenerator. DO NOT EDIT IT.");
            sb.AppendLine($"/// Source: {Path.GetFileName(file)}");
            sb.AppendLine("/// </auto-generated>");
            sb.AppendLine("/// ------------------------------------------------------------------------------");
            sb.AppendLine();
            sb.AppendLine("using EdenFramework.Core;");
            sb.AppendLine("using Newtonsoft.Json;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {_namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    [System.Serializable]");
            sb.AppendLine($"    public class {className} : ITableConfig");
            sb.AppendLine("    {");
            sb.AppendLine($"        {string.Join("\n        ", propertyDefs)}");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [System.Serializable]");
            sb.AppendLine($"    public class Table{className}");
            sb.AppendLine("    {");
            sb.AppendLine($"        public Dictionary<int, Config{className}> Configs;");
            sb.AppendLine();
            sb.AppendLine($"        public Config{className} GetConfigById(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            return Configs.TryGetValue(id, out var config) ? config : null;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private string InferTypeName(JToken value)
        {
            return value.Type switch
            {
                JTokenType.Integer => "int",
                JTokenType.Float => "float",
                JTokenType.Boolean => "bool",
                JTokenType.String => "string",
                _ => "string"
            };
        }
    }
}