using SavedData;

namespace GameConfig
{
    public class RemoteConfigModel : IModel
    {
        public string RateTriggerLevels = "5,20,50";
        public int ShopCoinReward1 = 250;
        public int ShopCoinReward2 = 600;
        public int ShopCoinReward3 = 1500;
        public int ShopCoinReward4 = 4000;
        public int ShopCoinReward5 = 10000;
        public int ShopCoinVideoReward = 75;
        public int ExtraMovesCost = 250;
        public int ExtraMovesCount = 15;
        public int HintCost = 150;
        public int PinCost = 150;
        public int SuperPinCost = 200;
        public int HintRewardWithCoin = 3;
        public int PinRewardWithCoin = 3;
        public int SuperPinRewardWithCoin = 1;
        public int WinRewardExperience = 20;
        public int TargetExperience = 100;
        public int StartingHints = 3;
        public int StartingPins = 3;
        public int StartingSuperPins = 3;
        public bool IsSuperPinActiveInDefault = false;
        public int PinBoosterIntroLevel = 4;
        public int SuperPinBoosterIntroLevel = 7;
        public int HintIntroDelay = 10;
        public int NewBadgeRewardCoins = 50;
        public int StartingCoins { get; set; } = 100;
        public int WinRewardCoins = 25;
        public int WinRewardCoinsHard = 30;
        public int WinRewardCoinsExtreme = 40;
        public int CompleteMonthCoinReward = 250;
        public int DailyChallengeUnlockLevel = 30;
        public int NoAdsPackCoinReward { get; set; } = 500;
    }
}