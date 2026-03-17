using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Framework.Core
{
    /// <summary>
    /// It's recommended to check IsValid before using the Asset
    /// </summary>
    public struct ResourceHandle<T>
    {
        private AsyncOperationHandle<T> _handle;

        public string Key { get; private set; }
        public DateTime LoadTime { get; private set; }

        public T Asset => _handle.Result;
        public bool IsValid => _handle.IsValid();

        public static ResourceHandle<T> Succeed(string key, AsyncOperationHandle<T> handle)
        {
            return new ResourceHandle<T> { _handle = handle, Key = key, LoadTime = DateTime.Now };
        }

        public static ResourceHandle<T> Failed(string key)
        {
            return new ResourceHandle<T> { _handle = default, Key = key, LoadTime = DateTime.Now };
        }

        public void Release()
        {
            _handle.Release();
        }
    }

    public static class ResourceMgr
    {
        public static void Initialize()
        {
            // pre-initialize Addressable (Optional, Addressable will auto-initialize on first use)
            Addressables.InitializeAsync();
        }

        #region Assets Loading

        public static async UniTask<ResourceHandle<T>> LoadAssetAsync<T>(string key)
        {
            if (!ValidateKey(key)) return ResourceHandle<T>.Failed(key);

            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return ResourceHandle<T>.Succeed(key, handle);
            }
            else
            {
                Log.Error($"[ResourceMgr] Failed to load asset for {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                return ResourceHandle<T>.Failed(key);
            }
        }

        public static void LoadAssetAsync<T>(string key, Action<ResourceHandle<T>> callback)
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
                    Log.Error($"[ResourceMgr] Load asset failed for {key} : {op.OperationException?.Message}");
                    Addressables.Release(op);
                    callback?.Invoke(ResourceHandle<T>.Failed(key));
                }
            };
        }

        public static ResourceHandle<T> LoadAsset<T>(string key)
        {
            if (!ValidateKey(key)) return ResourceHandle<T>.Failed(key);

            var handle = Addressables.LoadAssetAsync<T>(key);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return ResourceHandle<T>.Succeed(key, handle);
            }
            else
            {
                Log.Error($"[ResourceMgr] Load asset failed for {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                return ResourceHandle<T>.Failed(key);
            }
        }

        public static async UniTask<ResourceHandle<IList<T>>> LoadAssetsAsync<T>(string key)
        {
            if (!ValidateKey(key)) return ResourceHandle<IList<T>>.Failed(key);

            var handle = Addressables.LoadAssetsAsync<T>(key);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return ResourceHandle<IList<T>>.Succeed(key, handle);
            }
            else
            {
                Log.Error($"[ResourceMgr] Failed to load assets for {key} : {handle.OperationException?.Message}");
                Addressables.Release(handle);
                return ResourceHandle<IList<T>>.Failed(key);
            }
        }

        public static void LoadAssetsAsync<T>(string key, Action<T> callback)
        {
            if (!ValidateKey(key)) return;

            Addressables.LoadAssetsAsync<T>(key, callback);
        }

        #endregion

        #region Prefab Instantiation

        public static async UniTask<GameObject> InstantiateAsync(
            string key,
            Transform parent = null,
            bool worldPositionStays = false)
        {
            if (!ValidateKey(key)) return null;

            var handle = Addressables.InstantiateAsync(key, parent, worldPositionStays, true);
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Log.Error($"[ResourceMgr] Instantiate failed for {key} : {handle.OperationException?.Message}");
                Addressables.ReleaseInstance(handle);
                return null;
            }
        }

        public static void InstantiateAsync(
            string key,
            Action<GameObject> callback,
            Transform parent = null,
            bool worldPositionStays = false)
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
                    Log.Error($"[ResourceMgr] Instantiate failed for {key} : {op.OperationException?.Message}");
                    Addressables.ReleaseInstance(op);
                    callback?.Invoke(null);
                }
            };
        }

        public static GameObject Instantiate(string key, Transform parent = null, bool worldPositionStays = false)
        {
            if (!ValidateKey(key)) return null;

            var handle = Addressables.InstantiateAsync(key, parent, worldPositionStays, true);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Log.Error($"[ResourceMgr] Instantiate failed for {key} : {handle.OperationException?.Message}");
                Addressables.ReleaseInstance(handle);
                return null;
            }
        }

        public static void DestroyInstance(GameObject instance)
        {
            if (instance == null)
            {
                Log.Warning($"[ResourceMgr] Trying to destroy a null instance.");
                return;
            }

            if (!Addressables.ReleaseInstance(instance))
            {
                // Not created by Addressable, try normal destroy
                UnityEngine.Object.Destroy(instance);
            }
        }

        #endregion

        #region Helpers

        private static bool ValidateKey(string key)
        {
            if (!string.IsNullOrEmpty(key)) return true;
            Log.Error("[ResourceMgr] Key is null or empty");
            return false;
        }

        #endregion
    }
}