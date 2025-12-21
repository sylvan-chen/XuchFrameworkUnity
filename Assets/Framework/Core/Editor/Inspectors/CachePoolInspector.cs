using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;
using XuchFramework.Core.Utils;

namespace XuchFramework.Editor
{
    [CustomEditor(typeof(CachePool))]
    internal class CachePoolInspector : InspectorBase
    {
        private readonly Dictionary<string, List<CacheCollectionInfo>> _cacheCollectionInfosDict = new();
        private readonly HashSet<string> _expandedFoldout = new();
        private bool _showFullTypeName = false;

        private SerializedProperty _cacheExpireTimeProperty;

        public void OnEnable()
        {
            _cacheExpireTimeProperty = serializedObject.FindProperty("_cacheExpireTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_cacheExpireTimeProperty);
            serializedObject.ApplyModifiedProperties();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Statics info is available in play mode only", MessageType.Info);
                return;
            }

            var cachePool = target as CachePool;
            if (cachePool == null)
                return;

            _showFullTypeName = EditorGUILayout.Toggle("Show Full Type Name", _showFullTypeName);

            // Get cache pool info
            _cacheCollectionInfosDict.Clear();
            CacheCollectionInfo[] cacheCollectionInfoArray = cachePool.GetAllCacheCollectionInfos();
            foreach (var cacheCollectionInfo in cacheCollectionInfoArray)
            {
                string assemblyName = cacheCollectionInfo.CacheType.Assembly.GetName().Name;
                if (!_cacheCollectionInfosDict.TryGetValue(assemblyName, out var cacheCollectionInfos))
                {
                    cacheCollectionInfos = new List<CacheCollectionInfo>();
                    _cacheCollectionInfosDict.Add(assemblyName, cacheCollectionInfos);
                }
                cacheCollectionInfos.Add(cacheCollectionInfo);
            }

            foreach (var (assemblyName, cacheCollectionInfos) in _cacheCollectionInfosDict)
            {
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
                            GUILayout.Label(
                                _showFullTypeName ? "Full Type Name" : "Type Name",
                                EditorStyles.wordWrappedLabel,
                                _showFullTypeName ? GUILayout.Width(200) : GUILayout.Width(80));
                            EditorGUILayout.LabelField("Unused", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Using", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Acquired", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Released", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Created", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Discarded", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Idle", centeredStyle, GUILayout.Width(80));
                        }

                        cacheCollectionInfos.Sort(CompareCacheCollectionInfo);
                        foreach (CacheCollectionInfo cacheCollectionInfo in cacheCollectionInfos)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label(
                                    _showFullTypeName ? cacheCollectionInfo.CacheType.FullName : cacheCollectionInfo.CacheType.Name,
                                    EditorStyles.wordWrappedLabel,
                                    _showFullTypeName ? GUILayout.Width(200) : GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UnusedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UsingCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.AcquiredCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.ReleasedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.CreatedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.DiscardedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(
                                    GameHelper.SecondsToTimeStr_hms(cacheCollectionInfo.IdleTime),
                                    centeredStyle,
                                    GUILayout.Width(80));
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