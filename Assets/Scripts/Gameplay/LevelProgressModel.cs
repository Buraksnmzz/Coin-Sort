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
        public List<ObstacleScheduleSnapshot> ObstacleSchedules = new List<ObstacleScheduleSnapshot>();
        public bool DdaInitialized;
        public float DdaValue;
        public int DdaPreActivationFailureCount;
        public int DdaConsecutiveLosses;
        public float DdaFailureStreakStartValue;
        public bool DdaBreathLevel;
        public bool DdaEasyPhaseActive;
        public bool DdaEarlyWinIncreaseApplied;
        public float DdaEarlyWinIncreaseAmount;
        public long DdaLastAttemptUtcTicks;
        public int DdaObstacleStage;
        public int DdaConsecutiveMaximumDifficultyWins;
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
        public bool IsHiddenGroupCenter;
        public int ObstacleGroupId;
        public int IcedCountdown;
    }

    [Serializable]
    public class ObstacleScheduleSnapshot
    {
        public int Level;
        public int Probability;
        public int HiddenWeight;
        public int StickyWeight;
        public int IcedWeight;
    }
}
