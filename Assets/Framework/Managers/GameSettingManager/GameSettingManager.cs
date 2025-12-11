using UnityEngine;
using DigiEden.Framework.Utils;

namespace DigiEden.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/Game Setting Manager")]
    public sealed class GameSettingManager : ManagerBase
    {
        [SerializeField, Tooltip("帧率")]
        private int _frameRate = -1;

        [SerializeField, Tooltip("游戏速度")]
        private float _gameSpeed = 1f;

        [SerializeField, Tooltip("允许后台运行")]
        private bool _allowRunInBackground = true;

        [SerializeField, Tooltip("保持屏幕常亮")]
        private bool _neverSleep = false;

        private float _gameSpeedBeforePause = 1f;

        /// <summary>
        /// 帧率
        /// </summary>
        public int FrameRate
        {
            get => _frameRate;
            set => Application.targetFrameRate = _frameRate = value;
        }

        /// <summary>
        /// 游戏速度
        /// </summary>
        public float GameSpeed
        {
            get => _gameSpeed;
            private set => Time.timeScale = _gameSpeed = value >= 0f ? value : 0f;
        }

        /// <summary>
        /// 游戏是否暂停
        /// </summary>
        public bool IsGamePaused => Time.timeScale == 0f;

        /// <summary>
        /// 允许后台运行
        /// </summary>
        public bool AllowRunInBackground
        {
            get => _allowRunInBackground;
            set => Application.runInBackground = _allowRunInBackground = value;
        }

        /// <summary>
        /// 保持屏幕常亮
        /// </summary>
        public bool NeverSleep
        {
            get => _neverSleep;
            set
            {
                _neverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
#if UNITY_5_3_OR_NEWER
            Application.targetFrameRate = _frameRate;
            Application.runInBackground = _allowRunInBackground;
            Time.timeScale = _gameSpeed;
            Screen.sleepTimeout = _neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
#else
            Log.Fatal("Framework just support Unity 5.3 or later");
            Application.Quit();
#endif
#if UNITY_5_6_OR_NEWER
            Application.lowMemory += OnLowMemory;
#endif
        }

        protected override void OnDispose()
        {
            base.OnDispose();
#if UNITY_5_6_OR_NEWER
            Application.lowMemory -= OnLowMemory;
#endif
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }

            _gameSpeedBeforePause = _gameSpeed;
            GameSpeed = 0f;
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }

            GameSpeed = _gameSpeedBeforePause;
        }

        /// <summary>
        /// 重置游戏速度
        /// </summary>
        public void ResetGameSpeed()
        {
            GameSpeed = 1f;
        }

        /// <summary>
        /// 处理内存不足的情况
        /// </summary>
        private void OnLowMemory()
        {
            Log.Warning("[XFramework] [GameSetting] Low memory reported...");
            // TODO: 处理内存不足的情况
        }
    }
}