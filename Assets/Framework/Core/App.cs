namespace DigiEden.Framework
{
    /// <summary>
    /// 管理器的便捷访问入口
    /// </summary>
    public static class App
    {
        private static GameSettingManager _gameSettingManager;

        public static GameSettingManager GameSettingManager
        {
            get
            {
                if (_gameSettingManager == null && GameLauncher.Instance != null)
                {
                    _gameSettingManager = GameLauncher.Instance.GetManager<GameSettingManager>();
                }
                return _gameSettingManager;
            }
        }

        private static PoolManager _poolManager;

        public static PoolManager PoolManager
        {
            get
            {
                if (_poolManager == null && GameLauncher.Instance != null)
                {
                    _poolManager = GameLauncher.Instance.GetManager<PoolManager>();
                }
                return _poolManager;
            }
        }

        private static EventManager _eventManager;

        public static EventManager EventManager
        {
            get
            {
                if (_eventManager == null && GameLauncher.Instance != null)
                {
                    _eventManager = GameLauncher.Instance.GetManager<EventManager>();
                }
                return _eventManager;
            }
        }

        private static FsmManager _fsmManager;

        public static FsmManager FsmManager
        {
            get
            {
                if (_fsmManager == null && GameLauncher.Instance != null)
                {
                    _fsmManager = GameLauncher.Instance.GetManager<FsmManager>();
                }
                return _fsmManager;
            }
        }

        private static TableManager _tableManager;

        public static TableManager TableManager
        {
            get
            {
                if (_tableManager == null && GameLauncher.Instance != null)
                {
                    _tableManager = GameLauncher.Instance.GetManager<TableManager>();
                }
                return _tableManager;
            }
        }

        private static ResourceManager _resourceManager;

        public static ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null && GameLauncher.Instance != null)
                {
                    _resourceManager = GameLauncher.Instance.GetManager<ResourceManager>();
                }
                return _resourceManager;
            }
        }

        private static AudioManager _audioManager;

        public static AudioManager AudioManager
        {
            get
            {
                if (_audioManager == null && GameLauncher.Instance != null)
                {
                    _audioManager = GameLauncher.Instance.GetManager<AudioManager>();
                }
                return _audioManager;
            }
        }

        private static UIManager _uiManager;

        public static UIManager UIManager
        {
            get
            {
                if (_uiManager == null && GameLauncher.Instance != null)
                {
                    _uiManager = GameLauncher.Instance.GetManager<UIManager>();
                }
                return _uiManager;
            }
        }

        private static ProcedureManager _procedureManager;

        public static ProcedureManager ProcedureManager
        {
            get
            {
                if (_procedureManager == null && GameLauncher.Instance != null)
                {
                    _procedureManager = GameLauncher.Instance.GetManager<ProcedureManager>();
                }
                return _procedureManager;
            }
        }
    }
}