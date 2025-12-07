namespace DigiEden.Framework
{
    public delegate void IapGrantRewardEvent(string productId);

    public delegate void IapPurchaseFinishedEvent(string productId, bool succeed);
}