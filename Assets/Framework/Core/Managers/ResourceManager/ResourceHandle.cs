using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace XuchFramework.Core
{
    public interface IResourceHandle
    {
        public bool IsValid { get; }

        public void Release();
    }

    /// <summary>
    /// It's recommend to check IsValid before using the Asset
    /// </summary>
    public struct ResourceHandle<T> : IResourceHandle
    {
        private AsyncOperationHandle<T> _handle;

        public string Key { get; private set; }
        public DateTime LoadTime { get; private set; }

        public T Asset => _handle.Result;
        public bool IsValid => _handle.IsValid();

        public static ResourceHandle<T> Succeed(string key, AsyncOperationHandle<T> handle)
        {
            return new ResourceHandle<T>
            {
                _handle = handle,
                Key = key,
                LoadTime = DateTime.Now
            };
        }

        public static ResourceHandle<T> Failed(string key)
        {
            return new ResourceHandle<T>
            {
                _handle = default,
                Key = key,
                LoadTime = DateTime.Now
            };
        }

        public void Release()
        {
            Addressables.Release(_handle);
        }
    }
}