using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Framework.Core
{
    public enum SceneState
    {
        NotLoaded,
        Loading,
        LoadedInactive,
        LoadedActive,
        Unloading
    }

    public class SceneHandle
    {
        public AsyncOperationHandle<SceneInstance> Handle;
        public SceneState State;
        public DateTime LoadTime;

        public SceneInstance SceneInstance => Handle.Result;

        public static SceneHandle Create(AsyncOperationHandle<SceneInstance> handle, SceneState state)
        {
            return new SceneHandle { Handle = handle, State = state, LoadTime = DateTime.Now };
        }
    }

    public static class SceneMgr
    {
        public static void Dispose()
        {
            _cachedSceneHandle.Clear();
        }

        private static readonly Dictionary<string, SceneHandle> _cachedSceneHandle = new();

        public static async UniTask<bool> LoadSceneAsync(
            string key,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            Action<float> onProgress = null)
        {
            if (!ValidateKey(key)) return false;

            if (CheckSceneLoadState(key)) return true;

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

        public static void LoadSceneAsync(
            string key,
            Action<bool> callback,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            Action<float> onProgress = null)
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

            UniTask.RunOnThreadPool(
                    async () =>
                    {
                        while (!handle.IsDone)
                        {
                            onProgress?.Invoke(handle.PercentComplete);
                            await UniTask.Delay(16, true);
                        }

                        onProgress?.Invoke(1f);
                    }
                )
                .Forget();
        }

        private static bool CheckSceneLoadState(string key)
        {
            return _cachedSceneHandle.TryGetValue(key, out var sceneHandle)
                   && sceneHandle.State is SceneState.LoadedActive or SceneState.LoadedInactive or SceneState.Loading;
        }

        public static UniTask<bool> PreloadSceneAsync(string key, Action<float> onProgress = null) => LoadSceneAsync(
            key,
            LoadSceneMode.Additive,
            activateOnLoad: false,
            onProgress: onProgress
        );

        public static void PreloadSceneAsync(string key, Action<bool> callback, Action<float> onProgress = null) =>
            LoadSceneAsync(key, callback, LoadSceneMode.Additive, activateOnLoad: false, onProgress: onProgress);

        public static async UniTask<bool> ActivateSceneAsync(string key)
        {
            if (_cachedSceneHandle.TryGetValue(key, out var sceneHandle))
            {
                if (sceneHandle.State != SceneState.LoadedInactive)
                {
                    Log.Warning(
                        $"[ResourceManager] Scene not in inactive state. Key: {key}, State: {sceneHandle.State}"
                    );
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

        public static void ActivateSceneAsync(string key, Action<bool> callback)
        {
            if (_cachedSceneHandle.TryGetValue(key, out var sceneHandle))
            {
                if (sceneHandle.State != SceneState.LoadedInactive)
                {
                    Log.Warning(
                        $"[ResourceManager] Scene not in inactive state. Key: {key}, State: {sceneHandle.State}"
                    );
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

        public static async UniTask<bool> UnloadSceneAsync(string key)
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
                    Log.Error(
                        $"[ResourceManager] Failed to unload scene failed for {key}: {unloadOp.OperationException?.Message}"
                    );
                    sceneHandle.State = preState; // Revert state
                    return false;
                }
            }

            Log.Warning($"[ResourceManager] Scene not found. Key: {key}");
            return false;
        }

        public static void UnloadSceneAsync(string key, Action<bool> callback)
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
                        Log.Error(
                            $"[ResourceManager] Failed to unload scene for {key}: {op.OperationException?.Message}"
                        );
                        sceneHandle.State = preState; // Revert state
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

        public static async UniTask<int> UnloadAllScenesAsync(bool excludeActiveScene = true)
        {
            var activeName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var targets = _cachedSceneHandle.Keys.ToList();
            int count = 0;
            foreach (var key in targets)
            {
                if (excludeActiveScene && key == activeName) continue;
                if (await UnloadSceneAsync(key)) count++;
            }

            return count;
        }

        public static SceneState GetSceneState(string key)
        {
            return _cachedSceneHandle.TryGetValue(key, out var sceneHandle) ? sceneHandle.State : SceneState.NotLoaded;
        }

        private static bool ValidateKey(string key)
        {
            if (!string.IsNullOrEmpty(key)) return true;
            Log.Error("[SceneMgr] Key is null or empty");
            return false;
        }
    }
}