using System;
using GameConfig;
using General;
using SavedData;
using UnityEngine;

namespace Gameplay
{
    public class DynamicDifficultyService : IDynamicDifficultyService
    {
        private const float AutoMergeThreshold = 0.3f;
        private const float EasyPhaseDifficulty = 0.2f;
        private const float MinimumStartingDifficultyAfterBreak = 0.35f;
        private const float EarlyWinProgressThreshold = 0.5f;
        private const float FlawlessWinIncrease = 0.03f;
        private const float EasyPhaseWinMultiplier = 0.1f;
        private const float ConsecutiveLossDecrease = 0.05f;
        private const float ExtendedConsecutiveLossDecrease = 0.08f;
        private const float MaximumConsecutiveLossDecrease = 0.1f;
        private const float PostFailureWinCap = 0.05f;
        private const float EasyPhaseExitDecrease = 0.03f;
        private const float ReturnAfterBreakDecrease = 0.05f;
        private const int BreakLengthDays = 7;
        private const int PreDdaLevelCount = 21;
        private const int EasyPhaseRequiredExtraSlots = 2;
        private const int FirstStepUpBoard = 5;
        private const float StepUpPerBoard = 0.15f;
        private const float MaximumStepUpMultiplier = 2f;
        private const int MaximumObstacleStage = 3;
        private const int MaximumDifficultyWinsPerObstacleStage = 2;

        private readonly ISavedDataService _savedDataService;
        private readonly ILevelProgressService _levelProgressService;
        private readonly LevelProgressModel _progressModel;
        private readonly RemoteConfigModel _remoteConfigModel;
        private bool _isCurrentAttemptActive;

        public bool IsActive => _remoteConfigModel.isDdaActive &&
                                _levelProgressService.CurrentLevel >= _remoteConfigModel.ddaStartLevel;
        public float CurrentDifficulty => GetEffectiveDifficulty();

        public DynamicDifficultyService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _levelProgressService = ServiceLocator.GetService<ILevelProgressService>();
            _progressModel = _savedDataService.GetModel<LevelProgressModel>();
            _remoteConfigModel = _savedDataService.GetModel<RemoteConfigModel>();
        }

        public HardnessDealConfig GetDealConfig()
        {
            EnsureInitialized();
            var difficulty = GetEffectiveDifficulty();

            if (difficulty <= AutoMergeThreshold)
            {
                return new HardnessDealConfig(
                    HardnessDealType.AutoMerge,
                    Mathf.Lerp(1f, 0.25f, difficulty / AutoMergeThreshold));
            }

            return new HardnessDealConfig(
                HardnessDealType.Smart,
                Mathf.Lerp(0.25f, 1f, (difficulty - AutoMergeThreshold) / (1f - AutoMergeThreshold)));
        }

        public void BeginAttempt()
        {
            if (!IsActive)
            {
                _isCurrentAttemptActive = false;
                return;
            }

            EnsureInitialized();
            ApplyBreakReturnDecrease();
            _isCurrentAttemptActive = true;
            _progressModel.DdaEarlyWinIncreaseApplied = false;
            _progressModel.DdaEarlyWinIncreaseAmount = 0f;
            Save();
        }

        public void RegisterProgress(float progress)
        {
            if (!_isCurrentAttemptActive || progress < EarlyWinProgressThreshold || _progressModel.DdaEarlyWinIncreaseApplied ||
                _progressModel.DdaBreathLevel)
            {
                return;
            }

            EnsureInitialized();
            var increase = GetWinIncrease();
            AdjustDifficulty(increase);
            _progressModel.DdaEarlyWinIncreaseApplied = true;
            _progressModel.DdaEarlyWinIncreaseAmount = increase;
            Save();
        }

        public void RegisterWin(int boardIndex)
        {
            if (!_isCurrentAttemptActive)
            {
                return;
            }

            EnsureInitialized();

            if (!_progressModel.DdaBreathLevel && !_progressModel.DdaEarlyWinIncreaseApplied)
            {
                AdjustDifficulty(GetWinIncrease(boardIndex));
            }

            if (_progressModel.DdaConsecutiveLosses >= 3)
            {
                _progressModel.DdaValue = Mathf.Min(
                    _progressModel.DdaValue,
                    _progressModel.DdaFailureStreakStartValue - PostFailureWinCap);
                _progressModel.DdaBreathLevel = true;
            }
            else if (_progressModel.DdaBreathLevel)
            {
                _progressModel.DdaBreathLevel = false;
            }

            _progressModel.DdaConsecutiveLosses = 0;
            _progressModel.DdaEarlyWinIncreaseApplied = false;
            _progressModel.DdaEarlyWinIncreaseAmount = 0f;
            UpdateObstacleStageForWin();
            _isCurrentAttemptActive = false;
            Save();
        }

