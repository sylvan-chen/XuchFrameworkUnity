using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    /// <summary>
    /// 游戏启动器
    /// </summary>
    public class GameLauncher : MonoSingletonPersistent<GameLauncher>
    {
        [Space(5)]
        [SerializeField]
        private GameEntryBase _gameEntry;
        [SerializeField]
        private Transform _managerRoot;

        // 存储所有 Manager 的字典
        private readonly Dictionary<Type, ManagerBase> _managerMap = new();
        // 按层级顺序存储的 Manager 列表（用于按顺序执行生命周期方法）
        private readonly List<ManagerBase> _managerList = new();

        public bool IsInitialized { get; private set; } = false;

        #region 公开接口

        /// <summary>
        /// 获取指定类型的Manager
        /// </summary>
        public T GetManager<T>() where T : ManagerBase
        {
            var type = typeof(T);

            if (_managerMap.TryGetValue(type, out var manager))
            {
                return manager as T;
            }

            Log.Error($"[GameLauncher] Manager of type '{type.Name}' not found.");
            return null;
        }

        #endregion

        #region 初始化

        protected override void Awake()
        {
            base.Awake();

            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            if (IsInitialized)
                return;

            LoadManagers();
            await InitializeManagersAsync();
            _singletonState = MonoSingletonState.Initialized;

            _gameEntry.LaunchGame();
        }

        private void LoadManagers()
        {
            var managers = transform.GetComponentsInChildren<ManagerBase>();
            foreach (var manager in managers)
            {
                var type = manager.GetType();

                if (_managerMap.ContainsKey(type))
                {
                    Log.Warning($"[GameLauncher] Duplicate manager type found: {type.Name}");
                    continue;
                }

                _managerMap[type] = manager;
                _managerList.Add(manager);
            }
        }

        private async UniTask InitializeManagersAsync()
        {
            foreach (var manager in _managerList)
            {
                if (manager.IsInitialized)
                    continue;
                await manager.Initialize();
            }

            IsInitialized = true;
        }

        #endregion

        #region 启动

        private void Start()
        {
            StartupAsync().Forget();
        }

        private async UniTaskVoid StartupAsync()
        {
            await UniTask.WaitUntil(() => IsInitialized);

            await StartupManagersAsync();
            _gameEntry.EnterGame();
        }

        private async UniTask StartupManagersAsync()
        {
            foreach (var manager in _managerList)
            {
                if (!manager.IsInitialized)
                    continue;
                await manager.Startup();
            }
        }

        #endregion

        #region 更新

        private void Update()
        {
            foreach (var manager in _managerList)
            {
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.UpdateByPriority();
                }
            }
        }

        private void LateUpdate()
        {
            foreach (var manager in _managerList)
            {
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.LateUpdateByPriority();
                }
            }
        }

        private void FixedUpdate()
        {
            foreach (var manager in _managerList)
            {
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.FixedUpdateByPriority();
                }
            }
        }

        #endregion

        #region 销毁

        private void OnDestroy()
        {
            for (int i = _managerList.Count - 1; i >= 0; i--)
            {
                var manager = _managerList[i];
                if (manager.IsInitialized && !manager.IsDisposed)
                {
                    manager.Dispose();
                }
            }
        }

        #endregion
    }
}