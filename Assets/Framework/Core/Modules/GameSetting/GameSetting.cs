using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Modules/Game Setting")]
    public sealed class GameSetting : ModuleBase
    {
        [SerializeField, Tooltip("Frame rate, -1 presents to use the platform's default frame rate")]
        private int _frameRate = -1;
        [SerializeField]
        private float _gameSpeed = 1f;
        [SerializeField]
        private bool _allowRunInBackground = true;
        [SerializeField]
        private bool _neverSleep = false;

        private float _gameSpeedBeforePause = 1f;

        private Action OnLowMemory;

        public int FrameRate
        {
            get => _frameRate;
            set => Application.targetFrameRate = _frameRate = value;
        }

        public float GameSpeed
        {
            get => _gameSpeed;
            private set => Time.timeScale = _gameSpeed = value >= 0f ? value : 0f;
        }

        public bool IsGamePaused => Time.timeScale == 0f;

        public bool AllowRunInBackground
        {
            get => _allowRunInBackground;
            set => Application.runInBackground = _allowRunInBackground = value;
        }

        public bool NeverSleep
        {
            get => _neverSleep;
            set
            {
                _neverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        protected override UniTask OnInitialize()
        {
#if UNITY_5_3_OR_NEWER
            Application.targetFrameRate = _frameRate;
            Application.runInBackground = _allowRunInBackground;
            Time.timeScale = _gameSpeed;
            Screen.sleepTimeout = _neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
#else
            Log.Fatal("Framework just support Unity 5.3 or later");
            Application.Quit();
#endif
            return UniTask.CompletedTask;
        }

        private void OnEnable()
        {
#if UNITY_5_6_OR_NEWER
            Application.lowMemory += HandleLowMemory;
#endif
        }

        private void OnDisable()
        {
#if UNITY_5_6_OR_NEWER
            Application.lowMemory -= HandleLowMemory;
#endif
        }

        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }

            _gameSpeedBeforePause = _gameSpeed;
            GameSpeed = 0f;
        }

        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }

            GameSpeed = _gameSpeedBeforePause;
        }

        public void ResetGameSpeed()
        {
            GameSpeed = 1f;
        }

        private void HandleLowMemory()
        {
            Log.Warning("[GameSetting] Low memory reported...");
            OnLowMemory?.Invoke();
        }
    }
}