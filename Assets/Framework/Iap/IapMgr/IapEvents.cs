namespace Framework.Core
{
    public delegate void IapGrantRewardEvent(string productId);

    public delegate void IapPurchaseFinishedEvent(string productId, bool succeed);
}