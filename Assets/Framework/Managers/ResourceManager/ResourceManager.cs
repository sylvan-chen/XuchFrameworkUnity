using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/Resource Manager")]
    public partial class ResourceManager : ManagerBase
    {
        #region 生命周期

        protected override async UniTask OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            // 预热 Addressables (可选，Addressbale 本身有懒加载机制)
            var handle = Addressables.InitializeAsync();
            await handle.ToUniTask();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _cachedSceneHandle.Clear();
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async UniTask<ResourceHandle<T>> LoadAssetAsync<T>(string key)
        {
            if (!ValidateKey(key))
                return ResourceHandle<T>.Failed(key);

            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return ResourceHandle<T>.Succeed(key, handle);
            }
            else
            {
                Log.Error($"[ResourceManager] Failed to load asset for {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                return ResourceHandle<T>.Failed(key);
            }
        }

        /// <summary>
        /// 异步加载资源（回调方式）
        /// </summary>
        public void LoadAssetAsync<T>(string key, Action<ResourceHandle<T>> callback)
        {
            if (!ValidateKey(key))
            {
                callback?.Invoke(ResourceHandle<T>.Failed(key));
                return;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(ResourceHandle<T>.Succeed(key, op));
                }
                else
                {
                    Log.Error($"[ResourceManager] Load asset failed for {key} : {op.OperationException?.Message}");
                    Addressables.Release(op);
                    callback?.Invoke(ResourceHandle<T>.Failed(key));
                }
            };
        }

        /// <summary>
        /// 同步加载资源（阻塞方式，不推荐）
        /// </summary>
        [Obsolete("Synchronous loading is not recommended, please use LoadAssetAsync instead.")]
        public ResourceHandle<T> LoadAsset<T>(string key)
        {
            if (!ValidateKey(key))
                return ResourceHandle<T>.Failed(key);

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return ResourceHandle<T>.Succeed(key, handle);
            }
            else
            {
                Log.Error($"[ResourceManager] Load asset failed for {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                return ResourceHandle<T>.Failed(key);
            }
        }

        /// <summary>
        /// 批量加载资源
        /// </summary>
        public async UniTask<ResourceHandle<IList<T>>> LoadAssetsAsync<T>(string key)
        {
            if (!ValidateKey(key))
                return ResourceHandle<IList<T>>.Failed(key);

            var handle = Addressables.LoadAssetsAsync<T>(key);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return ResourceHandle<IList<T>>.Succeed(key, handle);
            }
            else
            {
                Log.Error($"[ResourceManager] Failed to load assets for {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                return ResourceHandle<IList<T>>.Failed(key);
            }
        }

        /// <summary>
        /// 批量加载资源（回调方式）
        /// </summary>
        public void LoadAssetsAsync<T>(string key, Action<T> callback)
        {
            if (!ValidateKey(key))
                return;

            Addressables.LoadAssetsAsync<T>(key, callback);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release(IResourceHandle resourceHandle)
        {
            resourceHandle.Release();
        }

        #endregion

        #region Prefab 管理

        /// <summary>
        /// 异步加载并实例化 Prefab，单独跟踪实例句柄，销毁需调用 DestroyInstance。
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldPositionStays = false)
        {
            if (!ValidateKey(key))
                return null;

            var handle = Addressables.InstantiateAsync(key, parent, worldPositionStays, true);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Log.Error($"[ResourceManager] Instantiate failed for {key} : {handle.OperationException?.Message}");
                Addressables.ReleaseInstance(handle);
                return null;
            }
        }

        /// <summary>
        /// 异步加载并实例化 Prefab（回调方式），单独跟踪实例句柄，销毁需调用 DestroyInstance。
        /// </summary>
        public void InstantiateAsync(string key, Action<GameObject> callback, Transform parent = null, bool worldPositionStays = false)
        {
            if (!ValidateKey(key))
            {
                callback?.Invoke(null);
                return;
            }

            var handle = Addressables.InstantiateAsync(key, parent, worldPositionStays, true);
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(op.Result);
                }
                else
                {
                    Log.Error($"[ResourceManager] Instantiate failed for {key} : {op.OperationException?.Message}");
                    Addressables.ReleaseInstance(op);
                    callback?.Invoke(null);
                }
            };
        }

        /// <summary>
        /// 同步加载并实例化 Prefab（阻塞方式，不推荐），单独跟踪实例句柄，销毁需调用 DestroyInstance。
        /// </summary>
        [Obsolete("Synchronous loading is not recommended, please use InstantiateAsync instead.")]
        public GameObject Instantiate(string key, Transform parent = null, bool worldPositionStays = false)
        {
            if (!ValidateKey(key))
                return null;

            var handle = Addressables.InstantiateAsync(key, parent, worldPositionStays, true);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Log.Error($"[ResourceManager] Instantiate failed for {key} : {handle.OperationException?.Message}");
                Addressables.ReleaseInstance(handle);
                return null;
            }
        }

        /// <summary>
        /// 销毁通过 InstantiateAsync 创建的实例。
        /// </summary>
        public void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                Log.Warning($"[ResourceManager] Trying to destroy a null instance.");
                return;
            }

            if (!Addressables.ReleaseInstance(instance))
            {
                // 不是由 Adressable 创建，尝试普通销毁
                GameObject.Destroy(instance);
            }
        }

        #endregion

        #region 场景管理

        public enum SceneState
        {
            NotLoaded,
            Loading,
            LoadedInactive,
            LoadedActive,
            Unloading
        }

        private class SceneHandle
        {
            public AsyncOperationHandle<SceneInstance> Handle;
            public SceneState State;
            public DateTime LoadTime;

            public SceneInstance SceneInstance => Handle.Result;

            public static SceneHandle Create(AsyncOperationHandle<SceneInstance> handle, SceneState state)
            {
                return new SceneHandle
                {
                    Handle = handle,
                    State = state,
                    LoadTime = DateTime.Now
                };
            }
        }

        private readonly Dictionary<string, SceneHandle> _cachedSceneHandle = new();

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public async UniTask<bool> LoadSceneAsync(
            string key, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true, Action<float> onProgress = null)
        {
            if (!ValidateKey(key))
                return false;

            if (CheckSceneLoadState(key))
                return true;

            var handle = Addressables.LoadSceneAsync(key, mode, activateOnLoad);
            var sceneHandle = SceneHandle.Create(handle, SceneState.Loading);
            _cachedSceneHandle[key] = sceneHandle;

            while (!handle.IsDone)
            {
                onProgress?.Invoke(handle.PercentComplete);
                await UniTask.Delay(16, true);
            }

            onProgress?.Invoke(1f);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                sceneHandle.State = activateOnLoad ? SceneState.LoadedActive : SceneState.LoadedInactive;
                return true;
            }
            else
            {
                Log.Error($"[ResourceManager] Load scene failed {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                _cachedSceneHandle.Remove(key);
                return false;
            }
        }

        /// <summary>
        /// 异步加载场景（回调方式）
        /// </summary>
        public void LoadSceneAsync(
            string key, Action<bool> callback, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true, Action<float> onProgress = null)
        {
            if (!ValidateKey(key))
            {
                callback?.Invoke(false);
                return;
            }

            if (CheckSceneLoadState(key))
            {
                callback?.Invoke(true);
                return;
            }

            var handle = Addressables.LoadSceneAsync(key, mode, activateOnLoad);
            var sceneHandle = SceneHandle.Create(handle, SceneState.Loading);
            _cachedSceneHandle[key] = sceneHandle;

            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    sceneHandle.State = activateOnLoad ? SceneState.LoadedActive : SceneState.LoadedInactive;
                    callback?.Invoke(true);
                }
                else
                {
                    Log.Error($"[ResourceManager] Load scene failed for {key} : {op.OperationException?.Message}");
                    Addressables.Release(op);
                    _cachedSceneHandle.Remove(key);
                    callback?.Invoke(false);
                }
            };

            UniTask.RunOnThreadPool(async () =>
            {
                while (!handle.IsDone)
                {
                    onProgress?.Invoke(handle.PercentComplete);
                    await UniTask.Delay(16, true);
                }

                onProgress?.Invoke(1f);
            }).Forget();
        }

        private bool CheckSceneLoadState(string key)
        {
            return _cachedSceneHandle.TryGetValue(key, out var sceneHandle)
                   && sceneHandle.State is SceneState.LoadedActive or SceneState.LoadedInactive or SceneState.Loading;
        }

        /// <summary>
        /// 预加载场景
        /// </summary>
        public UniTask<bool> PreloadSceneAsync(string key, Action<float> onProgress = null) => LoadSceneAsync(
            key,
            LoadSceneMode.Additive,
            activateOnLoad: false,
            onProgress: onProgress);

        /// <summary>
        /// 预加载场景（回调方式）
        /// </summary>
        public void PreloadSceneAsync(string key, Action<bool> callback, Action<float> onProgress = null) => LoadSceneAsync(
            key,
            callback,
            LoadSceneMode.Additive,
            activateOnLoad: false,
            onProgress: onProgress);

        /// <summary>
        /// 激活场景
        /// </summary>
        public async UniTask<bool> ActivateSceneAsync(string key)
        {
            if (_cachedSceneHandle.TryGetValue(key, out var sceneHandle))
            {
                if (sceneHandle.State != SceneState.LoadedInactive)
                {
                    Log.Warning($"[ResourceManager] Scene not in inactive state. Key: {key}, State: {sceneHandle.State}");
                    return false;
                }

                var activeOp = sceneHandle.SceneInstance.ActivateAsync();
                await activeOp.ToUniTask();
                sceneHandle.State = SceneState.LoadedActive;
                return true;
            }

            Log.Warning($"[ResourceManager] Trying to active a scene not loaded. Key: {key}");
            return false;
        }

        /// <summary>
        /// 激活场景（回调方式）
        /// </summary>
        public void ActivateSceneAsync(string key, Action<bool> callback)
        {
            if (_cachedSceneHandle.TryGetValue(key, out var sceneHandle))
            {
                if (sceneHandle.State != SceneState.LoadedInactive)
                {
                    Log.Warning($"[ResourceManager] Scene not in inactive state. Key: {key}, State: {sceneHandle.State}");
                    callback?.Invoke(false);
                    return;
                }

                var activeOp = sceneHandle.SceneInstance.ActivateAsync();
                activeOp.completed += _ =>
                {
                    sceneHandle.State = SceneState.LoadedActive;
                    callback?.Invoke(true);
                };
            }
            else
            {
                Log.Warning($"[ResourceManager] Trying to active a scene not loaded. Key: {key}");
                callback?.Invoke(false);
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public async UniTask<bool> UnloadSceneAsync(string key)
        {
            if (_cachedSceneHandle.TryGetValue(key, out var sceneHandle))
            {
                if (sceneHandle.State == SceneState.Unloading)
                {
                    Log.Warning($"[ResourceManager] Scene already unloading {key}");
                    return false;
                }

                var preState = sceneHandle.State;
                sceneHandle.State = SceneState.Unloading;
                var unloadOp = Addressables.UnloadSceneAsync(sceneHandle.Handle, true);
                await unloadOp.ToUniTask();
                if (unloadOp.Status == AsyncOperationStatus.Succeeded)
                {
                    _cachedSceneHandle.Remove(key);
                    return true;
                }
                else
                {
                    Log.Error($"[ResourceManager] Failed to unload scene failed for {key}: {unloadOp.OperationException?.Message}");
                    sceneHandle.State = preState; // 回退状态
                    return false;
                }
            }

            Log.Warning($"[ResourceManager] Scene not found. Key: {key}");
            return false;
        }

        /// <summary>
        /// 卸载场景（回调方式）
        /// </summary>
        public void UnloadSceneAsync(string key, Action<bool> callback)
        {
            if (_cachedSceneHandle.TryGetValue(key, out var sceneHandle))
            {
                if (sceneHandle.State == SceneState.Unloading)
                {
                    Log.Warning($"[ResourceManager] Scene already unloading {key}");
                    callback?.Invoke(false);
                    return;
                }

                var preState = sceneHandle.State;
                sceneHandle.State = SceneState.Unloading;
                var unloadOp = Addressables.UnloadSceneAsync(sceneHandle.Handle, true);
                unloadOp.Completed += op =>
                {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        _cachedSceneHandle.Remove(key);
                        callback?.Invoke(true);
                    }
                    else
                    {
                        Log.Error($"[ResourceManager] Failed to unload scene for {key}: {op.OperationException?.Message}");
                        sceneHandle.State = preState; // 回退状态
                        callback?.Invoke(false);
                    }
                };
            }
            else
            {
                Log.Warning($"[ResourceManager] Scene not found. Key: {key}");
                callback?.Invoke(false);
            }
        }

        /// <summary>
        /// 卸载所有已加载场景
        /// </summary>
        public async UniTask<int> UnloadAllScenesAsync(bool excludeActiveScene = true)
        {
            var activeName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var targets = _cachedSceneHandle.Keys.ToList();
            int count = 0;
            foreach (var key in targets)
            {
                if (excludeActiveScene && key == activeName)
                    continue;
                if (await UnloadSceneAsync(key))
                    count++;
            }

            return count;
        }

        public SceneState GetSceneState(string key)
        {
            return _cachedSceneHandle.TryGetValue(key, out var sceneHandle) ? sceneHandle.State : SceneState.NotLoaded;
        }

        #endregion

        #region 辅助方法

        private static bool ValidateKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
                return true;
            Log.Error("[ResourceManager] Key is null or empty");
            return false;
        }

        #endregion
    }
}