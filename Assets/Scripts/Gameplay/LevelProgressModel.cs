using System;
using System.Collections.Generic;
using SavedData;

namespace Gameplay
{
    public class LevelProgressModel : IModel
    {
        public int CurrentLevel = 1;
        public float ProgressPercentage;
        public int HighestCreatedTier;
        public int CurrentBoardIndex;
        public int PhaseIndex;
        public BoardSnapshot BoardSnapshot;
    }

    [Serializable]
    public class BoardSnapshot
    {
        public int BoardIndex;
        public List<SlotSnapshot> Slots = new List<SlotSnapshot>();
    }

    [Serializable]
    public class SlotSnapshot
    {
        public List<CoinSnapshot> Coins = new List<CoinSnapshot>();
    }

    [Serializable]
    public class CoinSnapshot
    {
        public int Tier;
        public CoinType Type;
    }
}
