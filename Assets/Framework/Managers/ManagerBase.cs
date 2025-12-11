using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    /// <summary>
    /// 所有管理器的基类
    /// </summary>
    public abstract class ManagerBase : MonoBehaviour
    {
        public bool IsInitialized { get; private set; } = false;
        public bool IsDisposed { get; private set; } = false;

        internal async UniTask Initialize()
        {
            try
            {
                OnInitialize();
                await OnInitializeAsync();
                IsInitialized = true;
                Log.Debug($"{GetType().Name} initialized.");
            }
            catch (System.Exception ex)
            {
                Log.Error($"{GetType().Name} initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        internal async UniTask Startup()
        {
            try
            {
                OnStartup();
                await OnStartupAsync();
                Log.Debug($"{GetType().Name} started up.");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name} startup failed： {ex.Message}\n{ex.StackTrace}");
            }
        }

        internal void Dispose()
        {
            try
            {
                OnDispose();
                Log.Debug($"{GetType().Name} disposed.");
            }
            catch (System.Exception ex)
            {
                Log.Error($"{GetType().Name} disposed exception: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsDisposed = true;
                IsInitialized = false;
            }
        }

        internal void UpdateByPriority()
        {
            OnUpdate();
        }

        internal void LateUpdateByPriority()
        {
            OnLateUpdate();
        }

        internal void FixedUpdateByPriority()
        {
            OnFixedUpdate();
        }

        protected virtual void OnInitialize() { }

        protected virtual async UniTask OnInitializeAsync()
        {
            await UniTask.NextFrame();
        }

        protected virtual void OnStartup() { }

        protected virtual async UniTask OnStartupAsync()
        {
            await UniTask.NextFrame();
        }

        protected virtual void OnDispose() { }

        protected virtual void OnUpdate() { }

        protected virtual void OnLateUpdate() { }

        protected virtual void OnFixedUpdate() { }
    }
}