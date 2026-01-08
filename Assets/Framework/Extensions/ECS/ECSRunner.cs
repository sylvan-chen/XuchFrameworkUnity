using System.Collections.Generic;
using UnityEngine;
using XuchFramework.Core;

namespace XuchFramework.Extensions.ECS
{
    public class ECSRunner : MonoSingleton<ECSRunner>
    {
        private WorldContext _world;
        private List<SystemBase> _systems = new();

        private void Start()
        {
            _world = new WorldContext();
        }

        private void Update()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy() { }

        public void RegisterSystem(SystemBase system)
        {
            system.Initialize(_world);
            _systems.Add(system);
        }
    }
}