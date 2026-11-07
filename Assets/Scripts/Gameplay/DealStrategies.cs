using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace Gameplay
{
    public enum DealStrategyType
    {
        Basic,
        Easy,
        Hard,
        AutoMerge,
        Smart
    }

    public interface IDealStrategy
    {
        DealStrategyType Type { get; }
        bool IncludesObstaclePlan { get; }
        Board.DealPlan CreatePlan(DealContext context);
    }

    public readonly struct DealContext
    {
        public Slot[] Slots { get; }
        public int SlotCapacity { get; }
        public int MaxTier { get; }

        public DealContext(Slot[] slots, int slotCapacity, int maxTier)
        {
            Slots = slots;
            SlotCapacity = slotCapacity;
            MaxTier = maxTier;
        }
    }

    public sealed class BasicDealStrategy : IDealStrategy
    {
        public DealStrategyType Type => DealStrategyType.Basic;
        public bool IncludesObstaclePlan => false;

        public Board.DealPlan CreatePlan(DealContext context)
        {
            return DealStrategyUtility.CreateBasicPlan(context, false);
        }
    }

    public sealed class EasyDealStrategy : IDealStrategy
    {
        public DealStrategyType Type => DealStrategyType.Easy;
        public bool IncludesObstaclePlan => false;

        public Board.DealPlan CreatePlan(DealContext context)
        {
            return DealStrategyUtility.CreateBasicPlan(context, true);
        }
    }

    public sealed class HardDealStrategy : IDealStrategy
    {
        private readonly float _hardness;

        public DealStrategyType Type => DealStrategyType.Hard;
        public bool IncludesObstaclePlan => false;

        public HardDealStrategy(float hardness = 1f)
        {
            _hardness = hardness;
        }

        public Board.DealPlan CreatePlan(DealContext context)
        {
            var plan = DealStrategyUtility.CreateBasicPlan(context, false);

            if (plan == null)
            {
                return null;
            }

            if (Random.value > _hardness)
            {
                DealStrategyUtility.ApplyEasy(plan, context);
                return plan;
            }

            DealStrategyUtility.ApplyHard(plan, context);
            return plan;
        }
    }

    public sealed partial class AutoMergeDealStrategy : IDealStrategy
    {
        private const int MaximumCandidateCount = 4;
        private readonly float _autoMergeValue;

        public DealStrategyType Type => DealStrategyType.AutoMerge;
        public bool IncludesObstaclePlan => false;

        public AutoMergeDealStrategy(float autoMergeValue = 0.5f)
        {
            _autoMergeValue = autoMergeValue;
        }

    }

    public sealed class SmartDealStrategy : IDealStrategy
    {
            private const int CandidateCount = 50;
            private const int MaximumDepth = 5;
            private const int MaximumStateCount = 50;
            private const int MaximumSolverDurationMilliseconds = 600;
            private const int MergeStackSize = 10;

            private readonly float _selectionValue;
            private readonly System.Action<Board.DealPlan> _applyObstaclePlan;

            public DealStrategyType Type => DealStrategyType.Smart;
            public bool IncludesObstaclePlan => true;

            public SmartDealStrategy(float selectionValue, System.Action<Board.DealPlan> applyObstaclePlan)
            {
                _selectionValue = Mathf.Clamp01(selectionValue);
                _applyObstaclePlan = applyObstaclePlan;
            }

        public Board.DealPlan CreatePlan(DealContext context)
            {
                if (GetBoardFillRatio(context) >= .75f)
                {
                    return CreateFallbackPlan(context, "board is at least 75% full");
                }

                var stopwatch = Stopwatch.StartNew();
                var candidates = new List<Candidate>();
                var planHashes = new HashSet<string>();
                var evaluatedStateCount = 0;
                var deadPrunedCount = 0;

                try
                {
                    for (var candidateIndex = 0; candidateIndex < CandidateCount; candidateIndex++)
                    {
                        if (stopwatch.ElapsedMilliseconds >= MaximumSolverDurationMilliseconds)
                        {
                            break;
                        }

                        var plan = DealStrategyUtility.CreateBasicPlan(context, false);

                        if (plan == null)
                        {
                            break;
                        }

                        _applyObstaclePlan(plan);
                        var planHash = CreatePlanHash(plan, context);

                        if (!planHashes.Add(planHash))
                        {
                            continue;
                        }

                        var result = Evaluate(plan, context, stopwatch);

                        if (result.TimedOut)
                        {
                            break;
                        }

                        evaluatedStateCount += result.StateCount;
                        deadPrunedCount += result.DeadPrunedCount;

                        if (!result.IsUnfair)
                        {
                            candidates.Add(new Candidate(plan, result.Risk));
                        }
                    }
                }
                catch (System.Exception exception)
                {
                    UnityEngine.Debug.LogError($"Smart Deal failed and is using Basic Deal. {exception}");
                    return CreateFallbackPlan(context, "solver error");
                }

                stopwatch.Stop();

                if (candidates.Count == 0)
                {
                    LogCandidateRisks(candidates, stopwatch.ElapsedMilliseconds, evaluatedStateCount, deadPrunedCount);
                    return CreateFallbackPlan(context, stopwatch.ElapsedMilliseconds >= MaximumSolverDurationMilliseconds
                        ? "solver time limit reached before a fair candidate was found"
                        : "no fair candidate was found");
                }

                candidates.Sort((first, second) => first.Risk.CompareTo(second.Risk));
                LogCandidateRisks(candidates, stopwatch.ElapsedMilliseconds, evaluatedStateCount, deadPrunedCount);
                var selectedIndex = Mathf.RoundToInt(_selectionValue * (candidates.Count - 1));
                var selected = candidates[selectedIndex];
                UnityEngine.Debug.Log($"Smart Deal selected risk={selected.Risk:F3}, candidates={candidates.Count}, duration={stopwatch.ElapsedMilliseconds}ms");
                return selected.Plan;
            }

            private static float GetBoardFillRatio(DealContext context)
            {
                var occupiedCells = 0;
                var usableCells = 0;

                foreach (var slot in context.Slots)
                {
                    if (!slot.IsUnlocked)
                    {
                        continue;
                    }

                    occupiedCells += slot.CoinCount;
                    usableCells += context.SlotCapacity;
                }

                return usableCells == 0 ? 1f : (float)occupiedCells / usableCells;
            }

            private static void LogCandidateRisks(
                List<Candidate> candidates,
                long durationMilliseconds,
                int evaluatedStateCount,
                int deadPrunedCount)
            {
                var risks = new StringBuilder();

                for (var index = 0; index < candidates.Count; index++)
                {
                    if (index > 0)
                    {
                        risks.Append(", ");
                    }

                    risks.Append(candidates[index].Risk.ToString("F3"));
                }

                UnityEngine.Debug.Log(
                    $"Smart Deal trap scores=[{risks}], candidates={candidates.Count}, states={evaluatedStateCount}, deadPruned={deadPrunedCount}, duration={durationMilliseconds}ms");
            }

            private static Board.DealPlan CreateFallbackPlan(DealContext context, string reason)
            {
                UnityEngine.Debug.LogWarning($"Smart Deal fell back to Basic Deal: {reason}");
                return DealStrategyUtility.CreateBasicPlan(context, false);
            }

            private static string CreatePlanHash(Board.DealPlan plan, DealContext context)
            {
                var hash = new StringBuilder();
                hash.Append(plan.SkippedSlot == null ? -1 : System.Array.IndexOf(context.Slots, plan.SkippedSlot));

                foreach (var stack in plan.Stacks)
                {
                    hash.Append('|');
                    hash.Append(System.Array.IndexOf(context.Slots, stack.TargetSlot));
                    hash.Append(':');
                    hash.Append(stack.Tier);
                    hash.Append(':');
                    hash.Append(stack.CoinCount);
                    hash.Append(':');
                    hash.Append((int)stack.Type);
                    hash.Append(':');
                    hash.Append(stack.ObstacleGroupId);
                }

                return hash.ToString();
            }

            private static EvaluationResult Evaluate(Board.DealPlan plan, DealContext context, Stopwatch stopwatch)
            {
                var root = CreateState(plan, context);
                var rootHash = root.CreateHash();
                var visited = new HashSet<ulong> { rootHash };
                var nodes = new List<SearchNode> { new SearchNode(root, 0, -1, -1) };
                var queue = new Queue<int>();
                var moveBuffer = new List<SolverMove>();
                queue.Enqueue(0);
                var timedOut = false;
                var deadPrunedCount = 0;

                if (root.IsDead())
                {
                    return new EvaluationResult(1f, true, false, nodes.Count, 1);
                }

                while (queue.Count > 0 && nodes.Count < MaximumStateCount)
                {
                    if (stopwatch.ElapsedMilliseconds >= MaximumSolverDurationMilliseconds)
                    {
                        timedOut = true;
                        break;
                    }

                    var nodeIndex = queue.Dequeue();
                    var node = nodes[nodeIndex];

                    if (node.Depth == MaximumDepth)
                    {
                        continue;
                    }

                    moveBuffer.Clear();
                    node.State.CollectMoves(moveBuffer, node.PreviousSourceSlot, node.PreviousTargetSlot);

                    foreach (var move in moveBuffer)
                    {
                        var childState = node.State.Apply(move);
                        var hash = childState.CreateHash();

                        if (!visited.Add(hash))
                        {
                            continue;
                        }

                        var childIndex = nodes.Count;
                        var isDead = childState.IsDead();
                        nodes.Add(new SearchNode(childState, node.Depth + 1, move.SourceSlot, move.TargetSlot, isDead));
                        node.Children.Add(childIndex);

                        if (isDead)
                        {
                            nodes[childIndex].Risk = 1f;
                            deadPrunedCount++;
                        }
                        else
                        {
                            queue.Enqueue(childIndex);
                        }

                        if (nodes.Count == MaximumStateCount)
                        {
                            break;
                        }
                    }
                }

                if (timedOut)
                {
                    return new EvaluationResult(0f, false, true, nodes.Count, deadPrunedCount);
                }

                for (var nodeIndex = nodes.Count - 1; nodeIndex >= 0; nodeIndex--)
                {
                    var node = nodes[nodeIndex];

                    if (node.IsDead)
                    {
                        node.Risk = 1f;
                        continue;
                    }

                    if (node.Children.Count == 0)
                    {
                        node.Risk = node.State.GetLeafRisk();
                        continue;
                    }

                    var minRisk = 1f;
                    var totalRisk = 0f;

                    foreach (var childIndex in node.Children)
                    {
                        var risk = nodes[childIndex].Risk;
                        minRisk = Mathf.Min(minRisk, risk);
                        totalRisk += risk;
                    }

                    node.Risk = .7f * minRisk + .3f * totalRisk / node.Children.Count;
                }

                var isUnfair = false;

                if (!isUnfair && nodes[0].Children.Count > 0)
                {
                    isUnfair = true;

                    foreach (var childIndex in nodes[0].Children)
                    {
                        if (nodes[childIndex].Risk < 1f)
                        {
                            isUnfair = false;
                            break;
                        }
                    }
                }

                return new EvaluationResult(nodes[0].Risk, isUnfair, false, nodes.Count, deadPrunedCount);
            }

            private static SolverState CreateState(Board.DealPlan plan, DealContext context)
            {
                var slots = new List<List<SolverCoin>>(context.Slots.Length);
                var unlockedSlots = new bool[context.Slots.Length];

                for (var slotIndex = 0; slotIndex < context.Slots.Length; slotIndex++)
                {
                    var slot = context.Slots[slotIndex];
                    unlockedSlots[slotIndex] = slot.IsUnlocked;
                    var coins = new List<SolverCoin>(slot.CoinCount);

                    for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
                    {
                        var coin = slot.GetCoin(coinIndex);
                        coins.Add(new SolverCoin(coin.Tier, coin.Type, coin.ObstacleGroupId, coin.IcedCountdown));
                    }

                    slots.Add(coins);
                }

                foreach (var stack in plan.Stacks)
                {
                    var slotIndex = System.Array.IndexOf(context.Slots, stack.TargetSlot);

                    for (var coinIndex = 0; coinIndex < stack.CoinCount; coinIndex++)
                    {
                        slots[slotIndex].Add(new SolverCoin(stack.Tier, stack.Type, stack.ObstacleGroupId, stack.IcedCountdown));
                    }
                }

                var state = new SolverState(slots, unlockedSlots, context.SlotCapacity);
                state.RevealTopHiddenCoins();
                return state;
            }

            private readonly struct Candidate
            {
                public Board.DealPlan Plan { get; }
                public float Risk { get; }

                public Candidate(Board.DealPlan plan, float risk)
                {
                    Plan = plan;
                    Risk = risk;
                }
            }

            private readonly struct EvaluationResult
            {
                public float Risk { get; }
                public bool IsUnfair { get; }
                public bool TimedOut { get; }
                public int StateCount { get; }
                public int DeadPrunedCount { get; }

                public EvaluationResult(float risk, bool isUnfair, bool timedOut, int stateCount, int deadPrunedCount)
                {
                    Risk = risk;
                    IsUnfair = isUnfair;
                    TimedOut = timedOut;
                    StateCount = stateCount;
                    DeadPrunedCount = deadPrunedCount;
                }
            }

            private sealed class SearchNode
            {
                public SolverState State { get; }
                public int Depth { get; }
                public int PreviousSourceSlot { get; }
                public int PreviousTargetSlot { get; }
                public List<int> Children { get; } = new List<int>();
                public bool IsDead { get; }
                public float Risk { get; set; }

                public SearchNode(
                    SolverState state,
                    int depth,
                    int previousSourceSlot,
                    int previousTargetSlot,
                    bool isDead = false)
                {
                    State = state;
                    Depth = depth;
                    PreviousSourceSlot = previousSourceSlot;
                    PreviousTargetSlot = previousTargetSlot;
                    IsDead = isDead;
                }
            }

            private readonly struct SolverCoin
            {
                public int Tier { get; }
                public CoinType Type { get; }
                public int GroupId { get; }
                public int IcedCountdown { get; }

                public SolverCoin(int tier, CoinType type, int groupId, int icedCountdown)
                {
                    Tier = tier;
                    Type = type;
                    GroupId = groupId;
                    IcedCountdown = icedCountdown;
                }

                public SolverCoin AsStandard()
                {
                    return new SolverCoin(Tier, CoinType.Standard, 0, 0);
                }
            }

            private readonly struct SolverMove
            {
                public int SourceSlot { get; }
                public int TargetSlot { get; }

                public SolverMove(int sourceSlot, int targetSlot)
                {
                    SourceSlot = sourceSlot;
                    TargetSlot = targetSlot;
                }
            }

            private sealed class SolverState
            {
                private readonly List<List<SolverCoin>> _slots;
                private readonly bool[] _unlockedSlots;
                private readonly int _slotCapacity;

                public SolverState(List<List<SolverCoin>> slots, bool[] unlockedSlots, int slotCapacity)
                {
                    _slots = slots;
                    _unlockedSlots = unlockedSlots;
                    _slotCapacity = slotCapacity;
                }

                public void CollectMoves(List<SolverMove> moves, int previousSourceSlot, int previousTargetSlot)
                {
                    for (var sourceIndex = 0; sourceIndex < _slots.Count; sourceIndex++)
                    {
                        for (var targetIndex = 0; targetIndex < _slots.Count; targetIndex++)
                        {
                            if (CanMove(sourceIndex, targetIndex, previousSourceSlot, previousTargetSlot))
                            {
                                moves.Add(new SolverMove(sourceIndex, targetIndex));
                            }
                        }
                    }
                }

                public SolverState Apply(SolverMove move)
                {
                    var slots = CloneSlots();
                    var source = slots[move.SourceSlot];
                    var target = slots[move.TargetSlot];
                    var topStackCount = GetTopStackCount(source);
                    var transferCount = Mathf.Min(topStackCount, _slotCapacity - target.Count);
                    var transferStartIndex = source.Count - transferCount;
                    var transferred = source.GetRange(transferStartIndex, transferCount);
                    source.RemoveRange(transferStartIndex, transferCount);
                    var targetTopTier = target.Count > 0 ? target[target.Count - 1].Tier : 0;

                    if (targetTopTier > 0)
                    {
                        for (var coinIndex = 0; coinIndex < transferred.Count; coinIndex++)
                        {
                            if (transferred[coinIndex].Type == CoinType.Rainbow)
                            {
                                transferred[coinIndex] = new SolverCoin(targetTopTier, CoinType.Standard, 0, 0);
                            }
                        }
                    }

                    target.AddRange(transferred);
                    ConvertStickyGroupWhenMergeReady(target, transferred);
                    var state = new SolverState(slots, _unlockedSlots, _slotCapacity);
                    state.RevealTopHiddenCoins();
                    state.TryMerge(move.TargetSlot);
                    return state;
                }

                public bool IsDead()
                {
                    if (GetEmptySlotCount() > 0 || HasMergeMove())
                    {
                        return false;
                    }

                    return !HasAvailableMove();
                }

                public float GetLeafRisk()
                {
                    var emptySlotCount = 0;
                    var emptyCellCount = 0;

                    for (var slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
                    {
                        if (!_unlockedSlots[slotIndex])
                        {
                            continue;
                        }

                        var emptyCells = _slotCapacity - _slots[slotIndex].Count;

                        if (emptyCells > 0)
                        {
                            emptySlotCount++;
                            emptyCellCount += emptyCells;
                        }
                    }

                    var expectedDeal = 4 * (emptySlotCount - 1);
                    return expectedDeal <= 0 ? (emptyCellCount == 0 ? 1f : 0f) : Mathf.Clamp01(1f - (float)emptyCellCount / expectedDeal);
                }

                public ulong CreateHash()
                {
                    var hash = 14695981039346656037UL;

                    for (var slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
                    {
                        AppendHash(ref hash, (ulong)slotIndex);
                        AppendHash(ref hash, _unlockedSlots[slotIndex] ? 1UL : 0UL);
                        AppendHash(ref hash, (ulong)_slots[slotIndex].Count);

                        foreach (var coin in _slots[slotIndex])
                        {
                            AppendHash(ref hash, (ulong)coin.Tier);
                            AppendHash(ref hash, (ulong)coin.Type);
                            AppendHash(ref hash, (ulong)coin.GroupId);
                            AppendHash(ref hash, (ulong)coin.IcedCountdown);
                        }
                    }

                    return hash;
                }

                public void RevealTopHiddenCoins()
                {
                    foreach (var slot in _slots)
                    {
                        while (slot.Count > 0 && slot[slot.Count - 1].Type == CoinType.Hidden)
                        {
                            var groupId = slot[slot.Count - 1].GroupId;

                            for (var coinIndex = 0; coinIndex < slot.Count; coinIndex++)
                            {
                                if (slot[coinIndex].Type == CoinType.Hidden && slot[coinIndex].GroupId == groupId)
                                {
                                    slot[coinIndex] = slot[coinIndex].AsStandard();
                                }
                            }
                        }
                    }
                }

                private void TryMerge(int targetSlot)
                {
                    var target = _slots[targetSlot];

                    if (GetMergeableTopCount(target) != MergeStackSize)
                    {
                        return;
                    }

                    var mergedTier = target[target.Count - 1].Tier;
                    var highestTier = GetHighestTier();
                    target.Clear();
                    var upgradedTier = mergedTier + 1;
                    var upgradedCoinCount = upgradedTier > highestTier ? 1 : 2;

                    for (var coinIndex = 0; coinIndex < upgradedCoinCount; coinIndex++)
                    {
                        target.Add(new SolverCoin(upgradedTier, CoinType.Standard, 0, 0));
                    }

                    ReduceIcedCountdowns();
                }

                private void ReduceIcedCountdowns()
                {
                    for (var slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
                    {
                        for (var coinIndex = 0; coinIndex < _slots[slotIndex].Count; coinIndex++)
                        {
                            var coin = _slots[slotIndex][coinIndex];

                            if (coin.Type != CoinType.Iced)
                            {
                                continue;
                            }

                            var countdown = coin.IcedCountdown - 1;
                            _slots[slotIndex][coinIndex] = countdown == 0
                                ? coin.AsStandard()
                                : new SolverCoin(coin.Tier, CoinType.Iced, coin.GroupId, countdown);
                        }
                    }
                }

                private bool HasMergeMove()
                {
                    for (var sourceIndex = 0; sourceIndex < _slots.Count; sourceIndex++)
                    {
                        for (var targetIndex = 0; targetIndex < _slots.Count; targetIndex++)
                        {
                            if (!CanMove(sourceIndex, targetIndex, -1, -1))
                            {
                                continue;
                            }

                            var source = _slots[sourceIndex];
                            var target = _slots[targetIndex];
                            var transferCount = Mathf.Min(GetTopStackCount(source), _slotCapacity - target.Count);

                            if (target.Count > 0 &&
                                target[target.Count - 1].Tier == source[source.Count - 1].Tier &&
                                GetMergeableTopCount(target) + transferCount == MergeStackSize)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                private bool HasAvailableMove()
                {
                    for (var sourceIndex = 0; sourceIndex < _slots.Count; sourceIndex++)
                    {
                        for (var targetIndex = 0; targetIndex < _slots.Count; targetIndex++)
                        {
                            if (CanMove(sourceIndex, targetIndex, -1, -1))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                private bool CanMove(int sourceIndex, int targetIndex, int previousSourceSlot, int previousTargetSlot)
                {
                    if (sourceIndex == targetIndex ||
                        !_unlockedSlots[sourceIndex] ||
                        !_unlockedSlots[targetIndex] ||
                        sourceIndex == previousTargetSlot && targetIndex == previousSourceSlot)
                    {
                        return false;
                    }

                    var source = _slots[sourceIndex];
                    var target = _slots[targetIndex];

                    if (source.Count == 0 || target.Count >= _slotCapacity)
                    {
                        return false;
                    }

                    var top = source[source.Count - 1];

                    if (top.Type == CoinType.Iced || top.Type == CoinType.Hidden)
                    {
                        return false;
                    }

                    var topStackCount = GetTopStackCount(source);

                    if (target.Count == 0 && topStackCount == source.Count)
                    {
                        return false;
                    }

                    if (top.Type == CoinType.Sticky && topStackCount > _slotCapacity - target.Count)
                    {
                        return false;
                    }

                    return top.Type == CoinType.Rainbow ||
                           target.Count == 0 ||
                           target[target.Count - 1].Tier == top.Tier;
                }

                private int GetEmptySlotCount()
                {
                    var count = 0;

                    for (var slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
                    {
                        if (_unlockedSlots[slotIndex] && _slots[slotIndex].Count == 0)
                        {
                            count++;
                        }
                    }

                    return count;
                }

                private int GetHighestTier()
                {
                    var highestTier = 0;

                    foreach (var slot in _slots)
                    {
                        foreach (var coin in slot)
                        {
                            highestTier = Mathf.Max(highestTier, coin.Tier);
                        }
                    }

                    return highestTier;
                }

                private List<List<SolverCoin>> CloneSlots()
                {
                    var result = new List<List<SolverCoin>>(_slots.Count);

                    foreach (var slot in _slots)
                    {
                        result.Add(new List<SolverCoin>(slot));
                    }

                    return result;
                }

                private static void ConvertStickyGroupWhenMergeReady(List<SolverCoin> target, List<SolverCoin> transferred)
                {
                    if (transferred.Count == 0 || transferred[0].Type != CoinType.Sticky || GetSameTierTopCount(target) != MergeStackSize)
                    {
                        return;
                    }

                    for (var coinIndex = target.Count - transferred.Count; coinIndex < target.Count; coinIndex++)
                    {
                        target[coinIndex] = target[coinIndex].AsStandard();
                    }
                }

                private static int GetTopStackCount(List<SolverCoin> slot)
                {
                    var top = slot[slot.Count - 1];

                    if (top.Type == CoinType.Rainbow)
                    {
                        return 1;
                    }

                    if (top.Type == CoinType.Sticky)
                    {
                        var count = 0;

                        for (var coinIndex = slot.Count - 1; coinIndex >= 0; coinIndex--)
                        {
                            var coin = slot[coinIndex];

                            if (coin.Type != CoinType.Sticky || coin.GroupId != top.GroupId)
                            {
                                break;
                            }

                            count++;
                        }

                        return count;
                    }

                    var stackCount = 0;

                    for (var coinIndex = slot.Count - 1; coinIndex >= 0; coinIndex--)
                    {
                        var coin = slot[coinIndex];

                        if (coin.Tier != top.Tier || coin.Type != CoinType.Standard)
                        {
                            break;
                        }

                        stackCount++;
                    }

                    return stackCount;
                }

                private static int GetMergeableTopCount(List<SolverCoin> slot)
                {
                    if (slot.Count == 0)
                    {
                        return 0;
                    }

                    var topTier = slot[slot.Count - 1].Tier;
                    var stackCount = 0;

                    for (var coinIndex = slot.Count - 1; coinIndex >= 0; coinIndex--)
                    {
                        var coin = slot[coinIndex];

                        if (coin.Tier != topTier || coin.Type != CoinType.Standard && coin.Type != CoinType.Sticky)
                        {
                            break;
                        }

                        stackCount++;
                    }

                    return stackCount;
                }

                private static int GetSameTierTopCount(List<SolverCoin> slot)
                {
                    if (slot.Count == 0)
                    {
                        return 0;
                    }

                    var tier = slot[slot.Count - 1].Tier;
                    var count = 0;

                    for (var coinIndex = slot.Count - 1; coinIndex >= 0 && slot[coinIndex].Tier == tier; coinIndex--)
                    {
                        count++;
                    }

                    return count;
                }

                private static void AppendHash(ref ulong hash, ulong value)
                {
                    const ulong fnvPrime = 1099511628211UL;
                    hash ^= value;
                    hash *= fnvPrime;
                }
            }
        }

    public sealed partial class AutoMergeDealStrategy
    {
        public Board.DealPlan CreatePlan(DealContext context)
        {
            var plan = DealStrategyUtility.CreateBasicPlan(context, false);

            if (plan == null)
            {
                return null;
            }

            var targetCount = Mathf.FloorToInt(_autoMergeValue) +
                              (Random.value <= _autoMergeValue % 1f ? 1 : 0);
            var mergedSlots = DealStrategyUtility.GetExistingAutoMergeSlots(plan, context);
            var attemptedCount = 0;

            for (var index = context.Slots.Length - 1;
                 index >= 0 && attemptedCount < MaximumCandidateCount && mergedSlots.Count < targetCount;
                 index--)
            {
                var candidate = context.Slots[index];

                if (!DealStrategyUtility.IsAutoMergeCandidate(candidate, context.MaxTier) || mergedSlots.Contains(candidate))
                {
                    continue;
                }

                attemptedCount++;

                if (DealStrategyUtility.TryCreateAutoMerge(plan, candidate, mergedSlots, context))
                {
                    mergedSlots.Add(candidate);
                }
            }

            return plan;
        }
    }

    internal static class DealStrategyUtility
    {
        public static Board.DealPlan CreateBasicPlan(DealContext context, bool easy)
        {
            var minTier = GetMinTier(context.MaxTier);
            var availableSlots = GetAvailableSlots(context);

            if (context.MaxTier <= minTier || availableSlots.Count == 0)
            {
                return null;
            }

            OrderSlots(availableSlots, context.MaxTier, easy);
            Slot skippedSlot = null;

            if (availableSlots.Count > 1)
            {
                var skippedIndex = easy ? 0 : Random.Range(0, availableSlots.Count);
                skippedSlot = availableSlots[skippedIndex];
                availableSlots.RemoveAt(skippedIndex);
            }

            var highestDealableTier = context.MaxTier == 4 ? context.MaxTier : context.MaxTier - 1;
            var tierCount = highestDealableTier - minTier + 1;
            var distributionCount = Mathf.Max(tierCount, availableSlots.Count);
            var stacks = new List<Board.DealStack>(distributionCount);

            for (var index = 0; index < distributionCount; index++)
            {
                var tier = minTier + index % tierCount;
                var slot = availableSlots[index % availableSlots.Count];
                var capacity = slot.AvailableCoinCapacity(context.SlotCapacity) - GetPlannedCoinCount(stacks, slot);
                var count = Mathf.Min(GetDealCount(tier, context.MaxTier), capacity);

                if (count > 0)
                {
                    stacks.Add(new Board.DealStack(tier, count, slot));
                }
            }

            return new Board.DealPlan(context.MaxTier, skippedSlot, stacks);
        }

        public static void ApplyHard(Board.DealPlan plan, DealContext context)
        {
            var highTierSlot = GetEligibleHighTierSlot(context.Slots, plan.MaxTier);
            var highIndex = GetTargetStackIndex(plan.Stacks, highTierSlot);

            if (highIndex < 0 || plan.Stacks.Count < 2)
            {
                return;
            }

            var selectedIndex = Random.Range(0, plan.Stacks.Count - 1);
            selectedIndex = selectedIndex >= highIndex ? selectedIndex + 1 : selectedIndex;
            var highStack = plan.Stacks[highIndex];
            var selectedStack = plan.Stacks[selectedIndex];

            if (!CanSwap(plan.Stacks, highStack, selectedStack, context.SlotCapacity))
            {
                return;
            }

            (highStack.TargetSlot, selectedStack.TargetSlot) = (selectedStack.TargetSlot, highStack.TargetSlot);
        }

        public static void ApplyEasy(Board.DealPlan plan, DealContext context)
        {
            var highTierSlot = GetEligibleHighTierSlot(context.Slots, plan.MaxTier);
            var stackIndex = GetTargetStackIndex(plan.Stacks, highTierSlot);

            if (plan.SkippedSlot == null || stackIndex < 0)
            {
                return;
            }

            var stack = plan.Stacks[stackIndex];

            if (stack.CoinCount <= plan.SkippedSlot.AvailableCoinCapacity(context.SlotCapacity))
            {
                stack.TargetSlot = plan.SkippedSlot;
            }
        }

        public static List<Slot> GetExistingAutoMergeSlots(Board.DealPlan plan, DealContext context)
        {
            var result = new List<Slot>();

            foreach (var slot in context.Slots)
            {
                if (IsAutoMergeCandidate(slot, context.MaxTier) &&
                    GetPlannedCoinCount(plan.Stacks, slot) == 10 - slot.CoinCount &&
                    ArePlannedCoinsOfTier(plan.Stacks, slot, slot.GetCoin(0).Tier))
                {
                    result.Add(slot);
                }
            }

            return result;
        }

        public static bool IsAutoMergeCandidate(Slot slot, int maxTier)
        {
            if (!slot.IsUnlocked || slot.CoinCount < 5 || slot.CoinCount >= 10 || slot.GetCoin(0).Tier >= maxTier - 2)
            {
                return false;
            }

            var tier = slot.GetCoin(0).Tier;

            for (var index = 0; index < slot.CoinCount; index++)
            {
                var coin = slot.GetCoin(index);

                if (coin.Type != CoinType.Standard || coin.Tier != tier)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryCreateAutoMerge(
            Board.DealPlan plan,
            Slot candidate,
            List<Slot> protectedSlots,
            DealContext context)
        {
            var requiredCount = 10 - candidate.CoinCount;
            var tier = candidate.GetCoin(0).Tier;

            foreach (var stack in plan.Stacks)
            {
                if (stack.TargetSlot == candidate || protectedSlots.Contains(stack.TargetSlot) ||
                    stack.Tier != tier || stack.CoinCount < requiredCount)
                {
                    continue;
                }

                var candidateIncoming = GetPlannedCoinCount(plan.Stacks, candidate);
                var sourceIncoming = GetPlannedCoinCount(plan.Stacks, stack.TargetSlot);

                if (sourceIncoming - requiredCount + candidateIncoming > stack.TargetSlot.AvailableCoinCapacity(context.SlotCapacity))
                {
                    continue;
                }

                foreach (var candidateStack in plan.Stacks)
                {
                    if (candidateStack.TargetSlot == candidate)
                    {
                        candidateStack.TargetSlot = stack.TargetSlot;
                    }
                }

                stack.CoinCount -= requiredCount;
                plan.Stacks.Add(new Board.DealStack(tier, requiredCount, candidate));
                plan.Stacks.RemoveAll(dealStack => dealStack.CoinCount == 0);
                return true;
            }

            return false;
        }

        private static List<Slot> GetAvailableSlots(DealContext context)
        {
            var slots = new List<Slot>();

            foreach (var slot in context.Slots)
            {
                if (slot.IsUnlocked && slot.AvailableCoinCapacity(context.SlotCapacity) > 0)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        private static void OrderSlots(List<Slot> slots, int maxTier, bool easy)
        {
            if (!easy)
            {
                Shuffle(slots);
                return;
            }

            var highTierSlots = new List<Slot>();

            for (var index = slots.Count - 1; index >= 0; index--)
            {
                if (slots[index].CoinCount > 0 && slots[index].GetCoin(slots[index].CoinCount - 1).Tier == maxTier)
                {
                    highTierSlots.Add(slots[index]);
                    slots.RemoveAt(index);
                }
            }

            highTierSlots.Reverse();
            Shuffle(slots);
            slots.AddRange(highTierSlots);
        }

        private static Slot GetEligibleHighTierSlot(Slot[] slots, int maxTier)
        {
            foreach (var slot in slots)
            {
                for (var index = slot.CoinCount - 1; index >= 0; index--)
                {
                    var coin = slot.GetCoin(index);

                    if (coin.Tier == maxTier && !HasBlockingCoinAbove(slot, index, maxTier))
                    {
                        return slot;
                    }
                }
            }

            return null;
        }

        private static bool HasBlockingCoinAbove(Slot slot, int highIndex, int maxTier)
        {
            for (var index = highIndex + 1; index < slot.CoinCount; index++)
            {
                var coin = slot.GetCoin(index);

                if (coin.Type == CoinType.Hidden || coin.Tier != maxTier)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanSwap(List<Board.DealStack> stacks, Board.DealStack first, Board.DealStack second, int capacity)
        {
            return GetPlannedCoinCount(stacks, first.TargetSlot) - first.CoinCount + second.CoinCount <= first.TargetSlot.AvailableCoinCapacity(capacity) &&
                   GetPlannedCoinCount(stacks, second.TargetSlot) - second.CoinCount + first.CoinCount <= second.TargetSlot.AvailableCoinCapacity(capacity);
        }

        private static int GetTargetStackIndex(List<Board.DealStack> stacks, Slot slot)
        {
            for (var index = 0; index < stacks.Count; index++)
            {
                if (stacks[index].TargetSlot == slot)
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool ArePlannedCoinsOfTier(List<Board.DealStack> stacks, Slot slot, int tier)
        {
            foreach (var stack in stacks)
            {
                if (stack.TargetSlot == slot && stack.Tier != tier)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetPlannedCoinCount(List<Board.DealStack> stacks, Slot slot)
        {
            var count = 0;

            foreach (var stack in stacks)
            {
                if (stack.TargetSlot == slot)
                {
                    count += stack.CoinCount;
                }
            }

            return count;
        }

        public static int GetMinTier(int maxTier)
        {
            if (maxTier < 12)
            {
                return 1;
            }

            var baseMin = 9 * ((maxTier - 6) / 9) + 1;
            var remainder = maxTier % 9;
            return baseMin + (remainder == 4 || remainder == 5 ? 1 : 0);
        }

        private static int GetDealCount(int tier, int maxTier)
        {
            if (maxTier == 4 && tier == maxTier)
            {
                return 1;
            }

            if (tier == maxTier - 1)
            {
                return Random.Range(1, 3);
            }

            return tier == maxTier - 2 ? Random.Range(3, 5) : Random.Range(3, 6);
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var randomIndex = Random.Range(0, index + 1);
                (items[index], items[randomIndex]) = (items[randomIndex], items[index]);
            }
        }
    }
}
