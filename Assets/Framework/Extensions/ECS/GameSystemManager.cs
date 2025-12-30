using XuchFramework.Core;

namespace XuchFramework.Extensions.ECS
{
    public class GameSystemManager : ModuleBase
    {
        private GameSystemBase[] _systems;

        protected override void OnInitialize() { }

        protected override void OnPostInitialize()
        {
            _systems = GetComponentsInChildren<GameSystemBase>();

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