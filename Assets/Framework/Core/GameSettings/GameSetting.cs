using System;
using UnityEngine;

namespace Framework.Core
{
    public static class GameSettings
    {
        private static float _gameSpeedBeforePause = 1f;

        private static event Action OnLowMemory;

        public static void Initialize()
        {
#if UNITY_5_6_OR_NEWER
            Application.lowMemory += HandleLowMemory;
#endif
        }

        public static void Dispose()
        {
#if UNITY_5_6_OR_NEWER
            Application.lowMemory -= HandleLowMemory;
#endif
        }

        public static int FrameRate
        {
            get => Application.targetFrameRate;
            set => Application.targetFrameRate = value;
        }

        public static float GameSpeed
        {
            get => Time.timeScale;
            private set => Time.timeScale = value >= 0f ? value : 0f;
        }

        public static bool IsGamePaused => Time.timeScale == 0f;

        public static bool AllowRunInBackground
        {
            get => Application.runInBackground;
            set => Application.runInBackground = value;
        }

        public static bool NeverSleep
        {
            get => Screen.sleepTimeout is SleepTimeout.NeverSleep;
            set { Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting; }
        }

        public static void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }

            _gameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        public static void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }

            GameSpeed = _gameSpeedBeforePause;
        }

        public static void ResetGameSpeed()
        {
            GameSpeed = 1f;
        }

        private static void HandleLowMemory()
        {
            Log.Warning("[GameSettings] Low memory reported...");
            OnLowMemory?.Invoke();
        }
    }
}