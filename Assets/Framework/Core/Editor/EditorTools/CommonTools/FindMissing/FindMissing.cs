using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindMissingPrefabs : EditorWindow
{
    [MenuItem("Tools/通用工具/Find Missing Prefabs", priority = 10003)]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindMissingPrefabs));
    }

    public void OnGUI()
    {
        if (GUILayout.Button("查找当前场景中丢失的预制体引用"))
        {
            FindMissing();
        }
    }

    private static void FindMissing()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true); // true 包含隐藏物体
        List<GameObject> missingList = new List<GameObject>();

        foreach (var go in allObjects)
        {
            // 检查 Prefab 状态是否为 Missing
            if (PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.MissingAsset)
            {
                missingList.Add(go);
                Debug.LogError($"Found Missing Prefab: {go.name}", go);
            }
        }

        if (missingList.Count > 0)
        {
            Selection.objects = missingList.ToArray(); // 自动选中所有问题物体
            Debug.Log($"共找到 {missingList.Count} 个丢失引用的物体。");
        }
        else
        {
            Debug.Log("未发现丢失 Prefab 引用的物体。");
        }
    }
}

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindMissingScripts));
    }

    public void OnGUI()
    {
        if (GUILayout.Button("在当前场景中查找丢失脚本的物体"))
        {
            FindInCurrentScene();
        }

        if (GUILayout.Button("在 Project 中查找所有预制体的丢失脚本"))
        {
            FindInProject();
        }
    }

    private static void FindInCurrentScene()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        List<GameObject> problemObjects = new List<GameObject>();
        int count = 0;

        foreach (var go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                // 核心逻辑：如果组件是 null，说明这就是个 Missing Script
                if (components[i] == null)
                {
                    problemObjects.Add(go);
                    Debug.LogError($"[Scene] 物体 '{go.name}' 身上第 {i} 个组件脚本丢失！", go);
                    count++;
                }
            }
        }

        if (problemObjects.Count > 0) Selection.objects = problemObjects.ToArray();
        Debug.Log($"场景查找结束，共发现 {count} 个丢失脚本的位置。");
    }

    private static void FindInProject()
    {
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        int total = allPrefabs.Length;

        // 记录开始时间
        double startTime = EditorApplication.timeSinceStartup;

        try
        {
            for (int i = 0; i < total; i++)
            {
                string guid = allPrefabs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // 显示进度条 (每处理 20 个更新一次 UI，避免拖慢速度)
                // 参数：标题, 当前处理的文件名, 进度(0.0 - 1.0)
                if (i % 20 == 0 || i == total - 1)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "正在扫描 Missing Scripts...",
                        $"[{i}/{total}] 正在检查: {System.IO.Path.GetFileName(path)}",
                        (float)i / total
                    );

                    if (cancel)
                    {
                        Debug.LogWarning("扫描已手动取消！");
                        break;
                    }
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // 预防某些极其特殊的损坏导致 Load 失败
                if (prefab == null) continue;

                // includeInactive = true 确保连隐藏子物体的脚本也查到
                Component[] components = prefab.GetComponentsInChildren<Component>(true);
                foreach (var c in components)
                {
                    if (c == null)
                    {
                        Debug.LogError($"[Project] 预制体可能有问题: {path}", prefab);
                        count++;
                        break; // 一个预制体只要发现一个丢失，就记录并跳过，节省时间
                    }
                }
            }
        }
        finally
        {
            // 无论是否报错或取消，最后必须清除进度条，否则它会一直卡在界面上
            EditorUtility.ClearProgressBar();
        }

        double duration = EditorApplication.timeSinceStartup - startTime;
        Debug.Log($"Project 查找结束。耗时: {duration:F2} 秒。共发现 {count} 个有问题的预制体。");
    }
}
