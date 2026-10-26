using General;
using UI.General;

namespace Gameplay
{
    public class GameplayPresenter : BasePresenter<GameplayView>
    {
        private IBoardGenerationService _boardGenerationService;
        private int _currentBoardIndex;

        protected override void OnInitialize()
        {
            _boardGenerationService = ServiceLocator.GetService<IBoardGenerationService>();
            _currentBoardIndex = 1;

            View.NextBoardButtonClicked += OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked += OnPreviousBoardButtonClicked;

            ShowBoard();
        }

        public override void Cleanup()
        {
            View.NextBoardButtonClicked -= OnNextBoardButtonClicked;
            View.PreviousBoardButtonClicked -= OnPreviousBoardButtonClicked;
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
            var currentLevel = GetFirstLevel(_currentBoardIndex);
            var boardState = _boardGenerationService.CreateBoard(_currentBoardIndex, currentLevel);
            View.SetBoard(boardState);
            View.SetLevel(currentLevel, 0f);
        }

        private static int GetFirstLevel(int boardIndex)
        {
            return boardIndex == 1 ? 1 : 28 + (boardIndex - 2) * 24;
        }
    }
}