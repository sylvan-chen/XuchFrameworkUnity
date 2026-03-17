using UnityEditor;
using UnityEngine;

namespace Framework.Core.Editor
{
    public class BoneComparerWindow : EditorWindow
    {
        private Transform _rootA;
        private Transform _rootB;

        private BoneComparer.CompareOptions _options;
        private BoneComparer.CompareResult _result;

        private Vector2 _scroll;
        private bool _showOnlyDifferences = true;

        [MenuItem("Tools/通用工具/Bone Comparer", priority = 10001)]
        public static void Open()
        {
            var window = GetWindow<BoneComparerWindow>("Bone Comparer");
            window.minSize = new Vector2(700f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_options == null)
            {
                _options = new BoneComparer.CompareOptions();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawRoots();
            EditorGUILayout.Space(8);
            DrawOptions();
            EditorGUILayout.Space(8);
            DrawActions();
            EditorGUILayout.Space(8);
            DrawResult();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Bone Comparer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("对比两个骨骼根节点下的整棵骨骼树，用于检查它们是否可以安全互换 SkinnedMesh。", MessageType.Info);
        }

        private void DrawRoots()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Bone Roots", EditorStyles.boldLabel);

            _rootA = (Transform)EditorGUILayout.ObjectField("Root A", _rootA, typeof(Transform), true);
            _rootB = (Transform)EditorGUILayout.ObjectField("Root B", _rootB, typeof(Transform), true);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Use Selected As A"))
            {
                if (Selection.activeTransform != null) _rootA = Selection.activeTransform;
            }

            if (GUILayout.Button("Use Selected As B"))
            {
                if (Selection.activeTransform != null) _rootB = Selection.activeTransform;
            }

            if (GUILayout.Button("Swap A / B"))
            {
                (_rootA, _rootB) = (_rootB, _rootA);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Compare Options", EditorStyles.boldLabel);

            _options.CheckName = EditorGUILayout.ToggleLeft("Check Name", _options.CheckName);
            _options.CheckPath = EditorGUILayout.ToggleLeft("Check Path", _options.CheckPath);
            _options.CheckChildrenCount = EditorGUILayout.ToggleLeft(
                "Check Children Count",
                _options.CheckChildrenCount
            );
            _options.CheckLocalPosition = EditorGUILayout.ToggleLeft(
                "Check Local Position",
                _options.CheckLocalPosition
            );
            _options.CheckLocalRotation = EditorGUILayout.ToggleLeft(
                "Check Local Rotation",
                _options.CheckLocalRotation
            );
            _options.CheckLocalScale = EditorGUILayout.ToggleLeft("Check Local Scale", _options.CheckLocalScale);
            _options.RequireChildOrderMatch = EditorGUILayout.ToggleLeft(
                "Require Child Order Match",
                _options.RequireChildOrderMatch
            );

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Tolerance", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!_options.CheckLocalPosition))
            {
                _options.PositionTolerance = EditorGUILayout.FloatField(
                    "Position Tolerance",
                    _options.PositionTolerance
                );
            }

            using (new EditorGUI.DisabledScope(!_options.CheckLocalRotation))
            {
                _options.RotationTolerance = EditorGUILayout.FloatField(
                    "Rotation Tolerance",
                    _options.RotationTolerance
                );
            }

            using (new EditorGUI.DisabledScope(!_options.CheckLocalScale))
            {
                _options.ScaleTolerance = EditorGUILayout.FloatField("Scale Tolerance", _options.ScaleTolerance);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Strict Preset"))
            {
                ApplyStrictPreset();
            }

            if (GUILayout.Button("Loose Preset"))
            {
                ApplyLoosePreset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_rootA == null || _rootB == null))
            {
                if (GUILayout.Button("Compare Bones", GUILayout.Height(30)))
                {
                    CompareNow();
                }
            }

            using (new EditorGUI.DisabledScope(_result == null))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Copy Report"))
                {
                    EditorGUIUtility.systemCopyBuffer = _result.GetReport();
                    Debug.Log("Bone compare report copied to clipboard.");
                }

                if (GUILayout.Button("Log Report To Console"))
                {
                    Debug.Log(_result.GetReport());
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResult()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);

            if (_result == null)
            {
                EditorGUILayout.HelpBox("还没有执行对比。", MessageType.None);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_result.IsMatch)
            {
                EditorGUILayout.HelpBox("两个骨架完全匹配，可以较高置信度认为能够互换 SkinnedMesh。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"发现 {_result.DifferenceCount} 处不一致，不能直接认为两个骨架可安全互换。", MessageType.Warning);
            }

            _showOnlyDifferences = EditorGUILayout.ToggleLeft("Show Only Differences", _showOnlyDifferences);

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Summary");
            EditorGUILayout.SelectableLabel(
                $"IsMatch: {_result.IsMatch}\nDifferenceCount: {_result.DifferenceCount}",
                EditorStyles.textArea,
                GUILayout.Height(40)
            );

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Details");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_result.Differences.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现差异。", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _result.Differences.Count; i++)
                {
                    var diff = _result.Differences[i];

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"#{i + 1}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("A", diff.PathA);
                    EditorGUILayout.LabelField("B", diff.PathB);
                    EditorGUILayout.LabelField("Message", diff.Message);
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void CompareNow()
        {
            if (_rootA == null || _rootB == null)
            {
                EditorUtility.DisplayDialog("Bone Comparer", "请先指定 Root A 和 Root B。", "OK");
                return;
            }

            _result = BoneComparer.Compare(_rootA, _rootB, _options);

            if (_result.IsMatch)
            {
                Debug.Log($"BoneComparer: '{_rootA.name}' 与 '{_rootB.name}' 对比通过。");
            }
            else
            {
                Debug.LogWarning(_result.GetReport());
            }
        }

        private void ApplyStrictPreset()
        {
            _options.CheckName = true;
            _options.CheckPath = true;
            _options.CheckChildrenCount = true;
            _options.CheckLocalPosition = true;
            _options.CheckLocalRotation = true;
            _options.CheckLocalScale = true;
            _options.RequireChildOrderMatch = true;

            _options.PositionTolerance = 0.0001f;
            _options.RotationTolerance = 0.01f;
            _options.ScaleTolerance = 0.0001f;
        }

        private void ApplyLoosePreset()
        {
            _options.CheckName = true;
            _options.CheckPath = true;
            _options.CheckChildrenCount = true;
            _options.CheckLocalPosition = false;
            _options.CheckLocalRotation = false;
            _options.CheckLocalScale = false;
            _options.RequireChildOrderMatch = false;

            _options.PositionTolerance = 0.001f;
            _options.RotationTolerance = 0.1f;
            _options.ScaleTolerance = 0.001f;
        }
    }
}