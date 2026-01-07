using System;
using XuchFramework.Extensions.ECS;

namespace GamePlay
{
    [Serializable]
    public struct PositionComponent : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }

    [Serializable]
    public struct VelocityComponent : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }
}