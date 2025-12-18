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

        private readonly List<ManagerBase> _cachedManagers = new();

        protected override async UniTask OnInitialize()
        {
            // 1. Initialize core managers
            await LaunchManagers("[core_managers]");

            // 2. Enter game
            if (_entryType == GameEntryType.CustomEntry)
                _gameEntry.EnterGame().Forget();
            else
                GameProcedure.Instance.Startup();
        }

        /// <summary>
        /// Launch managers under the specified root
        /// </summary>
        public async UniTask LaunchManagers(string rootName)
        {
            var managerRoot = transform.Find(rootName);
            if (managerRoot == null)
            {
                Log.Error($"[GameRunner] Launch managers failed. Can not find root for managers (Expected root name: '{rootName}')");
                return;
            }

            var managers = managerRoot.GetComponentsInChildren<ManagerBase>();
            Log.Debug($"[GameRunner] Found {managers.Length} managers under '{rootName}'");

            var initializeTasks = new List<UniTask>();
            var postInitializeTasks = new List<UniTask>();

            foreach (var manager in managers)
            {
                RegisterManager(manager);
                initializeTasks.Add(manager.Initialize());
                postInitializeTasks.Add(manager.PostInitialize());
            }

            await UniTask.WhenAll(initializeTasks);
            await UniTask.WhenAll(postInitializeTasks);
        }

        /// <summary>
        /// Register manager as GameModule instance, and cache it for update loop and dispose
        /// </summary>
        internal void RegisterManager(ManagerBase manager)
        {
            if (_cachedManagers.Any(x => x == manager))
            {
                Log.Warning($"[GameRunner] Duplicate manager register. Manager '{manager.GetType().FullName}' has already been registered");
                return;
            }

            var type = manager.GetType();
            var genericType = typeof(GameModule<>).MakeGenericType(type);
            var method = genericType.GetMethod("SetInstance", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                Log.Error($"[GameRunner] GameModule must have method 'SetInstance'. Error type for {genericType.FullName}");
                return;
            }
            method.Invoke(null, new object[] { manager });

            _cachedManagers.Add(manager);
        }

        private void Update()
        {
            for (int i = 0; i < _cachedManagers.Count; i++)
            {
                var manager = _cachedManagers[i];
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.UpdateInternal(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _cachedManagers.Count; i++)
            {
                var manager = _cachedManagers[i];
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.LateUpdateInternal(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _cachedManagers.Count; i++)
            {
                var manager = _cachedManagers[i];
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.FixedUpdateInternal(Time.fixedDeltaTime);
                }
            }
        }

        private void OnDestroy()
        {
            for (int i = _cachedManagers.Count - 1; i >= 0; i--)
            {
                var manager = _cachedManagers[i];
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.Dispose();
                }
            }
        }

        private bool IsCustomEntry => _entryType == GameEntryType.CustomEntry;
    }
}