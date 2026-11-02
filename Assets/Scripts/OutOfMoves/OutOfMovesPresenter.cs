using General;
using General.EventDispatcher;
using UI.General;

namespace OutOfMoves
{
    public class OutOfMovesPresenter: BasePresenter<OutOfMovesView>
    {
        private IUIService _uiService;
        private IEventDispatcherService _eventDispatcherService;

        protected override void OnInitialize()
        {
            _uiService = ServiceLocator.GetService<IUIService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            View.CloseButtonClicked += OnCloseButtonClicked;
            View.ContinueButtonClicked += OnContinueButtonClicked;
        }

        public override void Cleanup()
        {
            View.CloseButtonClicked -= OnCloseButtonClicked;
            View.ContinueButtonClicked -= OnContinueButtonClicked;
            base.Cleanup();
        }

        private void OnCloseButtonClicked()
        {
            _uiService.HidePopup<OutOfMovesPresenter>();
            _uiService.ShowPopup<LosePresenter>();
        }

        private void OnContinueButtonClicked()
        {
            _eventDispatcherService.Dispatch(new ShuffleBoardSignal());
        }
    }
}