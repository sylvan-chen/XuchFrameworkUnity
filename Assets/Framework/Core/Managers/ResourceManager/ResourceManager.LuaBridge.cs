using System;
using UnityEngine;

namespace XuchFramework.Core
{
    public partial class ResourceManager
    {
        public void LoadObject(string key, Action<ResourceHandle<UnityEngine.Object>> callback) => LoadAssetAsync(key, callback);

        public void LoadGameObject(string key, Action<ResourceHandle<GameObject>> callback) => LoadAssetAsync(key, callback);

        public void LoadSprite(string key, Action<ResourceHandle<Sprite>> callback) => LoadAssetAsync(key, callback);

        public void LoadMaterial(string key, Action<ResourceHandle<Material>> callback) => LoadAssetAsync(key, callback);

        public void LoadTexture(string key, Action<ResourceHandle<Texture>> callback) => LoadAssetAsync(key, callback);
    }
}