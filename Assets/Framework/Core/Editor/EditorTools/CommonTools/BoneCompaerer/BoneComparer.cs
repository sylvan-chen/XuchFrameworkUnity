using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Framework.Core.Editor
{
    /// <summary>
    /// 用于对比两个骨骼根节点下的整棵骨骼树是否一致，
    /// 以判断两个骨架是否可以安全互换 SkinnedMesh。
    /// </summary>
    public static class BoneComparer
    {
        [Serializable]
        public class CompareOptions
        {
            /// <summary>
            /// 是否检查骨骼名称。
            /// </summary>
            public bool CheckName = true;

            /// <summary>
            /// 是否检查层级路径。
            /// 路径比仅名称更严格，适合判断骨架是否真的一一对应。
            /// </summary>
            public bool CheckPath = true;

            /// <summary>
            /// 是否检查子节点数量。
            /// </summary>
            public bool CheckChildrenCount = true;

            /// <summary>
            /// 是否检查本地位置。
            /// </summary>
            public bool CheckLocalPosition = true;

            /// <summary>
            /// 是否检查本地旋转。
            /// </summary>
            public bool CheckLocalRotation = true;

            /// <summary>
            /// 是否检查本地缩放。
            /// </summary>
            public bool CheckLocalScale = true;

            /// <summary>
            /// 位置容差。
            /// </summary>
            public float PositionTolerance = 0.0001f;

            /// <summary>
            /// 旋转角度容差（度）。
            /// </summary>
            public float RotationTolerance = 0.01f;

            /// <summary>
            /// 缩放容差。
            /// </summary>
            public float ScaleTolerance = 0.0001f;

            /// <summary>
            /// 是否要求子节点顺序一致。
            /// 对于标准骨架通常建议为 true。
            /// 如果为 false，则会按名称匹配子节点。
            /// </summary>
            public bool RequireChildOrderMatch = true;
        }

        [Serializable]
        public class BoneDifference
        {
            public string PathA;
            public string PathB;
            public string Message;

            public BoneDifference(string pathA, string pathB, string message)
            {
                PathA = pathA;
                PathB = pathB;
                Message = message;
            }

            public override string ToString()
            {
                return $"[BoneDiff] A: {PathA} | B: {PathB} | {Message}";
            }
        }

        [Serializable]
        public class CompareResult
        {
            public Transform RootA;
            public Transform RootB;
            public bool IsMatch;
            public readonly List<BoneDifference> Differences = new List<BoneDifference>();

            public int DifferenceCount => Differences.Count;

            public void Add(string pathA, string pathB, string message)
            {
                Differences.Add(new BoneDifference(pathA, pathB, message));
            }

            public string GetReport()
            {
                var sb = new StringBuilder();
                sb.AppendLine("===== Bone Compare Result =====");
                sb.AppendLine($"RootA: {(RootA != null ? RootA.name : "NULL")}");
                sb.AppendLine($"RootB: {(RootB != null ? RootB.name : "NULL")}");
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

        /// <summary>
        /// 对比两个骨骼根节点。
        /// </summary>
        public static CompareResult Compare(Transform rootA, Transform rootB, CompareOptions options = null)
        {
            options ??= new CompareOptions();

            var result = new CompareResult { RootA = rootA, RootB = rootB, IsMatch = true };

            if (rootA == null && rootB == null)
            {
                result.IsMatch = true;
                return result;
            }

            if (rootA == null || rootB == null)
            {
                result.Add(
                    rootA != null ? GetPath(rootA, null) : "NULL",
                    rootB != null ? GetPath(rootB, null) : "NULL",
                    "其中一个骨骼根节点为空。"
                );
                result.IsMatch = false;
                return result;
            }

            CompareRecursive(rootA, rootB, rootA, rootB, options, result);

            result.IsMatch = result.Differences.Count == 0;
            return result;
        }

        /// <summary>
        /// 快速判断两个骨架是否可互换 SkinnedMesh。
        /// 默认使用较严格规则。
        /// </summary>
        public static bool CanReplaceSkinnedMesh(Transform rootA, Transform rootB, out string report)
        {
            var options = new CompareOptions
            {
                CheckName = true,
                CheckPath = true,
                CheckChildrenCount = true,
                CheckLocalPosition = true,
                CheckLocalRotation = true,
                CheckLocalScale = true,
                RequireChildOrderMatch = true,
                PositionTolerance = 0.0001f,
                RotationTolerance = 0.01f,
                ScaleTolerance = 0.0001f
            };

            var result = Compare(rootA, rootB, options);
            report = result.GetReport();
            return result.IsMatch;
        }

        private static void CompareRecursive(
            Transform currentA,
            Transform currentB,
            Transform rootA,
            Transform rootB,
            CompareOptions options,
            CompareResult result)
        {
            string pathA = currentA != null ? GetPath(currentA, rootA) : "NULL";
            string pathB = currentB != null ? GetPath(currentB, rootB) : "NULL";

            if (currentA == null && currentB == null) return;

            if (currentA == null || currentB == null)
            {
                result.Add(pathA, pathB, "一侧存在骨骼，另一侧不存在。");
                return;
            }

            if (options.CheckName && currentA.name != currentB.name)
            {
                result.Add(pathA, pathB, $"骨骼名称不一致: A='{currentA.name}', B='{currentB.name}'");
            }

            if (options.CheckPath && pathA != pathB)
            {
                result.Add(pathA, pathB, $"骨骼路径不一致: A='{pathA}', B='{pathB}'");
            }

            if (options.CheckLocalPosition
                && !NearlyEqual(currentA.localPosition, currentB.localPosition, options.PositionTolerance))
            {
                result.Add(
                    pathA,
                    pathB,
                    $"本地位置不一致: A={currentA.localPosition}, B={currentB.localPosition}, Tol={options.PositionTolerance}"
                );
            }

            if (options.CheckLocalRotation)
            {
                float angle = Quaternion.Angle(currentA.localRotation, currentB.localRotation);
                if (angle > options.RotationTolerance)
                {
                    result.Add(
                        pathA,
                        pathB,
                        $"本地旋转不一致: AngleDiff={angle}, A={currentA.localRotation.eulerAngles}, B={currentB.localRotation.eulerAngles}, Tol={options.RotationTolerance}"
                    );
                }
            }

            if (options.CheckLocalScale
                && !NearlyEqual(currentA.localScale, currentB.localScale, options.ScaleTolerance))
            {
                result.Add(
                    pathA,
                    pathB,
                    $"本地缩放不一致: A={currentA.localScale}, B={currentB.localScale}, Tol={options.ScaleTolerance}"
                );
            }

            if (options.CheckChildrenCount && currentA.childCount != currentB.childCount)
            {
                result.Add(pathA, pathB, $"子节点数量不一致: A={currentA.childCount}, B={currentB.childCount}");
            }

            if (options.RequireChildOrderMatch)
            {
                int max = Mathf.Max(currentA.childCount, currentB.childCount);
                for (int i = 0; i < max; i++)
                {
                    Transform childA = i < currentA.childCount ? currentA.GetChild(i) : null;
                    Transform childB = i < currentB.childCount ? currentB.GetChild(i) : null;

                    CompareRecursive(childA, childB, rootA, rootB, options, result);
                }
            }
            else
            {
                // 按名称匹配子节点，适合顺序可能不同但骨骼名称唯一的情况
                var mapA = BuildChildMap(currentA);
                var mapB = BuildChildMap(currentB);

                var allNames = new HashSet<string>(mapA.Keys);
                allNames.UnionWith(mapB.Keys);

                foreach (var childName in allNames)
                {
                    mapA.TryGetValue(childName, out var childA);
                    mapB.TryGetValue(childName, out var childB);

                    CompareRecursive(childA, childB, rootA, rootB, options, result);
                }
            }
        }

        private static Dictionary<string, Transform> BuildChildMap(Transform parent)
        {
            var map = new Dictionary<string, Transform>();

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                // 名称重复时保留第一个，并在比较阶段通过结构/数量差异暴露问题
                if (!map.ContainsKey(child.name)) map.Add(child.name, child);
            }

            return map;
        }

        /// <summary>
        /// 获取相对于 root 的层级路径。
        /// root 自己返回自身名字。
        /// </summary>
        private static string GetPath(Transform target, Transform root)
        {
            if (target == null) return "NULL";

            if (root == null) return target.name;

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
            }
            else
            {
                // target 不是 root 的子节点，返回全路径
                return GetFullPath(target);
            }

            return string.Join("/", stack);
        }

        private static string GetFullPath(Transform target)
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
    }
}