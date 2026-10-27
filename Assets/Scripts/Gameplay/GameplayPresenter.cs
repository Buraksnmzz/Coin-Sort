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

            ShowBoard();
        }

        public override void Cleanup()
        {
            View.NextBoardButtonClicked -= OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked -= OnPreviousBoardButtonClicked;
            View.CoinCreated -= OnCoinCreated;
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
            var boardState = _boardGenerationService.CreateBoard(_currentBoardIndex, _levelProgressService.CurrentBoardLevel);
            View.SetBoard(boardState);
            _levelProgressService.InitializeBoard(_currentBoardIndex, View.GetHighestBoardTier());
            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);
        }

        private void OnCoinCreated(int tier)
        {
            _levelProgressService.RegisterCoinCreated(tier);
            var nextBoardIndex = _levelProgressService.GetBoardIndex();

            if (nextBoardIndex != _currentBoardIndex)
            {
                _currentBoardIndex = nextBoardIndex;
                ShowBoard();
                return;
            }

            View.SetLevel(_levelProgressService.CurrentLevel, _levelProgressService.Progress);
        }
    }
}