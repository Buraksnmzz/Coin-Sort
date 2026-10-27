using System.Collections.Generic;
using General;
using UI.General;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private IBoardGenerationService _boardGenerationService;
        private ILevelProgressService _levelProgressService;
        private int _currentBoardIndex;

        protected override void OnInitialize()
        {
            _boardGenerationService = ServiceLocator.GetService<IBoardGenerationService>();
            _levelProgressService = ServiceLocator.GetService<ILevelProgressService>();
            _currentBoardIndex = _levelProgressService.GetBoardIndex();

            View.NextBoardButtonClicked += OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked += OnPreviousBoardButtonClicked;
            View.AutoPlayButtonClicked += OnAutoPlayButtonClicked;
            View.StopAutoPlayButtonClicked += OnStopAutoPlayButtonClicked;
            View.CoinsCreated += OnCoinsCreated;
            View.BoardStateChanged += OnBoardStateChanged;

            ShowBoard();
        }

        public override void Cleanup()
        {
            View.NextBoardButtonClicked -= OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked -= OnPreviousBoardButtonClicked;
            View.AutoPlayButtonClicked -= OnAutoPlayButtonClicked;
            View.StopAutoPlayButtonClicked -= OnStopAutoPlayButtonClicked;
            View.CoinsCreated -= OnCoinsCreated;
            View.BoardStateChanged -= OnBoardStateChanged;
            base.Cleanup();
        }

        private void OnNextBoardButtonClicked()
        {
            _currentBoardIndex++;
            ShowBoard();
        }

        private void OnPreviousBoardButtonClicked()
        {
            if (_currentBoardIndex == 1)
            {
                return;
            }

            _currentBoardIndex--;
            ShowBoard();
        }

        private void OnAutoPlayButtonClicked()
        {
            View.StartAutoPlay();
        }

        private void OnStopAutoPlayButtonClicked()
        {
            View.StopAutoPlay();
        }

        private void ShowBoard()
        {
            var generatedBoardState = _boardGenerationService.CreateBoard(_currentBoardIndex, _levelProgressService.CurrentBoardLevel);
            var boardState = _levelProgressService.RestoreBoardState(generatedBoardState);
            View.SetBoard(boardState);
            var isNewBoard = _levelProgressService.InitializeBoard(_currentBoardIndex, View.GetHighestBoardTier());

            if (isNewBoard)
            {
                _levelProgressService.SaveBoardState(View.GetCurrentBoardState());
            }

            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);
        }

        private void OnBoardStateChanged(BoardState boardState)
        {
            _levelProgressService.SaveBoardState(boardState);
        }

        private void OnCoinsCreated(IReadOnlyList<int> tiers)
        {
            var levelCompleted = false;

            foreach (var tier in tiers)
            {
                levelCompleted |= _levelProgressService.RegisterCoinCreated(tier);
            }

            UpdateLevel(levelCompleted);
        }

        private void UpdateLevel(bool levelCompleted)
        {
            if (levelCompleted)
            {
                View.StopAutoPlay();
            }

            var nextBoardIndex = _levelProgressService.GetBoardIndex();

            if (nextBoardIndex != _currentBoardIndex)
            {
                _currentBoardIndex = nextBoardIndex;
                ShowBoard();

                if (levelCompleted)
                {
                    _levelProgressService.Flush();
                }

                return;
            }

            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);

            if (levelCompleted)
            {
                View.UnlockSlotsForLevel(_levelProgressService.CurrentBoardLevel);
                _levelProgressService.Flush();
            }
        }
    }
}