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
        [SerializeField] private Image levelProgressFillImage;
        [SerializeField] private Button nextBoardButton;
        [SerializeField] private Button previousBoardButton;

        public event Action NextBoardButtonClicked;
        public event Action PreviousBoardButtonClicked;

        private void Start()
        {
            nextBoardButton.onClick.AddListener(OnNextBoardButtonClicked);
            previousBoardButton.onClick.AddListener(OnPreviousBoardButtonClicked);
        }

        public void SetBoard(BoardState boardState)
        {
            board.Initialize(boardState);
        }

        public void SetLevel(int level, float progress)
        {
            levelText.SetText(level.ToString());
            levelProgressFillImage.fillAmount = progress;
        }

        private void OnNextBoardButtonClicked()
        {
            NextBoardButtonClicked?.Invoke();
        }

        private void OnPreviousBoardButtonClicked()
        {
            PreviousBoardButtonClicked?.Invoke();
        }
    }
}