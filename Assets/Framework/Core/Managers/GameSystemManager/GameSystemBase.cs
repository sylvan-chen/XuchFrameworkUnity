using UnityEngine;

namespace XuchFramework.Core.ECS
{
    public abstract class GameSystemBase : MonoBehaviour
    {
        internal void Initialize()
        {
            OnInitialize();
        }

        internal void Dispose()
        {
            OnDispose();
        }

        internal void InternalUpdate(float deltaTime, float unscaledDeltaTime)
        {
            OnUpdate(deltaTime, unscaledDeltaTime);
        }

        protected virtual void OnInitialize() { }

        protected virtual void OnDispose() { }

        protected virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }
    }
}