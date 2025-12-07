using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

//using Meta.XR.MRUtilityKit;
// using DG.Tweening;

/// <summary>
/// added by wsh @ 2017.12.25
/// 功能：通用静态方法
/// </summary>
namespace DigiEden.Framework.Utils
{
    public static class GameUtility
    {
        public const string AssetsFolderName = "Assets";

        public static GameObject FindGameObjectInParents(GameObject go, string name)
        {
            var parent = go.transform;
            while (parent != null)
            {
                if (parent.name == name)
                    return parent.gameObject;
                parent = parent.parent;
            }
            return null;
        }

        public static string FormatToUnityPath(string path)
        {
            return path.Replace("\\", "/");
        }

        public static string FormatToSysFilePath(string path)
        {
            return path.Replace("/", "\\");
        }

        public static string FullPathToAssetPath(string full_path)
        {
            full_path = FormatToUnityPath(full_path);
            if (!full_path.StartsWith(Application.dataPath))
            {
                return null;
            }

            string ret_path = full_path.Replace(Application.dataPath, "");
            return AssetsFolderName + ret_path;
        }

        public static string GetFileExtension(string path)
        {
            return Path.GetExtension(path).ToLower();
        }

        public static string[] GetSpecifyFilesInFolder(string path, string[] extensions = null, bool exclude = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (extensions == null)
            {
                return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            }
            else if (exclude)
            {
                return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(f => !extensions.Contains(GetFileExtension(f))).ToArray();
            }
            else
            {
                return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(f => extensions.Contains(GetFileExtension(f))).ToArray();
            }
        }

        public static string[] GetSpecifyFilesInFolder(string path, string pattern)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
        }

        public static string[] GetAllFilesInFolder(string path)
        {
            return GetSpecifyFilesInFolder(path);
        }

        public static string[] GetAllDirsInFolder(string path)
        {
            return Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
        }

        public static void CheckFileAndCreateDirWhenNeeded(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            FileInfo file_info = new FileInfo(filePath);
            DirectoryInfo dir_info = file_info.Directory;
            if (!dir_info.Exists)
            {
                Directory.CreateDirectory(dir_info.FullName);
            }
        }

