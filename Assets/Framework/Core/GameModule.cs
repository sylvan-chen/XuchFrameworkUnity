using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Core
{
    public interface IGameModule
    {
        public bool IsInitialized { get; }
        public bool IsDisposed { get; }

        public UniTask Initialize();
        public UniTask PostInitialize();
        public void Dispose();
        public void UpdateInternal();
        public void LateUpdateInternal();
        public void FixedUpdateInternal();
    }

    [DisallowMultipleComponent]
    public abstract class GameModule<T> : MonoBehaviour, IGameModule where T : MonoBehaviour
    {
        protected static T _instance;

        public static T Instance => _instance;

        public bool IsInitialized { get; private set; } = false;
        public bool IsDisposed { get; private set; } = false;

        public async UniTask Initialize()
        {
            try
            {
                MakeSingleton();
                OnInitialize();
                await OnInitializeAsync();
                IsInitialized = true;
                Log.Debug($"{GetType().Name} initialized.");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name} initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void MakeSingleton()
        {
            // Make sure the instance is unique
            if (_instance != null && _instance != this)
            {
                Log.Warning($"Multiple instances of {GetType().Name} found. Destroying the new instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        public async UniTask PostInitialize()
        {
            try
            {
                OnPostInitialize();
                await OnPostInitializeAsync();
                Log.Debug($"{GetType().Name} post-initialized.");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name} post-initialize failed： {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            try
            {
                OnDispose();
                Log.Debug($"{GetType().Name} disposed.");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name} disposed exception: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsDisposed = true;
                IsInitialized = false;
            }
        }

        public void UpdateInternal()
        {
            OnUpdate();
        }

        public void LateUpdateInternal()
        {
            OnLateUpdate();
        }

        public void FixedUpdateInternal()
        {
            OnFixedUpdate();
        }

        /// <summary>
        /// Only for self-initialization, do not reference other managers here
        /// </summary>
        protected virtual void OnInitialize() { }

        protected virtual UniTask OnInitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Reference other managers during startup in this method to ensure all managers have been initialized
        /// </summary>
        protected virtual void OnPostInitialize() { }

        protected virtual UniTask OnPostInitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnDispose() { }

        protected virtual void OnUpdate() { }

        protected virtual void OnLateUpdate() { }

        protected virtual void OnFixedUpdate() { }
    }
}