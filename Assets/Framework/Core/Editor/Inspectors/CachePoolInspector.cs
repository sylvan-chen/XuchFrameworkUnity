using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;

namespace XuchFramework.Editor
{
    [CustomEditor(typeof(CachePoolEditorViewer))]
    internal class CachePoolInspector : InspectorBase
    {
        private readonly Dictionary<string, List<CacheCollectionInfo>> _cacheCollectionInfosDict = new();
        private readonly HashSet<string> _expandedFoldout = new();
        private bool _showFullTypeName = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available in play mode only", MessageType.Info);
                return;
            }

            _showFullTypeName = EditorGUILayout.Toggle("Show Full Type Name", _showFullTypeName);

            // Get cache pool info
            _cacheCollectionInfosDict.Clear();
            CacheCollectionInfo[] cacheCollectionInfoArray = CachePool.GetAllCacheCollectionInfos();
            foreach (CacheCollectionInfo cacheCollectionInfo in cacheCollectionInfoArray)
            {
                string assemblyName = cacheCollectionInfo.CacheType.Assembly.GetName().Name;
                if (!_cacheCollectionInfosDict.TryGetValue(assemblyName, out List<CacheCollectionInfo> cacheCollectionInfos))
                {
                    cacheCollectionInfos = new List<CacheCollectionInfo>();
                    _cacheCollectionInfosDict.Add(assemblyName, cacheCollectionInfos);
                }

                cacheCollectionInfos.Add(cacheCollectionInfo);
            }

            foreach (KeyValuePair<string, List<CacheCollectionInfo>> assemblyNameAndCacheCollectionInfosPair in _cacheCollectionInfosDict)
            {
                string assemblyName = assemblyNameAndCacheCollectionInfosPair.Key;
                List<CacheCollectionInfo> cacheCollectionInfos = assemblyNameAndCacheCollectionInfosPair.Value;
                // Each foldout represents an assembly
                bool isExpanded = _expandedFoldout.Contains(assemblyName);
                bool isExpandedByUser = EditorGUILayout.Foldout(isExpanded, assemblyName);
                if (isExpandedByUser != isExpanded)
                {
                    if (isExpandedByUser)
                    {
                        _expandedFoldout.Add(assemblyName);
                    }
                    else
                    {
                        _expandedFoldout.Remove(assemblyName);
                    }
                }

                if (isExpanded)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        GUIStyle centeredStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(_showFullTypeName ? "Full Type Name" : "Type Name", EditorStyles.wordWrappedLabel, GUILayout.Width(200));
                            EditorGUILayout.LabelField("Unused", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Using", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Spawned", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Unspawned", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Created", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Discarded", centeredStyle, GUILayout.Width(80));
                        }

                        cacheCollectionInfos.Sort(CompareCacheCollectionInfo);
                        foreach (CacheCollectionInfo cacheCollectionInfo in cacheCollectionInfos)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label(
                                    _showFullTypeName ? cacheCollectionInfo.CacheType.FullName : cacheCollectionInfo.CacheType.Name,
                                    EditorStyles.wordWrappedLabel,
                                    GUILayout.Width(200));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UnusedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UsingCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.SpawnedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UnspawnedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.CreatedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.DiscardedCount.ToString(), centeredStyle, GUILayout.Width(80));
                            }
                        }
                    }

                    EditorGUILayout.Separator();
                }
            }

            Repaint();
        }

        private int CompareCacheCollectionInfo(CacheCollectionInfo a, CacheCollectionInfo b)
        {
            return _showFullTypeName ? string.Compare(a.CacheType.FullName, b.CacheType.FullName, StringComparison.Ordinal)
                : string.Compare(a.CacheType.Name, b.CacheType.Name, StringComparison.Ordinal);
        }
    }
}