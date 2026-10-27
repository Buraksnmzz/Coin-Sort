using System;
using System.Collections.Generic;
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
        [SerializeField] private Button nextBoardButton;
        [SerializeField] private Button previousBoardButton;
        [SerializeField] private Button dealButton;

        public event Action NextBoardButtonClicked;
        public event Action PreviousBoardButtonClicked;
        public event Action<IReadOnlyList<int>> CoinsCreated;
        public event Action<BoardState> BoardStateChanged;

        private void Start()
        {
            nextBoardButton.onClick.AddListener(OnNextBoardButtonClicked);
            previousBoardButton.onClick.AddListener(OnPreviousBoardButtonClicked);
            dealButton.onClick.AddListener(OnDealButtonClicked);
        }

        public void SetBoard(BoardState boardState)
        {
            board.CoinsCreated -= OnCoinsCreated;
            board.CoinsCreated += OnCoinsCreated;
            board.BoardStateChanged -= OnBoardStateChanged;
            board.BoardStateChanged += OnBoardStateChanged;
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

        public void UnlockSlotsForLevel(int level)
        {
            board.UnlockSlotsForLevel(level);
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

        private void OnCoinsCreated(IReadOnlyList<int> tiers)
        {
            CoinsCreated?.Invoke(tiers);
        }

        private void OnBoardStateChanged(BoardState boardState)
        {
            BoardStateChanged?.Invoke(boardState);
        }
    }
}