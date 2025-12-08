using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DigiEden.Framework
{
    public partial class IapProvider2
    {
        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.LogWarning($"[IAPProvider] Store disconnected: {description.Message}");
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAPProvider] Fetched {products.Count} products from store.");
            _productState = ProductState.Fetched;
        }

        private void OnProductsFetchFailed(ProductFetchFailed fetchFailed)
        {
            Debug.LogWarning(
                $"[IAPProvider] Failed to fetch products from store. Failed count: {fetchFailed.FailedFetchProducts.Count}, Reason: {fetchFailed.FailureReason}");
            _productState = ProductState.Unfetched;
        }

        private void OnPurchasesFetched(Orders fetchedOrders)
        {
            Debug.Log(
                $"[IAPProvider] Fetched purchases from store. Confirmed: {fetchedOrders.ConfirmedOrders.Count}, Deferred: {fetchedOrders.DeferredOrders.Count}, Pending: {fetchedOrders.PendingOrders.Count}");

            _productOwnedCache.Clear();
            foreach (var confirmedOrder in fetchedOrders.ConfirmedOrders)
            {
                var productId = GetFirstProductInOrder(confirmedOrder)?.definition.id;
                if (productId == null)
                    continue;

                _productOwnedCache[productId] = true;
            }

            _purchaseFetchingState = PurchaseFetchingState.Fetched;
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription description)
        {
            Debug.LogError(
                $"[IAPProvider] Failed to fetch purchases from store. Message: {description.Message}, FailureReason: {description.FailureReason}");
            _purchaseFetchingState = PurchaseFetchingState.Unfetched;
        }

        private void OnPurchasePending(PendingOrder pendingOrder)
        {
            if (ValidateOrder(pendingOrder))
            {
                Debug.Log($"[IAPProvider] Purchase validated for product {GetFirstProductInOrder(pendingOrder)?.definition.id}. Granting reward...");
                GrantReward(GetFirstProductInOrder(pendingOrder)?.definition.id);
                Debug.Log($"[IAPProvider] Reward granted for {GetFirstProductInOrder(pendingOrder)?.definition.id}. Confirming purchase...");
                _storeController.ConfirmPurchase(pendingOrder);
            }
            else
            {
                HandleFailedPurchase("unknown", "Invalid pending order");
            }
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    HandleSucceedPurchase(GetFirstProductInOrder(confirmedOrder)?.definition.id ?? "unknown");
                    break;
                case FailedOrder failedOrder:
                    HandleFailedPurchase(GetFirstProductInOrder(failedOrder)?.definition.id ?? "unknown", failedOrder.FailureReason.ToString());
                    break;
                default:
                    HandleFailedPurchase("unknown", "Unknown order type");
                    break;
            }
        }

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            HandleFailedPurchase(GetFirstProductInOrder(failedOrder)?.definition.id ?? "unknown", failedOrder.FailureReason.ToString());
        }

        private void OnPurchaseDeferred(Order order)
        {
            Debug.Log($"[IAPProvider] Purchase deferred for {GetFirstProductInOrder(order)?.definition.id}. Awaiting further action.");
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            string productId = entitlement.Product?.definition.id ?? "unknown";
            Debug.Log($"[IAPProvider] Check entitlement for {productId}, Status: {entitlement.Status}");

            switch (entitlement.Status)
            {
                case EntitlementStatus.FullyEntitled:
                case EntitlementStatus.EntitledButNotFinished:
                    break;
                case EntitlementStatus.EntitledUntilConsumed:
                case EntitlementStatus.NotEntitled:
                    break;
                case EntitlementStatus.Unknown:
                    Debug.LogError($"[IAPProvider] Entitlement status is Unknown for {productId}.");
                    break;
                default:
                    Debug.LogError($"[IAPProvider] Unknown entitlement status: {entitlement.Status}");
                    break;
            }
        }

        #region 辅助方法

        private void GrantReward(string productId)
        {
            OnGrantReward.Invoke(productId);
        }

        private void HandleSucceedPurchase(string productId)
        {
            Debug.Log($"[IAPProvider] Purchase succeeded for product {productId}.");
            OnPurchaseFinished.Invoke(productId, true);
            _purchaseState = PurchaseState.Idle;
            RefetchPurchases();
        }

        private void HandleFailedPurchase(string productId, string reason)
        {
            Debug.LogError($"[IAPProvider] Purchase failed for product {productId}. Reason: {reason}");
            OnPurchaseFinished.Invoke(productId, false);
            _purchaseState = PurchaseState.Idle;
        }

        private void RefetchPurchases()
        {
            Debug.Log("[IAPProvider] Refetching purchases from store...");
            _purchaseFetchingState = PurchaseFetchingState.Unfetched;
            FetchPurchases();
        }

        private Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().FirstOrDefault()?.Product;
        }

        private bool ValidateOrder(Order order)
        {
            // 在这里添加订单验证逻辑

            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.LogError("[IAPProvider] Order validation failed. No product found in order.");
                return false;
            }

            return true;
        }

        #endregion
    }
}