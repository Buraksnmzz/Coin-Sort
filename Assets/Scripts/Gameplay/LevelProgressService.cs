using General;
using SavedData;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class LevelProgressService : ILevelProgressService
    {
        private const float LevelCompletionThreshold = 95f;
        private const int FirstBoardChangeLevel = 28;
        private const int BoardChangeLevelInterval = 24;
        private const int LevelLoopStart = 77;
        private const int LevelLoopEnd = 221;
        private const int LevelLoopLength = 144;
        private const int FirstLevelSmallTierProgressDivisor = 50;
        private const int SubsequentLevelSmallTierProgressDivisor = 200;

        private readonly ISavedDataService _savedDataService;
        private readonly LevelProgressModel _model;

        public int CurrentLevel => _model.CurrentLevel;
        public int CurrentBoardLevel => GetBoardLevel(_model.CurrentLevel);
        public float Progress => _model.ProgressPercentage / 100f;

        public LevelProgressService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _model = _savedDataService.GetModel<LevelProgressModel>();
        }

        public int GetBoardIndex()
        {
            return CurrentBoardLevel < FirstBoardChangeLevel
                ? 1
                : 2 + (CurrentBoardLevel - FirstBoardChangeLevel) / BoardChangeLevelInterval;
        }

        public bool InitializeBoard(int boardIndex, int highestTier)
        {
            if (_model.CurrentBoardIndex == boardIndex)
            {
                return false;
            }

            _model.CurrentBoardIndex = boardIndex;
            _model.HighestCreatedTier = highestTier;
            _model.PhaseIndex = 0;
            _model.BoardSnapshot = null;
            return true;
        }

        public BoardState RestoreBoardState(BoardState generatedBoardState)
        {
            if (_model.BoardSnapshot == null || _model.BoardSnapshot.BoardIndex != generatedBoardState.BoardIndex)
            {
                return generatedBoardState;
            }

            if (_model.BoardSnapshot.Slots.Count != generatedBoardState.Slots.Count)
            {
                throw new System.InvalidOperationException("Saved board slot count does not match the generated board.");
            }

            var slots = new List<SlotState>(generatedBoardState.Slots.Count);

            for (var slotIndex = 0; slotIndex < generatedBoardState.Slots.Count; slotIndex++)
            {
                var generatedSlotState = generatedBoardState.Slots[slotIndex];
                var savedSlot = _model.BoardSnapshot.Slots[slotIndex];
                var coins = new List<CoinState>(savedSlot.Coins.Count);

                foreach (var savedCoin in savedSlot.Coins)
                {
                    coins.Add(new CoinState(savedCoin.Tier, savedCoin.Type));
                }

                slots.Add(new SlotState(
                    generatedSlotState.UnlockCoinCost,
                    generatedSlotState.AutomaticUnlockLevel,
                    generatedSlotState.Type,
                    coins));
            }

            return new BoardState(generatedBoardState.BoardIndex, slots);
        }

        public void SaveBoardState(BoardState boardState)
        {
            var snapshot = new BoardSnapshot
            {
                BoardIndex = boardState.BoardIndex
            };

            foreach (var slotState in boardState.Slots)
            {
                var slotSnapshot = new SlotSnapshot();

                foreach (var coinState in slotState.Coins)
                {
                    slotSnapshot.Coins.Add(new CoinSnapshot
                    {
                        Tier = coinState.Tier,
                        Type = coinState.Type
                    });
                }

                snapshot.Slots.Add(slotSnapshot);
            }
            _model.BoardSnapshot = snapshot;
            _model.BoardSnapshot = snapshot;
        }

        public bool RegisterCoinCreated(int tier)
        {
            if (tier < 4)
            {
                _model.ProgressPercentage += (100f - _model.ProgressPercentage) / GetSmallTierProgressDivisor();

                if (_model.ProgressPercentage >= LevelCompletionThreshold)
                {
                    CompleteLevel(true);
                    return true;
                }

                return false;
            }

            if (_model.CurrentLevel == 1 && tier == 4 && tier > _model.HighestCreatedTier)
            {
                _model.HighestCreatedTier = tier;
                _model.PhaseIndex = 0;
                _model.ProgressPercentage += 100f / GetPhaseCount(tier, _model.PhaseIndex);
                return false;
            }

            if (tier > _model.HighestCreatedTier)
            {
                _model.HighestCreatedTier = tier;
                _model.PhaseIndex = 0;
                CompleteLevel(false);
                return true;
            }

            if (tier == _model.HighestCreatedTier)
            {
                _model.ProgressPercentage += 100f / GetPhaseCount(tier, _model.PhaseIndex);
            }
            else
            {
                _model.ProgressPercentage += (100f - _model.ProgressPercentage) / GetSmallTierProgressDivisor();
            }

            if (_model.ProgressPercentage >= LevelCompletionThreshold)
            {
                CompleteLevel(true);
                return true;
            }

            return false;
        }

        public void Flush()
        {
            Save();
        }

        private void CompleteLevel(bool advancePhase)
        {
            _model.CurrentLevel++;
            _model.ProgressPercentage = 0f;

            if (advancePhase)
            {
                _model.PhaseIndex++;
            }

        }

        private static int GetPhaseCount(int tier, int phaseIndex)
        {
            if (tier <= 5)
            {
                return 15;
            }

            if (tier == 6)
            {
                return phaseIndex % 2 == 0 ? 7 : 6;
            }

            if (phaseIndex % 3 == 0)
                return 4;
            
            if(phaseIndex % 3 == 1)
                return 2;
            
            return 6;
            //return phaseIndex % 3 == 2 ? 5 : 4;
        }

        private int GetSmallTierProgressDivisor()
        {
            return _model.CurrentLevel == 1
                ? FirstLevelSmallTierProgressDivisor
                : SubsequentLevelSmallTierProgressDivisor;
        }

        private static int GetBoardLevel(int level)
        {
            return level < LevelLoopEnd
                ? level
                : LevelLoopStart + (level - LevelLoopStart) % LevelLoopLength;
        }

        private void Save()
        {
            _savedDataService.SaveData(_model);
        }
    }
}
