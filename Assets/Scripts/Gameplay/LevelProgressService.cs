using General;
using SavedData;
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
        private const int SmallTierProgressDivisor = 50;

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
            Save();
            return true;
        }

        public bool RegisterCoinCreated(int tier)
        {
            if (tier < 4)
            {
                _model.ProgressPercentage += (100f - _model.ProgressPercentage) / SmallTierProgressDivisor;

                if (_model.ProgressPercentage >= LevelCompletionThreshold)
                {
                    CompleteLevel(true);
                    return true;
                }

                Save();
                return false;
            }

            if (_model.CurrentLevel == 1 && tier == 4 && tier > _model.HighestCreatedTier)
            {
                _model.HighestCreatedTier = tier;
                _model.PhaseIndex = 0;
                _model.ProgressPercentage += 100f / GetPhaseCount(tier, _model.PhaseIndex);
                Save();
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
                _model.ProgressPercentage += (100f - _model.ProgressPercentage) / SmallTierProgressDivisor;
            }

            if (_model.ProgressPercentage >= LevelCompletionThreshold)
            {
                CompleteLevel(true);
                return true;
            }

            Save();
            return false;
        }

        private void CompleteLevel(bool advancePhase)
        {
            _model.CurrentLevel++;
            _model.ProgressPercentage = 0f;

            if (advancePhase)
            {
                _model.PhaseIndex++;
            }

            Save();
        }

        private static int GetPhaseCount(int tier, int phaseIndex)
        {
            if (tier <= 5)
            {
                return 10;
            }

            if (tier == 6)
            {
                return phaseIndex % 2 == 0 ? 6 : 4;
            }

            return phaseIndex % 3 == 2 ? 4 : 3;
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
