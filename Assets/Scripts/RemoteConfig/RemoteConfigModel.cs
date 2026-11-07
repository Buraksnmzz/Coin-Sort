using SavedData;

namespace GameConfig
{
    public class RemoteConfigModel : IModel
    {
        public string rateTriggerLevels = "5,20,50";
        public readonly int shopCoinReward1 = 250;
        public int shopCoinReward2 = 600;
        public int shopCoinReward3 = 1500;
        public int shopCoinReward4 = 4000;
        public int shopCoinReward5 = 10000;
        public readonly int shopCoinVideoReward = 75;
        public int extraMovesCost = 250;
        public int extraMovesCount = 15;
        public int hintCost = 150;
        public int pinCost = 150;
        public int superPinCost = 200;
        public int hintRewardWithCoin = 3;
        public int pinRewardWithCoin = 3;
        public int superPinRewardWithCoin = 1;
        public int winRewardExperience = 20;
        public int targetExperience = 100;
        public int startingHints = 3;
        public int startingPins = 3;
        public int startingSuperPins = 3;
        public bool isSuperPinActiveInDefault = false;
        public int pinBoosterIntroLevel = 4;
        public int superPinBoosterIntroLevel = 7;
        public int hintIntroDelay = 10;
        public int newBadgeRewardCoins = 50;
        public int StartingCoins { get; set; } = 100;
        public int winRewardCoins = 25;
        public int winRewardCoinsHard = 30;
        public int winRewardCoinsExtreme = 40;
        public readonly int completeMonthCoinReward = 250;
        public int dailyChallengeUnlockLevel = 30;
        public int NoAdsPackCoinReward { get; set; } = 500;
        public int unlockableSlotCoinCost = 600;
        public int shuffleCost = 100;
        public bool isHardnessConfigActive = true;
        public int rainbowUnlockLevel = 2;
        public int hiddenUnlockLevel = 2;
        public int stickyUnlockLevel = 2;
        public int icedUnlockLevel = 2;
        
        public string hardnessConfig = @"{
    ""levels"": [
        { ""level_id"": 5, ""auto_merge_a"": 0.5 },
        { ""level_id"": 14, ""auto_merge_a"": 0.5 },
        { ""level_id"": 17, ""auto_merge_a"": 0.5 },
        { ""level_id"": 28, ""hard_deal_a"": 0.13 },
        { ""level_id"": 32, ""auto_merge_a"": 0.5 },
        { ""level_id"": 42, ""auto_merge_a"": 0.5 },
        { ""level_id"": 52, ""hard_deal_a"": 0.5 },
        { ""level_id"": 53, ""magic_deal_a"": 0.2 },
        { ""level_id"": 61, ""magic_deal_a"": 0.3 },
        { ""level_id"": 67, ""magic_deal_a"": 0.85 },
        { ""level_id"": 94, ""magic_deal_a"": 0.85 },
        { ""level_id"": 98, ""smart_deal_a"": 1 },
        { ""level_id"": 109, ""smart_deal_a"": 0.5 },
        { ""level_id"": 112, ""auto_merge_a"": 0.5 },
        { ""level_id"": 115, ""magic_deal_a"": 0.3 },
        { ""level_id"": 118, ""magic_deal_a"": 0.85 },
        { ""level_id"": 121, ""smart_deal_a"": 0.5 },
        { ""level_id"": 125, ""magic_deal_a"": 0.2 },
        { ""level_id"": 129, ""auto_merge_a"": 0.5 },
        { ""level_id"": 133, ""magic_deal_a"": 0.3 },
        { ""level_id"": 139, ""magic_deal_a"": 0.85 },
        { ""level_id"": 142, ""smart_deal_a"": 0.5 },
        { ""level_id"": 148, ""hard_deal_a"": 0.5 },
        { ""level_id"": 149, ""magic_deal_a"": 0.2 },
        { ""level_id"": 153, ""auto_merge_a"": 0.5 },
        { ""level_id"": 157, ""magic_deal_a"": 0.3 },
        { ""level_id"": 163, ""magic_deal_a"": 0.85 },
        { ""level_id"": 166, ""smart_deal_a"": 0.5 },
        { ""level_id"": 173, ""magic_deal_a"": 0.2 },
        { ""level_id"": 177, ""auto_merge_a"": 0.5 },
        { ""level_id"": 181, ""magic_deal_a"": 0.3 },
        { ""level_id"": 187, ""magic_deal_a"": 0.85 },
        { ""level_id"": 190, ""smart_deal_a"": 0.5 },
        { ""level_id"": 196, ""hard_deal_a"": 0.55 },
        { ""level_id"": 197, ""magic_deal_a"": 0.2 },
        { ""level_id"": 201, ""auto_merge_a"": 0.5 },
        { ""level_id"": 205, ""magic_deal_a"": 0.3 },
        { ""level_id"": 211, ""magic_deal_a"": 0.85 },
        { ""level_id"": 214, ""smart_deal_a"": 0.5 },
        { ""level_id"": 220, ""hard_deal_a"": 0.6 },
        { ""level_id"": 221, ""magic_deal_a"": 0.2 }
    ]
}";
    }
}