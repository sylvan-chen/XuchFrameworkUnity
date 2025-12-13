using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ProcedureManager))]
    [AddComponentMenu("XuchFramework/Game Runner")]
    public sealed class GameRunner : MonoSingletonPersistent<GameRunner>
    {
        private ProcedureManager _procedureManager;
        private readonly List<ManagerBase> _cachedManagers = new();

        protected override void Awake()
        {
            base.Awake();
            _procedureManager = GetComponent<ProcedureManager>();
        }

        private void Start()
        {
            LaunchGame().Forget();
        }

        private async UniTaskVoid LaunchGame()
        {
            // 1. Initialize core managers

            var coreManagerRoot = transform.Find("[core_managers]");
            if (coreManagerRoot == null)
            {
                Log.Error("[GameRunner] Launch game failed. Can't find root for core managers (Expected root name: '[core_managers]')");
                return;
            }

            var coreManagers = coreManagerRoot.GetComponentsInChildren<ManagerBase>();
            Log.Debug($"[GameRunner] Found {coreManagers.Length} core managers");

            foreach (var manager in coreManagers)
            {
                RegisterManager(manager);
                await manager.Initialize();
            }

            foreach (var manager in coreManagers)
            {
                await manager.PostInitialize();
            }

            // 2. Start procedure

            RegisterManager(_procedureManager);
            await _procedureManager.Initialize();
            await _procedureManager.PostInitialize();

            _procedureManager.Startup();
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

        /// <summary>
        /// Register manager as GameModule instance, and cache it for update loop and dispose.
        /// </summary>
        internal void RegisterManager(ManagerBase mgr)
        {
            var type = mgr.GetType();
            var genericType = typeof(GameModule<>).MakeGenericType(type);
            var method = genericType.GetMethod("SetInstance", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                Log.Error($"[GameRunner] GameModule must have method 'SetInstance'. Error type for {genericType.FullName}");
                return;
            }
            method.Invoke(null, new object[] { mgr });

            _cachedManagers.Add(mgr);
        }
    }
}