using System.Collections.Generic;
using System.Text;
using GameConfig;
using UnityEngine;

namespace Gameplay
{
    public sealed class ObstacleDealPlanner
    {
        private const int MaximumPackagesPerDeal = 3;
        private const int MaximumPackagesOnBoard = 5;

        public ObstacleDealResult Apply(
            Board.DealPlan plan,
            DealContext context,
            int level,
            RemoteConfigModel config,
            LevelProgressModel progress,
            out string obstacleLog)
        {
            var result = new ObstacleDealResult();
            AddRainbows(plan, context, level, config, ref result);
            var isDynamicDifficultyActive = config.isDdaActive &&
                                            level >= config.ddaStartLevel &&
                                            progress.DdaInitialized;
            var schedule = ObstacleSchedule.Resolve(
                level,
                progress,
                isDynamicDifficultyActive);
            var packageCount = CountPackages(context);
            var probabilityRolls = new[] { -1, -1 };
            var typeRolls = new[] { -1, -1 };
            var typeWeightTotals = new[] { 0, 0 };
            var selectedTypes = new[] { CoinType.Standard, CoinType.Standard };

            for (var attempt = 0; attempt < 2 && attempt < MaximumPackagesPerDeal && packageCount < MaximumPackagesOnBoard; attempt++)
            {
                probabilityRolls[attempt] = Random.Range(0, 100);

                if (probabilityRolls[attempt] >= schedule.Probability)
                {
                    break;
                }

                var type = SelectType(
                    plan,
                    context,
                    level,
                    config,
                    schedule,
                    out typeRolls[attempt],
                    out typeWeightTotals[attempt]);
                selectedTypes[attempt] = type;

                if (type == CoinType.Standard)
                {
                    break;
                }

                if (AddPackage(plan, context, type, GetNextGroupId(context, plan)))
                {
                    packageCount++;
                    result.HasPackageDeal = true;
                }
            }

            obstacleLog = isDynamicDifficultyActive
                ? CreateObstacleLog(
                    progress,
                    schedule,
                    probabilityRolls,
                    typeRolls,
                    typeWeightTotals,
                    selectedTypes)
                : null;
            return result;
        }

        private static void AddRainbows(Board.DealPlan plan, DealContext context, int level, RemoteConfigModel config, ref ObstacleDealResult result)
        {
            var chance = level < config.rainbowUnlockLevel ? 0 : level <= 36 ? 20 : 5;
            if (Random.Range(0, 100) >= chance || !TryAddRainbow(plan, context))
            {
                return;
            }

            result.HasRainbowDeal = true;
            if (Random.Range(0, 100) < 30)
            {
                result.HasRainbowDeal |= TryAddRainbow(plan, context);
            }
        }

        private static CoinType SelectType(
            Board.DealPlan plan,
            DealContext context,
            int level,
            RemoteConfigModel config,
            ObstacleSchedule schedule,
            out int roll,
            out int total)
        {
            var hidden = level >= config.hiddenUnlockLevel && CanAddPackage(plan, context, CoinType.Hidden) ? schedule.HiddenWeight : 0;
            var sticky = level >= config.stickyUnlockLevel && CanAddPackage(plan, context, CoinType.Sticky) ? schedule.StickyWeight : 0;
            var iced = level >= config.icedUnlockLevel && CanAddPackage(plan, context, CoinType.Iced) ? schedule.IcedWeight : 0;
            total = hidden + sticky + iced;

            if (total == 0)
            {
                roll = -1;
                return CoinType.Standard;
            }

            roll = Random.Range(0, total);
            var value = roll;
            if ((value -= hidden) < 0) return CoinType.Hidden;
            if ((value -= sticky) < 0) return CoinType.Sticky;
            return CoinType.Iced;
        }

        private static string CreateObstacleLog(
            LevelProgressModel progress,
            ObstacleSchedule schedule,
            int[] probabilityRolls,
            int[] typeRolls,
            int[] typeWeightTotals,
            CoinType[] selectedTypes)
        {
            var totalWeight = schedule.HiddenWeight + schedule.StickyWeight + schedule.IcedWeight;
            var log = new StringBuilder();
            log.Append($"[DDA Obstacle] D={(progress.DdaEasyPhaseActive ? 0.2f : progress.DdaValue):F3}, stage={progress.DdaObstacleStage}");
            log.Append($", P1={schedule.Probability}, roll1={FormatRoll(probabilityRolls[0])}");
            log.Append($", P2={schedule.Probability}, roll2={FormatRoll(probabilityRolls[1])}");
            log.Append($", weights=[Hidden={schedule.HiddenWeight}, Sticky={schedule.StickyWeight}, Iced={schedule.IcedWeight}]");
            log.Append($", typeOdds=[Hidden={FormatPercentage(schedule.HiddenWeight, totalWeight)}, Sticky={FormatPercentage(schedule.StickyWeight, totalWeight)}, Iced={FormatPercentage(schedule.IcedWeight, totalWeight)}]");
            log.Append($", type1={selectedTypes[0]} ({FormatTypeRoll(typeRolls[0], typeWeightTotals[0])})");
            log.Append($", type2={selectedTypes[1]} ({FormatTypeRoll(typeRolls[1], typeWeightTotals[1])})");
            return log.ToString();
        }