        public static void CheckDirAndCreateWhenNeeded(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public static bool SafeWriteAllBytes(string outFile, byte[] outBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(outFile))
                {
                    return false;
                }

                CheckFileAndCreateDirWhenNeeded(outFile);
                if (File.Exists(outFile))
                {
                    File.SetAttributes(outFile, FileAttributes.Normal);
                }

                File.WriteAllBytes(outFile, outBytes);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeWriteAllBytes failed! path = {0} with err = {1}", outFile, ex.Message));
                return false;
            }
        }

        public static bool SafeWriteAllLines(string outFile, string[] outLines)
        {
            try
            {
                if (string.IsNullOrEmpty(outFile))
                {
                    return false;
                }

                CheckFileAndCreateDirWhenNeeded(outFile);
                if (File.Exists(outFile))
                {
                    File.SetAttributes(outFile, FileAttributes.Normal);
                }

                File.WriteAllLines(outFile, outLines);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeWriteAllLines failed! path = {0} with err = {1}", outFile, ex.Message));
                return false;
            }
        }

        public static bool SafeWriteAllText(string outFile, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(outFile))
                {
                    return false;
                }

                CheckFileAndCreateDirWhenNeeded(outFile);
                if (File.Exists(outFile))
                {
                    File.SetAttributes(outFile, FileAttributes.Normal);
                }

                File.WriteAllText(outFile, text);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeWriteAllText failed! path = {0} with err = {1}", outFile, ex.Message));
                return false;
            }
        }

        public static byte[] SafeReadAllBytes(string inFile)
        {
            try
            {
                if (string.IsNullOrEmpty(inFile))
                {
                    return null;
                }

                if (!File.Exists(inFile))
                {
                    return null;
                }

                File.SetAttributes(inFile, FileAttributes.Normal);
                return File.ReadAllBytes(inFile);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeReadAllBytes failed! path = {0} with err = {1}", inFile, ex.Message));
                return null;
            }
        }

        public static string[] SafeReadAllLines(string inFile)
        {
            try
            {
                if (string.IsNullOrEmpty(inFile))
                {
                    return null;
                }

                if (!File.Exists(inFile))
                {
                    return null;
                }

                File.SetAttributes(inFile, FileAttributes.Normal);
                return File.ReadAllLines(inFile);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeReadAllLines failed! path = {0} with err = {1}", inFile, ex.Message));
                return null;
            }
        }

        public static string SafeReadAllText(string inFile)
        {
            try
            {
                if (string.IsNullOrEmpty(inFile))
                {
                    return null;
                }

                if (!File.Exists(inFile))
                {
                    return null;
                }

                File.SetAttributes(inFile, FileAttributes.Normal);
                return File.ReadAllText(inFile);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeReadAllText failed! path = {0} with err = {1}", inFile, ex.Message));
                return null;
            }
        }

        public static void DeleteDirectory(string dirPath)
        {
            string[] files = Directory.GetFiles(dirPath);
            string[] dirs = Directory.GetDirectories(dirPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(dirPath, false);
        }

        public static bool SafeClearDir(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    return true;
                }

                if (Directory.Exists(folderPath))
                {
                    DeleteDirectory(folderPath);
                }

                Directory.CreateDirectory(folderPath);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeClearDir failed! path = {0} with err = {1}", folderPath, ex.Message));
                return false;
            }
        }

        public static bool SafeDeleteDir(string folderPath)
        {
            try
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    return true;
                }

                if (Directory.Exists(folderPath))
                {
                    DeleteDirectory(folderPath);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeDeleteDir failed! path = {0} with err: {1}", folderPath, ex.Message));
                return false;
            }
        }

        public static bool SafeDeleteFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return true;
                }

                if (!File.Exists(filePath))
                {
                    return true;
                }

                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeDeleteFile failed! path = {0} with err: {1}", filePath, ex.Message));
                return false;
            }
        }

        public static bool SafeRenameFile(string sourceFileName, string destFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceFileName))
                {
                    return false;
                }

                if (!File.Exists(sourceFileName))
                {
                    return true;
                }

                SafeDeleteFile(destFileName);
                File.SetAttributes(sourceFileName, FileAttributes.Normal);
                File.Move(sourceFileName, destFileName);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeRenameFile failed! path = {0} with err: {1}", sourceFileName, ex.Message));
                return false;
            }
        }

        public static bool SafeCopyFile(string fromFile, string toFile)
        {
            try
            {
                if (string.IsNullOrEmpty(fromFile))
                {
                    return false;
                }

                if (!File.Exists(fromFile))
                {
                    return false;
                }

                CheckFileAndCreateDirWhenNeeded(toFile);
                SafeDeleteFile(toFile);
                File.Copy(fromFile, toFile, true);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("SafeCopyFile failed! formFile = {0}, toFile = {1}, with err = {2}", fromFile, toFile, ex.Message));
                return false;
            }
        }

        public static string GetMD5Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            System.Security.Cryptography.MD5 md5Hash = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static string GetFileMD5Hash(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            if (!File.Exists(filename))
            {
                return null;
            }

            System.Security.Cryptography.MD5 md5Hash = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hash.ComputeHash(File.ReadAllBytes(filename));
            System.Text.StringBuilder sBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        public static Vector3 GetPosition(Transform transform)
        {
            return transform.position;
        }

        public static void SetPosition(Transform transform, Vector3 position)
        {
            transform.position = position;
        }

        public static Quaternion GetRotation(Transform transform)
        {
            return transform.rotation;
        }

        public static void SetRotation(Transform transform, Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public static Vector3 GetLocalPosition(Transform transform, Transform parent = null)
        {
            if (parent == null)
            {
                return transform.localPosition;
            }

            return parent.InverseTransformPoint(transform.position);
        }

        public static void SetLocalPosition(Transform transform, Vector3 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public static Quaternion GetLocalRotation(Transform transform, Transform parent = null)
        {
            if (parent == null)
            {
                return transform.localRotation;
            }

            return Quaternion.Inverse(parent.rotation) * transform.rotation;
        }

        public static void SetLocalRotation(Transform transform, Quaternion localRotation)
        {
            transform.localRotation = localRotation;
        }

        public static Vector3 GetLocalScale(Transform transform)
        {
            return transform.localScale;
        }

        public static void SetLocalScale(Transform transform, Vector3 localScale)
        {
            transform.localScale = localScale;
        }

        public static Vector3 GetEulerAngles(Transform transform)
        {
            return transform.eulerAngles;
        }

        public static Vector3 GetLocalEulerAngles(Transform transform, Transform parent = null)
        {
            Quaternion localRotation = GetLocalRotation(transform, parent);
            return QuaternionToEulerAngles(localRotation);
        }

        public static Vector3 QuaternionToEulerAngles(Quaternion quaternion)
        {
            return quaternion.eulerAngles;
        }

        public static void SetEulerAngles(Transform transform, Vector3 eulerAngles)
        {
            transform.eulerAngles = eulerAngles;
        }

        public static Vector3 CreateVector3(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        public static Vector2 CreateVector2(float x, float y)
        {
            return new Vector2(x, y);
        }

        public static Quaternion CreateQuaternionEuler(float x, float y, float z)
        {
            return Quaternion.Euler(x, y, z);
        }

        public static bool IsNull(UnityEngine.Object target)
        {
            return target == null;
        }

        public static void Destroy(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target, true);
            }
        }

        public static UnityEngine.Object FindObjectOfType(Type type)
        {
            // Debug.Log("FindObjectOfType: " + type);
            return UnityEngine.Object.FindFirstObjectByType(type);
        }

        public static bool RayCastUtil(
            Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        public static void SetGameObjectLayer(GameObject go, int layer, bool includeChildren = true)
        {
            go.layer = layer;
            if (includeChildren)
            {
                Transform[] children = go.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    children[i].gameObject.layer = layer;
                }
            }
        }

        //从起点到终点发射射线，检测碰撞到指定层级的物体，返回碰撞点和碰撞物体
        public static bool Raycast(Vector3 start, Vector3 end, int layerMask, out Vector3 hitPoint, out GameObject hitObject)
        {
            hitPoint = Vector3.zero;
            hitObject = null;
            RaycastHit hit;
            if (Physics.Linecast(start, end, out hit, layerMask))
            {
                hitPoint = hit.point;
                hitObject = hit.collider.gameObject;
                return true;
            }

            return false;
        }

        public static void RemoveComponentIfExits(GameObject obj, Type type)
        {
            var t = obj.GetComponent(type);
            if (t != null)
            {
                UnityEngine.Object.Destroy(t);
            }
        }

        public static void RemoveComponentIfExists<T>(GameObject obj) where T : Component
        {
            var t = obj.GetComponent<T>();
            if (t != null)
            {
                UnityEngine.Object.Destroy(t);
            }
        }

        public static void RemoveComponentsIfExits(GameObject obj, Type type)
        {
            var t = obj.GetComponents(type);
            for (var i = 0; i < t.Length; i++)
            {
                UnityEngine.Object.Destroy(t[i]);
            }
        }

        public static void RemoveComponentsIfExists<T>(GameObject obj) where T : Component
        {
            var t = obj.GetComponents<T>();
            for (var i = 0; i < t.Length; i++)
            {
                UnityEngine.Object.Destroy(t[i]);
            }
        }

        public static void RemoveComponentImmediateIfExists(GameObject obj, Type type)
        {
            var t = obj.GetComponent(type);
            if (t != null)
            {
                UnityEngine.Object.DestroyImmediate(t);
            }
        }

        public static void RemoveComponentImmediateIfExists<T>(GameObject obj) where T : Component
        {
            var t = obj.GetComponent<T>();
            if (t != null)
            {
                UnityEngine.Object.DestroyImmediate(t);
            }
        }

        public static void RemoveComponentsImmediateIfExists(GameObject obj, Type type)
        {
            var t = obj.GetComponents(type);
            for (var i = 0; i < t.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(t[i]);
            }
        }

        public static void RemoveComponentsImmediateIfExists<T>(GameObject obj) where T : Component
        {
            var t = obj.GetComponents<T>();
            for (var i = 0; i < t.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(t[i]);
            }
        }

        public static Transform RecursiveSearchChildren(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;
            Transform target = parent.Find(childName);
            if (target != null)
                return target;
            for (int i = 0; i < parent.childCount; i++)
            {
                target = RecursiveSearchChildren(parent.GetChild(i), childName);

                if (target != null)
                    return target;
            }

            return target;
        }

        // 记录所有子节点的Layer
        public static Dictionary<Transform, int> RecordLayers(Transform root)
        {
            var layers = new Dictionary<Transform, int>();
            RecordLayersRecursive(root, layers);
            return layers;
        }

        // 递归记录Layer
        private static void RecordLayersRecursive(Transform current, Dictionary<Transform, int> layers)
        {
            layers.Add(current, current.gameObject.layer);
            foreach (Transform child in current)
            {
                RecordLayersRecursive(child, layers);
            }
        }

        // 根据记录的Layer还原节点的Layer
        public static void RestoreLayers(Dictionary<Transform, int> layers)
        {
            foreach (var kvp in layers)
            {
                kvp.Key.gameObject.layer = kvp.Value;
            }
        }

        public static float DistanceBetweenPoints(Vector3 point1, Vector3 point2)
        {
            return Vector3.Distance(point1, point2);
        }

        /// <summary>
        /// Ignore collision between two game objects.
        /// </summary>
        /// <param name="gameObject1">GameObject 1</param>
        /// <param name="gameObject2">GameObject 2</param>
        /// <param name="ignore">true to ignore collision, false to enable collision</param>
        public static void IgnoreCollision2(GameObject gameObject1, GameObject gameObject2, bool ignore, bool IncludingTrigger = true)
        {
            Collider[] colliders1 = gameObject1.GetComponents<Collider>();
            Collider[] colliders2 = gameObject2.GetComponents<Collider>();
            foreach (Collider collider1 in colliders1)
            {
                foreach (Collider collider2 in colliders2)
                {
                    if (!IncludingTrigger && collider1.isTrigger && collider2.isTrigger)
                    {
                        continue;
                    }
                    Physics.IgnoreCollision(collider1, collider2, ignore);
                }
            }
        }

        private static int CalculateSurfaceDirection(Collider collider, Vector3 hitPoint)
        {
            // 将击中点转换到碰撞体的局部坐标系
            Vector3 localHitPoint = collider.transform.InverseTransformPoint(hitPoint);
            Vector3 localCenter;
            Vector3 size;
            if (collider is BoxCollider box)
            {
                size = box.size;
                localCenter = box.center;
            }
            else if (collider is SphereCollider sphere)
            {
                float diameter = sphere.radius * 2f;
                size = new Vector3(diameter, diameter, diameter);
                localCenter = sphere.center;
            }
            else if (collider is CapsuleCollider capsule)
            {
                float diameter = capsule.radius * 2f;
                size = capsule.direction switch
                {
                    0 => new Vector3(capsule.height, diameter, diameter), // X轴
                    1 => new Vector3(diameter, capsule.height, diameter), // Y轴
                    2 => new Vector3(diameter, diameter, capsule.height), // Z轴
                    _ => new Vector3(diameter, capsule.height, diameter)
                };
                localCenter = capsule.center;
            }
            else
            {
                // 对于其他碰撞体，使用bounds
                Bounds bounds = collider.bounds;
                size = bounds.size;
                localCenter = collider.transform.InverseTransformPoint(bounds.center);
            }

            // 计算击中点相对于碰撞体中心的局部位置
            Vector3 relativeHitPoint = localHitPoint - localCenter;

            // 归一化到 [-1, 1] 范围内
            Vector3 normalizedPosition = new Vector3(
                relativeHitPoint.x / (size.x * 0.5f),
                relativeHitPoint.y / (size.y * 0.5f),
                relativeHitPoint.z / (size.z * 0.5f));

            // 修正值：左右方向和上下方向需要更严格的判断
            float strictnessCorrection = 0.3f; // 降低阈值，让判断更合理

            // 获取各个方向的绝对值
            float absX = Mathf.Abs(normalizedPosition.x);
            float absY = Mathf.Abs(normalizedPosition.y);
            float absZ = Mathf.Abs(normalizedPosition.z);

            // 找出最大的绝对值分量
            float maxAbsValue = Mathf.Max(absX, absY, absZ);

            // 添加调试信息
            // Debug.Log($"Hit point: {hitPoint}, Local: {localHitPoint}, Normalized: {normalizedPosition}, Max: {maxAbsValue}");

            // 先检查是否满足严格性要求，再判断方向
            if (absX == maxAbsValue && absX > strictnessCorrection)
            {
                // 左右方向（X轴）- 需要达到更高的阈值才能被识别
                return normalizedPosition.x > 0 ? 1 : -1; // 正值为右，负值为左
            }
            else if (absY == maxAbsValue && absY > strictnessCorrection)
            {
                // 上下方向（Y轴）- 需要达到更高的阈值才能被识别
                return normalizedPosition.y > 0 ? 2 : -2; // 正值为上，负值为下
            }
            else
            {
                // 如果左右或上下方向不够明显，或者是前后方向，默认返回前后方向
                return normalizedPosition.z > 0 ? 3 : -3; // 正值为前，负值为后
            }
        }

        public static Quaternion GetRaySurfaceDirector(Collider collider, Vector3 hitPoint)
        {
            int surfaceDirection = CalculateSurfaceDirection(collider, hitPoint);
            var rotation = surfaceDirection switch
            {
                // 位于右方向，往左投影
                1 => Quaternion.LookRotation(-collider.transform.right),
                // 位于左方向，往右投影
                -1 => Quaternion.LookRotation(collider.transform.right),
                // 位于上方向，往下投影
                2 => Quaternion.LookRotation(-collider.transform.up),
                // 位于下方向，往上投影
                -2 => Quaternion.LookRotation(collider.transform.up),
                // 位于后方向，往前投影
                -3 => Quaternion.LookRotation(collider.transform.forward),
                // 默认往后投影
                _ => Quaternion.LookRotation(-collider.transform.forward),
            };
            return rotation;
        }

        public static void IgnoreCollision(GameObject gameObject1, GameObject gameObject2, bool ignore, bool IncludingTrigger = false)
        {
            // Debug.Log($"Ignoring collision between {gameObject1.name} and {gameObject2.name}: {ignore}");
            Collider[] colliders1 = gameObject1.GetComponentsInChildren<Collider>(true);
            Collider[] colliders2 = gameObject2.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider1 in colliders1)
            {
                if (collider1.isTrigger && !IncludingTrigger)
                {
                    continue;
                }
                foreach (Collider collider2 in colliders2)
                {
                    if (!IncludingTrigger && collider2.isTrigger)
                    {
                        continue;
                    }
                    Physics.IgnoreCollision(collider1, collider2, ignore);
                }
            }
        }

        public static void IgnoreCollision(GameObject gameObject, Collider collider, bool ignore, bool IncludingTrigger = false)
        {
            // Debug.Log($"Ignoring collision for {gameObject.name} with collider {collider.name}: {ignore}");
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (!IncludingTrigger && c.isTrigger)
                {
                    continue;
                }
                Physics.IgnoreCollision(c, collider, ignore);
            }
        }

        public static void IgnoreSelfCollision(GameObject gameObject, bool ignore, bool IncludingTrigger = false)
        {
            // Debug.Log($"Ignoring self collision for {gameObject.name}: {ignore}");
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!IncludingTrigger && colliders[i].isTrigger)
                {
                    continue;
                }
                for (int j = i + 1; j < colliders.Length; j++)
                {
                    if (!IncludingTrigger && colliders[j].isTrigger)
                    {
                        continue;
                    }
                    Physics.IgnoreCollision(colliders[i], colliders[j], ignore);
                }
            }
        }

        private static SimulationMode originSimulationMode = Physics.simulationMode;

        public static void PausePhysics(bool pause)
        {
            if (pause)
            {
                originSimulationMode = Physics.simulationMode;
                Physics.simulationMode = SimulationMode.Script;
                Physics.Simulate(0.003f);
            }
            else
            {
                Physics.simulationMode = originSimulationMode;
            }
        }

        /// <summary>
        /// Ignore collision between two layers.
        /// </summary>
        /// <param name="layer1">Layer 1</param>
        /// <param name="layer2">Layer 1</param>
        /// <param name="ignore">true to ignore collision, false to enable collision</param>
        public static void IgnoreLayerCollision(int layer1, int layer2, bool ignore)
        {
            Physics.IgnoreLayerCollision(layer1, layer2, ignore);
        }

        public static bool IsColorApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) && Mathf.Approximately(a.b, b.b);
        }

        public static bool IsVectorsApproximatelyEqual(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < 0.001f;
        }

        public static Transform GetTopmostParent(Transform transform)
        {
            if (transform == null)
                return null;

            while (transform.parent != null)
            {
                transform = transform.parent;
            }
            return transform;
        }

        public static Color HexToColor(string hex)
        {
            // 支持带或不带 #
            hex = hex.Replace("#", "");
            if (hex.Length == 6)
                hex += "FF"; // 补 Alpha
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        /// <summary>
        /// Execute action on next frame (async void for XLua compatibility)
        /// </summary>
        public static async void AsyncExecuteOnNextFrame(Action action)
        {
            await UniTask.Yield(PlayerLoopTiming.Update);
            // await UniTask.Yield(TimeSpan.FromSeconds(2));
            // await UniTask.Delay(TimeSpan.FromSeconds(1));
            action?.Invoke();
        }

        /// <summary>
        /// Execute action after specified frame count
        /// </summary>
        public static async void AsyncExecuteDelayFrames(Action action, int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            action?.Invoke();
        }

        #region SplineContainer

        public static Material[] CloneMaterials(Renderer renderer)
        {
            if (renderer == null || renderer.sharedMaterials == null)
            {
                return null;
            }

            Material[] clonedMaterials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                clonedMaterials[i] = new Material(renderer.sharedMaterials[i]);
            }
            return clonedMaterials;
        }

        public static Material CloneMaterial(Material material)
        {
            if (material == null)
            {
                return null;
            }

            return new Material(material);
        }

        public static float GetCSTime()
        {
            return Time.time;
        }

        public static MaterialPropertyBlock GetMaterialPropertyBlock(Renderer renderer, int index = 0)
        {
            if (renderer == null)
            {
                return null;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock, index);
            return propertyBlock;
        }

        public static void SetMaterialPropertyBlock(Renderer renderer, MaterialPropertyBlock propertyBlock, int index = 0)
        {
            if (renderer == null || propertyBlock == null)
            {
                return;
            }

            renderer.SetPropertyBlock(propertyBlock, index);
        }

        #endregion
    }
}