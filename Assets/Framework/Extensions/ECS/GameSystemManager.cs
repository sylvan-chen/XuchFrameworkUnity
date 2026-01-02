using UnityEngine;
using XuchFramework.Core;

namespace XuchFramework.Extensions.ECS
{
    public class GameSystemManager : ModuleBase
    {
        [SerializeField]
        private Transform _gameSystemRoot;

        private GameSystemBase[] _systems;

        protected override void OnPostInitialize()
        {
            _systems = _gameSystemRoot.GetComponentsInChildren<GameSystemBase>();

            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Initialize();
            }
        }

        protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].InternalUpdate(deltaTime, unscaledDeltaTime);
            }
        }

        protected override void OnDispose()
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Dispose();
            }
        }
    }
}