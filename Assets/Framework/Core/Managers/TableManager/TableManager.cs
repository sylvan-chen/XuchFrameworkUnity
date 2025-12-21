using System;
using System.Collections;
using System.Collections.Generic;
using Alchemy.Inspector;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using XuchFramework.Core.Utils;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Managers/Table Manager")]
    public class TableManager : ModuleBase
    {
        [SerializeField]
        private bool _preloadOnInit = false;
        [SerializeField, EnableIf(nameof(_preloadOnInit))]
        private string _preloadTableAddressLabel = "table";
        [SerializeField, EnableIf(nameof(_preloadOnInit))]
        private string _tableClassNamespace = "XuchFramework.Table";

        // typeof(T) -> (id -> T)
        private readonly Dictionary<Type, Dictionary<int, ITableConfig>> _cachedTables = new();

        protected override UniTask OnInitialize()
        {
            if (_preloadOnInit)
            {
                LoadAllTables(_preloadTableAddressLabel, _tableClassNamespace, true).Forget();
            }

            return UniTask.CompletedTask;
        }

        protected override void OnDispose()
        {
            ClearAllConfigCache();
        }

        public async UniTask LoadAllTables(string preloadTableAddressLabel, string tableClassNamespace, bool isOverride = false)
        {
            Log.Debug("[TableManager] Start loading tables...");

            var handle = await GameModule<ResourceManager>.Instance.LoadAssetsAsync<TextAsset>(preloadTableAddressLabel);

            if (!handle.IsValid)
            {
                Log.Error("[TableManager] Failed to load tables.");
                return;
            }

            foreach (var jsonAsset in handle.Asset)
            {
                var jsonContent = jsonAsset.text;
                var fileName = StringHelper.ToPascalCase(jsonAsset.name);
                var typeFullName = $"{tableClassNamespace}.Config{fileName}";
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

        public async UniTask LoadTable<T>(string key, bool isOverride = false) where T : ITableConfig
        {
            Log.Debug($"[TableManager] Start loading table: {key}, Type: {typeof(T).FullName}...");
            var handle = await GameModule<ResourceManager>.Instance.LoadAssetAsync<TextAsset>(key);
            if (handle.IsValid)
            {
                CacheTableAsync<T>(handle.Asset.text, isOverride);
            }
            else
            {
                Log.Error($"[TableManager] Failed to load table: {key}.");
            }
        }

        public async UniTask LoadTable(Type tableType, string key, bool isOverride = false)
        {
            Log.Debug($"[TableManager] Start loading table: {key}...");
            var handle = await GameModule<ResourceManager>.Instance.LoadAssetAsync<TextAsset>(key);
            if (handle.IsValid)
            {
                CacheTableAsync(tableType, handle.Asset.text, isOverride);
            }
            else
            {
                Log.Error($"[TableManager] Failed to load table: {key}.");
            }
        }

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

        public T GetConfigById<T>(int id) where T : class, ITableConfig
        {
            var table = GetTable<T>();
            if (table == null)
            {
                Log.Error($"[TableManager] Table not found: {typeof(T).Name}");
                return null;
            }

            if (!table.TryGetValue(id, out var config))
            {
                Log.Error($"[TableManager] Config with ID {id} not found in table {typeof(T).Name}");
                return null;
            }

            return config;
        }

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
                    Log.Debug($"[TableManager] Duplicate config cache attempt, override it. Type: {tableType.Name}");
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