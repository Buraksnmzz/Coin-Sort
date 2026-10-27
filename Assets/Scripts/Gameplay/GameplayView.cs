using System;
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

        public event Action NextBoardButtonClicked;
        public event Action PreviousBoardButtonClicked;
        public event Action<int> CoinCreated;

        private void Start()
        {
            nextBoardButton.onClick.AddListener(OnNextBoardButtonClicked);
            previousBoardButton.onClick.AddListener(OnPreviousBoardButtonClicked);
        }

        public void SetBoard(BoardState boardState)
        {
            board.CoinCreated -= OnCoinCreated;
            board.CoinCreated += OnCoinCreated;
            board.Initialize(boardState);
        }

        public int GetHighestBoardTier()
        {
            return board.HighestTier;
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

        private void OnCoinCreated(int tier)
        {
            CoinCreated?.Invoke(tier);
        }
    }
}