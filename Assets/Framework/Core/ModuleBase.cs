using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XuchFramework.Core
{
    public abstract class ModuleBase : MonoBehaviour
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
                Log.Debug($"{GetType().Name} initialized");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name} initialize failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        internal async UniTask PostInitialize()
        {
            try
            {
                OnPostInitialize();
                await OnPostInitializeAsync();
                Log.Debug($"{GetType().Name} post-initialized");
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name} post-initialize failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        internal void Dispose()
        {
            try
            {
                OnDispose();
                Log.Debug($"{GetType().Name} disposed");
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

        internal void UpdateInternal(float deltaTime, float unscaledDeltaTime)
        {
            OnUpdate(deltaTime, unscaledDeltaTime);
        }

        internal void LateUpdateInternal(float deltaTime, float unscaledDeltaTime)
        {
            OnLateUpdate(deltaTime, unscaledDeltaTime);
        }

        internal void FixedUpdateInternal(float fixedDeltaTime)
        {
            OnFixedUpdate(fixedDeltaTime);
        }

        protected virtual void OnInitialize() { }

        protected virtual UniTask OnInitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnPostInitialize() { }

        protected virtual UniTask OnPostInitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnDispose() { }

        protected virtual void OnUpdate(float deltaTime, float unscaledDeltaTime) { }

        protected virtual void OnLateUpdate(float deltaTime, float unscaledDeltaTime) { }

        protected virtual void OnFixedUpdate(float fixedDeltaTime) { }
    }
}