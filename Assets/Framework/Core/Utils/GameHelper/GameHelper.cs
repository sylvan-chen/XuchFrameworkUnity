using UnityEngine;

namespace XuchFramework.Core.Utils
{
    public static class GameHelper
    {
        public static bool FloatEquals(float a, float b, float epsilon = 0.001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }
    }
}