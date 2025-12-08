using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DigiEden.Framework.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace DigiEden.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/Table Manager")]
    public class TableManager : ManagerBase
    {
        [Header("预加载设置")]
        [SerializeField]
        private bool _preloadOnInit = true;

        [SerializeField]
        private string _tableAdressLabel = "table";

        [SerializeField]
        private string _tableClassNamespace = "DigiEden.Table";

        // 所有配置表缓存: typeof(T) -> (id -> T)
        private readonly Dictionary<Type, Dictionary<int, ITableConfig>> _cachedTables = new();

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (_preloadOnInit)
            {
                LoadAllTables(true).Forget();
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            ClearAllConfigCache();
        }

        /// <summary>
        /// 加载所有配置表
        /// </summary>
        public async UniTask LoadAllTables(bool isOverride = false)
        {
            Log.Debug("[TableManager] Start loading tables...");

            var handle = await App.ResourceManager.LoadAssetsAsync<TextAsset>(_tableAdressLabel);

            if (!handle.IsValid)
            {
                Log.Error("[TableManager] Failed to load tables.");
                return;
            }

            foreach (var jsonAsset in handle.Asset)
            {
                var jsonContent = jsonAsset.text;
                var fileName = StringHelper.ToPascalCase(jsonAsset.name);
                var typeFullName = $"{_tableClassNamespace}.Config{fileName}";
                var tableType = TypeHelper.GetType(typeFullName);
                if (tableType == null)
                {
                    Log.Warning($"[TableManager] Failed to load table: {fileName}. Type not found.");
                    continue;
                }
                
                Log.Debug($"[TableManager] Loading table: {fileName}, Type: {typeFullName}...");
                CacheTableAsync(tableType, jsonContent, isOverride);
            }
        }

        /// <summary>
        /// 异步加载配置表实例
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <param name="tableAddress">配置表资源地址</param>
        /// <param name="isOverride">是否覆盖已加载的配置</param>
        public async UniTask LoadTable<T>(string tableAddress, bool isOverride = false) where T : ITableConfig
        {
            Log.Debug($"[TableManager] Start loading table: {tableAddress}, Type: {typeof(T).FullName}...");
            var handle = await App.ResourceManager.LoadAssetAsync<TextAsset>(tableAddress);
            if (handle.IsValid)
            {
                CacheTableAsync<T>(handle.Asset.text, isOverride);
            }
            else
            {
                Log.Error($"[TableManager] Failed to load table: {tableAddress}.");
            }
        }

        /// <summary>
        /// 异步加载配置表实例
        /// </summary>
        /// <param name="tableType">配置表类型</param>
        /// <param name="tableAddress">配置表资源地址</param>
        /// <param name="isOverride">是否覆盖已加载的配置</param>
        public async UniTask LoadTable(Type tableType, string tableAddress, bool isOverride = false)
        {
            Log.Debug($"[TableManager] Start loading table: {tableAddress}...");
            var handle = await App.ResourceManager.LoadAssetAsync<TextAsset>(tableAddress);
            if (handle.IsValid)
            {
                CacheTableAsync(tableType, handle.Asset.text, isOverride);
            }
            else
            {
                Log.Error($"[TableManager] Failed to load table: {tableAddress}.");
            }
        }

        /// <summary>
        /// 获取配置表实例
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置表内容列表</returns>
        public Dictionary<int, T> GetTable<T>() where T : ITableConfig
        {
            var tableType = typeof(T);

            if (_cachedTables.TryGetValue(tableType, out var table))
            {
                return table as Dictionary<int, T>;
            }

            Log.Error($"[TableManager] Table not found: {tableType}");
            return null;
        }

        /// <summary>
        /// 获取配置表实例
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfigById<T>(int id) where T : ITableConfig
        {
            var table = GetTable<T>();
            if (table != null && table.TryGetValue(id, out var config))
            {
                return config;
            }

            Log.Error($"[TableManager] Config with ID {id} not found in table {typeof(T).Name}");
            return default;
        }

        /// <summary>
        /// 清除所有配置文件缓存
        /// </summary>
        public void ClearAllConfigCache()
        {
            _cachedTables.Clear();
            Log.Debug("[TableManager] All config caches cleared.");
        }

        private void CacheTableAsync<T>(string jsonContent, bool isOverride = false) where T : ITableConfig
        {
            CacheTableAsync(typeof(T), jsonContent, isOverride);
        }

        private void CacheTableAsync(Type tableType, string jsonContent, bool isOverride = false)
        {
            if (_cachedTables.ContainsKey(tableType))
            {
                if (isOverride)
                {
                    Log.Debug($"[TableManager] Duplicate config cache attempt, covering it. Type: {tableType.Name}");
                }
                else
                {
                    Log.Warning($"[TableManager] Duplicate config cache attempt, skip it. Type: {tableType.Name}");
                    return;
                }
            }

            var listType = typeof(List<>).MakeGenericType(tableType);
            if (JsonConvert.DeserializeObject(jsonContent, listType) is not IEnumerable table)
            {
                Log.Error($"[TableManager] Failed to load table {tableType.Name}, invalid JSON format.");
                return;
            }

            var tableMap = new Dictionary<int, ITableConfig>();
            foreach (var item in table)
            {
                if (item is not ITableConfig tableConfig)
                {
                    Log.Error($"[TableManager] Invalid config in table {tableType.Name}.");
                    continue;
                }

                tableMap[tableConfig.Id] = tableConfig;
            }

            _cachedTables[tableType] = tableMap;
        }
    }
}