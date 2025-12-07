namespace DigiEden.Framework
{
    public interface IIAPProvider
    {
        /// <summary>
        /// 发放奖励事件
        /// </summary>
        public event IapGrantRewardEvent OnGrantReward;

        /// <summary>
        /// 购买完成事件
        /// </summary>
        public event IapPurchaseFinishedEvent OnPurchaseFinished;

        public bool IsInitialized { get; }

        /// <summary>
        /// 初始化IAPProvider
        /// </summary>
        public void Initialize(IAPProviderInitParams initParams);

        /// <summary>
        /// 释放IAPProvider
        /// </summary>
        public void Dispose();

        /// <summary>
        /// 购买商品
        /// </summary>
        /// <param name="productId">商品ID</param>
        public void PurchaseProduct(string productId);

        /// <summary>
        /// 恢复购买（主要用于iOS）
        /// </summary>
        /// <param name="callback">恢复结果回调，参数表示是否成功</param>
        public void RestorePurchases(System.Action<bool> callback = null);

        /// <summary>
        /// 检查商品是否已经购买
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="callback">检查结果回调</param>
        public void CheckProductOwned(string productId, System.Action<bool> callback = null);
    }

    public struct IAPProviderInitParams
    {
        public enum CatalogSourceType
        {
            UnityCatalog,
            CustomJson,
        }

        public CatalogSourceType CatalogType;
        public string CustomJsonPath;
    }
}