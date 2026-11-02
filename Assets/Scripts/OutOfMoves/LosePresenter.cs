using General;
using General.EventDispatcher;
using UI.General;

namespace OutOfMoves
{
    public class LosePresenter: BasePresenter<LoseView>
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
            _uiService.HidePopup<LosePresenter>();
            _uiService.ShowPopup<RestartPresenter>();
        }

        private void OnContinueButtonClicked()
        {
            _eventDispatcherService.Dispatch(new ShuffleBoardSignal());
        }
    }
}