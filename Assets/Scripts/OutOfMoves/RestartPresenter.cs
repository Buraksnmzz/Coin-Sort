using General;
using General.EventDispatcher;
using UI.General;

namespace OutOfMoves
{
    public class RestartPresenter: BasePresenter<RestartView>
    {
        private IEventDispatcherService _eventDispatcherService;

        protected override void OnInitialize()
        {
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            View.RestartButtonClicked += OnRestartButtonClicked;
        }

        public override void Cleanup()
        {
            View.RestartButtonClicked -= OnRestartButtonClicked;
            base.Cleanup();
        }

        private void OnRestartButtonClicked()
        {
            View.Hide();
            _eventDispatcherService.Dispatch(new RestartLevelSignal());
        }
    }
}