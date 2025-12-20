using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemy.Inspector;
using Cysharp.Threading.Tasks;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Game Runner")]
    public sealed class GameRunner : MonoSingletonPersistent<GameRunner>
    {
        public enum GameEntryType
        {
            CustomEntry,
            Procedure
        }

        [Space(5)]
        [SerializeField]
        private GameEntryType _entryType = GameEntryType.CustomEntry;
        [SerializeField, ShowIf(nameof(IsCustomEntry))]
        private GameEntryBase _gameEntry;

        private readonly List<ModuleBase> _cachedModules = new();

        protected override async UniTask OnInitialize()
        {
            // 1. Initialize core modules
            await LaunchModules("[core_modules]");

            // 2. Enter game
            if (_entryType == GameEntryType.CustomEntry)
                _gameEntry.EnterGame().Forget();
            else
                GameProcedure.Instance.Startup();
        }

        /// <summary>
        /// Launch modules under the specified root
        /// </summary>
        public async UniTask LaunchModules(string rootName)
        {
            var moduleRoot = transform.Find(rootName);
            if (moduleRoot == null)
            {
                Log.Error($"[GameRunner] Launch modules failed. Can not find root for modules (Expected root name: '{rootName}')");
                return;
            }

            var modules = moduleRoot.GetComponentsInChildren<ModuleBase>();
            Log.Debug($"[GameRunner] Found {modules.Length} modules under '{rootName}'");

            var initializeTasks = new List<UniTask>();
            var postInitializeTasks = new List<UniTask>();

            foreach (var module in modules)
            {
                RegisterModule(module);
                initializeTasks.Add(module.Initialize());
                postInitializeTasks.Add(module.PostInitialize());
            }

            await UniTask.WhenAll(initializeTasks);
            await UniTask.WhenAll(postInitializeTasks);
        }

        /// <summary>
        /// Register module as GameModule instance, and cache it for update loop and dispose
        /// </summary>
        internal void RegisterModule(ModuleBase module)
        {
            if (_cachedModules.Any(x => x == module))
            {
                Log.Warning($"[GameRunner] Duplicate module register. Module '{module.GetType().FullName}' has already been registered");
                return;
            }

            var type = module.GetType();
            var genericType = typeof(GameModule<>).MakeGenericType(type);
            var method = genericType.GetMethod("SetInstance", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                Log.Error($"[GameRunner] GameModule must have method 'SetInstance'. Error type for {genericType.FullName}");
                return;
            }
            method.Invoke(null, new object[] { module });

            _cachedModules.Add(module);
        }

        private void Update()
        {
            for (int i = 0; i < _cachedModules.Count; i++)
            {
                var module = _cachedModules[i];
                if (module.IsInitialized && !module.IsDisposed)
                {
                    module.UpdateInternal(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _cachedModules.Count; i++)
            {
                var module = _cachedModules[i];
                if (module.IsInitialized && !module.IsDisposed)
                {
                    module.LateUpdateInternal(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _cachedModules.Count; i++)
            {
                var module = _cachedModules[i];
                if (module.IsInitialized && !module.IsDisposed)
                {
                    module.FixedUpdateInternal(Time.fixedDeltaTime);
                }
            }
        }

        private void OnDestroy()
        {
            for (int i = _cachedModules.Count - 1; i >= 0; i--)
            {
                var module = _cachedModules[i];
                if (module.IsInitialized && !module.IsDisposed)
                {
                    module.Dispose();
                }
            }
        }

        private bool IsCustomEntry => _entryType == GameEntryType.CustomEntry;

        [Button]
        public void LogTime()
        {
            var now = DateTime.Now;
            Log.Debug($"{now.Year}_{now.Month:D2}_{now.Day:D2}_{now.Hour:D2}_{now.Minute:D2}_{now.Second:D2}");
        }
    }
}