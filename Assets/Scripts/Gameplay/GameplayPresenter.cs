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
            View.CoinCreated += OnCoinCreated;
            View.BoardStateChanged += OnBoardStateChanged;

            ShowBoard();
        }

        public override void Cleanup()
        {
            View.NextBoardButtonClicked -= OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked -= OnPreviousBoardButtonClicked;
            View.CoinCreated -= OnCoinCreated;
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

        private void ShowBoard()
        {
            var generatedBoardState = _boardGenerationService.CreateBoard(_currentBoardIndex, _levelProgressService.CurrentBoardLevel);
            var boardState = _levelProgressService.RestoreBoardState(generatedBoardState);
            View.SetBoard(boardState);
            var isNewBoard = _levelProgressService.InitializeBoard(_currentBoardIndex, View.GetHighestBoardTier());

            if (isNewBoard)
            {
                _levelProgressService.SaveBoardState(boardState);
            }

            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);
        }

        private void OnBoardStateChanged(BoardState boardState)
        {
            _levelProgressService.SaveBoardState(boardState);
        }

        private void OnCoinCreated(int tier)
        {
            var levelCompleted = _levelProgressService.RegisterCoinCreated(tier);
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
                _levelProgressService.Flush();
            }
        }
    }
}