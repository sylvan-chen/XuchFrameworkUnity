using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor
{
    public class BetterRename
    {
        [MenuItem("Tools/通用工具/递归小写重命名", priority = 10000)]
        private static void RenameToLowerCase()
        {
            if (Selection.activeObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择一个目录", "确认");
                return;
            }

            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorUtility.DisplayDialog("错误", "获取目录路径失败", "确认");
                return;
            }

            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                EditorUtility.DisplayDialog("错误", "请选择一个目录而不是文件", "确认");
                return;
            }

            if (!EditorUtility.DisplayDialog("递归小写重命名", $"递归重命名所有目录和文件为小写 ('{selectedPath}')，确认?", "确认", "取消"))
            {
                return;
            }

            try
            {
                RenameDirectory(selectedPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("完成", "重命名完成", "确认");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"重命名失败：{ex.Message}", "确认");
                Debug.LogError($"[BetterRename] Rename failed: {ex}");
            }
        }

        private static void RenameDirectory(string path)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                RenameDirectory(directory);

                string dirName = Path.GetFileName(directory);
                string newName = ConvertToSnakeCase(dirName);

                if (newName != dirName)
                {
                    string error = AssetDatabase.RenameAsset(directory, newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[BetterRename] 重命名目录失败 '{directory}': {error}");
                    }
                }
            }

            foreach (string file in Directory.GetFiles(path))
            {
                // Skip .meta files
                if (file.EndsWith(".meta")) continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file);
                string newName = ConvertToSnakeCase(fileName) + extension;

                if (newName != Path.GetFileName(file))
                {
                    string error = AssetDatabase.RenameAsset(file, newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[BetterRename] 重命名文件失败 '{file}': {error}");
                    }
                }
            }
        }

        [MenuItem("GameObject/Rename Recursive (snake_case)", false, 0)]
        public static void RenameSelectedToSnakeCase()
        {
            // 获取当前选中的所有 GameObject
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("请先在 Hierarchy 中选择要重命名的节点！");
                return;
            }

            // 遍历所有选中的根节点进行递归重命名
            foreach (GameObject go in selectedObjects)
            {
                RenameRecursive(go.transform);
            }

            Debug.Log("重命名完成！如果结果不满意，可以按 Ctrl+Z (Cmd+Z) 撤销。");
        }

        private static void RenameRecursive(Transform current)
        {
            // 注册 Undo 操作，以便支持 Ctrl+Z 撤销
            Undo.RecordObject(current.gameObject, "Rename to snake_case");

            // 转换名称
            current.gameObject.name = ConvertToSnakeCase(current.gameObject.name);

            // 遍历并递归所有子节点
            foreach (Transform child in current)
            {
                RenameRecursive(child);
            }
        }

        /// <summary>
        /// Convert PascalCase or camelCase to snake_case
        /// e.g. "MyClassName" -> "my_class_name"
        ///     "XMLParser" -> "xml_parser"
        ///     "getHTTPResponse" -> "get_http_response"
        /// </summary>
        private static string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (c is ' ' or '_' or '-')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
                    continue;
                }

                if (char.IsUpper(c))
                {
                    // 在大写字符前加下划线的条件：
                    // 1. 不是第一个字符
                    // 2. 上一个字符不是下划线
                    // 3. 上一个或者下一个字符是小写（检查下一个字符的目的是处理 'XMLParser' 这种情况）
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    {
                        bool prevIsLower = i > 0 && char.IsLower(name[i - 1]);
                        bool nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                        if (prevIsLower || nextIsLower)
                        {
                            sb.Append('_');
                        }
                    }
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        // Verify menu item is valid only when a folder is selected
        [MenuItem("Tools/通用工具/递归小写重命名", true, priority = 10000)]
        private static bool ValidateRenameToLowerCase()
        {
            if (Selection.activeObject == null) return false;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }
    }
}