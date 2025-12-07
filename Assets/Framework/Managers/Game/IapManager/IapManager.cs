using System;
using UnityEngine;

namespace DigiEden.Framework
{
    public enum IapPlatform
    {
        AppStore, // For Google or Apple App Store
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/IAP Manager")]
    public class IapManager : ManagerBase
    {
        [Header("支付平台")]
        [SerializeField]
        private IapPlatform _platform = IapPlatform.AppStore;

        [Header("Catalog配置")]
        [SerializeField]
        private IAPProviderInitParams.CatalogSourceType _catalogType = IAPProviderInitParams.CatalogSourceType.UnityCatalog;

        [Tooltip("Assets/Resources目录下的路径，不含扩展名")]
        [SerializeField]
        private string _customJsonPath = "IAPCatalog";

        // [Header("收据验证设置")]
        // [SerializeField]
        // private bool _enableReceiptValidation = true;

        // [Tooltip("苹果团队ID")]
        // [SerializeField]
        // private string _appleTeamId;

        private IIAPProvider _provider;

        public event Action<string> OnGrantReward = delegate { };
        public event Action<string, bool> OnPurchaseFinished = delegate { };

        private void OnGrantRewardWrapper(string productId) => OnGrantReward.Invoke(productId);
        private void OnPurchaseFinishedWrapper(string productId, bool succeed) => OnPurchaseFinished.Invoke(productId, succeed);

        /// <summary>
        /// 初始化IAP管理器
        /// </summary>
        protected override void OnInitialize()
        {
            switch (_platform)
            {
                case IapPlatform.AppStore:
                    _provider = new IapProvider2();
                    break;
                default:
                    Debug.LogError("[IAPManager] 未设置支付平台，无法初始化IAP");
                    return;
            }

            RegisterProviderEvents();

            Debug.Log("[IAPManager] 初始化IAP管理器，平台：" + _platform);
            _provider.Initialize(
                new IAPProviderInitParams
                {
                    CatalogType = _catalogType,
                    CustomJsonPath = _customJsonPath
                });
        }

        protected override void OnDispose()
        {
            UnregisterProviderEvents();
            _provider?.Dispose();
        }

        private void RegisterProviderEvents()
        {
            _provider.OnGrantReward += OnGrantRewardWrapper;
            _provider.OnPurchaseFinished += OnPurchaseFinishedWrapper;
        }

        private void UnregisterProviderEvents()
        {
            _provider.OnGrantReward -= OnGrantRewardWrapper;
            _provider.OnPurchaseFinished -= OnPurchaseFinishedWrapper;
        }

        /// <summary>
        /// 购买商品
        /// </summary>
        /// <param name="productId">商品ID</param>
        public void PurchaseProduct(string productId)
        {
            // _impl.PurchaseProduct(productId);
            _provider.PurchaseProduct(productId);
        }

        /// <summary>
        /// 检查商品是否已拥有
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="callback">回调函数</param>
        public void CheckProductOwned(string productId, Action<bool> callback = null)
        {
            _provider.CheckProductOwned(productId, callback);
        }

        /// <summary>
        /// 恢复购买（主要用于iOS）
        /// </summary>
        public void RestorePurchases(Action<bool> callback = null)
        {
            _provider.RestorePurchases(callback);
        }
    }
}