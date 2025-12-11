using UnityEngine;
using UnityEditor;
using System.IO;

namespace Xuch.Editor
{
    public class GameTools
    {
        [MenuItem("Tools/GameTools/Rename To Lowercase")]
        static void RenameToLowerCase()
        {
            // 1. 检查是否选中了对象
            if (Selection.activeObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选中一个文件夹", "确定");
                return;
            }

            // 2. 获取选中资源的路径
            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            // 3. 检查路径是否有效
            if (string.IsNullOrEmpty(selectedPath))
            {
                EditorUtility.DisplayDialog("错误", "无法获取选中资源的路径", "确定");
                return;
            }

            // 4. 检查是否是文件夹
            if (!AssetDatabase.IsValidFolder(selectedPath))
            {
                EditorUtility.DisplayDialog("错误", "请选中一个文件夹，而不是文件", "确定");
                return;
            }

            // 5. 确认操作
            if (!EditorUtility.DisplayDialog(
                "重命名确认",
                $"将重命名文件夹 '{selectedPath}' 及其所有子文件和文件夹为小写，是否继续？",
                "确定",
                "取消"))
            {
                return;
            }

            // 6. 执行重命名
            try
            {
                RenameDirectory(selectedPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("成功", "重命名完成", "确定");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"重命名失败: {ex.Message}", "确定");
                Debug.LogError($"[GameTools] Rename failed: {ex}");
            }
        }

        static void RenameDirectory(string path)
        {
            // 递归处理子文件夹
            foreach (string directory in Directory.GetDirectories(path))
            {
                RenameDirectory(directory);
                
                string dirName = Path.GetFileName(directory);
                string newName = dirName.ToLowerInvariant().Replace(" ", "_");
                
                if (newName != dirName)
                {
                    string error = AssetDatabase.RenameAsset(directory, newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[GameTools] Failed to rename directory '{directory}': {error}");
                    }
                }
            }

            // 处理文件
            foreach (string file in Directory.GetFiles(path))
            {
                // 跳过 .meta 文件
                if (file.EndsWith(".meta"))
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file);
                string newName = fileName.ToLowerInvariant().Replace(" ", "_") + extension;

                if (newName != Path.GetFileName(file))
                {
                    string error = AssetDatabase.RenameAsset(file, newName);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[GameTools] Failed to rename file '{file}': {error}");
                    }
                }
            }
        }

        // 添加菜单验证（灰色显示/隐藏菜单项）
        [MenuItem("Tools/GameTools/Rename To Lowercase", true)]
        static bool ValidateRenameToLowerCase()
        {
            // 只有选中文件夹时才启用菜单
            if (Selection.activeObject == null)
                return false;

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);
        }
    }
}
