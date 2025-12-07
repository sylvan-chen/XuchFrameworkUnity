using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DigiEden.Editor
{
    /// <summary>
    /// 策划工具 - 配表数据类生成工具
    /// </summary>
    public class TableClassGenerator : EditorWindow
    {
        private const string DEFAULT_JSON_DIR = "DEFAULT:./Res/tables";
        private const string DEFAULT_OUTPUT_DIR = "DEFAULT:./MonkeyLike/TableConfigs";
        private const string DEFAULT_NAMESPACE = "DigiEden.Table";

        private const string JSON_DIR_KEY = "ConfigGenerator_JsonDir";
        private const string OUTPUT_DIR_KEY = "ConfigGenerator_OutputDir";
        private const string NAMESPACE_KEY = "ConfigGenerator_Namespace";
        private const string SEARCH_OPTION_KEY = "ConfigGenerator_SearchOption";

        private SearchOption _searchOption = SearchOption.AllDirectories;
        private string _jsonDirectory = DEFAULT_JSON_DIR;
        private string _outputDirectory = DEFAULT_OUTPUT_DIR;
        private string _namespaceName = DEFAULT_NAMESPACE;
        private Vector2 _jsonFilesScroll;

        [MenuItem("Tools/策划工具/配表数据类生成工具")]
        private static void ShowWindow()
        {
            var window = GetWindow<TableClassGenerator>();
            window.titleContent = new GUIContent("配表数据类生成工具");
            window.minSize = new Vector2(800, 800);
            window.Show();
        }

        private void OnEnable()
        {
            // 加载设置
            _jsonDirectory = EditorPrefs.GetString(JSON_DIR_KEY, DEFAULT_JSON_DIR);
            _outputDirectory = EditorPrefs.GetString(OUTPUT_DIR_KEY, DEFAULT_OUTPUT_DIR);
            _namespaceName = EditorPrefs.GetString(NAMESPACE_KEY, DEFAULT_NAMESPACE);
            _searchOption = (SearchOption)EditorPrefs.GetInt(SEARCH_OPTION_KEY, (int)SearchOption.AllDirectories);
        }

        private void OnDisable()
        {
            // 保存设置
            EditorPrefs.SetString(JSON_DIR_KEY, _jsonDirectory);
            EditorPrefs.SetString(OUTPUT_DIR_KEY, _outputDirectory);
            EditorPrefs.SetString(NAMESPACE_KEY, _namespaceName);
            EditorPrefs.SetInt(SEARCH_OPTION_KEY, (int)_searchOption);
        }

        void OnGUI()
        {
            GUILayout.Label("基本设置", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                _jsonDirectory = EditorGUILayout.TextField("JSON 目录:", _jsonDirectory);
                if (GUILayout.Button("浏览", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("选择 JSON 目录", _jsonDirectory, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        _jsonDirectory = selectedPath;
                    }
                }

                _jsonDirectory = NormalizePath(_jsonDirectory);
            }

            EditorGUILayout.Space(5);

            _searchOption = (SearchOption)EditorGUILayout.EnumPopup("搜索选项:", _searchOption);

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                _outputDirectory = EditorGUILayout.TextField("输出目录:", _outputDirectory);
                if (GUILayout.Button("浏览", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("选择输出目录", _outputDirectory, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        _outputDirectory = selectedPath;
                    }
                }

                _outputDirectory = NormalizePath(_outputDirectory);
            }

            EditorGUILayout.Space(5);

            _namespaceName = EditorGUILayout.TextField("生成类的命名空间:", _namespaceName);

            EditorGUILayout.Space(10);

            // 信息检查
            bool jsonDirExists = Directory.Exists(_jsonDirectory);
            if (!jsonDirExists)
            {
                EditorGUILayout.HelpBox($"JSON 目录不存在: {_jsonDirectory}", MessageType.Warning);
            }
            else
            {
                string[] jsonFiles = Directory.GetFiles(_jsonDirectory, "*.json", _searchOption);
                string searchInfo = _searchOption == SearchOption.TopDirectoryOnly ? "（仅顶层目录）" : "（包含子目录）";
                EditorGUILayout.HelpBox($"找到 {jsonFiles.Length} 个 JSON 配置文件 {searchInfo}", MessageType.Info);
                EditorGUILayout.Space(3);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("JSON 文件列表", EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    float listMaxHeight = Mathf.Min(220f, (jsonFiles.Length * (EditorGUIUtility.singleLineHeight + 2)) + 8f);
                    using (var scroll = new EditorGUILayout.ScrollViewScope(_jsonFilesScroll, GUILayout.Height(listMaxHeight)))
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
                EditorGUILayout.HelpBox("命名空间不能为空", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            GUI.enabled = jsonDirExists && namespaceNameValid;
            if (GUILayout.Button("生成数据类", GUILayout.Height(35)))
            {
                Debug.Log($"[TableClassGenerator] 生成数据类: JSON 目录={_jsonDirectory}, 输出目录={_outputDirectory}, 命名空间={_namespaceName}");
                GenerateClasses();
            }

            GUI.enabled = true;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("重置为默认设置"))
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
                Debug.LogError("[TableClassGenerator] JSON 目录不存在: " + _jsonDirectory);
                return;
            }

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            string[] files = Directory.GetFiles(_jsonDirectory, "*.json", _searchOption);

            if (files.Length == 0)
            {
                Debug.LogWarning($"[XFramework] [TableClassGenerator] 在目录 {_jsonDirectory} 中未找到 JSON 文件（搜索选项: {_searchOption}）");
                return;
            }

            int successCount = 0;
            float progressStep = 1.0f / files.Length;

            EditorUtility.DisplayProgressBar("生成配表数据类", "开始生成...", 0f);

            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    float progress = i * progressStep;
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    EditorUtility.DisplayProgressBar("生成配表数据类", $"处理文件: {fileName} ({i + 1}/{files.Length})", progress);

                    string jsonContent = File.ReadAllText(file);
                    try
                    {
                        var rootArr = JToken.Parse(jsonContent) as JArray;

                        if (rootArr == null)
                        {
                            Debug.LogError($"[TableClassGenerator] 解析 JSON 文件失败，根节点不是数组: {file}");
                            continue;
                        }

                        if (rootArr.Count == 0 || rootArr[0].Type != JTokenType.Object)
                        {
                            Debug.LogError($"[TableClassGenerator] JSON 跟数组缺少对象元素: {file}");
                            continue;
                        }

                        var sampleObj = rootArr[0] as JObject;
                        if (sampleObj == null)
                        {
                            Debug.LogError($"[TableClassGenerator] JSON 文件格式不正确，无法解析为对象: {file}");
                            continue;
                        }

                        // var rootObj = JObject.Parse(jsonContent);
                        //
                        // var configsContainer = rootObj["configs"] as JObject;
                        // if (configsContainer == null || !configsContainer.Properties().Any())
                        // {
                        //     Debug.LogError($"[TableClassGenerator] JSON 文件未找到有效的配置数据容器 (configs): {file}");
                        //     continue;
                        // }
                        //
                        // var firstProperty = configsContainer.Properties().FirstOrDefault();
                        // if (firstProperty == null || firstProperty.Value.Type != JTokenType.Object)
                        // {
                        //     Debug.LogError($"[TableClassGenerator] JSON 文件未读取到有效对象: {file}");
                        //     continue;
                        // }
                        //
                        // var sampleObj = firstProperty.Value as JObject;
                        // if (sampleObj == null)
                        // {
                        //     Debug.LogError($"[TableClassGenerator] JSON 文件格式不正确，无法解析为对象: {file}");
                        //     continue;
                        // }

                        string className = ToPascalCase(fileName);
                        string code = GenerateClassCode(file, className, sampleObj);

                        string outputPath = Path.Combine(_outputDirectory, $"Config{className}.cs");
                        File.WriteAllText(outputPath, code, Encoding.UTF8);

                        Debug.Log($"[TableClassGenerator] 生成配表数据类: {className} -> {outputPath}");
                        successCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[TableClassGenerator] 解析 JSON 文件失败: {file}\n{ex}");
                    }
                }

                EditorUtility.DisplayProgressBar("生成配表数据类", "刷新资源数据库...", 1.0f);
                AssetDatabase.Refresh();

                Debug.Log($"[XFramework] [TableClassGenerator] 数据类生成完成，成功生成 {successCount}/{files.Length} 个类文件");
                EditorUtility.DisplayDialog("生成完成", $"成功生成 {successCount}/{files.Length} 个数据类\n输出目录: {_outputDirectory}", "确定");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TableClassGenerator] 生成数据类失败: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log("[TableClassGenerator] 数据类生成结束");
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // DEFAULT 路径自动转换为绝对路径
            if (path.StartsWith("DEFAULT:"))
                path = Path.Combine(Application.dataPath, path.Substring("DEFAULT:".Length));

            path = path.Replace('\\', '/');

            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var stack = new Stack<string>();

            foreach (var seg in segments)
            {
                if (seg == ".")
                    continue;    // 去掉 "/."
                if (seg == "..") // 去掉 "/.." 以及它前面的一个段
                {
                    if (stack.Count > 0)
                        stack.Pop();
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
                string propertyName = ToPascalCase(property.Name);

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
            sb.AppendLine("using DigiEden.Framework;");
            sb.AppendLine("using Newtonsoft.Json;");
            // sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {_namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    [System.Serializable]");
            sb.AppendLine($"    public class Config{className} : ITableConfig");
            sb.AppendLine("    {");
            sb.AppendLine($"        {string.Join("\n        ", propertyDefs)}");
            sb.AppendLine("    }");
            // sb.AppendLine();
            // sb.AppendLine("    [System.Serializable]");
            // sb.AppendLine($"    public class Table{className}");
            // sb.AppendLine("    {");
            // sb.AppendLine($"        public Dictionary<int, Config{className}> Configs;");
            // sb.AppendLine();
            // sb.AppendLine($"        public Config{className} GetConfigById(int id)");
            // sb.AppendLine("        {");
            // sb.AppendLine("            return Configs.TryGetValue(id, out var config) ? config : null;");
            // sb.AppendLine("        }");
            // sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
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

        private string ToPascalCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            str = str.Replace("_", " ").Replace("-", " ");
            var words = str.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }

            return string.Join("", words);
        }
    }
}