        public void RegisterLoss()
        {
            if (_levelProgressService.CurrentLevel <= PreDdaLevelCount)
            {
                _progressModel.DdaPreActivationFailureCount++;
            }

            if (!IsActive)
            {
                Save();
                return;
            }

            if (!_isCurrentAttemptActive)
            {
                return;
            }

            EnsureInitialized();
            _progressModel.DdaObstacleStage = 0;
            _progressModel.DdaConsecutiveMaximumDifficultyWins = 0;

            if (_progressModel.DdaEarlyWinIncreaseApplied)
            {
                AdjustDifficulty(-_progressModel.DdaEarlyWinIncreaseAmount);
                _progressModel.DdaEarlyWinIncreaseApplied = false;
                _progressModel.DdaEarlyWinIncreaseAmount = 0f;
            }

            if (_progressModel.DdaConsecutiveLosses == 0)
            {
                _progressModel.DdaFailureStreakStartValue = _progressModel.DdaValue;
            }

            _progressModel.DdaConsecutiveLosses++;

            if (_progressModel.DdaConsecutiveLosses >= 2)
            {
                var decrease = _progressModel.DdaConsecutiveLosses == 2
                    ? ConsecutiveLossDecrease
                    : ExtendedConsecutiveLossDecrease;
                var minimumValue = _progressModel.DdaFailureStreakStartValue - MaximumConsecutiveLossDecrease;
                _progressModel.DdaValue = Mathf.Max(minimumValue, _progressModel.DdaValue - decrease);
            }

            _isCurrentAttemptActive = false;
            Save();
        }

        public void UpdateEasyPhase(BoardState boardState)
        {
            if (!IsActive)
            {
                return;
            }

            EnsureInitialized();
            var unlockedSlotCount = 0;
            var highestTier = 0;

            foreach (var slot in boardState.Slots)
            {
                foreach (var coin in slot.Coins)
                {
                    highestTier = Mathf.Max(highestTier, coin.Tier);
                }

                if (slot.Type == SlotType.Unlocked)
                {
                    unlockedSlotCount++;
                }
            }

            var minimumTier = DealStrategyUtility.GetMinTier(highestTier);
            var shouldUseEasyPhase = unlockedSlotCount >= highestTier - minimumTier + EasyPhaseRequiredExtraSlots;

            if (shouldUseEasyPhase == _progressModel.DdaEasyPhaseActive)
            {
                return;
            }

            _progressModel.DdaEasyPhaseActive = shouldUseEasyPhase;

            if (shouldUseEasyPhase)
            {
                _progressModel.DdaObstacleStage = Mathf.Max(0, _progressModel.DdaObstacleStage - 1);
            }
            else
            {
                AdjustDifficulty(-EasyPhaseExitDecrease);
            }

            Save();
        }

        private void EnsureInitialized()
        {
            if (!IsActive || _progressModel.DdaInitialized)
            {
                return;
            }

            _progressModel.DdaValue = _progressModel.DdaPreActivationFailureCount > 5
                ? MinimumStartingDifficultyAfterBreak
                : _progressModel.DdaPreActivationFailureCount >= 3
                    ? 0.28f
                    : Mathf.Clamp01(_remoteConfigModel.ddaStartingValue);
            _progressModel.DdaInitialized = true;
            Save();
        }

        private float GetEffectiveDifficulty()
        {
            return _progressModel.DdaEasyPhaseActive ? EasyPhaseDifficulty : _progressModel.DdaValue;
        }

        private float GetWinIncrease(int boardIndex = 0)
        {
            var resolvedBoardIndex = boardIndex == 0 ? _levelProgressService.GetBoardIndex() : boardIndex;
            var stepUpMultiplier = resolvedBoardIndex < FirstStepUpBoard
                ? 1f
                : Mathf.Min(
                    MaximumStepUpMultiplier,
                    1f + StepUpPerBoard * (resolvedBoardIndex - FirstStepUpBoard + 1));
            var easyPhaseMultiplier = _progressModel.DdaEasyPhaseActive ? EasyPhaseWinMultiplier : 1f;
            return FlawlessWinIncrease * stepUpMultiplier * easyPhaseMultiplier;
        }

        private void ApplyBreakReturnDecrease()
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            if (_progressModel.DdaLastAttemptUtcTicks > 0)
            {
                var elapsed = new TimeSpan(nowTicks - _progressModel.DdaLastAttemptUtcTicks);

                if (elapsed.TotalDays >= BreakLengthDays)
                {
                    _progressModel.DdaValue = Mathf.Max(
                        MinimumStartingDifficultyAfterBreak,
                        _progressModel.DdaValue - ReturnAfterBreakDecrease);
                    _progressModel.DdaObstacleStage = Mathf.Max(0, _progressModel.DdaObstacleStage - 1);
                }
            }

            _progressModel.DdaLastAttemptUtcTicks = nowTicks;
        }

        private void AdjustDifficulty(float amount)
        {
            _progressModel.DdaValue = Mathf.Clamp01(_progressModel.DdaValue + amount);
        }

        private void UpdateObstacleStageForWin()
        {
            if (!Mathf.Approximately(GetEffectiveDifficulty(), 1f))
            {
                _progressModel.DdaConsecutiveMaximumDifficultyWins = 0;
                return;
            }

            _progressModel.DdaConsecutiveMaximumDifficultyWins++;

            if (_progressModel.DdaConsecutiveMaximumDifficultyWins < MaximumDifficultyWinsPerObstacleStage)
            {
                return;
            }

            _progressModel.DdaObstacleStage = Mathf.Min(
                MaximumObstacleStage,
                _progressModel.DdaObstacleStage + 1);
            _progressModel.DdaConsecutiveMaximumDifficultyWins = 0;
        }

        private void Save()
        {
            _savedDataService.SaveData(_progressModel);
        }
    }
}
