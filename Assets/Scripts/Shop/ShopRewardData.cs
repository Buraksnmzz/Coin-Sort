namespace UI.Shop
{
    public class ShopRewardData
    {
        public int CoinReward = 0;
        public bool IsNoAds = false;
        public RewardType RewardType;
    }

    public enum RewardType
    {
        NoAds,
        Pack,
        Coin
    }
}