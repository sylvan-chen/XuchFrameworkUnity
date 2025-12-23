using System;

namespace XuchFramework.Core.Utils
{
    /// <summary> A mask to represent the X, Y, and Z axes </summary>
    /// <remarks> It shows on inspector like -> [ ] X [ ] Y [ ] Z </remarks>
    [Serializable]
    public class AxisMask
    {
        public bool X;
        public bool Y;
        public bool Z;
    }
}