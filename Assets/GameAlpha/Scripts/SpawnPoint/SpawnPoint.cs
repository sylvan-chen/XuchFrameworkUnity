using Cysharp.Threading.Tasks;
using UnityEngine;
using Xuch.Framework;

namespace Xuch.Gameplay
{
    public class SpawnPoint : MonoBehaviour
    {
        private GameObject _player;
        private GameObject _mountable;
        private GameObject _tiger;

        private void Start()
        {
            SpawnPlayer().Forget();
        }

        private void OnDestroy()
        {
            if (App.ResourceManager != null)
            {
                App.ResourceManager.DestroyInstance(_player);
                App.ResourceManager.DestroyInstance(_mountable);
            }
        }

        private async UniTaskVoid SpawnPlayer()
        {
            _player = await App.ResourceManager.InstantiateAsync("Assets/Res/core/prefabs/player_rig_2.prefab", transform);
            _player.transform.localPosition = Vector3.zero;
            _player.transform.localRotation = Quaternion.identity;
            _player.transform.SetParent(null);

            // App.PhotonSceneManager.ConnectScene().AsUniTask().Forget();
            // if ( App.PhotonSceneManager.BLocal)
            // {
            _tiger = await App.ResourceManager.InstantiateAsync("Assets/Res/game/models/tiger/tiger.prefab", transform);
            _tiger.transform.localPosition = Vector3.zero;
            _tiger.transform.localRotation = Quaternion.identity;
            _tiger.transform.SetParent(null);
            // }
        }

#if UNITY_EDITOR
        [SerializeField]
        private float gizmoHeight = 1.8f;

        [SerializeField]
        private float gizmoRadius = 0.35f;

        [SerializeField]
        private float arrowOffset = 0.1f;

        [SerializeField]
        private float arrowLength = 1f;

        private void OnDrawGizmos()
        {
            // 设置Gizmo颜色
            Gizmos.color = new Color(0, 1, 1, 0.3f); // 半透明青色

            // 绘制一个胶囊体表示玩家位置（高度1.8米，半径0.3米，模拟玩家大小）
            DrawWireCapsule(transform.position, transform.rotation, gizmoHeight, gizmoRadius, Color.cyan);

            // 绘制箭头表示方向（从胶囊体底部稍微上方开始）
            Vector3 arrowStart = transform.position + Vector3.up * arrowOffset;
            DrawArrow(arrowStart, transform.forward, Color.green, arrowLength);
        }

        private void DrawWireCapsule(Vector3 position, Quaternion rotation, float height, float radius, Color color)
        {
            Gizmos.color = color;

            // 计算胶囊体的上下半球中心点
            float cylinderHeight = height - 2 * radius;
            Vector3 upper = position + Vector3.up * (cylinderHeight / 2);
            Vector3 lower = position - Vector3.up * (cylinderHeight / 2);

            // 绘制上下两个半球（用圆圈近似）
            DrawWireCircle(upper, radius, Vector3.up);
            DrawWireCircle(upper, radius, Vector3.right);
            DrawWireCircle(upper, radius, Vector3.forward);

            DrawWireCircle(lower, radius, Vector3.up);
            DrawWireCircle(lower, radius, Vector3.right);
            DrawWireCircle(lower, radius, Vector3.forward);

            // 绘制连接上下的四条边线
            Gizmos.DrawLine(upper + Vector3.forward * radius, lower + Vector3.forward * radius);
            Gizmos.DrawLine(upper - Vector3.forward * radius, lower - Vector3.forward * radius);
            Gizmos.DrawLine(upper + Vector3.right * radius, lower + Vector3.right * radius);
            Gizmos.DrawLine(upper - Vector3.right * radius, lower - Vector3.right * radius);
        }

        private void DrawWireCircle(Vector3 center, float radius, Vector3 normal)
        {
            Vector3 forward = Vector3.Slerp(normal, -normal, 0.5f);
            if (forward.magnitude < 0.1f)
                forward = Vector3.up;

            Vector3 right = Vector3.Cross(normal, forward).normalized;
            forward = Vector3.Cross(right, normal).normalized;

            int segments = 20;
            Vector3 prevPoint = center + right * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2;
                Vector3 nextPoint = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        private void DrawArrow(Vector3 position, Vector3 direction, Color color, float length)
        {
            Gizmos.color = color;

            // 绘制箭头主干
            Vector3 endPoint = position + direction * length;
            Gizmos.DrawLine(position, endPoint);

            // 绘制箭头头部
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;

            float arrowHeadLength = length * 0.25f;
            Gizmos.DrawLine(endPoint, endPoint + right * arrowHeadLength);
            Gizmos.DrawLine(endPoint, endPoint + left * arrowHeadLength);
        }
#endif
    }
}