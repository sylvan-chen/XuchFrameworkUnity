using UnityEditor;
using UnityEngine;

namespace Framework.Core.Editor
{
    public class SkinnedMeshRendererComparerWindow : EditorWindow
    {
        private SkinnedMeshRenderer _rendererA;
        private SkinnedMeshRenderer _rendererB;

        private SkinnedMeshRendererComparer.CompareOptions _options;
        private SkinnedMeshRendererComparer.CompareResult _result;

        private Vector2 _scroll;
        private Vector2 _pageScroll;

        [MenuItem("Tools/通用工具/SkinnedMeshRenderer Comparer", priority = 10002)]
        public static void Open()
        {
            var window = GetWindow<SkinnedMeshRendererComparerWindow>("SMR Comparer");
            window.minSize = new Vector2(850f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_options == null)
            {
                _options = new SkinnedMeshRendererComparer.CompareOptions();
            }
        }

        private void OnGUI()
        {
            _pageScroll = EditorGUILayout.BeginScrollView(_pageScroll);

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawRendererSelection();
            EditorGUILayout.Space(8);

            DrawQuickInfo();
            EditorGUILayout.Space(8);

            DrawOptions();
            EditorGUILayout.Space(8);

            DrawActions();
            EditorGUILayout.Space(8);

            DrawResult();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("SkinnedMeshRenderer Comparer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "用于比较两个 SkinnedMeshRenderer 是否真的兼容，重点检查 bones[]、rootBone、bindposes、boneWeights 等关键蒙皮信息。",
                MessageType.Info
            );
        }

        private void DrawRendererSelection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Renderer Selection", EditorStyles.boldLabel);

