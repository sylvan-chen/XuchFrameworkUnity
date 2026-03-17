using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Framework.Utils;

namespace Framework.Editor
{
    /// <summary>
    /// 拆分所有子网格之间共享的部分，使所有子网格的顶点仅自己独占
    /// - 方便应用顶点色（两个子网格共有的顶点，只能单一颜色，做不到混合）
    /// </summary>
    public class VertexSplitter : EditorWindow
    {
        private const string TITLE = "子网格顶点拆分器";

        private List<string> _prefabDirs = new() { "Assets/Res/prefabs" };

        private string _savePath = "Assets/Res/splited_meshes/";

        private bool _isAutoApply = false;
        private bool _skipExist = false;
        private bool _showClearSettings = false;

        private readonly List<GameObject> _prefabs = new();
        private bool _isProcessing = false;
        private Vector2 _scrollPosition;
        private Vector2 _pathScrollPosition;

        private bool _checkNormal = true;
        private bool _checkBoneWeight = true;
        private bool _checkVertexColorGray = false;

        [MenuItem("Tools/美术工具/子网格顶点拆分器", priority = 10050)]
        public static void ShowWindow()
        {
            var window = GetWindow<VertexSplitter>(TITLE);
            window.minSize = new Vector2(600, 800);
            // window.CollectPrefabs();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(TITLE, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Target prefabs directory:");

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_pathScrollPosition, GUILayout.Height(150)))
            {
                _pathScrollPosition = scrollView.scrollPosition;

                for (int i = 0; i < _prefabDirs.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _prefabDirs[i] = EditorGUILayout.TextField($"Directory {i + 1}:", _prefabDirs[i]);

                        if (GUILayout.Button("...", GUILayout.Width(50)))
                        {
                            string selectedPath = EditorUtility.OpenFolderPanel(
                                "Choose prefab directory",
                                _prefabDirs[i],
                                ""
                            );
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                // Convert to relative path
                                string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                                _prefabDirs[i] = relativePath;
                            }
                        }

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            _prefabDirs.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add"))
                {
                    _prefabDirs.Add("Assets/");
                }
                if (GUILayout.Button("Reset"))
                {
                    _prefabDirs = new List<string>()
                    {
                        "Assets/Res/prefabs/avatar/top",
                        "Assets/Res/prefabs/avatar/bottom",
                        "Assets/Res/prefabs/avatar/suit",
                        "Assets/Res/prefabs/avatar/shoesl",
                        "Assets/Res/prefabs/avatar/shoesr",
                    };
                }
            }

