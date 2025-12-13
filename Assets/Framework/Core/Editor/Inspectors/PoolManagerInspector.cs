using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using XuchFramework.Core;

namespace XuchFramework.Editor
{
    [CustomEditor(typeof(PoolManager))]
    internal sealed class PoolManagerInspector : InspectorBase
    {
        private readonly HashSet<string> _expandedFoldout = new();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available in play mode only.", MessageType.Info);
                return;
            }

            var targetComponent = target as PoolManager;
            if (targetComponent == null)
                return;

            EditorGUILayout.LabelField("Pool Count", targetComponent.PoolCount.ToString());

            var pools = targetComponent.GetAllPools();
            foreach (var pool in pools)
            {
                DrawPool(pool, pool.GetAllPoolObjectInfos());
            }

            Repaint();
        }

        private void DrawPool(PoolBase pool, PoolObjectInfo[] poolObjectInfos)
        {
            string poolName = $"{pool.ObjectType.Name} Pool";
            bool isExpanded = _expandedFoldout.Contains(poolName);
            bool isExpandedByUser = EditorGUILayout.Foldout(isExpanded, poolName);
            if (isExpandedByUser != isExpanded)
            {
                if (isExpandedByUser)
                {
                    _expandedFoldout.Add(poolName);
                }
                else
                {
                    _expandedFoldout.Remove(poolName);
                }
            }

            if (!isExpandedByUser)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Object Type", pool.ObjectType.Name);
                EditorGUILayout.LabelField("Allow Multi-Reference", pool.AllowMultiReference.ToString());
                EditorGUILayout.LabelField("Object Expired Time", pool.ObjectExpiredTime.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.LabelField("Auto Clear Interval", pool.AutoClearInterval.ToString(CultureInfo.InvariantCulture));
                EditorGUILayout.LabelField("Capacity", pool.Capacity.ToString());
                EditorGUILayout.LabelField("Count", pool.Count.ToString());
                EditorGUILayout.Separator();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    if (poolObjectInfos.Length > 0)
                    {
                        // 居中样式
                        GUIStyle centeredStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(35));
                            EditorGUILayout.LabelField("Locked", centeredStyle, GUILayout.Width(70));
                            EditorGUILayout.LabelField("Ref Count", centeredStyle, GUILayout.Width(70));
                            EditorGUILayout.LabelField("Last Use", centeredStyle, GUILayout.Width(180));
                            EditorGUILayout.LabelField("State", centeredStyle, GUILayout.Width(70));
                            EditorGUILayout.LabelField("Idle Time", centeredStyle, GUILayout.Width(100));
                        }

                        int index = 0;
                        foreach (PoolObjectInfo poolObjectInfo in poolObjectInfos)
                        {
                            index++;
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(35));
                                EditorGUILayout.LabelField(poolObjectInfo.Locked.ToString(), centeredStyle, GUILayout.Width(70));
                                EditorGUILayout.LabelField(poolObjectInfo.ReferenceCount.ToString(), centeredStyle, GUILayout.Width(70));
                                EditorGUILayout.LabelField(
                                    poolObjectInfo.LastUseTime.ToString("yy-MM-dd HH:mm:ss"),
                                    centeredStyle,
                                    GUILayout.Width(180));
                                string stateText;
                                string idleTimeText = "-";
                                var originalColor = GUI.color;
                                if (poolObjectInfo.IsInUse)
                                {
                                    stateText = "In Use";
                                }
                                else
                                {
                                    stateText = "Idle";
                                    double remainingTime = (poolObjectInfo.LastUseTime - DateTime.MinValue).TotalSeconds + pool.ObjectExpiredTime;
                                    if (remainingTime.CompareTo(PoolManager.DEFAULT_OBJECT_EXPIRED_TIME) >= 0)
                                    {
                                        idleTimeText = "INF";
                                    }
                                    else
                                    {
                                        if (DateTime.Now > poolObjectInfo.LastUseTime.AddSeconds(pool.ObjectExpiredTime))
                                        {
                                            GUI.color = Color.red;
                                            stateText = "Expired";
                                            idleTimeText = "-";
                                        }
                                        else
                                        {
                                            TimeSpan idleSpan = DateTime.Now - poolObjectInfo.LastUseTime;
                                            idleTimeText = idleSpan.ToString(@"hh\:mm\:ss");
                                        }
                                    }
                                }

                                EditorGUILayout.LabelField(stateText, centeredStyle, GUILayout.Width(70));
                                EditorGUILayout.LabelField(idleTimeText, centeredStyle, GUILayout.Width(100));
                                GUI.color = originalColor;
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Squeeze"))
                            {
                                pool.Squeeze();
                            }

                            if (GUILayout.Button("Discard All Unused"))
                            {
                                pool.DiscardAllUnused();
                            }

                            if (GUILayout.Button("Discard All Expired"))
                            {
                                pool.DiscardAllExpired();
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Empty pool.");
                    }
                }
            }

            EditorGUILayout.Separator();
        }
    }
}