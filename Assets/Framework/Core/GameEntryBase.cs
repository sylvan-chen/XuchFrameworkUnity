using UnityEngine;

namespace Xuch.Framework
{
    public abstract class GameEntryBase : MonoBehaviour
    {
        /// <summary>
        /// GameLauncher 初始化时
        /// </summary>
        public abstract void OnLauncherInitialize();

        /// <summary>
        /// GameLauncher 启动时
        /// </summary>
        public abstract void OnLauncherStartup();
    }
}