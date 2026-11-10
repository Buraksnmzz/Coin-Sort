using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class GameplayView : BaseView
    {
        [SerializeField] private Board board;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image levelProgressFillImage;
        [SerializeField] private Image hammerIconImage;
        [SerializeField] private Image fanIconImage;
        [SerializeField] private Button nextBoardButton;
        [SerializeField] private Button previousBoardButton;
        [SerializeField] private Button dealButton;
        [SerializeField] private Button fanBoosterButton;
        [SerializeField] private Button hammerBoosterButton;
        [SerializeField] private Button handBoosterButton;
        [SerializeField] private Button autoPlayButton;
        [SerializeField] private Button stopAutoPlayButton;
        [SerializeField] private Button debugEasyDealButton;
        [SerializeField] private Button debugBasicDealButton;
        [SerializeField] private Button debugHardDealButton;
        [SerializeField] private Button debugAutoMergeDealButton;
        [SerializeField] private Button debugSmartDealButton;
        [SerializeField] private TextMeshProUGUI debugDealtTypeText;

        public event Action NextBoardButtonClicked;
        public event Action PreviousBoardButtonClicked;
        public event Action AutoPlayButtonClicked;
        public event Action StopAutoPlayButtonClicked;
        public event Action<IReadOnlyList<int>> CoinsCreated;
        public event Action<int, int> HighestTierCoinsDestroyed;
        public event Action<BoardState> BoardStateChanged;

        private void Start()
        {
            hammerIconImage.transform.localScale = Vector3.zero;
            hammerIconImage.transform.localRotation = Quaternion.identity;
            nextBoardButton.onClick.AddListener(OnNextBoardButtonClicked);
            previousBoardButton.onClick.AddListener(OnPreviousBoardButtonClicked);
            dealButton.onClick.AddListener(OnDealButtonClicked);
            autoPlayButton.onClick.AddListener(OnAutoPlayButtonClicked);
            stopAutoPlayButton.onClick.AddListener(OnStopAutoPlayButtonClicked);
            debugEasyDealButton.onClick.AddListener(OnDebugEasyDealButtonClicked);
            debugBasicDealButton.onClick.AddListener(OnDebugBasicDealButtonClicked);
            debugHardDealButton.onClick.AddListener(OnDebugHardDealButtonClicked);
            debugAutoMergeDealButton.onClick.AddListener(OnDebugAutoMergeDealButtonClicked);
            debugSmartDealButton.onClick.AddListener(OnDebugSmartDealButtonClicked);
            fanBoosterButton.onClick.AddListener(OnShuffleBoosterButtonClicked);
            hammerBoosterButton.onClick.AddListener(OnHammerBoosterButtonClicked);
            handBoosterButton.onClick.AddListener(OnHandBoosterButtonClicked);
        }

        public void SetBoard(BoardState boardState)
        {
            board.CoinsCreated -= OnCoinsCreated;
            board.CoinsCreated += OnCoinsCreated;
            board.BoardStateChanged -= OnBoardStateChanged;
            board.BoardStateChanged += OnBoardStateChanged;
            board.DealExecuted -= OnDealExecuted;
            board.DealExecuted += OnDealExecuted;
            board.HighestTierCoinsDestroyed -= OnHighestTierCoinsDestroyed;
            board.HighestTierCoinsDestroyed += OnHighestTierCoinsDestroyed;
            board.HammerTargetSelected -= PlayHammerAnimation;
            board.HammerTargetSelected += PlayHammerAnimation;
            board.Initialize(boardState);
        }

        public int GetHighestBoardTier()
        {
            return board.HighestTier;
        }

        public BoardState GetCurrentBoardState()
        {
            return board.GetCurrentBoardState();
        }

        public bool HasAvailableMoves()
        {
            return board.HasAvailableMoves();
        }

        public bool CanContinuePlaying()
        {
            return board.CanContinuePlaying();
        }

        public void UnlockSlotsForLevel(int level)
        {
            board.UnlockSlotsForLevel(level);
        }

        public void StartAutoPlay()
        {
            board.StartAutoPlay();
        }

        public void StopAutoPlay()
        {
            board.StopAutoPlay();
        }

        public void ShuffleBoard()
        {
            board.ShuffleBoard();
        }

        public void SetUsesEasyDeal(bool usesEasyDeal)
        {
            board.SetUsesEasyDeal(usesEasyDeal);
        }

        public void SetLevel(int level, float progress)
        {
            levelText.SetText(level.ToString());
            levelProgressFillImage.fillAmount = progress;
            progressText.SetText("%" + (int)(progress * 100f));
        }

        private void OnNextBoardButtonClicked()
        {
            NextBoardButtonClicked?.Invoke();
        }

        private void OnPreviousBoardButtonClicked()
        {
            PreviousBoardButtonClicked?.Invoke();
        }

        private void OnDealButtonClicked()
        {
            board.Deal();
        }

        private void OnDebugEasyDealButtonClicked()
        {
            board.DealEasy();
        }

        private void OnDebugBasicDealButtonClicked()
        {
            board.DealBasic();
        }

        private void OnDebugHardDealButtonClicked()
        {
            board.DealHard();
        }

        private void OnDebugAutoMergeDealButtonClicked()
        {
            board.DealAutoMerge();
        }

        private void OnDebugSmartDealButtonClicked()
        {
            board.DealSmart();
        }

        private void OnAutoPlayButtonClicked()
        {
            AutoPlayButtonClicked?.Invoke();
        }

        private void OnStopAutoPlayButtonClicked()
        {
            StopAutoPlayButtonClicked?.Invoke();
        }

        private void OnShuffleBoosterButtonClicked()
        {
            board.ShuffleBoard();
        }

        private void OnHammerBoosterButtonClicked()
        {
            board.ActivateHammerBooster();
        }

        private void OnHandBoosterButtonClicked()
        {
            board.ActivateHandBooster();
        }

        private void OnCoinsCreated(IReadOnlyList<int> tiers)
        {
            CoinsCreated?.Invoke(tiers);
        }

        private void OnHighestTierCoinsDestroyed(int tier, int count)
        {
            HighestTierCoinsDestroyed?.Invoke(tier, count);
        }

        private void OnBoardStateChanged(BoardState boardState)
        {
            BoardStateChanged?.Invoke(boardState);
        }

        private void OnDealExecuted(DealStrategyType dealStrategyType)
        {
            debugDealtTypeText.SetText(dealStrategyType.ToString());
        }

        private void PlayHammerAnimation(Slot targetSlot)
        {
            hammerIconImage.transform.DOKill();
            hammerIconImage.transform.position = targetSlot.transform.position + Vector3.right * 10f;
            hammerIconImage.transform.localScale = Vector3.zero;
            hammerIconImage.transform.localRotation = Quaternion.identity;

            var sequence = DOTween.Sequence();
            sequence.Append(hammerIconImage.transform.DOScale(Vector3.one, 0.5f));
            sequence.Join(hammerIconImage.transform.DOLocalRotate(new Vector3(0f, 0f, -35f), 0.5f));
            sequence.Append(hammerIconImage.transform.DOLocalRotate(new Vector3(0f, 0f, 50f), 0.2f));
            sequence.AppendCallback(() => board.DestroySlotCoinsWithHammer(targetSlot));
            sequence.Append(hammerIconImage.transform.DOLocalRotate(new Vector3(0f, 0f, -35f), 0.3f));
            sequence.Append(hammerIconImage.transform.DOScale(Vector3.zero, 0.2f));
            sequence.OnComplete(() =>
            {
                hammerIconImage.transform.localRotation = Quaternion.identity;
                board.CompleteHammerAnimation();
            });
        }
    }
}