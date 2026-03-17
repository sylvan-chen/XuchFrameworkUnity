namespace Framework.Core
{
    public partial class IapProvider
    {
        private enum InitState
        {
            Uninitialized,
            Initializing,
            Initialized,
        }

        private enum ProductState
        {
            Unfetched,
            Fetching,
            Fetched,
        }

        private enum PurchaseFetchingState
        {
            Unfetched,
            Fetching,
            Fetched,
        }

        private enum PurchaseState
        {
            Idle,
            Purchasing,
        }

        private enum RestoreState
        {
            Idle,
            Restoring,
        }
    }
}