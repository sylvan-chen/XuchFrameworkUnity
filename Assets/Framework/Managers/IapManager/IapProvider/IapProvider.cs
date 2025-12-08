using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DigiEden.Framework
{
    public partial class IapProvider2 : IIAPProvider
    {
        private const int STORE_CONNECT_RETRY_COUNT = 5;
        private const int PRODUCT_FETCH_RETRY_COUNT = 5;

        public bool IsInitialized => _initState == InitState.Initialized;

        public event IapGrantRewardEvent OnGrantReward = delegate { };
        public event IapPurchaseFinishedEvent OnPurchaseFinished = delegate { };

        private InitState _initState = InitState.Uninitialized;
        private ProductState _productState = ProductState.Unfetched;
        private PurchaseFetchingState _purchaseFetchingState = PurchaseFetchingState.Unfetched;
        private PurchaseState _purchaseState = PurchaseState.Idle;
        private RestoreState _restoreState = RestoreState.Idle;

        private IAPProviderInitParams _initParams;
        private StoreController _storeController;

        private Dictionary<string, bool> _productOwnedCache = new();

        public void Initialize(IAPProviderInitParams initParams)
        {
            Debug.Log("[IAPProvider] Start initializing IAPProvider...");
            _initParams = initParams;
            _initState = InitState.Initializing;
            _storeController = UnityIAPServices.StoreController();
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            RegisterIapCallbacks();
            await ConnectStoreAsync();

            FetchProducts();
            FetchPurchases();

            _initState = InitState.Initialized;
            Debug.Log("[IAPProvider] IAPProvider initialized.");
        }

        private async UniTask ConnectStoreAsync()
        {
            Debug.Log("[IAPProvider] Connecting to store...");
            _storeController.SetStoreReconnectionRetryPolicyOnDisconnection(new MaximumNumberOfAttemptsRetryPolicy(STORE_CONNECT_RETRY_COUNT));
            await _storeController.Connect().AsUniTask();
            Debug.Log("[IAPProvider] Connected to store.");
        }

        private void RegisterIapCallbacks()
        {
            Debug.Log("[IAPProvider] Registering IAP callbacks...");
            _storeController.OnStoreDisconnected += OnStoreDisconnected;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

            _storeController.OnPurchaseFailed += OnPurchaseFailed;
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

            _storeController.OnCheckEntitlement += OnCheckEntitlement;
            Debug.Log("[IAPProvider] IAP callbacks registered.");
        }

        private void UnRegisterIapCallbacks()
        {
            Debug.Log("[IAPProvider] Unregistering IAP callbacks...");
            _storeController.OnStoreDisconnected -= OnStoreDisconnected;
            _storeController.OnProductsFetched -= OnProductsFetched;
            _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            _storeController.OnPurchasesFetched -= OnPurchasesFetched;
            _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;

            _storeController.OnPurchaseFailed -= OnPurchaseFailed;
            _storeController.OnPurchasePending -= OnPurchasePending;
            _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;

            _storeController.OnCheckEntitlement -= OnCheckEntitlement;
            Debug.Log("[IAPProvider] IAP callbacks unregistered.");
        }

        private void FetchProducts()
        {
            if (_productState == ProductState.Fetching)
                return;

            Debug.Log("[IAPProvider] Fetching products from store...");
            _productState = ProductState.Fetching;
            _storeController.FetchProducts(LoadProductDefinitions(), new MaximumNumberOfAttemptsRetryPolicy(PRODUCT_FETCH_RETRY_COUNT));
        }

        private void FetchPurchases()
        {
            if (_purchaseFetchingState == PurchaseFetchingState.Fetching)
                return;

            Debug.Log("[IAPProvider] Fetching purchases from store...");
            _purchaseFetchingState = PurchaseFetchingState.Fetching;
            _storeController.FetchPurchases();
        }

        public void Dispose()
        {
            UnRegisterIapCallbacks();
            _storeController = null;
            _initState = InitState.Uninitialized;
            Debug.Log("[IAPProvider] IAPProvider disposed.");
        }

        public void CheckProductOwned(string productId, Action<bool> callback = null)
        {
            Debug.Log($"[IAPProvider] Attempting to check product owned for {productId}...");

            if (!IsInitialized)
            {
                Debug.LogWarning("[IAPProvider] Unable to check product owned. IAPProvider not initialized. Please wait.");
                callback?.Invoke(false);
                return;
            }

            if (_purchaseFetchingState != PurchaseFetchingState.Fetched)
            {
                Debug.LogWarning("[IAPProvider] Unable to check product owned. Purchases not fetched yet.");
                callback?.Invoke(false);
                return;
            }

            bool isOwned = _productOwnedCache.ContainsKey(productId) && _productOwnedCache[productId];
            Debug.Log($"[IAPProvider] Product owned for {productId}: {isOwned}");
            callback?.Invoke(isOwned);
        }

        public void PurchaseProduct(string productId)
        {
            Debug.Log($"[IAPProvider] Attempting to purchase product: {productId}");

            if (!IsInitialized)
            {
                HandleFailedPurchase(productId, "IAPProvider not initialized");
                return;
            }

            if (_purchaseState == PurchaseState.Purchasing)
            {
                Debug.LogWarning("[IAPProvider] Purchase already in progress. Please wait.");
                return;
            }

            _purchaseState = PurchaseState.Purchasing;
            PurchaseProductInternal(productId).Forget();
        }

        private async UniTaskVoid PurchaseProductInternal(string productId)
        {
            if (_productState != ProductState.Fetched)
            {
                Debug.Log("[IAPProvider] Products not fetched yet. Trying to fetch products...");
                FetchProducts();
                bool fetched = await UniTask.WaitUntil(() => _productState == ProductState.Fetched).TimeoutWithoutException(TimeSpan.FromSeconds(30));
                if (!fetched)
                {
                    HandleFailedPurchase(productId, "Timeout waiting for products fetching");
                    return;
                }
            }

            var product = _storeController.GetProductById(productId);
            if (product == null)
            {
                HandleFailedPurchase(productId, "Product not found");
                return;
            }

            _storeController.PurchaseProduct(product);
        }

        public void RestorePurchases(Action<bool> callback = null)
        {
            Debug.Log("[IAPProvider] Attempting to restore purchases...");

            if (!IsInitialized)
            {
                Debug.LogError("[IAPProvider] Restore purchases failed. IAPProvider not initialized.");
                callback?.Invoke(false);
                return;
            }

            if (_restoreState == RestoreState.Restoring)
            {
                Debug.LogWarning("[IAPProvider] Restore already in progress. Please wait.");
                return;
            }

            _restoreState = RestoreState.Restoring;

            _storeController.RestoreTransactions((success, error) =>
            {
                if (success)
                {
                    Debug.Log("[IAPProvider] Restore purchases valid.");
                    HandleRestorePurchasesSuccess(callback).Forget();
                }
                else
                {
                    Debug.LogError($"[IAPProvider] Restore purchases invalid: {error}");
                    FinishRestore(false, callback, "Restore transactions failed");
                }
            });
        }

        private async UniTaskVoid HandleRestorePurchasesSuccess(Action<bool> callback)
        {
            Debug.Log("[IAPProvider] Handling post-restore operations, trying to refetch purchases...");
            RefetchPurchases();
            try
            {
                await UniTask.WaitUntil(() => _purchaseFetchingState == PurchaseFetchingState.Fetched).Timeout(TimeSpan.FromSeconds(30));

                FinishRestore(true, callback);
            }
            catch (TimeoutException)
            {
                FinishRestore(false, callback, "Timeout waiting for purchases fetching");
            }
        }

        private void FinishRestore(bool success, Action<bool> callback, string error = null)
        {
            if (success)
                Debug.Log($"[IAPProvider] Restore purchases process succeeded.");
            else
                Debug.LogError($"[IAPProvider] Failed to restore purchases process. Error: {error}");

            _restoreState = RestoreState.Idle;
            callback?.Invoke(success);
        }

        private List<ProductDefinition> LoadProductDefinitions()
        {
            Debug.Log("[IAPProvider] Loading product definitions from local...");
            List<ProductDefinition> result = null;

            switch (_initParams.CatalogType)
            {
                case IAPProviderInitParams.CatalogSourceType.UnityCatalog:
                    Debug.Log("[IAPProvider] Catalog type: Unity catalog");
                    result = LoadProductDefinitionsFromUnityCatalog();
                    break;
                case IAPProviderInitParams.CatalogSourceType.CustomJson:
                    Debug.Log("[IAPProvider] Catalog type: Custom JSON");
                    result = LoadProductDefinitionsFromCustomJson();
                    break;
                default:
                    Debug.LogError("[IAPProvider] Unknown product definition source.");
                    break;
            }

            return result;
        }

        private List<ProductDefinition> LoadProductDefinitionsFromUnityCatalog()
        {
            var productCatalog = ProductCatalog.LoadDefaultCatalog();
            if (productCatalog == null || productCatalog.allProducts.Count == 0)
            {
                Debug.LogError("[IAPProvider] No products found in the unity catalog.");
                return null;
            }

            if (productCatalog.allProducts.Count != productCatalog.allValidProducts.Count)
            {
                Debug.LogWarning("[IAPProvider] Some products in the unity catalog are invalid and will be ignored.");
                foreach (var product in productCatalog.allProducts)
                {
                    if (!productCatalog.allValidProducts.Contains(product))
                        Debug.LogWarning($"[IAPProvider] Invalid product in unity catalog - ID: {product.id}, Type: {product.type}");
                }
            }

            var result = new List<ProductDefinition>();
            foreach (var product in productCatalog.allValidProducts)
            {
                Debug.Log($"[IAPProvider] Product definition - ID: {product.id}, Type: {product.type}");
                result.Add(new ProductDefinition(product.id, product.type));
            }

            return result;
        }

        private List<ProductDefinition> LoadProductDefinitionsFromCustomJson()
        {
            var asset = Resources.Load<TextAsset>(_initParams.CustomJsonPath);
            if (asset == null)
            {
                Debug.LogError($"[IAPProvider] Failed to load custom JSON from path: {_initParams.CustomJsonPath}");
                return null;
            }

            var result = new List<ProductDefinition>();
            var products = JsonConvert.DeserializeObject<List<IapProductDefinition>>(asset.text);
            if (products == null || products.Count == 0)
            {
                Debug.LogError("[IAPProvider] No products found in the custom JSON.");
                return result;
            }

            foreach (var product in products)
            {
                var productType = product.Type switch
                {
                    0 => ProductType.Consumable,
                    1 => ProductType.NonConsumable,
                    2 => ProductType.Subscription,
                    _ => ProductType.Consumable
                };

                Debug.Log($"[IAPProvider] Product definition - ID: {product.Id}, Type: {productType}");
                result.Add(new ProductDefinition(product.Id, productType));
            }

            return result;
        }
    }
}