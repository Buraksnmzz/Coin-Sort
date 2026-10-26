using System.Collections.Generic;

namespace Gameplay
{
    public class BoardState
    {
        public int BoardIndex { get; }
        public IReadOnlyList<SlotState> Slots { get; }

        public BoardState(int boardIndex, IReadOnlyList<SlotState> slots)
        {
            BoardIndex = boardIndex;
            Slots = slots;
        }
    }

    public class SlotState
    {
        public SlotType Type { get; }
        public IReadOnlyList<CoinState> Coins { get; }
        public int UnlockCoinCost { get; }
        public int AutomaticUnlockLevel { get; }

        public SlotState(int unlockCoinCost, int automaticUnlockLevel, SlotType type, IReadOnlyList<CoinState> coins)
        {
            UnlockCoinCost = unlockCoinCost;
            AutomaticUnlockLevel = automaticUnlockLevel;
            Type = type;
            Coins = coins;
        }
    }

    public class CoinState
    {
        public int Tier { get; }
        public CoinType Type { get; }

        public CoinState(int tier, CoinType type)
        {
            Tier = tier;
            Type = type;
        }
    }
}
