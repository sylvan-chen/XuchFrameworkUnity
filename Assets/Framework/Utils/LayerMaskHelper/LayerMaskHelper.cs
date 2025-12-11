using UnityEngine;

namespace Xuch.Framework.Utils
{
    public static class LayerMaskHelper
    {
        public static LayerMask GetPhysicsLayerMask(int currentLayer)
        {
            int finalMask = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(currentLayer, i))
                    finalMask = finalMask | (1 << i);
            }
            return finalMask;
        }
    }
}