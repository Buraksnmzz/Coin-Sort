using UnityEngine;

namespace Gameplay
{
    public readonly struct ObstacleSchedule
    {
        public int Probability { get; }
        public int HiddenWeight { get; }
        public int StickyWeight { get; }
        public int IcedWeight { get; }

        private ObstacleSchedule(int probability, int hiddenWeight, int stickyWeight, int icedWeight)
        {
            Probability = probability;
            HiddenWeight = hiddenWeight;
            StickyWeight = stickyWeight;
            IcedWeight = icedWeight;
        }

        public static ObstacleSchedule Resolve(int level, LevelProgressModel progress, bool isDynamicDifficultyActive)
        {
            ObstacleSchedule schedule;

            if (level < 125)
            {
                schedule = Fixed(level);
            }
            else
            {
                schedule = ResolveRandomized(level, progress);
            }

            return isDynamicDifficultyActive ? ApplyDynamicDifficulty(schedule, progress) : schedule;
        }

        private static ObstacleSchedule ResolveRandomized(int level, LevelProgressModel progress)
        {
            foreach (var snapshot in progress.ObstacleSchedules)
            {
                if (snapshot.Level == level)
                {
                    return new ObstacleSchedule(
                        snapshot.Probability,
                        snapshot.HiddenWeight,
                        snapshot.StickyWeight,
                        snapshot.IcedWeight);
                }
            }

            var schedule = Randomized(level);
            progress.ObstacleSchedules.Add(new ObstacleScheduleSnapshot
            {
                Level = level,
                Probability = schedule.Probability,
                HiddenWeight = schedule.HiddenWeight,
                StickyWeight = schedule.StickyWeight,
                IcedWeight = schedule.IcedWeight
            });
            return schedule;
        }

        private static ObstacleSchedule ApplyDynamicDifficulty(ObstacleSchedule schedule, LevelProgressModel progress)
        {
            var difficulty = progress.DdaEasyPhaseActive ? 0.2f : progress.DdaValue;
            var difficultyMultiplier = difficulty < 0.3f
                ? Mathf.Lerp(0.5f, 1f, difficulty / 0.3f)
                : 1f;
            var stageMultiplier = Mathf.Pow(1.1f, progress.DdaObstacleStage);
            var probability = Mathf.Clamp(
                Mathf.RoundToInt(schedule.Probability * difficultyMultiplier * stageMultiplier),
                0,
                100);
            ShiftWeightsTowardHarder(
                schedule.HiddenWeight,
                schedule.StickyWeight,
                schedule.IcedWeight,
                progress.DdaObstacleStage,
                out var hiddenWeight,
                out var stickyWeight,
                out var icedWeight);
            return new ObstacleSchedule(probability, hiddenWeight, stickyWeight, icedWeight);
        }

        private static void ShiftWeightsTowardHarder(
            int hiddenWeight,
            int stickyWeight,
            int icedWeight,
            int stage,
            out int adjustedHiddenWeight,
            out int adjustedStickyWeight,
            out int adjustedIcedWeight)
        {
            adjustedHiddenWeight = hiddenWeight;
            adjustedStickyWeight = stickyWeight;
            adjustedIcedWeight = icedWeight;

            for (var index = 0; index < stage; index++)
            {
                var totalWeight = adjustedHiddenWeight + adjustedStickyWeight + adjustedIcedWeight;
                var transfer = Mathf.RoundToInt(totalWeight * 0.1f);

                if (transfer == 0)
                {
                    continue;
                }

                var transferToIced = Mathf.Min(adjustedStickyWeight, transfer / 2);
                var transferToSticky = Mathf.Min(adjustedHiddenWeight, transfer - transferToIced);
                var remainingTransfer = transfer - transferToIced - transferToSticky;

                if (remainingTransfer > 0)
                {
                    var additionalTransferToIced = Mathf.Min(adjustedStickyWeight - transferToIced, remainingTransfer);
                    transferToIced += additionalTransferToIced;
                }

                adjustedHiddenWeight -= transferToSticky;
                adjustedStickyWeight += transferToSticky - transferToIced;
                adjustedIcedWeight += transferToIced;
            }
        }

        private static ObstacleSchedule Fixed(int level)
        {
            if (level <= 12) return new ObstacleSchedule(0, 0, 0, 0);
            if (level <= 28) return new ObstacleSchedule(40, 40, 0, 0); 
            if (level <= 36) return new ObstacleSchedule(15, 15, 0, 0);
            if (level <= 45) return new ObstacleSchedule(40, 20, 20, 0);
            if (level <= 52) return new ObstacleSchedule(50, 36, 26, 0);
            if (level <= 63) return new ObstacleSchedule(25, 12, 13, 1);
            if (level <= 70) return new ObstacleSchedule(50, 31, 20, 14);
            if (level <= 76) return new ObstacleSchedule(75, 42, 31, 14);
            if (level <= 81) return new ObstacleSchedule(10, 5, 4, 2);
            if (level <= 84) return new ObstacleSchedule(35, 16, 27, 0);
            if (level <= 94) return new ObstacleSchedule(25, 13, 13, 3);
            if (level <= 100) return new ObstacleSchedule(85, 42, 42, 60);
            if (level <= 105) return new ObstacleSchedule(10, 2, 2, 7);
            if (level <= 109) return new ObstacleSchedule(25, 19, 12, 0);
            if (level <= 118) return new ObstacleSchedule(35, 19, 19, 5);
            return new ObstacleSchedule(70, 18, 18, 62);
        }

        private static ObstacleSchedule Randomized(int level)
        {
            var position = (level - 125) % 24;
            if (position < 5) return new ObstacleSchedule(10, 2, 2, 7);
            if (position < 9) return new ObstacleSchedule(Random.Range(25, 36), Random.Range(16, 20), Random.Range(12, 28), 0);
            if (position < 18) return new ObstacleSchedule(Random.Range(25, 46), Random.Range(13, 26), Random.Range(13, 26), Random.Range(3, 7));
            return ((level - 125) / 24) % 2 == 0 ? new ObstacleSchedule(85, 42, 42, 60) : new ObstacleSchedule(70, 18, 18, 62);
        }
    }
}
