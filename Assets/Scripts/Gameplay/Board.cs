using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameConfig;
using General;
using SavedData;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class Board : MonoBehaviour
    {
        private const int MergeStackSize = 10;
        private const float HardDealHardness = 1f;
        private const float AutoMergeDealValue = 0.5f;
        private const int MaximumAutoMergeCandidates = 4;
        private const float ShuffleCoinDelay = 0.01f;
        private const float ShuffleScaleDuration = 0.2f;

        private enum AutoPlayDealMode
        {
            Normal,
            Easy,
            Basic,
            Hard,
            AutoMerge
        }

        private enum BoosterMode
        {
            None,
            Hammer,
            Hand
        }

        [SerializeField] private Slot[] slots;
        [SerializeField] private CoinBase coinPrefab;
        [SerializeField] private CoinSpriteConfig coinSpriteConfig;
        [SerializeField] private Transform selectedCoinParent;
        [SerializeField] private int slotCapacity = 10;
        [SerializeField] private AutoPlayDealMode autoPlayDealMode;
        private float selectionLiftOffset = 10f;
        private float selectionAnimationDuration = 0.2f;
        private float transferAnimationDuration = 0.35f;

        private readonly List<CoinBase> _selectedCoins = new List<CoinBase>();
        private readonly HashSet<int> _completedObstacleUnlockLevels = new HashSet<int>();
        private readonly ObstacleDealPlanner _obstacleDealPlanner = new ObstacleDealPlanner();
        private BoardState _boardState;
        private Slot _selectedSlot;
        private bool _isAnimating;
        private bool _usesEasyDeal;
        private Coroutine _autoPlayCoroutine;
        private int _currentMaxTier;
        private float _previousTimeScale;
        private bool _usesDynamicDifficultyDeal;
        private HardnessDealConfig _dynamicDifficultyDealConfig;
        private float _dynamicDifficultyValue;
        private BoosterMode _boosterMode;

        public event System.Action<IReadOnlyList<int>> CoinsCreated;
        public event System.Action<Slot> HammerTargetSelected;
        public event System.Action<int, int> HighestTierCoinsDestroyed;
        public event System.Action<BoardState> BoardStateChanged;
        public event System.Action<DealStrategyType> DealExecuted;

        public int HighestTier => GetHighestTier();

        public void StartAutoPlay()
        {
            if (_autoPlayCoroutine != null)
            {
                return;
            }

            if (_selectedSlot != null)
            {
                ReturnSelectedCoins();
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 20f;
            _autoPlayCoroutine = StartCoroutine(AutoPlay());
        }

        public void StopAutoPlay()
        {
            if (_autoPlayCoroutine == null)
            {
                return;
            }

            StopCoroutine(_autoPlayCoroutine);
            _autoPlayCoroutine = null;
            Time.timeScale = _previousTimeScale;
        }

        public void Initialize(BoardState boardState)
        {
            ResetSelection();
            _boardState = boardState;
            _currentMaxTier = 0;

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var slot = slots[slotIndex];
                var slotState = boardState.Slots[slotIndex];

                slot.Clicked -= OnSlotClicked;
                slot.Clicked += OnSlotClicked;
                slot.Initialize(slotState);

                foreach (var coinState in slotState.Coins)
                {
                    var coin = Instantiate(coinPrefab, slot.GetNextCoinParent());
                    coin.Initialize(coinState, coinSpriteConfig);
                }
            }

            UpdateRetiredCoins();
            BoardStateChanged?.Invoke(CreateBoardState());
        }

        private void OnDestroy()
        {
            StopAutoPlay();

            foreach (var slot in slots)
            {
                slot.Clicked -= OnSlotClicked;
            }
        }

        private void OnSlotClicked(Slot slot)
        {
            if (_isAnimating || !slot.IsUnlocked)
            {
                return;
            }

            if (_boosterMode == BoosterMode.Hammer)
            {
                SelectHammerTarget(slot);
                return;
            }

            if (_selectedSlot == null)
            {
                SelectTopStack(slot);
                return;
            }

            if (_selectedSlot == slot)
            {
                ReturnSelectedCoins();
                _boosterMode = BoosterMode.None;
                return;
            }

            if (slot.CoinCount >= slotCapacity || (!CanTransferTo(slot) && _boosterMode != BoosterMode.Hand))
            {
                return;
            }

            TransferSelectedCoins(slot);
        }

        public void ActivateHammerBooster()
        {
            if (_isAnimating)
            {
                return;
            }

            StopAutoPlay();
            ResetSelection();
            _boosterMode = BoosterMode.Hammer;
        }

        public void ActivateHandBooster()
        {
            if (_isAnimating)
            {
                return;
            }

            StopAutoPlay();
            ResetSelection();
            _boosterMode = BoosterMode.Hand;
        }

        private IEnumerator AutoPlay()
        {
            while (true)
            {
                while (_isAnimating)
                {
                    yield return null;
                }

                var moves = GetAvailableAutoPlayMoves();

                if (moves.Count == 0)
                {
                    DealForAutoPlay();

                    if (!CanContinuePlaying())
                    {
                        StopAutoPlay();
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(0.1f);
                    continue;
                }

                var move = moves[Random.Range(0, moves.Count)];
                OnSlotClicked(move.SourceSlot);

                while (_isAnimating)
                {
                    yield return null;
                }

                if (_selectedSlot == move.SourceSlot)
                {
                    OnSlotClicked(move.TargetSlot);
                }

                while (_isAnimating)
                {
                    yield return null;
                }
            }
        }

        private void DealForAutoPlay()
        {
            switch (autoPlayDealMode)
            {
                case AutoPlayDealMode.Normal:
                    Deal();
                    break;
                case AutoPlayDealMode.Easy:
                    DealEasy();
                    break;
                case AutoPlayDealMode.Basic:
                    DealBasic();
                    break;
                case AutoPlayDealMode.Hard:
                    DealHard();
                    break;
                case AutoPlayDealMode.AutoMerge:
                    DealAutoMerge();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        private void SelectTopStack(Slot slot)
        {
            if (slot.CoinCount == 0)
            {
                return;
            }

            var topCoin = slot.GetCoin(slot.CoinCount - 1);
            if (topCoin.Type == CoinType.Iced)
            {
                return;
            }

            _selectedSlot = slot;

            if (topCoin.Type == CoinType.Sticky)
            {
                AddStickyGroup(slot, topCoin);
                MoveSelectedCoinsToOverlay();
                AnimateSelectedCoins(Vector3.up * selectionLiftOffset, selectionAnimationDuration);
                return;
            }

            if (topCoin.Type == CoinType.Rainbow)
            {
                _selectedCoins.Add(topCoin);
                MoveSelectedCoinsToOverlay();
                AnimateSelectedCoins(Vector3.up * selectionLiftOffset, selectionAnimationDuration);
                return;
            }

            for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                var coin = slot.GetCoin(coinIndex);

                if (coin.Type != CoinType.Standard || coin.Tier != topCoin.Tier)
                {
                    break;
                }

                _selectedCoins.Add(coin);
            }

            _selectedCoins.Reverse();
            MoveSelectedCoinsToOverlay();
            AnimateSelectedCoins(Vector3.up * selectionLiftOffset, selectionAnimationDuration);
        }

        private bool CanTransferTo(Slot targetSlot)
        {
            if (_selectedCoins[0].Type == CoinType.Sticky)
            {
                return targetSlot.CoinCount + _selectedCoins.Count <= slotCapacity &&
                       (targetSlot.CoinCount == 0 ||
                        targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier == _selectedCoins[0].Tier);
            }

            return targetSlot.CoinCount < slotCapacity &&
                   (_selectedCoins[0].Type == CoinType.Rainbow ||
                   targetSlot.CoinCount == 0 ||
                   targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier == _selectedCoins[0].Tier);
        }

        private List<AutoPlayMove> GetAvailableAutoPlayMoves()
        {
            var moves = new List<AutoPlayMove>();

            foreach (var sourceSlot in slots)
            {
                if (!sourceSlot.IsUnlocked || sourceSlot.CoinCount == 0)
                {
                    continue;
                }

                var sourceTopTier = sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Tier;
                if (sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Type == CoinType.Iced)
                {
                    continue;
                }
                var movesEntireStack = GetTopStackCount(sourceSlot) == sourceSlot.CoinCount;

                foreach (var targetSlot in slots)
                {
                    if (sourceSlot == targetSlot || !targetSlot.IsUnlocked || targetSlot.CoinCount >= slotCapacity)
                    {
                        continue;
                    }

                    if (targetSlot.CoinCount == 0 && movesEntireStack)
                    {
                        continue;
                    }

                    if (sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Type == CoinType.Sticky && targetSlot.CoinCount == 0)
                    {
                        continue;
                    }

                    if (sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Type == CoinType.Rainbow ||
                        targetSlot.CoinCount == 0 ||
                        targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier == sourceTopTier)
                    {
                        moves.Add(new AutoPlayMove(sourceSlot, targetSlot));
                    }
                }
            }

            return moves;
        }

        private void ReturnSelectedCoins()
        {
            var currentPositions = new List<Vector3>(_selectedCoins.Count);

            foreach (var coin in _selectedCoins)
            {
                currentPositions.Add(coin.transform.position);
                coin.transform.SetParent(_selectedSlot.GetNextCoinParent(), true);
                coin.SetIgnoredByLayout(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_selectedSlot.GetNextCoinParent());

            var targetPositions = new List<Vector3>(_selectedCoins.Count);

            foreach (var coin in _selectedCoins)
            {
                targetPositions.Add(coin.transform.position);
            }

            for (var coinIndex = 0; coinIndex < _selectedCoins.Count; coinIndex++)
            {
                var coin = _selectedCoins[coinIndex];
                coin.SetIgnoredByLayout(true);
                coin.transform.position = currentPositions[coinIndex];
            }

            _isAnimating = true;
            var sequence = DOTween.Sequence();

            for (var coinIndex = 0; coinIndex < _selectedCoins.Count; coinIndex++)
            {
                sequence.Join(_selectedCoins[coinIndex].MoveTo(targetPositions[coinIndex], selectionAnimationDuration)
                    .SetEase(Ease.OutSine));
            }

            sequence.OnComplete(() =>
            {
                var targetSlot = _selectedSlot;
                ClearSelection();
                var upgradedTiers = TryMergeTargetStack(targetSlot);

                if (upgradedTiers.Count == 0)
                {
                    return;
                }

                UpdateRetiredCoins();
                BoardStateChanged?.Invoke(CreateBoardState());
                CoinsCreated?.Invoke(upgradedTiers);
            });
        }

        private void MoveSelectedCoinsToOverlay()
        {
            foreach (var coin in _selectedCoins)
            {
                coin.transform.SetParent(selectedCoinParent, true);
                coin.SetIgnoredByLayout(true);
            }
        }

        private void TransferSelectedCoins(Slot targetSlot)
        {
            var transferCount = Mathf.Min(_selectedCoins.Count, slotCapacity - targetSlot.CoinCount);
            var transferStartIndex = _selectedCoins.Count - transferCount;
            var transferredCoins = _selectedCoins.GetRange(transferStartIndex, transferCount);
            var remainingCoins = _selectedCoins.GetRange(0, transferStartIndex);
            var targetTopTier = targetSlot.CoinCount > 0 ? targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier : 0;
            var sourcePositions = new List<Vector3>(transferredCoins.Count);

            foreach (var coin in transferredCoins)
            {
                sourcePositions.Add(coin.transform.position);
                coin.transform.SetParent(targetSlot.GetNextCoinParent(), true);
                coin.SetIgnoredByLayout(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)targetSlot.GetNextCoinParent());

            var targetPositions = new List<Vector3>(transferredCoins.Count);

            foreach (var coin in transferredCoins)
            {
                targetPositions.Add(coin.transform.position);
            }

            for (var coinIndex = 0; coinIndex < transferredCoins.Count; coinIndex++)
            {
                var coin = transferredCoins[coinIndex];
                coin.transform.SetParent(selectedCoinParent, true);
                coin.SetIgnoredByLayout(true);
                coin.transform.position = sourcePositions[coinIndex];
            }

            var remainingCoinPositions = new List<Vector3>(remainingCoins.Count);

            foreach (var coin in remainingCoins)
            {
                coin.transform.SetParent(_selectedSlot.GetNextCoinParent(), true);
                coin.SetIgnoredByLayout(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_selectedSlot.GetNextCoinParent());

            foreach (var coin in remainingCoins)
            {
                remainingCoinPositions.Add(coin.transform.position);
                coin.transform.SetParent(selectedCoinParent, true);
                coin.SetIgnoredByLayout(true);
            }

            _isAnimating = true;
            var sequence = DOTween.Sequence();

            for (var coinIndex = 0; coinIndex < transferredCoins.Count; coinIndex++)
            {
                sequence.Join(transferredCoins[coinIndex].MoveTo(targetPositions[coinIndex], transferAnimationDuration)
                    .SetEase(Ease.InOutSine));
            }

            for (var coinIndex = 0; coinIndex < remainingCoins.Count; coinIndex++)
            {
                sequence.Join(remainingCoins[coinIndex].MoveTo(remainingCoinPositions[coinIndex], transferAnimationDuration)
                    .SetEase(Ease.InOutSine));
            }

            sequence.OnComplete(() =>
            {
                var createdTiers = new List<int>();

                foreach (var coin in transferredCoins)
                {
                    coin.transform.SetParent(targetSlot.GetNextCoinParent(), true);
                    coin.SetIgnoredByLayout(false);
                }

                foreach (var coin in remainingCoins)
                {
                    coin.transform.SetParent(_selectedSlot.GetNextCoinParent(), true);
                    coin.SetIgnoredByLayout(false);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)targetSlot.GetNextCoinParent());
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_selectedSlot.GetNextCoinParent());

                if (targetTopTier > 0)
                {
                    foreach (var coin in transferredCoins)
                    {
                        if (coin.Type == CoinType.Rainbow)
                        {
                            coin.TransformRainbow(targetTopTier, coinSpriteConfig);
                            createdTiers.Add(targetTopTier);
                        }
                    }
                }

                ConvertStickyGroupWhenMergeReady(targetSlot, transferredCoins);
                RevealTopHiddenCoins(targetSlot);
                RevealTopHiddenCoins(_selectedSlot);
                createdTiers.AddRange(TryMergeTargetStack(targetSlot));
                UpdateRetiredCoins();
                _selectedCoins.Clear();
                _selectedSlot = null;
                _boosterMode = BoosterMode.None;
                _isAnimating = false;
                BoardStateChanged?.Invoke(CreateBoardState());

                CoinsCreated?.Invoke(createdTiers);
            });
        }

        private void SelectHammerTarget(Slot slot)
        {
            if (slot.CoinCount == 0)
            {
                return;
            }

            _isAnimating = true;
            _boosterMode = BoosterMode.None;
            HammerTargetSelected?.Invoke(slot);
        }

        public void DestroySlotCoinsWithHammer(Slot slot)
        {
            var highestTier = GetHighestTier();
            var destroyedHighestTierCoinCount = 0;
            var coins = new List<CoinBase>(slot.CoinCount);

            for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
            {
                var coin = slot.GetCoin(coinIndex);
                destroyedHighestTierCoinCount += coin.Tier == highestTier ? 1 : 0;
                coins.Add(coin);
            }

            var sequence = DOTween.Sequence();

            foreach (var coin in coins)
            {
                sequence.Join(coin.transform.DOScale(Vector3.zero, ShuffleScaleDuration));
            }

            sequence.OnComplete(() =>
            {
                foreach (var coin in coins)
                {
                    coin.gameObject.SetActive(false);
                    Destroy(coin.gameObject);
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slot.GetNextCoinParent());
                UpdateRetiredCoins();
                BoardStateChanged?.Invoke(CreateBoardState());

                if (destroyedHighestTierCoinCount > 0)
                {
                    HighestTierCoinsDestroyed?.Invoke(highestTier, destroyedHighestTierCoinCount);
                }
            });
        }

        public void CompleteHammerAnimation()
        {
            _isAnimating = false;
        }

        private List<int> TryMergeTargetStack(Slot targetSlot)
        {
            if (GetMergeableTopCount(targetSlot) != MergeStackSize)
            {
                return new List<int>();
            }

            var mergedTier = targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier;
            var upgradedTier = mergedTier + 1;
            var upgradedCoinCount = upgradedTier > GetHighestTier() ? 1 : 2;

            for (var coinIndex = targetSlot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                var coin = targetSlot.GetCoin(coinIndex);
                coin.gameObject.SetActive(false);
                Destroy(coin.gameObject);
            }

            for (var coinIndex = 0; coinIndex < upgradedCoinCount; coinIndex++)
            {
                var upgradedCoin = Instantiate(coinPrefab, targetSlot.GetNextCoinParent());
                upgradedCoin.Initialize(new CoinState(upgradedTier, CoinType.Standard), coinSpriteConfig);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)targetSlot.GetNextCoinParent());
            ReduceIcedCountdowns();
            var upgradedTiers = new List<int>(upgradedCoinCount);

            for (var coinIndex = 0; coinIndex < upgradedCoinCount; coinIndex++)
            {
                upgradedTiers.Add(upgradedTier);
            }

            return upgradedTiers;
        }

        public void Deal()
        {
            ExecuteDealStrategy(GetDealStrategy());
        }

        public void DealEasy()
        {
            _usesDynamicDifficultyDeal = false;
            ExecuteDealStrategy(new EasyDealStrategy());
        }

        public void DealBasic()
        {
            _usesDynamicDifficultyDeal = false;
            ExecuteDealStrategy(new BasicDealStrategy());
        }

        public void DealHard()
        {
            _usesDynamicDifficultyDeal = false;
            ExecuteDealStrategy(new HardDealStrategy());
        }

        public void DealAutoMerge()
        {
            _usesDynamicDifficultyDeal = false;
            ExecuteDealStrategy(new AutoMergeDealStrategy());
        }

        public void DealSmart()
        {
            _usesDynamicDifficultyDeal = false;
            ExecuteDealStrategy(CreateSmartDealStrategy(.9f));
        }

        private void ExecuteDealStrategy(IDealStrategy strategy)
        {
            if (_isAnimating)
            {
                return;
            }

            var deal = strategy.CreatePlan(new DealContext(slots, slotCapacity, GetHighestTier()));

            if (deal == null)
            {
                return;
            }

            if (!strategy.IncludesObstaclePlan)
            {
                ApplyObstacleDeals(deal, strategy.Type == DealStrategyType.Easy);
            }

            if (_usesDynamicDifficultyDeal)
            {
                Debug.Log(
                    $"[DDA] D={_dynamicDifficultyValue:F3}, deal={_dynamicDifficultyDealConfig.DealType}, value={_dynamicDifficultyDealConfig.Value:F3}");
            }

            if (!string.IsNullOrEmpty(deal.ObstacleLog))
            {
                Debug.Log(deal.ObstacleLog);
            }

            DealExecuted?.Invoke(strategy.Type);
            ExecuteDeal(deal.Stacks);
        }

        private void ApplyObstacleDeals(DealPlan deal, bool isEasyDeal)
        {
            var remoteConfigModel = ServiceLocator.GetService<ISavedDataService>().GetModel<RemoteConfigModel>();
            var levelProgressService = ServiceLocator.GetService<ILevelProgressService>();
            var currentLevel = levelProgressService.CurrentLevel;
            _obstacleDealPlanner.Apply(
                deal,
                new DealContext(slots, slotCapacity, deal.MaxTier),
                currentLevel,
                remoteConfigModel,
                ServiceLocator.GetService<ISavedDataService>().GetModel<LevelProgressModel>(),
                out var obstacleLog);
            deal.ObstacleLog = obstacleLog;
        }

        private IDealStrategy GetDealStrategy()
        {
            var remoteConfigModel = ServiceLocator.GetService<ISavedDataService>().GetModel<RemoteConfigModel>();
            var dynamicDifficultyService = ServiceLocator.GetService<IDynamicDifficultyService>();

            if (dynamicDifficultyService.IsActive)
            {
                var dynamicConfig = dynamicDifficultyService.GetDealConfig();
                _usesDynamicDifficultyDeal = true;
                _dynamicDifficultyDealConfig = dynamicConfig;
                _dynamicDifficultyValue = dynamicDifficultyService.CurrentDifficulty;
                return dynamicConfig.DealType == HardnessDealType.AutoMerge
                    ? new AutoMergeDealStrategy(dynamicConfig.Value)
                    : CreateSmartDealStrategy(dynamicConfig.Value);
            }

            _usesDynamicDifficultyDeal = false;

            if (!remoteConfigModel.isHardnessConfigActive)
            {
                return _usesEasyDeal ? new EasyDealStrategy() : new BasicDealStrategy();
            }

            var level = ServiceLocator.GetService<ILevelProgressService>().CurrentLevel;
            var config = HardnessDealConfigResolver.Resolve(remoteConfigModel.hardnessConfig, level);

            switch (config.DealType)
            {
                case HardnessDealType.Hard:
                    return new HardDealStrategy(config.Value);
                case HardnessDealType.AutoMerge:
                    return new AutoMergeDealStrategy(config.Value);
                case HardnessDealType.Magic:
                    return new BasicDealStrategy();
                case HardnessDealType.Smart:
                    return CreateSmartDealStrategy(config.Value);
                default:
                    return _usesEasyDeal ? new EasyDealStrategy() : new BasicDealStrategy();
            }
        }

        private IDealStrategy CreateSmartDealStrategy(float selectionValue)
        {
            return new SmartDealStrategy(selectionValue, deal => ApplyObstacleDeals(deal, false));
        }

        private void Deal(bool usesEasyDeal)
        {
            if (_isAnimating)
            {
                return;
            }

            var maxTier = GetHighestTier();
            var minTier = GetMinTier(maxTier);
            var availableSlots = GetAvailableSlots();

            if (!CanDeal(maxTier, availableSlots))
            {
                return;
            }

            OrderDealSlots(availableSlots, maxTier, usesEasyDeal);

            if (availableSlots.Count > 1)
            {
                var skippedSlotIndex = usesEasyDeal ? 0 : Random.Range(0, availableSlots.Count);
                availableSlots.RemoveAt(skippedSlotIndex);
            }

            var highestDealableTier = maxTier == 4 ? maxTier : maxTier - 1;
            var dealableTierCount = highestDealableTier - minTier + 1;
            var distributionCount = Mathf.Max(dealableTierCount, availableSlots.Count);
            var stacks = CreateDealStacks(
                availableSlots,
                minTier,
                maxTier,
                dealableTierCount,
                distributionCount);

            ExecuteDeal(stacks);
        }

        private DealPlan CreateBasicDeal()
        {
            var maxTier = GetHighestTier();
            var minTier = GetMinTier(maxTier);
            var availableSlots = GetAvailableSlots();

            if (!CanDeal(maxTier, availableSlots))
            {
                return null;
            }

            OrderDealSlots(availableSlots, maxTier, false);
            Slot skippedSlot = null;

            if (availableSlots.Count > 1)
            {
                var skippedSlotIndex = Random.Range(0, availableSlots.Count);
                skippedSlot = availableSlots[skippedSlotIndex];
                availableSlots.RemoveAt(skippedSlotIndex);
            }

            var highestDealableTier = maxTier == 4 ? maxTier : maxTier - 1;
            var dealableTierCount = highestDealableTier - minTier + 1;
            var distributionCount = Mathf.Max(dealableTierCount, availableSlots.Count);
            var stacks = CreateDealStacks(
                availableSlots,
                minTier,
                maxTier,
                dealableTierCount,
                distributionCount);

            return new DealPlan(maxTier, skippedSlot, stacks);
        }

        private List<DealStack> CreateDealStacks(
            List<Slot> availableSlots,
            int minTier,
            int maxTier,
            int dealableTierCount,
            int distributionCount)
        {
            var stacks = new List<DealStack>(distributionCount);

            for (var distributionIndex = 0; distributionIndex < distributionCount; distributionIndex++)
            {
                var tier = minTier + distributionIndex % dealableTierCount;
                var slot = availableSlots[distributionIndex % availableSlots.Count];
                var plannedCoinCount = GetPlannedCoinCount(stacks, slot);
                var availableCoinCapacity = slot.AvailableCoinCapacity(slotCapacity) - plannedCoinCount;
                var coinCount = Mathf.Min(GetDealCount(tier, maxTier), availableCoinCapacity);

                if (coinCount > 0)
                {
                    stacks.Add(new DealStack(tier, coinCount, slot));
                }
            }

            return stacks;
        }

        private void ExecuteDeal(List<DealStack> stacks)
        {
            var initialCoinCounts = new Dictionary<Slot, int>();

            foreach (var slot in slots)
            {
                initialCoinCounts.Add(slot, slot.CoinCount);
            }

            foreach (var stack in stacks)
            {
                for (var coinIndex = 0; coinIndex < stack.CoinCount; coinIndex++)
                {
                    var coin = Instantiate(coinPrefab, stack.TargetSlot.GetNextCoinParent());
                    coin.Initialize(
                        new CoinState(stack.Tier, stack.Type, stack.IsHiddenGroup && coinIndex == 2, stack.ObstacleGroupId, stack.IcedCountdown),
                        coinSpriteConfig);

                    if (stack.Type == CoinType.Hidden)
                    {
                        coin.transform.SetSiblingIndex(initialCoinCounts[stack.TargetSlot] + coinIndex);
                    }
                }
            }

            var upgradedTiers = new List<int>();

            foreach (var slot in GetDealTargetSlots(stacks))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slot.GetNextCoinParent());
                RevealTopHiddenCoins(slot);
                upgradedTiers.AddRange(TryMergeTargetStack(slot));
            }

            UpdateRetiredCoins();
            BoardStateChanged?.Invoke(CreateBoardState());
            CoinsCreated?.Invoke(upgradedTiers);
        }

        private void RevealTopHiddenCoins(Slot slot)
        {
            while (slot.CoinCount > 0)
            {
                var topCoin = slot.GetCoin(slot.CoinCount - 1);

                if (topCoin.Type != CoinType.Hidden)
                {
                    return;
                }

                var groupId = topCoin.ObstacleGroupId;
                for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
                {
                    var coin = slot.GetCoin(coinIndex);
                    if (coin.Type == CoinType.Hidden && coin.ObstacleGroupId == groupId)
                    {
                        coin.RevealHidden();
                    }
                }
            }
        }

        private void AddStickyGroup(Slot slot, CoinBase topCoin)
        {
            for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                var coin = slot.GetCoin(coinIndex);
                if (coin.Type != CoinType.Sticky || coin.ObstacleGroupId != topCoin.ObstacleGroupId)
                {
                    break;
                }

                _selectedCoins.Add(coin);
            }

            _selectedCoins.Reverse();
        }

        private void ConvertStickyGroupWhenMergeReady(Slot targetSlot, List<CoinBase> transferredCoins)
        {
            if (transferredCoins.Count == 0 || transferredCoins[0].Type != CoinType.Sticky || GetSameTierTopCount(targetSlot) != MergeStackSize)
            {
                return;
            }

            foreach (var coin in transferredCoins)
            {
                coin.ConvertStickyToStandard();
            }
        }

        private static int GetSameTierTopCount(Slot slot)
        {
            if (slot.CoinCount == 0) return 0;
            var tier = slot.GetCoin(slot.CoinCount - 1).Tier;
            var count = 0;
            for (var index = slot.CoinCount - 1; index >= 0 && slot.GetCoin(index).Tier == tier; index--)
            {
                count++;
            }
            return count;
        }

        private void ReduceIcedCountdowns()
        {
            var groups = new Dictionary<int, List<CoinBase>>();
            foreach (var slot in slots)
            {
                for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
                {
                    var coin = slot.GetCoin(coinIndex);
                    if (coin.Type != CoinType.Iced)
                    {
                        continue;
                    }

                    if (!groups.TryGetValue(coin.ObstacleGroupId, out var group))
                    {
                        group = new List<CoinBase>();
                        groups.Add(coin.ObstacleGroupId, group);
                    }

                    group.Add(coin);
                }
            }

            foreach (var group in groups.Values)
            {
                var countdown = group[0].IcedCountdown - 1;
                foreach (var coin in group)
                {
                    if (countdown == 0) coin.BreakIce();
                    else coin.SetIcedCountdown(countdown);
                }
            }
        }

        private void TryApplyHardDeal(DealPlan deal)
        {
            var highTierSlot = GetEligibleHighTierSlot(deal.MaxTier);

            if (highTierSlot == null)
            {
                return;
            }

            var highTierStackIndex = GetStackIndexTargetingSlot(deal.Stacks, highTierSlot);

            if (highTierStackIndex < 0)
            {
                return;
            }

            var swappableStackIndices = new List<int>();

            for (var stackIndex = 0; stackIndex < deal.Stacks.Count; stackIndex++)
            {
                if (stackIndex != highTierStackIndex)
                {
                    swappableStackIndices.Add(stackIndex);
                }
            }

            if (swappableStackIndices.Count == 0)
            {
                return;
            }

            var selectedStackIndex = swappableStackIndices[Random.Range(0, swappableStackIndices.Count)];
            var highTierStack = deal.Stacks[highTierStackIndex];
            var selectedStack = deal.Stacks[selectedStackIndex];

            if (!CanSwapDealStacks(deal.Stacks, highTierStack, selectedStack))
            {
                return;
            }

            highTierStack.TargetSlot = selectedStack.TargetSlot;
            selectedStack.TargetSlot = highTierSlot;
        }

        private void TryApplyAutoMergeDeal(DealPlan deal)
        {
            var targetAutoMergeCount = GetTargetAutoMergeCount();
            var autoMergeSlots = GetExistingAutoMergeSlots(deal);

            if (autoMergeSlots.Count >= targetAutoMergeCount)
            {
                return;
            }

            var attemptedCandidateCount = 0;

            for (var slotIndex = slots.Length - 1;
                 slotIndex >= 0 && attemptedCandidateCount < MaximumAutoMergeCandidates;
                 slotIndex--)
            {
                var candidateSlot = slots[slotIndex];

                if (!IsAutoMergeCandidate(candidateSlot, deal.MaxTier) || autoMergeSlots.Contains(candidateSlot))
                {
                    continue;
                }

                attemptedCandidateCount++;

                if (!TryCreateAutoMerge(deal, candidateSlot, autoMergeSlots))
                {
                    continue;
                }

                autoMergeSlots.Add(candidateSlot);

                if (autoMergeSlots.Count >= targetAutoMergeCount)
                {
                    return;
                }
            }
        }

        private static int GetTargetAutoMergeCount()
        {
            var guaranteedAutoMergeCount = Mathf.FloorToInt(AutoMergeDealValue);
            var fractionalAutoMergeChance = AutoMergeDealValue - guaranteedAutoMergeCount;
            return guaranteedAutoMergeCount + (Random.value <= fractionalAutoMergeChance ? 1 : 0);
        }

        private List<Slot> GetExistingAutoMergeSlots(DealPlan deal)
        {
            var autoMergeSlots = new List<Slot>();

            foreach (var slot in slots)
            {
                if (!IsAutoMergeCandidate(slot, deal.MaxTier))
                {
                    continue;
                }

                var requiredCoinCount = MergeStackSize - slot.CoinCount;
                var plannedCoinCount = GetPlannedCoinCount(deal.Stacks, slot);

                if (plannedCoinCount != requiredCoinCount ||
                    !AreAllPlannedCoinsOfTier(deal.Stacks, slot, GetSlotCoinTier(slot)))
                {
                    continue;
                }

                autoMergeSlots.Add(slot);
            }

            return autoMergeSlots;
        }

        private bool TryCreateAutoMerge(DealPlan deal, Slot candidateSlot, List<Slot> autoMergeSlots)
        {
            var requiredCoinCount = MergeStackSize - candidateSlot.CoinCount;
            var candidateTier = GetSlotCoinTier(candidateSlot);

            foreach (var stack in deal.Stacks)
            {
                if (stack.TargetSlot == candidateSlot ||
                    autoMergeSlots.Contains(stack.TargetSlot) ||
                    stack.Tier != candidateTier ||
                    stack.CoinCount < requiredCoinCount)
                {
                    continue;
                }

                var incomingCandidateCoinCount = GetPlannedCoinCount(deal.Stacks, candidateSlot);
                var sourcePlannedCoinCount = GetPlannedCoinCount(deal.Stacks, stack.TargetSlot);
                var sourceCapacity = stack.TargetSlot.AvailableCoinCapacity(slotCapacity);

                if (sourcePlannedCoinCount - requiredCoinCount + incomingCandidateCoinCount > sourceCapacity)
                {
                    continue;
                }

                MoveCandidateStacksToSource(deal.Stacks, candidateSlot, stack.TargetSlot);
                stack.CoinCount -= requiredCoinCount;
                deal.Stacks.Add(new DealStack(candidateTier, requiredCoinCount, candidateSlot));
                RemoveEmptyDealStacks(deal.Stacks);
                return true;
            }

            return false;
        }

        private static void MoveCandidateStacksToSource(List<DealStack> stacks, Slot candidateSlot, Slot sourceSlot)
        {
            foreach (var stack in stacks)
            {
                if (stack.TargetSlot == candidateSlot)
                {
                    stack.TargetSlot = sourceSlot;
                }
            }
        }

        private static void RemoveEmptyDealStacks(List<DealStack> stacks)
        {
            for (var stackIndex = stacks.Count - 1; stackIndex >= 0; stackIndex--)
            {
                if (stacks[stackIndex].CoinCount == 0)
                {
                    stacks.RemoveAt(stackIndex);
                }
            }
        }

        private bool IsAutoMergeCandidate(Slot slot, int maxTier)
        {
            if (!slot.IsUnlocked || slot.CoinCount < 5 || slot.CoinCount >= MergeStackSize)
            {
                return false;
            }

            var tier = GetSlotCoinTier(slot);

            if (tier >= maxTier - 2)
            {
                return false;
            }

            for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
            {
                var coin = slot.GetCoin(coinIndex);

                if (coin.Type != CoinType.Standard || coin.Tier != tier)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreAllPlannedCoinsOfTier(List<DealStack> stacks, Slot slot, int tier)
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

        private static int GetSlotCoinTier(Slot slot)
        {
            return slot.GetCoin(0).Tier;
        }

        private void TryApplyEasyDeal(DealPlan deal)
        {
            if (deal.SkippedSlot == null)
            {
                return;
            }

            var highTierSlot = GetEligibleHighTierSlot(deal.MaxTier);

            if (highTierSlot == null)
            {
                return;
            }

            var highTierStackIndex = GetStackIndexTargetingSlot(deal.Stacks, highTierSlot);

            if (highTierStackIndex < 0)
            {
                return;
            }

            var highTierStack = deal.Stacks[highTierStackIndex];

            if (highTierStack.CoinCount > deal.SkippedSlot.AvailableCoinCapacity(slotCapacity))
            {
                return;
            }

            highTierStack.TargetSlot = deal.SkippedSlot;
        }

        private Slot GetEligibleHighTierSlot(int maxTier)
        {
            foreach (var slot in slots)
            {
                for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
                {
                    var coin = slot.GetCoin(coinIndex);

                    if (coin.Tier != maxTier)
                    {
                        continue;
                    }

                    if (HasBlockingCoinAbove(slot, coinIndex, maxTier))
                    {
                        break;
                    }

                    return slot;
                }
            }

            return null;
        }

        private static bool HasBlockingCoinAbove(Slot slot, int highTierCoinIndex, int maxTier)
        {
            for (var coinIndex = highTierCoinIndex + 1; coinIndex < slot.CoinCount; coinIndex++)
            {
                var coin = slot.GetCoin(coinIndex);

                if (coin.Type == CoinType.Hidden || coin.Tier != maxTier)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanSwapDealStacks(List<DealStack> stacks, DealStack firstStack, DealStack secondStack)
        {
            var firstTargetCoinCount = GetPlannedCoinCount(stacks, firstStack.TargetSlot);
            var secondTargetCoinCount = GetPlannedCoinCount(stacks, secondStack.TargetSlot);
            var firstTargetCapacity = firstStack.TargetSlot.AvailableCoinCapacity(slotCapacity);
            var secondTargetCapacity = secondStack.TargetSlot.AvailableCoinCapacity(slotCapacity);

            return firstTargetCoinCount - firstStack.CoinCount + secondStack.CoinCount <= firstTargetCapacity &&
                   secondTargetCoinCount - secondStack.CoinCount + firstStack.CoinCount <= secondTargetCapacity;
        }

        private static int GetStackIndexTargetingSlot(List<DealStack> stacks, Slot slot)
        {
            for (var stackIndex = 0; stackIndex < stacks.Count; stackIndex++)
            {
                if (stacks[stackIndex].TargetSlot == slot)
                {
                    return stackIndex;
                }
            }

            return -1;
        }

        private static int GetPlannedCoinCount(List<DealStack> stacks, Slot slot)
        {
            var coinCount = 0;

            foreach (var stack in stacks)
            {
                if (stack.TargetSlot == slot)
                {
                    coinCount += stack.CoinCount;
                }
            }

            return coinCount;
        }

        private static List<Slot> GetDealTargetSlots(List<DealStack> stacks)
        {
            var targetSlots = new List<Slot>();

            foreach (var stack in stacks)
            {
                if (!targetSlots.Contains(stack.TargetSlot))
                {
                    targetSlots.Add(stack.TargetSlot);
                }
            }

            return targetSlots;
        }

        public void SetUsesEasyDeal(bool usesEasyDeal)
        {
            _usesEasyDeal = usesEasyDeal;
        }

        public BoardState GetCurrentBoardState()
        {
            return CreateBoardState();
        }

        public void ShuffleBoard()
        {
            if (_isAnimating)
            {
                return;
            }

            StopAutoPlay();
            ResetSelection();

            var shuffledSlots = new List<List<CoinState>>(slots.Length);
            var obstacleGroups = new List<List<CoinState>>();
            var coinCountsByTier = new Dictionary<int, List<CoinState>>();

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                shuffledSlots.Add(new List<CoinState>());
                List<CoinState> obstacleGroup = null;
                var obstacleGroupId = -1;

                for (var coinIndex = 0; coinIndex < slots[slotIndex].CoinCount; coinIndex++)
                {
                    var coin = slots[slotIndex].GetCoin(coinIndex);
                    var coinState = new CoinState(coin.Tier, coin.Type, coin.IsHiddenGroupCenter, coin.ObstacleGroupId, coin.IcedCountdown);

                    if (coin.Type != CoinType.Standard)
                    {
                        if (coin.Type == CoinType.Rainbow || obstacleGroup == null || obstacleGroupId != coin.ObstacleGroupId)
                        {
                            obstacleGroup = new List<CoinState>();
                            obstacleGroups.Add(obstacleGroup);
                            obstacleGroupId = coin.ObstacleGroupId;
                        }

                        obstacleGroup.Add(coinState);
                        continue;
                    }

                    obstacleGroup = null;
                    obstacleGroupId = -1;

                    if (!coinCountsByTier.TryGetValue(coin.Tier, out var coins))
                    {
                        coins = new List<CoinState>();
                        coinCountsByTier.Add(coin.Tier, coins);
                    }

                    coins.Add(coinState);
                }
            }

            DistributeObstacleGroups(obstacleGroups, shuffledSlots);
            TrimLowestTierCoins(coinCountsByTier);
            DistributeCompleteStacks(coinCountsByTier, shuffledSlots);
            DistributeRemainingCoins(coinCountsByTier, shuffledSlots);
            AnimateShuffle(CreateShuffledBoardState(shuffledSlots));
        }

        private void AnimateShuffle(BoardState shuffledBoardState)
        {
            var coins = new List<CoinBase>();

            foreach (var slot in slots)
            {
                for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
                {
                    coins.Add(slot.GetCoin(coinIndex));
                }
            }

            _isAnimating = true;
            var scaleDownSequence = DOTween.Sequence();

            foreach (var coin in coins)
            {
                scaleDownSequence.Join(coin.transform.DOScale(Vector3.zero, ShuffleScaleDuration));
            }

            scaleDownSequence.OnComplete(() => MoveShuffledCoins(shuffledBoardState, coins));
        }

        private void MoveShuffledCoins(BoardState shuffledBoardState, List<CoinBase> coins)
        {
            var shuffledCoins = new List<ShuffleCoin>();

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                foreach (var coinState in shuffledBoardState.Slots[slotIndex].Coins)
                {
                    var coin = GetMatchingCoin(coins, coinState);
                    coins.Remove(coin);
                    var sourcePosition = coin.transform.position;
                    coin.transform.SetParent(slots[slotIndex].GetNextCoinParent(), true);
                    coin.SetIgnoredByLayout(false);
                    shuffledCoins.Add(new ShuffleCoin(coin, sourcePosition));
                }
            }

            foreach (var coin in coins)
            {
                Destroy(coin.gameObject);
            }

            foreach (var slot in slots)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slot.GetNextCoinParent());
            }

            var sequence = DOTween.Sequence();

            for (var coinIndex = 0; coinIndex < shuffledCoins.Count; coinIndex++)
            {
                var shuffledCoin = shuffledCoins[coinIndex];
                var targetPosition = shuffledCoin.Coin.transform.position;
                shuffledCoin.Coin.SetIgnoredByLayout(true);
                shuffledCoin.Coin.transform.position = shuffledCoin.SourcePosition;
                var startTime = coinIndex * ShuffleCoinDelay;
                sequence.Insert(startTime, shuffledCoin.Coin.transform.DOScale(Vector3.one, ShuffleScaleDuration));
                sequence.Insert(startTime, shuffledCoin.Coin.MoveTo(targetPosition, ShuffleScaleDuration));
            }

            sequence.OnComplete(() =>
            {
                foreach (var shuffledCoin in shuffledCoins)
                {
                    shuffledCoin.Coin.SetIgnoredByLayout(false);
                }

                UpdateRetiredCoins();
                _isAnimating = false;
                BoardStateChanged?.Invoke(CreateBoardState());
            });
        }

        private static CoinBase GetMatchingCoin(List<CoinBase> coins, CoinState coinState)
        {
            foreach (var coin in coins)
            {
                if (coin.Tier == coinState.Tier &&
                    coin.Type == coinState.Type &&
                    coin.IsHiddenGroupCenter == coinState.IsHiddenGroupCenter &&
                    coin.ObstacleGroupId == coinState.ObstacleGroupId &&
                    coin.IcedCountdown == coinState.IcedCountdown)
                {
                    return coin;
                }
            }

            throw new System.InvalidOperationException("No matching coin found while shuffling the board.");
        }

        private void DistributeObstacleGroups(
            List<List<CoinState>> obstacleGroups,
            List<List<CoinState>> shuffledSlots)
        {
            Shuffle(obstacleGroups);

            foreach (var obstacleGroup in obstacleGroups)
            {
                var slotIndices = GetUnlockedSlotIndices();
                Shuffle(slotIndices);

                foreach (var slotIndex in slotIndices)
                {
                    if (shuffledSlots[slotIndex].Count + obstacleGroup.Count > slotCapacity)
                    {
                        continue;
                    }

                    shuffledSlots[slotIndex].AddRange(obstacleGroup);
                    break;
                }
            }
        }

        private static void TrimLowestTierCoins(Dictionary<int, List<CoinState>> coinCountsByTier)
        {
            var lowestTiers = new List<int>(coinCountsByTier.Keys);
            lowestTiers.Sort();

            var tierCount = Mathf.Min(2, lowestTiers.Count);

            for (var tierIndex = 0; tierIndex < tierCount; tierIndex++)
            {
                var coins = coinCountsByTier[lowestTiers[tierIndex]];

                if (coins.Count > MergeStackSize)
                {
                    coins.RemoveRange(MergeStackSize, coins.Count - MergeStackSize);
                }
            }
        }

        private void DistributeCompleteStacks(
            Dictionary<int, List<CoinState>> coinCountsByTier,
            List<List<CoinState>> shuffledSlots)
        {
            var tiers = new List<int>(coinCountsByTier.Keys);
            tiers.Sort((firstTier, secondTier) =>
            {
                var countComparison = coinCountsByTier[secondTier].Count.CompareTo(coinCountsByTier[firstTier].Count);
                return countComparison != 0 ? countComparison : firstTier.CompareTo(secondTier);
            });

            foreach (var tier in tiers)
            {
                var coins = coinCountsByTier[tier];

                while (coins.Count >= MergeStackSize)
                {
                    var slotIndex = GetMostAvailableSlotIndex(shuffledSlots, MergeStackSize);

                    if (slotIndex < 0)
                    {
                        return;
                    }

                    shuffledSlots[slotIndex].AddRange(coins.GetRange(0, MergeStackSize));
                    coins.RemoveRange(0, MergeStackSize);
                }
            }
        }

        private void DistributeRemainingCoins(
            Dictionary<int, List<CoinState>> coinCountsByTier,
            List<List<CoinState>> shuffledSlots)
        {
            var tiers = new List<int>(coinCountsByTier.Keys);
            tiers.Sort((firstTier, secondTier) => secondTier.CompareTo(firstTier));

            foreach (var tier in tiers)
            {
                foreach (var coin in coinCountsByTier[tier])
                {
                    var slotIndex = GetBestSlotIndex(coin, shuffledSlots);

                    if (slotIndex >= 0)
                    {
                        shuffledSlots[slotIndex].Add(coin);
                    }
                }
            }
        }

        private List<int> GetUnlockedSlotIndices()
        {
            var slotIndices = new List<int>();

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                if (slots[slotIndex].IsUnlocked)
                {
                    slotIndices.Add(slotIndex);
                }
            }

            return slotIndices;
        }

        private int GetMostAvailableSlotIndex(List<List<CoinState>> shuffledSlots, int requiredCapacity)
        {
            var selectedSlotIndex = -1;
            var largestAvailableCapacity = -1;

            foreach (var slotIndex in GetUnlockedSlotIndices())
            {
                var availableCapacity = slotCapacity - shuffledSlots[slotIndex].Count;

                if (availableCapacity < requiredCapacity || availableCapacity <= largestAvailableCapacity)
                {
                    continue;
                }

                largestAvailableCapacity = availableCapacity;
                selectedSlotIndex = slotIndex;
            }

            return selectedSlotIndex;
        }

        private int GetBestSlotIndex(CoinState coin, List<List<CoinState>> shuffledSlots)
        {
            var selectedSlotIndex = -1;
            var bestScore = int.MaxValue;

            foreach (var slotIndex in GetUnlockedSlotIndices())
            {
                var coins = shuffledSlots[slotIndex];

                if (coins.Count >= slotCapacity)
                {
                    continue;
                }

                var score = GetSlotScore(coin, coins);

                if (score < bestScore)
                {
                    bestScore = score;
                    selectedSlotIndex = slotIndex;
                }
            }

            return selectedSlotIndex;
        }

        private static int GetSlotScore(CoinState coin, List<CoinState> slotCoins)
        {
            var hasMatchingTier = false;
            var distinctTierCount = 0;
            var tiers = new HashSet<int>();

            foreach (var slotCoin in slotCoins)
            {
                hasMatchingTier |= slotCoin.Tier == coin.Tier;

                if (tiers.Add(slotCoin.Tier))
                {
                    distinctTierCount++;
                }
            }

            if (hasMatchingTier)
            {
                return 0;
            }

            if (slotCoins.Count == 0)
            {
                return 1;
            }

            return distinctTierCount == 1 ? 2 : 3;
        }

        private BoardState CreateShuffledBoardState(List<List<CoinState>> shuffledSlots)
        {
            var slotStates = new List<SlotState>(slots.Length);

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var slotState = _boardState.Slots[slotIndex];
                slotStates.Add(new SlotState(
                    slotState.UnlockCoinCost,
                    slotState.AutomaticUnlockLevel,
                    slotState.Type,
                    shuffledSlots[slotIndex]));
            }

            return new BoardState(_boardState.BoardIndex, slotStates);
        }

        public bool HasAvailableMoves()
        {
            foreach (var sourceSlot in slots)
            {
                if (!sourceSlot.IsUnlocked || sourceSlot.CoinCount == 0)
                {
                    continue;
                }

                var sourceTopTier = sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Tier;
                if (sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Type == CoinType.Iced)
                {
                    continue;
                }

                foreach (var targetSlot in slots)
                {
                    if (sourceSlot == targetSlot || !targetSlot.IsUnlocked || targetSlot.CoinCount >= slotCapacity)
                    {
                        continue;
                    }

                    if (sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Type != CoinType.Sticky && (targetSlot.CoinCount == 0 ||
                        targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier == sourceTopTier)
                    || sourceSlot.GetCoin(sourceSlot.CoinCount - 1).Type == CoinType.Sticky && targetSlot.CoinCount > 0 && targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier == sourceTopTier)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool CanContinuePlaying()
        {
            return HasAvailableMoves() || CanDeal();
        }

        public void UnlockSlotsForLevel(int level)
        {
            var hasUnlockedSlot = false;

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var slotState = _boardState.Slots[slotIndex];

                if (!slots[slotIndex].IsUnlocked &&
                    slotState.AutomaticUnlockLevel > 0 &&
                    slotState.AutomaticUnlockLevel <= level)
                {
                    slots[slotIndex].Unlock();
                    UpdateSlotType(slotIndex, SlotType.Unlocked, slotState.UnlockCoinCost);
                    hasUnlockedSlot = true;
                }
            }

            if (hasUnlockedSlot)
            {
                SetNextLockedSlotUnlockable();
                BoardStateChanged?.Invoke(CreateBoardState());
            }
        }

        private void SetNextLockedSlotUnlockable()
        {
            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var slotState = _boardState.Slots[slotIndex];

                if (slotState.Type != SlotType.Locked)
                {
                    continue;
                }

                var unlockCoinCost = GetUnlockCoinCost();
                slots[slotIndex].SetUnlockable(unlockCoinCost, slotState.AutomaticUnlockLevel);
                UpdateSlotType(slotIndex, SlotType.Unlockable, unlockCoinCost);
                return;
            }
        }

        private int GetUnlockCoinCost()
        {
            foreach (var slotState in _boardState.Slots)
            {
                if (slotState.UnlockCoinCost > 0)
                {
                    return slotState.UnlockCoinCost;
                }
            }

            return 0;
        }

        private void UpdateSlotType(int slotIndex, SlotType slotType, int unlockCoinCost)
        {
            var slotStates = new List<SlotState>(_boardState.Slots);
            var slotState = slotStates[slotIndex];
            slotStates[slotIndex] = new SlotState(
                unlockCoinCost,
                slotState.AutomaticUnlockLevel,
                slotType,
                slotState.Coins);
            _boardState = new BoardState(_boardState.BoardIndex, slotStates);
        }

        private BoardState CreateBoardState()
        {
            var slotStates = new List<SlotState>(slots.Length);

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var coins = new List<CoinState>(slots[slotIndex].CoinCount);

                for (var coinIndex = 0; coinIndex < slots[slotIndex].CoinCount; coinIndex++)
                {
                    var coin = slots[slotIndex].GetCoin(coinIndex);

                    if (!coin.gameObject.activeSelf)
                    {
                        continue;
                    }

                    coins.Add(new CoinState(coin.Tier, coin.Type, coin.IsHiddenGroupCenter, coin.ObstacleGroupId, coin.IcedCountdown));
                }

                var originalSlotState = _boardState.Slots[slotIndex];
                slotStates.Add(new SlotState(
                    originalSlotState.UnlockCoinCost,
                    originalSlotState.AutomaticUnlockLevel,
                    originalSlotState.Type,
                    coins));
            }

            return new BoardState(_boardState.BoardIndex, slotStates);
        }

        private int GetTopStackCount(Slot slot)
        {
            if (slot.CoinCount == 0)
            {
                return 0;
            }

            var topTier = slot.GetCoin(slot.CoinCount - 1).Tier;

            if (slot.GetCoin(slot.CoinCount - 1).Type == CoinType.Rainbow)
            {
                return 1;
            }

            var stackCount = 0;

            for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                if (slot.GetCoin(coinIndex).Tier != topTier || slot.GetCoin(coinIndex).Type != CoinType.Standard)
                {
                    break;
                }

                stackCount++;
            }

            return stackCount;
        }

        private static int GetMergeableTopCount(Slot slot)
        {
            if (slot.CoinCount == 0)
            {
                return 0;
            }

            var topTier = slot.GetCoin(slot.CoinCount - 1).Tier;
            var stackCount = 0;

            for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                var coin = slot.GetCoin(coinIndex);

                if (coin.Tier != topTier || (coin.Type != CoinType.Standard && coin.Type != CoinType.Sticky))
                {
                    break;
                }

                stackCount++;
            }

            return stackCount;
        }

        private int GetHighestTier()
        {
            var highestTier = 0;

            foreach (var slot in slots)
            {
                for (var coinIndex = 0; coinIndex < slot.CoinCount; coinIndex++)
                {
                    var coin = slot.GetCoin(coinIndex);

                    if (coin.gameObject.activeSelf)
                    {
                        highestTier = Mathf.Max(highestTier, coin.Tier);
                    }
                }
            }

            return highestTier;
        }

        private void UpdateRetiredCoins()
        {
            var maxTier = GetHighestTier();

            if (maxTier == _currentMaxTier)
            {
                return;
            }

            _currentMaxTier = maxTier;
            var minTier = GetMinTier(maxTier);

            foreach (var slot in slots)
            {
                for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
                {
                    var coin = slot.GetCoin(coinIndex);

                    if (coin.Tier < minTier)
                    {
                        coin.gameObject.SetActive(false);
                        Destroy(coin.gameObject);
                    }
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slot.GetNextCoinParent());
            }
        }

        private List<Slot> GetAvailableSlots()
        {
            var availableSlots = new List<Slot>();

            foreach (var slot in slots)
            {
                if (slot.IsUnlocked && slot.AvailableCoinCapacity(slotCapacity) > 0)
                {
                    availableSlots.Add(slot);
                }
            }

            return availableSlots;
        }

        private bool CanDeal()
        {
            var maxTier = GetHighestTier();
            return CanDeal(maxTier, GetAvailableSlots());
        }

        private static bool CanDeal(int maxTier, List<Slot> availableSlots)
        {
            return maxTier > GetMinTier(maxTier) && availableSlots.Count > 0;
        }

        private void OrderDealSlots(List<Slot> availableSlots, int maxTier, bool usesEasyDeal)
        {
            if (!usesEasyDeal)
            {
                Shuffle(availableSlots);
                return;
            }

            var highTierSlots = new List<Slot>();

            for (var slotIndex = availableSlots.Count - 1; slotIndex >= 0; slotIndex--)
            {
                var slot = availableSlots[slotIndex];

                if (slot.CoinCount == 0 || slot.GetCoin(slot.CoinCount - 1).Tier != maxTier)
                {
                    continue;
                }

                highTierSlots.Add(slot);
                availableSlots.RemoveAt(slotIndex);
            }

            highTierSlots.Reverse();
            Shuffle(availableSlots);
            availableSlots.AddRange(highTierSlots);
        }

        private static int GetMinTier(int maxTier)
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

            if (tier == maxTier - 2)
            {
                return Random.Range(3, 5);
            }

            return Random.Range(3, 6);
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (var itemIndex = items.Count - 1; itemIndex > 0; itemIndex--)
            {
                var randomIndex = Random.Range(0, itemIndex + 1);
                (items[itemIndex], items[randomIndex]) = (items[randomIndex], items[itemIndex]);
            }
        }

        private void AnimateSelectedCoins(Vector3 offset, float duration, TweenCallback onComplete = null)
        {
            _isAnimating = true;
            var sequence = DOTween.Sequence();

            foreach (var coin in _selectedCoins)
            {
                coin.SetIgnoredByLayout(true);
                sequence.Join(coin.MoveTo(coin.transform.position + offset, duration).SetEase(Ease.OutSine));
            }

            sequence.OnComplete(() =>
            {
                _isAnimating = false;
                onComplete?.Invoke();
            });
        }

        private void ClearSelection()
        {
            foreach (var coin in _selectedCoins)
            {
                coin.transform.SetParent(_selectedSlot.GetNextCoinParent(), true);
                coin.SetIgnoredByLayout(false);
            }

            if (_selectedSlot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_selectedSlot.GetNextCoinParent());
            }

            _selectedCoins.Clear();
            _selectedSlot = null;
            _isAnimating = false;
        }

        private void ResetSelection()
        {
            if (_selectedSlot != null)
            {
                ClearSelection();
            }
        }

        private readonly struct AutoPlayMove
        {
            public Slot SourceSlot { get; }
            public Slot TargetSlot { get; }

            public AutoPlayMove(Slot sourceSlot, Slot targetSlot)
            {
                SourceSlot = sourceSlot;
                TargetSlot = targetSlot;
            }
        }

        private readonly struct ShuffleCoin
        {
            public CoinBase Coin { get; }
            public Vector3 SourcePosition { get; }

            public ShuffleCoin(CoinBase coin, Vector3 sourcePosition)
            {
                Coin = coin;
                SourcePosition = sourcePosition;
            }
        }

        public sealed class DealPlan
        {
            public int MaxTier { get; }
            public Slot SkippedSlot { get; }
            public List<DealStack> Stacks { get; }
            public string ObstacleLog { get; set; }

            public DealPlan(int maxTier, Slot skippedSlot, List<DealStack> stacks)
            {
                MaxTier = maxTier;
                SkippedSlot = skippedSlot;
                Stacks = stacks;
            }
        }

        public sealed class DealStack
        {
            public int Tier { get; }
            public int CoinCount { get; set; }
            public Slot TargetSlot { get; set; }
            public CoinType Type { get; }
            public bool IsHiddenGroup { get; }
            public int ObstacleGroupId { get; }
            public int IcedCountdown { get; }

            public DealStack(
                int tier,
                int coinCount,
                Slot targetSlot,
                CoinType type = CoinType.Standard,
                bool isHiddenGroup = false,
                int obstacleGroupId = 0,
                int icedCountdown = 0)
            {
                Tier = tier;
                CoinCount = coinCount;
                TargetSlot = targetSlot;
                Type = type;
                IsHiddenGroup = isHiddenGroup;
                ObstacleGroupId = obstacleGroupId;
                IcedCountdown = icedCountdown;
            }
        }
    }
}
