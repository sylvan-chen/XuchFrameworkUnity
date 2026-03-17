using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Framework.Core.Editor
{
    /// <summary>
    /// 对比两个 SkinnedMeshRenderer 的关键蒙皮信息，
    /// 用于判断 mesh / bones / bindposes / rootBone 是否真正兼容。
    /// </summary>
    public static class SkinnedMeshRendererComparer
    {
        [Serializable]
        public class CompareOptions
        {
            public bool CheckRendererName = false;

            public bool CheckRootBone = true;
            public bool CheckRootBoneTransform = true;

            public bool CheckBonesLength = true;
            public bool CheckBoneName = true;
            public bool CheckBonePath = true;
            public bool CheckBoneTransform = true;
            public bool RequireBoneOrderMatch = true;

            public bool CheckSharedMeshReference = false;
            public bool CheckMeshName = true;
            public bool CheckVertexCount = true;
            public bool CheckSubMeshCount = true;
            public bool CheckBlendShapeCount = true;
            public bool CheckBounds = true;

            public bool CheckBindposesLength = true;
            public bool CheckBindposes = true;

            public bool CheckBoneWeights = true;
            public bool CheckBoneWeightsCount = true;
            public bool CheckBoneWeightsContent = true;

            public bool CheckQuality = false;
            public bool CheckUpdateWhenOffscreen = false;
            public bool CheckSkinnedMotionVectors = false;
            public bool CheckLocalBounds = false;

            public float PositionTolerance = 0.0001f;
            public float RotationTolerance = 0.01f;
            public float ScaleTolerance = 0.0001f;
            public float FloatTolerance = 0.0001f;
            public float BoundsTolerance = 0.0001f;
            public float MatrixTolerance = 0.0001f;
        }

        [Serializable]
        public class Difference
        {
            public string Category;
            public string PathA;
            public string PathB;
            public string Message;

            public Difference(string category, string pathA, string pathB, string message)
            {
                Category = category;
                PathA = pathA;
                PathB = pathB;
                Message = message;
            }

            public override string ToString()
            {
                return $"[{Category}] A: {PathA} | B: {PathB} | {Message}";
            }
        }

        [Serializable]
        public class CompareResult
        {
            public SkinnedMeshRenderer RendererA;
            public SkinnedMeshRenderer RendererB;
            public bool IsMatch;

            public readonly List<Difference> Differences = new List<Difference>();

            public int DifferenceCount => Differences.Count;

            public void Add(string category, string pathA, string pathB, string message)
            {
                Differences.Add(new Difference(category, pathA, pathB, message));
            }

            public string GetReport()
            {
                var sb = new StringBuilder();
                sb.AppendLine("===== SkinnedMeshRenderer Compare Result =====");
                sb.AppendLine($"RendererA: {(RendererA != null ? RendererA.name : "NULL")}");
                sb.AppendLine($"RendererB: {(RendererB != null ? RendererB.name : "NULL")}");
                sb.AppendLine($"IsMatch: {IsMatch}");
                sb.AppendLine($"DifferenceCount: {DifferenceCount}");
                sb.AppendLine();

                for (int i = 0; i < Differences.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {Differences[i]}");
                }

                return sb.ToString();
            }
        }

        public static CompareResult Compare(
            SkinnedMeshRenderer rendererA,
            SkinnedMeshRenderer rendererB,
            CompareOptions options = null)
        {
            options ??= new CompareOptions();

            var result = new CompareResult { RendererA = rendererA, RendererB = rendererB, IsMatch = true };

            if (rendererA == null && rendererB == null)
            {
                result.IsMatch = true;
                return result;
            }

            if (rendererA == null || rendererB == null)
            {
                result.Add(
                    "Renderer",
                    GetRendererPath(rendererA),
                    GetRendererPath(rendererB),
                    "其中一个 SkinnedMeshRenderer 为空。"
                );
                result.IsMatch = false;
                return result;
            }

            CompareRendererBasic(rendererA, rendererB, options, result);
            CompareRootBone(rendererA, rendererB, options, result);
            CompareBones(rendererA, rendererB, options, result);
            CompareMesh(rendererA, rendererB, options, result);
            CompareBindposes(rendererA, rendererB, options, result);
            CompareBoneWeights(rendererA, rendererB, options, result);
            CompareRendererProperties(rendererA, rendererB, options, result);

            result.IsMatch = result.Differences.Count == 0;
            return result;
        }

        public static bool CanSafelyReplace(
            SkinnedMeshRenderer sourceRenderer,
            SkinnedMeshRenderer targetRenderer,
            out string report)
        {
            var options = new CompareOptions
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

            var result = Compare(sourceRenderer, targetRenderer, options);
            report = result.GetReport();
            return result.IsMatch;
        }

        private static void CompareRendererBasic(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            string pathA = GetRendererPath(a);
            string pathB = GetRendererPath(b);

            if (options.CheckRendererName && a.name != b.name)
            {
                result.Add("Renderer", pathA, pathB, $"Renderer 名称不一致: A='{a.name}', B='{b.name}'");
            }
        }

        private static void CompareRootBone(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            string pathA = GetRendererPath(a);
            string pathB = GetRendererPath(b);

            var rootBoneA = a.rootBone;
            var rootBoneB = b.rootBone;

            string rootBonePathA = GetRelativePathFromRendererRoot(a, rootBoneA);
            string rootBonePathB = GetRelativePathFromRendererRoot(b, rootBoneB);

            if (options.CheckRootBone)
            {
                if (rootBoneA == null || rootBoneB == null)
                {
                    if (rootBoneA != rootBoneB)
                    {
                        result.Add("RootBone", pathA, pathB, $"rootBone 不一致: A='{rootBonePathA}', B='{rootBonePathB}'");
                    }
                }
                else
                {
                    if (rootBoneA.name != rootBoneB.name)
                    {
                        result.Add(
                            "RootBone",
                            rootBonePathA,
                            rootBonePathB,
                            $"rootBone 名称不一致: A='{rootBoneA.name}', B='{rootBoneB.name}'"
                        );
                    }

                    if (rootBonePathA != rootBonePathB)
                    {
                        result.Add(
                            "RootBone",
                            rootBonePathA,
                            rootBonePathB,
                            $"rootBone 路径不一致: A='{rootBonePathA}', B='{rootBonePathB}'"
                        );
                    }
                }
            }

            if (options.CheckRootBoneTransform && rootBoneA != null && rootBoneB != null)
            {
                CompareTransform("RootBoneTransform", rootBoneA, rootBoneB, options, result);
            }
        }

        private static void CompareBones(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            var bonesA = a.bones ?? Array.Empty<Transform>();
            var bonesB = b.bones ?? Array.Empty<Transform>();

            if (options.CheckBonesLength && bonesA.Length != bonesB.Length)
            {
                result.Add(
                    "Bones",
                    GetRendererPath(a),
                    GetRendererPath(b),
                    $"bones.Length 不一致: A={bonesA.Length}, B={bonesB.Length}"
                );
            }

            if (options.RequireBoneOrderMatch)
            {
                int max = Mathf.Max(bonesA.Length, bonesB.Length);
                for (int i = 0; i < max; i++)
                {
                    Transform boneA = i < bonesA.Length ? bonesA[i] : null;
                    Transform boneB = i < bonesB.Length ? bonesB[i] : null;

                    CompareBoneEntry(a, b, boneA, boneB, i, options, result);
                }
            }
            else
            {
                var mapA = BuildBoneMap(a, bonesA);
                var mapB = BuildBoneMap(b, bonesB);

                var allPaths = new HashSet<string>(mapA.Keys);
                allPaths.UnionWith(mapB.Keys);

                foreach (string bonePath in allPaths)
                {
                    mapA.TryGetValue(bonePath, out var boneA);
                    mapB.TryGetValue(bonePath, out var boneB);

                    CompareBoneEntry(a, b, boneA, boneB, -1, options, result);
                }
            }
        }

        private static void CompareBoneEntry(
            SkinnedMeshRenderer rendererA,
            SkinnedMeshRenderer rendererB,
            Transform boneA,
            Transform boneB,
            int index,
            CompareOptions options,
            CompareResult result)
        {
            string pathA = GetRelativePathFromRendererRoot(rendererA, boneA);
            string pathB = GetRelativePathFromRendererRoot(rendererB, boneB);

            string category = index >= 0 ? $"Bone[{index}]" : "Bone";

            if (boneA == null || boneB == null)
            {
                if (boneA != boneB)
                {
                    result.Add(category, pathA, pathB, "一侧 bones[] 中存在骨骼，另一侧不存在。");
                }
                return;
            }

            if (options.CheckBoneName && boneA.name != boneB.name)
            {
                result.Add(category, pathA, pathB, $"骨骼名称不一致: A='{boneA.name}', B='{boneB.name}'");
            }

            if (options.CheckBonePath && pathA != pathB)
            {
                result.Add(category, pathA, pathB, $"骨骼路径不一致: A='{pathA}', B='{pathB}'");
            }

            if (options.CheckBoneTransform)
            {
                CompareTransform(category, boneA, boneB, options, result);
            }
        }

        private static void CompareMesh(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            var meshA = a.sharedMesh;
            var meshB = b.sharedMesh;

            string pathA = GetRendererPath(a);
            string pathB = GetRendererPath(b);

            if (meshA == null || meshB == null)
            {
                if (meshA != meshB)
                {
                    result.Add(
                        "Mesh",
                        pathA,
                        pathB,
                        $"其中一个 sharedMesh 为空: A='{(meshA ? meshA.name : "NULL")}', B='{(meshB ? meshB.name : "NULL")}'"
                    );
                }
                return;
            }

            if (options.CheckSharedMeshReference && meshA != meshB)
            {
                result.Add("Mesh", pathA, pathB, $"sharedMesh 引用不是同一个对象: A='{meshA.name}', B='{meshB.name}'");
            }

            if (options.CheckMeshName && meshA.name != meshB.name)
            {
                result.Add("Mesh", pathA, pathB, $"Mesh 名称不一致: A='{meshA.name}', B='{meshB.name}'");
            }

            if (options.CheckVertexCount && meshA.vertexCount != meshB.vertexCount)
            {
                result.Add("Mesh", pathA, pathB, $"vertexCount 不一致: A={meshA.vertexCount}, B={meshB.vertexCount}");
            }

            if (options.CheckSubMeshCount && meshA.subMeshCount != meshB.subMeshCount)
            {
                result.Add("Mesh", pathA, pathB, $"subMeshCount 不一致: A={meshA.subMeshCount}, B={meshB.subMeshCount}");
            }

            if (options.CheckBlendShapeCount && meshA.blendShapeCount != meshB.blendShapeCount)
            {
                result.Add(
                    "Mesh",
                    pathA,
                    pathB,
                    $"blendShapeCount 不一致: A={meshA.blendShapeCount}, B={meshB.blendShapeCount}"
                );
            }

            if (options.CheckBounds)
            {
                if (!NearlyEqual(meshA.bounds.center, meshB.bounds.center, options.BoundsTolerance)
                    || !NearlyEqual(meshA.bounds.size, meshB.bounds.size, options.BoundsTolerance))
                {
                    result.Add(
                        "Mesh",
                        pathA,
                        pathB,
                        $"Mesh bounds 不一致: A(center={meshA.bounds.center}, size={meshA.bounds.size}), "
                        + $"B(center={meshB.bounds.center}, size={meshB.bounds.size})"
                    );
                }
            }
        }

        private static void CompareBindposes(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            if (!options.CheckBindposesLength && !options.CheckBindposes) return;

            var meshA = a.sharedMesh;
            var meshB = b.sharedMesh;

            if (meshA == null || meshB == null) return;

            var bindposesA = meshA.bindposes;
            var bindposesB = meshB.bindposes;

            if (options.CheckBindposesLength && bindposesA.Length != bindposesB.Length)
            {
                result.Add(
                    "Bindposes",
                    GetRendererPath(a),
                    GetRendererPath(b),
                    $"bindposes.Length 不一致: A={bindposesA.Length}, B={bindposesB.Length}"
                );
            }

            if (options.CheckBindposes)
            {
                int max = Mathf.Max(bindposesA.Length, bindposesB.Length);
                for (int i = 0; i < max; i++)
                {
                    bool hasA = i < bindposesA.Length;
                    bool hasB = i < bindposesB.Length;

                    if (!hasA || !hasB)
                    {
                        result.Add($"Bindpose[{i}]", GetRendererPath(a), GetRendererPath(b), "一侧存在 bindpose，另一侧不存在。");
                        continue;
                    }

                    if (!NearlyEqual(bindposesA[i], bindposesB[i], options.MatrixTolerance))
                    {
                        string bonePathA = i < a.bones.Length
                            ? GetRelativePathFromRendererRoot(a, a.bones[i])
                            : $"Index{i}";
                        string bonePathB = i < b.bones.Length
                            ? GetRelativePathFromRendererRoot(b, b.bones[i])
                            : $"Index{i}";

                        result.Add($"Bindpose[{i}]", bonePathA, bonePathB, "bindpose 矩阵不一致。");
                    }
                }
            }
        }

        private static void CompareBoneWeights(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            if (!options.CheckBoneWeights) return;

            var meshA = a.sharedMesh;
            var meshB = b.sharedMesh;

            if (meshA == null || meshB == null) return;

            if (options.CheckBoneWeightsCount)
            {
                int countA = GetBoneWeightCount(meshA);
                int countB = GetBoneWeightCount(meshB);
                if (countA != countB)
                {
                    result.Add(
                        "BoneWeights",
                        GetRendererPath(a),
                        GetRendererPath(b),
                        $"BoneWeight 数量不一致: A={countA}, B={countB}"
                    );
                }
            }

            if (!options.CheckBoneWeightsContent) return;

            // 先尝试兼容新版 BoneWeight1
            bool compared = CompareBoneWeightsUsingBoneWeight1(a, b, options, result);

            if (!compared)
            {
                CompareBoneWeightsUsingLegacyBoneWeight(a, b, options, result);
            }
        }

        private static bool CompareBoneWeightsUsingBoneWeight1(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            var meshA = a.sharedMesh;
            var meshB = b.sharedMesh;

            try
            {
                var bonesPerVertexA = meshA.GetBonesPerVertex();
                var bonesPerVertexB = meshB.GetBonesPerVertex();
                var allWeightsA = meshA.GetAllBoneWeights();
                var allWeightsB = meshB.GetAllBoneWeights();

                if (bonesPerVertexA.Length != bonesPerVertexB.Length)
                {
                    result.Add(
                        "BoneWeights",
                        GetRendererPath(a),
                        GetRendererPath(b),
                        $"BonesPerVertex 长度不一致: A={bonesPerVertexA.Length}, B={bonesPerVertexB.Length}"
                    );
                    return true;
                }

                if (allWeightsA.Length != allWeightsB.Length)
                {
                    result.Add(
                        "BoneWeights",
                        GetRendererPath(a),
                        GetRendererPath(b),
                        $"AllBoneWeights 长度不一致: A={allWeightsA.Length}, B={allWeightsB.Length}"
                    );
                    return true;
                }

                for (int i = 0; i < bonesPerVertexA.Length; i++)
                {
                    if (bonesPerVertexA[i] != bonesPerVertexB[i])
                    {
                        result.Add(
                            "BoneWeights",
                            GetRendererPath(a),
                            GetRendererPath(b),
                            $"顶点 {i} 的 bonesPerVertex 不一致: A={bonesPerVertexA[i]}, B={bonesPerVertexB[i]}"
                        );
                        return true;
                    }
                }

                for (int i = 0; i < allWeightsA.Length; i++)
                {
                    var wa = allWeightsA[i];
                    var wb = allWeightsB[i];

                    if (wa.boneIndex != wb.boneIndex || Mathf.Abs(wa.weight - wb.weight) > options.FloatTolerance)
                    {
                        result.Add(
                            "BoneWeights",
                            GetRendererPath(a),
                            GetRendererPath(b),
                            $"AllBoneWeights[{i}] 不一致: "
                            + $"A=(boneIndex={wa.boneIndex}, weight={wa.weight}), "
                            + $"B=(boneIndex={wb.boneIndex}, weight={wb.weight})"
                        );
                        return true;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CompareBoneWeightsUsingLegacyBoneWeight(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            var meshA = a.sharedMesh;
            var meshB = b.sharedMesh;

            var weightsA = meshA.boneWeights;
            var weightsB = meshB.boneWeights;

            if (weightsA.Length != weightsB.Length)
            {
                result.Add(
                    "BoneWeights",
                    GetRendererPath(a),
                    GetRendererPath(b),
                    $"boneWeights.Length 不一致: A={weightsA.Length}, B={weightsB.Length}"
                );
                return;
            }

            for (int i = 0; i < weightsA.Length; i++)
            {
                if (!NearlyEqual(weightsA[i], weightsB[i], options.FloatTolerance))
                {
                    result.Add("BoneWeights", GetRendererPath(a), GetRendererPath(b), $"boneWeights[{i}] 不一致。");
                    return;
                }
            }
        }

        private static void CompareRendererProperties(
            SkinnedMeshRenderer a,
            SkinnedMeshRenderer b,
            CompareOptions options,
            CompareResult result)
        {
            string pathA = GetRendererPath(a);
            string pathB = GetRendererPath(b);

            if (options.CheckQuality && a.quality != b.quality)
            {
                result.Add("RendererProperty", pathA, pathB, $"quality 不一致: A={a.quality}, B={b.quality}");
            }

            if (options.CheckUpdateWhenOffscreen && a.updateWhenOffscreen != b.updateWhenOffscreen)
            {
                result.Add(
                    "RendererProperty",
                    pathA,
                    pathB,
                    $"updateWhenOffscreen 不一致: A={a.updateWhenOffscreen}, B={b.updateWhenOffscreen}"
                );
            }

            if (options.CheckSkinnedMotionVectors && a.skinnedMotionVectors != b.skinnedMotionVectors)
            {
                result.Add(
                    "RendererProperty",
                    pathA,
                    pathB,
                    $"skinnedMotionVectors 不一致: A={a.skinnedMotionVectors}, B={b.skinnedMotionVectors}"
                );
            }

            if (options.CheckLocalBounds)
            {
                if (!NearlyEqual(a.localBounds.center, b.localBounds.center, options.BoundsTolerance)
                    || !NearlyEqual(a.localBounds.size, b.localBounds.size, options.BoundsTolerance))
                {
                    result.Add(
                        "RendererProperty",
                        pathA,
                        pathB,
                        $"localBounds 不一致: A(center={a.localBounds.center}, size={a.localBounds.size}), "
                        + $"B(center={b.localBounds.center}, size={b.localBounds.size})"
                    );
                }
            }
        }

        private static void CompareTransform(
            string category,
            Transform a,
            Transform b,
            CompareOptions options,
            CompareResult result)
        {
            string pathA = GetFullPath(a);
            string pathB = GetFullPath(b);

            if (!NearlyEqual(a.localPosition, b.localPosition, options.PositionTolerance))
            {
                result.Add(
                    category,
                    pathA,
                    pathB,
                    $"localPosition 不一致: A={a.localPosition}, B={b.localPosition}, Tol={options.PositionTolerance}"
                );
            }

            float angle = Quaternion.Angle(a.localRotation, b.localRotation);
            if (angle > options.RotationTolerance)
            {
                result.Add(
                    category,
                    pathA,
                    pathB,
                    $"localRotation 不一致: AngleDiff={angle}, A={a.localRotation.eulerAngles}, B={b.localRotation.eulerAngles}, Tol={options.RotationTolerance}"
                );
            }

            if (!NearlyEqual(a.localScale, b.localScale, options.ScaleTolerance))
            {
                result.Add(
                    category,
                    pathA,
                    pathB,
                    $"localScale 不一致: A={a.localScale}, B={b.localScale}, Tol={options.ScaleTolerance}"
                );
            }
        }

        private static Dictionary<string, Transform> BuildBoneMap(SkinnedMeshRenderer renderer, Transform[] bones)
        {
            var map = new Dictionary<string, Transform>();
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                string path = GetRelativePathFromRendererRoot(renderer, bone);
                if (!map.ContainsKey(path))
                {
                    map.Add(path, bone);
                }
            }

            return map;
        }

        public static int GetBoneWeightCount(Mesh mesh)
        {
            if (mesh == null) return 0;

            try
            {
                return mesh.GetAllBoneWeights().Length;
            }
            catch
            {
                return mesh.boneWeights != null ? mesh.boneWeights.Length : 0;
            }
        }

        public static string GetRendererPath(SkinnedMeshRenderer renderer)
        {
            return renderer != null ? GetFullPath(renderer.transform) : "NULL";
        }

        public static string GetRelativePathFromRendererRoot(SkinnedMeshRenderer renderer, Transform target)
        {
            if (target == null) return "NULL";

            if (renderer == null) return GetFullPath(target);

            Transform pivot = renderer.rootBone != null ? renderer.rootBone.root : renderer.transform.root;
            return GetRelativePath(target, pivot);
        }

        public static string GetRelativePath(Transform target, Transform root)
        {
            if (target == null) return "NULL";

            if (root == null) return GetFullPath(target);

            if (target == root) return root.name;

            var stack = new Stack<string>();
            Transform current = target;

            while (current != null && current != root)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            if (current == root)
            {
                stack.Push(root.name);
                return string.Join("/", stack);
            }

            return GetFullPath(target);
        }

        public static string GetFullPath(Transform target)
        {
            if (target == null) return "NULL";

            var stack = new Stack<string>();
            Transform current = target;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }

        private static bool NearlyEqual(Vector3 a, Vector3 b, float tolerance)
        {
            return Mathf.Abs(a.x - b.x) <= tolerance
                   && Mathf.Abs(a.y - b.y) <= tolerance
                   && Mathf.Abs(a.z - b.z) <= tolerance;
        }

        private static bool NearlyEqual(Matrix4x4 a, Matrix4x4 b, float tolerance)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > tolerance) return false;
            }

            return true;
        }

        private static bool NearlyEqual(BoneWeight a, BoneWeight b, float tolerance)
        {
            return a.boneIndex0 == b.boneIndex0
                   && a.boneIndex1 == b.boneIndex1
                   && a.boneIndex2 == b.boneIndex2
                   && a.boneIndex3 == b.boneIndex3
                   && Mathf.Abs(a.weight0 - b.weight0) <= tolerance
                   && Mathf.Abs(a.weight1 - b.weight1) <= tolerance
                   && Mathf.Abs(a.weight2 - b.weight2) <= tolerance
                   && Mathf.Abs(a.weight3 - b.weight3) <= tolerance;
        }
    }
}