            if (GUILayout.Button("Collect"))
            {
                CollectPrefabs();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                _savePath = EditorGUILayout.TextField("Save Path:", _savePath);

                if (GUILayout.Button("...", GUILayout.Width(50)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("Choose Save Path", _savePath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        if (!relativePath.EndsWith("/"))
                        {
                            relativePath += "/";
                        }
                        _savePath = relativePath;
                    }
                }
            }

            EditorGUILayout.Space();

            _showClearSettings = EditorGUILayout.Foldout(_showClearSettings, "Mesh Clean", EditorStyles.boldFont);
            if (_showClearSettings)
            {
                EditorGUI.indentLevel++;

                _checkNormal = EditorGUILayout.Toggle("Normal check", _checkNormal);
                _checkBoneWeight = EditorGUILayout.Toggle("Bone weight check", _checkBoneWeight);
                _checkVertexColorGray = EditorGUILayout.Toggle("Ensure vertex color gray", _checkVertexColorGray);

                if (GUILayout.Button("Check and Clean"))
                {
                    ValidateMeshAssets();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Found {_prefabs.Count} prefabs.");

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.Height(250)))
            {
                _scrollPosition = scrollView.scrollPosition;

                foreach (var prefab in _prefabs)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);

                    var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    EditorGUILayout.LabelField($"({renderers.Length} renderers)", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            _isAutoApply = EditorGUILayout.Toggle("Apply to prefab", _isAutoApply);

            _skipExist = EditorGUILayout.Toggle("Skip exists", _skipExist);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(_isProcessing || _prefabs.Count == 0))
            {
                if (GUILayout.Button("Start", GUILayout.Height(30)))
                {
                    ProcessAllPrefabs();
                }
            }

            if (_isProcessing)
            {
                EditorGUILayout.HelpBox("Processing, please wait...", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Revert All Meshes", GUILayout.Height(30)))
            {
                RevertAllMeshes();
            }
        }

        private void CollectPrefabs()
        {
            _prefabs.Clear();

            foreach (var dir in _prefabDirs)
            {
                if (!Directory.Exists(dir))
                {
                    Debug.LogWarning($"Path not found: {dir}");
                    return;
                }

                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { dir });

                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null)
                    {
                        var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                        if (renderers.Length > 0)
                        {
                            _prefabs.Add(prefab);
                        }
                    }
                }

                Debug.Log($"Found {_prefabs.Count} prefabs in '{dir}'");
            }
        }

        private void ProcessAllPrefabs()
        {
            _isProcessing = true;
            int processedCount = 0;
            int totalCount = _prefabs.Count;

            try
            {
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                    AssetDatabase.Refresh();
                }

                foreach (var prefab in _prefabs)
                {
                    EditorUtility.DisplayProgressBar(
                        "Splitting Vertices",
                        $"Processing: {prefab.name} ({processedCount + 1}/{totalCount})",
                        (float)processedCount / totalCount
                    );

                    ProcessPrefab(prefab);
                    processedCount++;
                }

                EditorUtility.DisplayDialog("Done", $"Done!\nProcessed {processedCount}/{totalCount} prefabs.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to split vertices: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isProcessing = false;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ProcessPrefab(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                {
                    Debug.LogWarning($"No shared vertices in renderer for {prefab.name}, skipping.");
                    continue;
                }

                var splitedMesh = SplitMesh(renderer.sharedMesh);
                if (splitedMesh != null)
                {
                    SaveMeshAsAsset(splitedMesh);

                    if (_isAutoApply) renderer.sharedMesh = splitedMesh;

                    Debug.Log(
                        $"Processed: {prefab.name}.{renderer.name}.{renderer.sharedMesh.name} -> {splitedMesh.name}"
                    );
                }
            }
        }

        public Mesh SplitMesh(Mesh originalMesh)
        {
            if (originalMesh == null)
            {
                Debug.LogError("No mesh found on the SkinnedMeshRenderer!");
                return null;
            }

            var newMesh = new Mesh() { name = $"{originalMesh.name}_splited", };

            var originalVertices = originalMesh.vertices;
            var originalNormals = originalMesh.normals;
            var originalTangents = originalMesh.tangents;
            var originalUVs = originalMesh.uv;
            var originalBonesPerVertex = originalMesh.GetBonesPerVertex().ToArray();
            var originalBoneWeights = originalMesh.GetAllBoneWeights().ToArray();
            var originalColors = originalMesh.colors;

            // Analyze vertex usage
            var vertexSubMeshUsage = new Dictionary<int, HashSet<int>>();
            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                var subTriangles = originalMesh.GetTriangles(subMeshIndex);
                Debug.Log($"Analyzing submesh {subMeshIndex}, index count: {subTriangles.Length}");
                if (!vertexSubMeshUsage.ContainsKey(subMeshIndex))
                {
                    vertexSubMeshUsage[subMeshIndex] = new HashSet<int>();
                }
                foreach (var vertexIndex in subTriangles)
                {
                    vertexSubMeshUsage[subMeshIndex].Add(vertexIndex);
                }
            }

            Debug.Log(
                $"Submesh count: {originalMesh.subMeshCount}, vertex count per submesh: {string.Join(", ", vertexSubMeshUsage.Select(kvp => kvp.Value.Count))}"
            );

            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newTangents = new List<Vector4>();
            var newUVs = new List<Vector2>();
            var newColors = new List<Color>();
            var newBonesPerVertex = new List<byte>();
            var newBoneWeights = new List<BoneWeight1>();
            var newSubTriangles = new List<int>[originalMesh.subMeshCount];

            var originalIndexToNewIndex = new Dictionary<int, Dictionary<int, int>>();

            // Pre-calculate bone weight start indices
            var boneWeightStartIndex = new int[originalMesh.vertexCount + 1];
            int currentBoneWeightIndex = 0;
            for (int i = 0; i < originalMesh.vertexCount; i++)
            {
                boneWeightStartIndex[i] = currentBoneWeightIndex;
                currentBoneWeightIndex += originalBonesPerVertex[i];
            }
            boneWeightStartIndex[originalMesh.vertexCount] = currentBoneWeightIndex;

            foreach (var kvp in vertexSubMeshUsage)
            {
                int submeshIndex = kvp.Key;
                var usedBySubMeshes = kvp.Value;

                foreach (int originalIndex in usedBySubMeshes)
                {
                    int newIndex = newVertices.Count;

                    newVertices.Add(originalVertices[originalIndex]);

                    if (originalNormals != null && originalNormals.Length > originalIndex)
                        newNormals.Add(originalNormals[originalIndex]);

                    if (originalTangents != null && originalTangents.Length > originalIndex)
                        newTangents.Add(originalTangents[originalIndex]);

                    if (originalUVs != null && originalUVs.Length > originalIndex)
                        newUVs.Add(originalUVs[originalIndex]);

                    if (originalColors != null && originalColors.Length > originalIndex)
                        newColors.Add(originalColors[originalIndex]);
                    else
                        newColors.Add(Color.white);

                    if (!originalIndexToNewIndex.ContainsKey(originalIndex))
                        originalIndexToNewIndex[originalIndex] = new Dictionary<int, int>();
                    originalIndexToNewIndex[originalIndex][submeshIndex] = newIndex;

                    // Process bone info
                    if (originalBonesPerVertex != null && originalBonesPerVertex.Length > originalIndex)
                    {
                        var boneCount = originalBonesPerVertex[originalIndex];

                        newBonesPerVertex.Add(boneCount);

                        // Copy all bone weights for this vertex
                        int startIndex = boneWeightStartIndex[originalIndex];
                        for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
                        {
                            if (startIndex + boneIdx < originalBoneWeights.Length)
                            {
                                newBoneWeights.Add(originalBoneWeights[startIndex + boneIdx]);
                            }
                        }
                    }
                }
            }

            Debug.Log($"Original vertex count: {originalMesh.vertexCount}, new vertex count: {newVertices.Count}");

            // Rebuild triangle indices
            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                newSubTriangles[subMeshIndex] = new List<int>();
                var originalTriangles = originalMesh.GetTriangles(subMeshIndex);

                for (int i = 0; i < originalTriangles.Length; i++)
                {
                    int originalVertexIndex = originalTriangles[i];
                    int newVertexIndex = originalIndexToNewIndex[originalVertexIndex][subMeshIndex];
                    newSubTriangles[subMeshIndex].Add(newVertexIndex);
                }
            }

            newMesh.SetVertices(newVertices);
            if (newNormals.Count > 0) newMesh.SetNormals(newNormals);
            if (newTangents.Count > 0) newMesh.SetTangents(newTangents);
            if (newUVs.Count > 0) newMesh.SetUVs(0, newUVs);

            newMesh.SetColors(newColors);
            newMesh.subMeshCount = originalMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                newMesh.SetTriangles(newSubTriangles[subMeshIndex], subMeshIndex);
            }

            newMesh.bindposes = originalMesh.bindposes;

            var newBonesPerVertexArray = new NativeArray<byte>(newBonesPerVertex.ToArray(), Allocator.Persistent);
            var newBoneWeightsArray = new NativeArray<BoneWeight1>(newBoneWeights.ToArray(), Allocator.Persistent);

            newMesh.SetBoneWeights(newBonesPerVertexArray, newBoneWeightsArray);

            newBonesPerVertexArray.Dispose();
            newBoneWeightsArray.Dispose();

            newMesh.RecalculateBounds();

            return newMesh;
        }

        private void SaveMeshAsAsset(Mesh mesh)
        {
            string fileName = $"{mesh.name}.asset";
            // If fileName exists, add suffix
            int index = 1;
            while (File.Exists(Path.Combine(_savePath, fileName)))
            {
                if (_skipExist) return;

                fileName = $"{mesh.name}_{index++}.asset";
            }
            string fullPath = Path.Combine(_savePath, fileName);

            // Save mesh asset
            AssetDatabase.CreateAsset(mesh, fullPath);
        }

        private void ValidateMeshAssets()
        {
            if (!Directory.Exists(_savePath))
            {
                EditorUtility.DisplayDialog("Error", $"Save path does not exist: {_savePath}", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Cleanup",
                $"Will check and clean up mesh assets in path '{_savePath}'.\n\n" + "Do you want to continue?",
                "OK",
                "Cancel"
            );

            if (!confirmed) return;

            int deletedCount = 0;
            var deletedFiles = new List<string>();
            var fileNameToIssues = new Dictionary<string, List<string>>();

            try
            {
                // Get all assets under _savePath
                string[] meshGuids = AssetDatabase.FindAssets("t:Mesh", new[] { _savePath });
                var totalCount = meshGuids.Length;

                if (totalCount == 0)
                {
                    EditorUtility.DisplayDialog("Done", $"No mesh files found in path '{_savePath}'.", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Checking Mesh Assets", "Checking...", 0f);

                for (int i = 0; i < meshGuids.Length; i++)
                {
                    string guid = meshGuids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(path);
                    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                    EditorUtility.DisplayProgressBar(
                        "Checking Mesh Assets",
                        $"Checking: {Path.GetFileName(path)} ({i + 1}/{totalCount})",
                        (float)i / totalCount
                    );

                    var fileIssues = new List<string>();

                    // Check if mesh is corrupted
                    if (mesh == null)
                    {
                        fileIssues.Add("Mesh corrupted or cannot be loaded");
                        Debug.Log($"Mesh asset corrupted or cannot be loaded: {path}");
                        AssetDatabase.DeleteAsset(path);
                        deletedFiles.Add(fileName);
                        deletedCount++;
                        continue;
                    }

                    bool shouldDelete = false;

                    // Check vertex data integrity
                    if (mesh.vertexCount == 0)
                    {
                        fileIssues.Add("Mesh vertex data is empty");
                        Debug.Log($"Mesh vertex data is empty: {path}");
                        shouldDelete = true;
                    }
                    else if (mesh.triangles == null || mesh.triangles.Length == 0)
                    {
                        fileIssues.Add("Mesh triangle data is empty");
                        Debug.Log($"Mesh triangle data is empty: {path}");
                        shouldDelete = true;
                    }

                    // Check submesh integrity
                    if (!shouldDelete && mesh.subMeshCount > 0)
                    {
                        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                        {
                            var triangles = mesh.GetTriangles(subMeshIndex);
                            if (triangles.Length == 0)
                            {
                                fileIssues.Add($"Submesh {subMeshIndex} has no triangle data");
                                Debug.Log($"Submesh {subMeshIndex} has no triangle data: {path}");
                                shouldDelete = true;
                            }
                            else if (triangles.Length % 3 != 0)
                            {
                                fileIssues.Add($"Submesh {subMeshIndex} triangle index count is not a multiple of 3");
                                Debug.Log(
                                    $"Submesh {subMeshIndex} triangle index count is not a multiple of 3: {path}"
                                );
                                shouldDelete = true;
                            }
                        }
                    }

                    // Check normals
                    if (!shouldDelete && _checkNormal)
                    {
                        Vector3[] normals = mesh.normals;
                        if (normals == null || normals.Length == 0)
                        {
                            fileIssues.Add("Normal data is empty");
                            Debug.Log($"Normal data is empty: {path}");
                            shouldDelete = true;
                        }
                        if (normals != null && normals.Length > 0 && normals.Length != mesh.vertexCount)
                        {
                            fileIssues.Add("Normal count does not match vertex count");
                            Debug.Log($"Normal count does not match vertex count: {path}");
                            shouldDelete = true;
                        }
                    }

                    // Check bone weights
                    if (!shouldDelete && _checkBoneWeight)
                    {
                        BoneWeight[] boneWeights = mesh.boneWeights;
                        if (boneWeights == null || boneWeights.Length == 0)
                        {
                            fileIssues.Add("Bone weights are empty");
                            Debug.Log($"Bone weights are empty: {path}");
                            shouldDelete = true;
                        }
                        if (boneWeights != null && boneWeights.Length > 0)
                        {
                            if (boneWeights.Length != mesh.vertexCount)
                            {
                                fileIssues.Add("Bone weight count does not match vertex count");
                                Debug.Log($"Bone weight count does not match vertex count: {path}");
                                shouldDelete = true;
                            }
                            else
                            {
                                // Check weight sum
                                for (int w = 0; w < boneWeights.Length; w++)
                                {
                                    float weightSum = boneWeights[w].weight0
                                                      + boneWeights[w].weight1
                                                      + boneWeights[w].weight2
                                                      + boneWeights[w].weight3;
                                    if (Mathf.Abs(weightSum - 1.0f) > 0.01f)
                                    {
                                        fileIssues.Add($"Vertex {w} bone weight sum is not 1 (actual: {weightSum:F3})");
                                        Debug.Log(
                                            $"Vertex {w} bone weight sum is not 1 (actual: {weightSum:F3}): {path}"
                                        );
                                        shouldDelete = true;
                                        break; // Only report the first error to avoid too many logs
                                    }
                                }
                            }
                        }
                    }

                    // Check if vertex color is grayscale
                    if (!shouldDelete && _checkVertexColorGray)
                    {
                        Color[] vertexColors = mesh.colors;
                        if (vertexColors != null && vertexColors.Length > 0)
                        {
                            bool allGray = true;
                            foreach (Color color in vertexColors)
                            {
                                float r = color.r;
                                float g = color.g;
                                float b = color.b;
                                if (GameUtils.FloatEquals(r, g)
                                    || GameUtils.FloatEquals(r, b)
                                    || GameUtils.FloatEquals(g, b))
                                {
                                    allGray = false;
                                    break;
                                }
                            }
                            if (!allGray)
                            {
                                fileIssues.Add("Vertex color is not grayscale");
                                Debug.Log($"Vertex color is not grayscale: {path}");
                                shouldDelete = true;
                            }
                        }
                    }

                    // Perform delete or record for fix
                    if (shouldDelete)
                    {
                        AssetDatabase.DeleteAsset(path);
                        deletedFiles.Add(fileName);
                        deletedCount++;
                    }

                    if (fileIssues.Count > 0)
                    {
                        fileNameToIssues[fileName] = fileIssues;
                    }
                }

                AssetDatabase.Refresh();

                // Show results
                ShowValidationResults(totalCount, deletedCount, deletedFiles, fileNameToIssues);

                Debug.Log($"Mesh asset validation completed - Checked: {totalCount}, Deleted: {deletedCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error occurred during mesh asset validation: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error occurred during validation:\n{ex.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ShowValidationResults(
            int totalCount,
            int deletedCount,
            List<string> deletedFiles,
            Dictionary<string, List<string>> fileNameToIssues)
        {
            string resultMessage = $"Check completed!\n\n"
                                   + $"Total checked: {totalCount} mesh files\n"
                                   + $"Deleted files: {deletedCount}\n";

            if (deletedCount > 0)
            {
                resultMessage += $"\nDeleted files:\n";
                foreach (string fileName in deletedFiles)
                {
                    resultMessage += $"• {fileName}\n";
                }
            }

            if (fileNameToIssues.Count > 0)
            {
                resultMessage += "\nFiles with issues:\n";
                foreach (var kvp in fileNameToIssues.Take(5))
                {
                    resultMessage += $"\n{kvp.Key}:\n";
                    foreach (var issue in kvp.Value)
                    {
                        resultMessage += $"  - {issue}\n";
                    }
                }
                if (fileNameToIssues.Count > 5)
                {
                    resultMessage +=
                        $"\n...{fileNameToIssues.Count - 5} more files with issues not shown (see console logs)";
                }
            }

            EditorUtility.DisplayDialog(deletedCount > 0 ? "Cleanup Complete" : "Check Complete", resultMessage, "OK");
        }

        /// <summary>
        /// Revert all clothing meshes to pre-split state
        /// </summary>
        private void RevertAllMeshes()
        {
            if (_prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Notice",
                    "No prefabs collected. Please click the \"Collect\" button first.",
                    "OK"
                );
                return;
            }

            int totalRenderers = 0;
            int affectedPrefabs = 0;

            foreach (var prefab in _prefabs)
            {
                var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (renderers.Length > 0)
                {
                    totalRenderers += renderers.Length;
                    affectedPrefabs++;
                }
            }

            // Confirmation dialog
            bool confirmed = EditorUtility.DisplayDialog(
                "Confirm Revert",
                $"Will revert the following prefab meshes to original state:\n\n"
                + $"• Prefab count: {affectedPrefabs}\n"
                + $"• Renderer count: {totalRenderers}\n\n"
                + "This operation will undo all mesh modifications and restore to pre-split state.\n\n"
                + "Do you want to continue?",
                "Confirm Revert",
                "Cancel"
            );

            if (!confirmed) return;

            int processedPrefabs = 0;
            int processedRenderers = 0;
            int revertedRenderers = 0;
            var revertedPrefabs = new List<string>();

            try
            {
                foreach (var prefab in _prefabs)
                {
                    EditorUtility.DisplayProgressBar(
                        "Reverting Meshes",
                        $"Processing: {prefab.name} ({processedPrefabs + 1}/{affectedPrefabs})",
                        (float)processedPrefabs / affectedPrefabs
                    );

                    var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    bool prefabChanged = false;
                    foreach (var renderer in renderers)
                    {
                        processedRenderers++;

                        var so = new SerializedObject(renderer);
                        var sp = so.FindProperty("m_Mesh");

                        if (sp == null)
                        {
                            Debug.LogWarning($"Mesh property not found: {renderer.name}");
                            continue;
                        }

                        PrefabUtility.RevertPropertyOverride(sp, InteractionMode.UserAction);
                        revertedRenderers++;
                        prefabChanged = true;
                        Debug.Log($"Reverted mesh: {renderer.name} (prefab: {prefab.name})");
                    }
                    if (prefabChanged)
                    {
                        revertedPrefabs.Add(prefab.name);
                    }
                    processedPrefabs++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Show results
                string resultMessage = $"Mesh revert completed!\n\n"
                                       + $"Processed prefabs: {processedPrefabs}\n"
                                       + $"Processed renderers: {processedRenderers}\n"
                                       + $"Successfully reverted renderers: {revertedRenderers}\n";

                if (revertedPrefabs.Count > 0)
                {
                    resultMessage += $"\nModified prefabs:\n";
                    foreach (string prefabName in revertedPrefabs.Take(10)) // Show max 10
                    {
                        resultMessage += $"• {prefabName}\n";
                    }
                    if (revertedPrefabs.Count > 10)
                    {
                        resultMessage += $"... {revertedPrefabs.Count - 10} more prefabs";
                    }
                }
                else
                {
                    resultMessage += "\nNo mesh overrides found to revert.";
                }

                EditorUtility.DisplayDialog("Revert Complete", resultMessage, "OK");

                Debug.Log(
                    $"Mesh revert operation completed - Processed prefabs: {processedPrefabs}, Reverted renderers: {revertedRenderers}"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error occurred while reverting meshes: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Error occurred during revert:\n{ex.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}