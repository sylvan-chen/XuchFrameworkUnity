using System.IO;
using UnityEditor;
using UnityEngine;

namespace XuchFramework.Editor
{
    public class BetterRename
    {
        [MenuItem("Tools/Common Tools/Rename to Lowercase Recursively", priority = 10000)]
        private static void RenameToLowerCase()
        {
            if (Selection.activeObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Select a folder first", "OK");
                return;
            }

            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorUtility.DisplayDialog("Error", "Failed to get path of select object", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder (not file)", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Rename to lowercase",
                    $"Renaming all folder and files to lowercase recursively for '{selectedPath}', continue?",
                    "OK",
                    "Cancel"))
            {
                return;
            }

            try
            {
                RenameDirectory(selectedPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Done", "Rename finish", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Rename failed: {ex.Message}", "OK");
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
                        Debug.LogError($"[BetterRename] Failed to rename directory '{directory}': {error}");
                    }
                }
            }

            foreach (string file in Directory.GetFiles(path))
            {
                // Skip .meta files
                if (file.EndsWith(".meta"))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file);
                string newName = ConvertToSnakeCase(fileName) + extension;

                if (newName != Path.GetFileName(file))
                {
                    string error = AssetDatabase.RenameAsset(file, newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[BetterRename] Failed to rename file '{file}': {error}");
                    }
                }
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
            if (string.IsNullOrEmpty(name))
                return name;

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (c is ' ' or '_' or '-')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                        sb.Append('_');
                    continue;
                }

                if (char.IsUpper(c))
                {
                    // Condition of adding underscore before uppercase letter:
                    // 1. Not the first character
                    // 2. Previous character is not underscore
                    // 3. Previous character is lowercase, or next character is lowercase (handle consecutive uppercase like 'XMLParser')
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
        [MenuItem("Tools/Common Tools/Rename to Lowercase Recursively", true, priority = 10000)]
        private static bool ValidateRenameToLowerCase()
        {
            if (Selection.activeObject == null)
                return false;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }
    }
}