            _rendererA = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Renderer A",
                _rendererA,
                typeof(SkinnedMeshRenderer),
                true
            );
            _rendererB = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "Renderer B",
                _rendererB,
                typeof(SkinnedMeshRenderer),
                true
            );

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected As A"))
            {
                _rendererA = GetSelectedRenderer();
            }

            if (GUILayout.Button("Use Selected As B"))
            {
                _rendererB = GetSelectedRenderer();
            }

            if (GUILayout.Button("Swap A / B"))
            {
                (_rendererA, _rendererB) = (_rendererB, _rendererA);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickInfo()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Quick Info", EditorStyles.boldLabel);

            DrawRendererInfo("A", _rendererA);
            EditorGUILayout.Space(4);
            DrawRendererInfo("B", _rendererB);

            EditorGUILayout.EndVertical();
        }

        private void DrawRendererInfo(string label, SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
            {
                EditorGUILayout.HelpBox($"Renderer {label} 未指定。", MessageType.None);
                return;
            }

            var mesh = renderer.sharedMesh;

            EditorGUILayout.LabelField($"Renderer {label}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Path", SkinnedMeshRendererComparer.GetRendererPath(renderer));
            EditorGUILayout.LabelField(
                "RootBone",
                renderer.rootBone ? SkinnedMeshRendererComparer.GetFullPath(renderer.rootBone) : "NULL"
            );
            EditorGUILayout.LabelField("Bones Length", renderer.bones != null ? renderer.bones.Length.ToString() : "0");
            EditorGUILayout.LabelField("Mesh", mesh ? mesh.name : "NULL");
            EditorGUILayout.LabelField("Vertex Count", mesh ? mesh.vertexCount.ToString() : "0");
            EditorGUILayout.LabelField("SubMesh Count", mesh ? mesh.subMeshCount.ToString() : "0");
            EditorGUILayout.LabelField("BlendShape Count", mesh ? mesh.blendShapeCount.ToString() : "0");
            EditorGUILayout.LabelField("Bindposes Length", mesh ? mesh.bindposes.Length.ToString() : "0");
            EditorGUILayout.LabelField(
                "BoneWeight Count",
                mesh ? SkinnedMeshRendererComparer.GetBoneWeightCount(mesh).ToString() : "0"
            );
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Compare Options", EditorStyles.boldLabel);

            DrawOptionsGroupRenderer();
            EditorGUILayout.Space(6);
            DrawOptionsGroupBones();
            EditorGUILayout.Space(6);
            DrawOptionsGroupMesh();
            EditorGUILayout.Space(6);
            DrawOptionsGroupAdvanced();
            EditorGUILayout.Space(6);
            DrawToleranceOptions();
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Strict Preset"))
            {
                ApplyStrictPreset();
            }

            if (GUILayout.Button("Diagnostic Preset"))
            {
                ApplyDiagnosticPreset();
            }

            if (GUILayout.Button("Loose Preset"))
            {
                ApplyLoosePreset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawOptionsGroupRenderer()
        {
            EditorGUILayout.LabelField("Renderer / RootBone", EditorStyles.boldLabel);
            _options.CheckRendererName = EditorGUILayout.ToggleLeft("Check Renderer Name", _options.CheckRendererName);
            _options.CheckRootBone = EditorGUILayout.ToggleLeft("Check RootBone Path/Name", _options.CheckRootBone);
            _options.CheckRootBoneTransform = EditorGUILayout.ToggleLeft(
                "Check RootBone Local Transform",
                _options.CheckRootBoneTransform
            );
        }

        private void DrawOptionsGroupBones()
        {
            EditorGUILayout.LabelField("Bones", EditorStyles.boldLabel);
            _options.CheckBonesLength = EditorGUILayout.ToggleLeft("Check bones.Length", _options.CheckBonesLength);
            _options.CheckBoneName = EditorGUILayout.ToggleLeft("Check Bone Name", _options.CheckBoneName);
            _options.CheckBonePath = EditorGUILayout.ToggleLeft("Check Bone Path", _options.CheckBonePath);
            _options.CheckBoneTransform = EditorGUILayout.ToggleLeft(
                "Check Bone Local Transform",
                _options.CheckBoneTransform
            );
            _options.RequireBoneOrderMatch = EditorGUILayout.ToggleLeft(
                "Require Bone Order Match",
                _options.RequireBoneOrderMatch
            );
        }

        private void DrawOptionsGroupMesh()
        {
            EditorGUILayout.LabelField("Mesh / Skinning", EditorStyles.boldLabel);
            _options.CheckSharedMeshReference = EditorGUILayout.ToggleLeft(
                "Check Same SharedMesh Reference",
                _options.CheckSharedMeshReference
            );
            _options.CheckMeshName = EditorGUILayout.ToggleLeft("Check Mesh Name", _options.CheckMeshName);
            _options.CheckVertexCount = EditorGUILayout.ToggleLeft("Check Vertex Count", _options.CheckVertexCount);
            _options.CheckSubMeshCount = EditorGUILayout.ToggleLeft("Check SubMesh Count", _options.CheckSubMeshCount);
            _options.CheckBlendShapeCount = EditorGUILayout.ToggleLeft(
                "Check BlendShape Count",
                _options.CheckBlendShapeCount
            );
            _options.CheckBounds = EditorGUILayout.ToggleLeft("Check Mesh Bounds", _options.CheckBounds);
            _options.CheckBindposesLength = EditorGUILayout.ToggleLeft(
                "Check Bindposes Length",
                _options.CheckBindposesLength
            );
            _options.CheckBindposes = EditorGUILayout.ToggleLeft("Check Bindposes Content", _options.CheckBindposes);
            _options.CheckBoneWeights = EditorGUILayout.ToggleLeft("Check BoneWeights", _options.CheckBoneWeights);

            using (new EditorGUI.DisabledScope(!_options.CheckBoneWeights))
            {
                _options.CheckBoneWeightsCount = EditorGUILayout.ToggleLeft(
                    "Check BoneWeights Count",
                    _options.CheckBoneWeightsCount
                );
                _options.CheckBoneWeightsContent = EditorGUILayout.ToggleLeft(
                    "Check BoneWeights Content",
                    _options.CheckBoneWeightsContent
                );
            }
        }

        private void DrawOptionsGroupAdvanced()
        {
            EditorGUILayout.LabelField("Advanced Renderer Properties", EditorStyles.boldLabel);
            _options.CheckQuality = EditorGUILayout.ToggleLeft("Check Quality", _options.CheckQuality);
            _options.CheckUpdateWhenOffscreen = EditorGUILayout.ToggleLeft(
                "Check Update When Offscreen",
                _options.CheckUpdateWhenOffscreen
            );
            _options.CheckSkinnedMotionVectors = EditorGUILayout.ToggleLeft(
                "Check Skinned Motion Vectors",
                _options.CheckSkinnedMotionVectors
            );
            _options.CheckLocalBounds = EditorGUILayout.ToggleLeft(
                "Check Renderer LocalBounds",
                _options.CheckLocalBounds
            );
        }

        private void DrawToleranceOptions()
        {
            EditorGUILayout.LabelField("Tolerance", EditorStyles.boldLabel);

            _options.PositionTolerance = EditorGUILayout.FloatField("Position Tolerance", _options.PositionTolerance);
            _options.RotationTolerance = EditorGUILayout.FloatField("Rotation Tolerance", _options.RotationTolerance);
            _options.ScaleTolerance = EditorGUILayout.FloatField("Scale Tolerance", _options.ScaleTolerance);
            _options.FloatTolerance = EditorGUILayout.FloatField("Float Tolerance", _options.FloatTolerance);
            _options.BoundsTolerance = EditorGUILayout.FloatField("Bounds Tolerance", _options.BoundsTolerance);
            _options.MatrixTolerance = EditorGUILayout.FloatField("Matrix Tolerance", _options.MatrixTolerance);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_rendererA == null || _rendererB == null))
            {
                if (GUILayout.Button("Compare", GUILayout.Height(32f)))
                {
                    CompareNow();
                }
            }

            using (new EditorGUI.DisabledScope(_rendererA == null || _rendererB == null))
            {
                if (GUILayout.Button("Try Safe Replace Check", GUILayout.Height(24f)))
                {
                    bool ok = SkinnedMeshRendererComparer.CanSafelyReplace(_rendererA, _rendererB, out string report);
                    _result = SkinnedMeshRendererComparer.Compare(_rendererA, _rendererB, BuildSafeReplaceOptions());

                    if (ok)
                    {
                        Debug.Log("SkinnedMeshRenderer safe replace check passed.\n" + report);
                    }
                    else
                    {
                        Debug.LogWarning("SkinnedMeshRenderer safe replace check failed.\n" + report);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(_result == null))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Copy Report"))
                {
                    EditorGUIUtility.systemCopyBuffer = _result.GetReport();
                    Debug.Log("SMR compare report copied to clipboard.");
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
                EditorGUILayout.HelpBox("未发现差异。当前两者在已勾选项下匹配。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"发现 {_result.DifferenceCount} 处差异。", MessageType.Warning);
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Summary");
            EditorGUILayout.SelectableLabel(
                $"IsMatch: {_result.IsMatch}\nDifferenceCount: {_result.DifferenceCount}",
                EditorStyles.textArea,
                GUILayout.Height(40f)
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
                    EditorGUILayout.LabelField($"#{i + 1} [{diff.Category}]", EditorStyles.boldLabel);
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
            if (_rendererA == null || _rendererB == null)
            {
                EditorUtility.DisplayDialog("SMR Comparer", "请先指定 Renderer A 和 Renderer B。", "OK");
                return;
            }

            _result = SkinnedMeshRendererComparer.Compare(_rendererA, _rendererB, _options);

            if (_result.IsMatch)
            {
                Debug.Log("SkinnedMeshRenderer compare passed.\n" + _result.GetReport());
            }
            else
            {
                Debug.LogWarning(_result.GetReport());
            }
        }

        private SkinnedMeshRenderer GetSelectedRenderer()
        {
            if (Selection.activeGameObject == null) return null;

            return Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
        }

        private void ApplyStrictPreset()
        {
            _options.CheckRendererName = false;

            _options.CheckRootBone = true;
            _options.CheckRootBoneTransform = true;

            _options.CheckBonesLength = true;
            _options.CheckBoneName = true;
            _options.CheckBonePath = true;
            _options.CheckBoneTransform = true;
            _options.RequireBoneOrderMatch = true;

            _options.CheckSharedMeshReference = false;
            _options.CheckMeshName = true;
            _options.CheckVertexCount = true;
            _options.CheckSubMeshCount = true;
            _options.CheckBlendShapeCount = true;
            _options.CheckBounds = true;

            _options.CheckBindposesLength = true;
            _options.CheckBindposes = true;

            _options.CheckBoneWeights = true;
            _options.CheckBoneWeightsCount = true;
            _options.CheckBoneWeightsContent = true;

            _options.CheckQuality = false;
            _options.CheckUpdateWhenOffscreen = false;
            _options.CheckSkinnedMotionVectors = false;
            _options.CheckLocalBounds = false;

            _options.PositionTolerance = 0.0001f;
            _options.RotationTolerance = 0.01f;
            _options.ScaleTolerance = 0.0001f;
            _options.FloatTolerance = 0.0001f;
            _options.BoundsTolerance = 0.0001f;
            _options.MatrixTolerance = 0.0001f;
        }

        private void ApplyDiagnosticPreset()
        {
            _options.CheckRendererName = true;

            _options.CheckRootBone = true;
            _options.CheckRootBoneTransform = true;

            _options.CheckBonesLength = true;
            _options.CheckBoneName = true;
            _options.CheckBonePath = true;
            _options.CheckBoneTransform = true;
            _options.RequireBoneOrderMatch = true;

            _options.CheckSharedMeshReference = true;
            _options.CheckMeshName = true;
            _options.CheckVertexCount = true;
            _options.CheckSubMeshCount = true;
            _options.CheckBlendShapeCount = true;
            _options.CheckBounds = true;

            _options.CheckBindposesLength = true;
            _options.CheckBindposes = true;

            _options.CheckBoneWeights = true;
            _options.CheckBoneWeightsCount = true;
            _options.CheckBoneWeightsContent = true;

            _options.CheckQuality = true;
            _options.CheckUpdateWhenOffscreen = true;
            _options.CheckSkinnedMotionVectors = true;
            _options.CheckLocalBounds = true;

            _options.PositionTolerance = 0.0001f;
            _options.RotationTolerance = 0.01f;
            _options.ScaleTolerance = 0.0001f;
            _options.FloatTolerance = 0.0001f;
            _options.BoundsTolerance = 0.0001f;
            _options.MatrixTolerance = 0.0001f;
        }

        private void ApplyLoosePreset()
        {
            _options.CheckRendererName = false;

            _options.CheckRootBone = true;
            _options.CheckRootBoneTransform = false;

            _options.CheckBonesLength = true;
            _options.CheckBoneName = true;
            _options.CheckBonePath = true;
            _options.CheckBoneTransform = false;
            _options.RequireBoneOrderMatch = true;

            _options.CheckSharedMeshReference = false;
            _options.CheckMeshName = false;
            _options.CheckVertexCount = true;
            _options.CheckSubMeshCount = true;
            _options.CheckBlendShapeCount = true;
            _options.CheckBounds = false;

            _options.CheckBindposesLength = true;
            _options.CheckBindposes = true;

            _options.CheckBoneWeights = true;
            _options.CheckBoneWeightsCount = true;
            _options.CheckBoneWeightsContent = false;

            _options.CheckQuality = false;
            _options.CheckUpdateWhenOffscreen = false;
            _options.CheckSkinnedMotionVectors = false;
            _options.CheckLocalBounds = false;

            _options.PositionTolerance = 0.001f;
            _options.RotationTolerance = 0.1f;
            _options.ScaleTolerance = 0.001f;
            _options.FloatTolerance = 0.001f;
            _options.BoundsTolerance = 0.001f;
            _options.MatrixTolerance = 0.001f;
        }

        private SkinnedMeshRendererComparer.CompareOptions BuildSafeReplaceOptions()
        {
            return new SkinnedMeshRendererComparer.CompareOptions
            {
                CheckRendererName = false,
                CheckRootBone = true,
                CheckRootBoneTransform = true,
                CheckBonesLength = true,
                CheckBoneName = true,
                CheckBonePath = true,
                CheckBoneTransform = true,
                RequireBoneOrderMatch = true,
                CheckSharedMeshReference = false,
                CheckMeshName = true,
                CheckVertexCount = true,
                CheckSubMeshCount = true,
                CheckBlendShapeCount = true,
                CheckBounds = true,
                CheckBindposesLength = true,
                CheckBindposes = true,
                CheckBoneWeights = true,
                CheckBoneWeightsCount = true,
                CheckBoneWeightsContent = true,
                CheckQuality = false,
                CheckUpdateWhenOffscreen = false,
                CheckSkinnedMotionVectors = false,
                CheckLocalBounds = false,
                PositionTolerance = 0.0001f,
                RotationTolerance = 0.01f,
                ScaleTolerance = 0.0001f,
                FloatTolerance = 0.0001f,
                BoundsTolerance = 0.0001f,
                MatrixTolerance = 0.0001f
            };
        }
    }
}