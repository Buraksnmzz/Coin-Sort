using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class Board : MonoBehaviour
    {
        private const int MergeStackSize = 10;

        [SerializeField] private Slot[] slots;
        [SerializeField] private CoinBase coinPrefab;
        [SerializeField] private CoinSpriteConfig coinSpriteConfig;
        [SerializeField] private Transform selectedCoinParent;
        [SerializeField] private int slotCapacity = 10;
        private float selectionLiftOffset = 10f;
        private float selectionAnimationDuration = 0.2f;
        private float transferAnimationDuration = 0.35f;

        private readonly List<CoinBase> _selectedCoins = new List<CoinBase>();
        private BoardState _boardState;
        private Slot _selectedSlot;
        private bool _isAnimating;
        private Coroutine _autoPlayCoroutine;
        private int _currentMaxTier;
        private float _previousTimeScale;

        public event System.Action<IReadOnlyList<int>> CoinsCreated;
        public event System.Action<BoardState> BoardStateChanged;

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
            Time.timeScale = 2f;
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

            if (_selectedSlot == null)
            {
                SelectTopStack(slot);
                return;
            }

            if (_selectedSlot == slot)
            {
                ReturnSelectedCoins();
                return;
            }

            if (!CanTransferTo(slot))
            {
                return;
            }

            TransferSelectedCoins(slot);
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
                    Deal();
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

        private void SelectTopStack(Slot slot)
        {
            if (slot.CoinCount == 0)
            {
                return;
            }

            var topCoin = slot.GetCoin(slot.CoinCount - 1);
            _selectedSlot = slot;

            for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                var coin = slot.GetCoin(coinIndex);

                if (coin.Tier != topCoin.Tier)
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
            if (targetSlot.CoinCount >= slotCapacity)
            {
                return false;
            }

            return targetSlot.CoinCount == 0 ||
                   targetSlot.GetCoin(targetSlot.CoinCount - 1).Tier == _selectedCoins[0].Tier;
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

                    if (targetSlot.CoinCount == 0 ||
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

            sequence.OnComplete(ClearSelection);
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
                var upgradedTiers = TryMergeTargetStack(targetSlot);
                UpdateRetiredCoins();
                _selectedCoins.Clear();
                _selectedSlot = null;
                _isAnimating = false;
                BoardStateChanged?.Invoke(CreateBoardState());

                CoinsCreated?.Invoke(upgradedTiers);
            });
        }

        private List<int> TryMergeTargetStack(Slot targetSlot)
        {
            if (GetTopStackCount(targetSlot) != MergeStackSize)
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
            var upgradedTiers = new List<int>(upgradedCoinCount);

            for (var coinIndex = 0; coinIndex < upgradedCoinCount; coinIndex++)
            {
                upgradedTiers.Add(upgradedTier);
            }

            return upgradedTiers;
        }

        public void Deal()
        {
            if (_isAnimating)
            {
                return;
            }

            var maxTier = GetHighestTier();
            var minTier = GetMinTier(maxTier);
            var availableSlots = GetAvailableSlots();

            if (maxTier <= minTier || availableSlots.Count <= 1)
            {
                return;
            }

            Shuffle(availableSlots);
            availableSlots.RemoveAt(Random.Range(0, availableSlots.Count));

            var dealableTierCount = maxTier - minTier;
            var distributionCount = Mathf.Max(dealableTierCount, availableSlots.Count);

            for (var distributionIndex = 0; distributionIndex < distributionCount; distributionIndex++)
            {
                var tier = minTier + distributionIndex % dealableTierCount;
                var slot = availableSlots[distributionIndex % availableSlots.Count];
                var coinCount = Mathf.Min(GetDealCount(tier, maxTier), slot.AvailableCoinCapacity(slotCapacity));

                for (var coinIndex = 0; coinIndex < coinCount; coinIndex++)
                {
                    var coin = Instantiate(coinPrefab, slot.GetNextCoinParent());
                    coin.Initialize(new CoinState(tier, CoinType.Standard), coinSpriteConfig);
                }
            }

            var upgradedTiers = new List<int>();

            foreach (var slot in availableSlots)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)slot.GetNextCoinParent());
                upgradedTiers.AddRange(TryMergeTargetStack(slot));
            }

            UpdateRetiredCoins();
            BoardStateChanged?.Invoke(CreateBoardState());
            CoinsCreated?.Invoke(upgradedTiers);
        }

        public BoardState GetCurrentBoardState()
        {
            return CreateBoardState();
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
                    hasUnlockedSlot = true;
                }
            }

            if (hasUnlockedSlot)
            {
                BoardStateChanged?.Invoke(CreateBoardState());
            }
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

                    coins.Add(new CoinState(coin.Tier, coin.Type));
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
            var stackCount = 0;

            for (var coinIndex = slot.CoinCount - 1; coinIndex >= 0; coinIndex--)
            {
                if (slot.GetCoin(coinIndex).Tier != topTier)
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
    }
}
