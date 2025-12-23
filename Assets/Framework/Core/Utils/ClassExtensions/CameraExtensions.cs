using UnityEngine;

namespace XuchFramework.Extensions
{
    public static class CameraExtensions
    {
        public static void ExcludeLayer(this Camera cam, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Layer '{layerName}' 不存在");
                return;
            }

            cam.cullingMask &= ~(1 << layer);
        }

        public static void IncludeLayer(this Camera cam, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Layer '{layerName}' 不存在");
                return;
            }

            cam.cullingMask |= 1 << layer;
        }
    }
}