        private static string FormatRoll(int roll)
        {
            return roll < 0 ? "not-rolled" : roll.ToString();
        }

        private static string FormatTypeRoll(int roll, int total)
        {
            return roll < 0 ? "not-rolled" : $"{roll}/{total}";
        }

        private static string FormatPercentage(int value, int total)
        {
            return total == 0 ? "0.0%" : $"{(float)value / total:P1}";
        }

        private static bool AddPackage(Board.DealPlan plan, DealContext context, CoinType type, int groupId)
        {
            var size = type == CoinType.Sticky ? SelectStickySize() : 3;
            var target = SelectPackageTarget(plan, context, size);
            if (target == null) return false;
            plan.Stacks.Add(new Board.DealStack(SelectMiddleWeightedTier(plan.MaxTier), size, target, type, type == CoinType.Hidden, groupId, type == CoinType.Iced ? 3 : 0));
            return true;
        }

        private static bool CanAddPackage(Board.DealPlan plan, DealContext context, CoinType type)
        {
            return SelectPackageTarget(plan, context, type == CoinType.Sticky ? 2 : 3) != null;
        }

        private static Slot SelectPackageTarget(Board.DealPlan plan, DealContext context, int requiredCapacity)
        {
            var candidates = new List<Slot>();
            foreach (var slot in context.Slots)
            {
                if (slot.IsUnlocked && GetPlannedStandardCount(plan, slot) > 0 &&
                    slot.AvailableCoinCapacity(context.SlotCapacity) - GetPlannedCount(plan, slot) >= requiredCapacity)
                    candidates.Add(slot);
            }
            return SelectWeighted(candidates);
        }

        private static bool TryAddRainbow(Board.DealPlan plan, DealContext context)
        {
            var candidates = new List<Slot>();
            foreach (var slot in context.Slots)
                if (slot.IsUnlocked && slot.AvailableCoinCapacity(context.SlotCapacity) > GetPlannedCount(plan, slot))
                    candidates.Add(slot);
            var target = SelectWeighted(candidates);
            if (target == null) return false;
            var tier = target.CoinCount > 0 ? target.GetCoin(target.CoinCount - 1).Tier : DealStrategyUtility.GetMinTier(plan.MaxTier);
            plan.Stacks.Add(new Board.DealStack(tier, 1, target, CoinType.Rainbow));
            return true;
        }

        private static Slot SelectWeighted(List<Slot> candidates)
        {
            if (candidates.Count == 0) return null;
            var weights = candidates.Count == 2 ? new[] { 65, 35 } : candidates.Count == 3 ? new[] { 45, 35, 20 } : new[] { 35, 30, 25, 10 };
            var count = Mathf.Min(candidates.Count, weights.Length);
            var value = Random.Range(0, Sum(weights, count));
            for (var index = 0; index < count; index++)
            {
                value -= weights[index];
                if (value < 0) return candidates[index];
            }
            return candidates[0];
        }

        private static int Sum(int[] values, int count) { var total = 0; for (var i = 0; i < count; i++) total += values[i]; return total; }
        private static int SelectStickySize() { var value = Random.Range(0, 100); return value < 5 ? 2 : value < 95 ? 3 : 5; }
        private static int GetPlannedCount(Board.DealPlan plan, Slot slot) { var total = 0; foreach (var stack in plan.Stacks) if (stack.TargetSlot == slot) total += stack.CoinCount; return total; }
        private static int GetPlannedStandardCount(Board.DealPlan plan, Slot slot) { var total = 0; foreach (var stack in plan.Stacks) if (stack.TargetSlot == slot && stack.Type == CoinType.Standard) total += stack.CoinCount; return total; }
        private static int CountPackages(DealContext context) { var groups = new HashSet<int>(); foreach (var slot in context.Slots) for (var i = 0; i < slot.CoinCount; i++) { var coin = slot.GetCoin(i); if (coin.Type != CoinType.Standard && coin.Type != CoinType.Rainbow) groups.Add(coin.ObstacleGroupId); } return groups.Count; }
        private static int GetNextGroupId(DealContext context, Board.DealPlan plan) { var highest = 0; foreach (var slot in context.Slots) for (var i = 0; i < slot.CoinCount; i++) highest = Mathf.Max(highest, slot.GetCoin(i).ObstacleGroupId); foreach (var stack in plan.Stacks) highest = Mathf.Max(highest, stack.ObstacleGroupId); return highest + 1; }
        private static int SelectMiddleWeightedTier(int maxTier) { var min = DealStrategyUtility.GetMinTier(maxTier); var max = maxTier == 4 ? maxTier : maxTier - 1; var weights = new[] { .2f, .8f, 1f, .8f, .2f }; var total = 0f; for (var i = min; i <= max; i++) total += weights[Mathf.Min((i - min) * weights.Length / (max - min + 1), 4)]; var value = Random.value * total; for (var i = min; i <= max; i++) { value -= weights[Mathf.Min((i - min) * weights.Length / (max - min + 1), 4)]; if (value <= 0) return i; } return max; }
    }

    public struct ObstacleDealResult { public bool HasRainbowDeal; public bool HasPackageDeal; }
}
