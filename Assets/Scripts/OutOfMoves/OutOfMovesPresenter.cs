using General;
using UI.General;

namespace OutOfMoves
{
    public class OutOfMovesPresenter: BasePresenter<OutOfMovesView>
    {
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            _uiService = ServiceLocator.GetService<IUIService>();
            View.CloseButtonClicked += OnCloseButtonClicked;
        }

        public override void Cleanup()
        {
            View.CloseButtonClicked -= OnCloseButtonClicked;
            base.Cleanup();
        }

        private void OnCloseButtonClicked()
        {
            _uiService.HidePopup<OutOfMovesPresenter>();
            _uiService.ShowPopup<LosePresenter>();
        }
    }
}