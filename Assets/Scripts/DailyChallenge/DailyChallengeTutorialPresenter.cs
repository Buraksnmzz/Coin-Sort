using General;
using Home;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeTutorialPresenter : BasePresenter<DailyChallengeTutorialView>
    {
        private IUIService _uiService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.ContinueClicked += OnContinueClicked;
        }

        public override void Cleanup()
        {
            if (View != null)
                View.ContinueClicked -= OnContinueClicked;

            base.Cleanup();
        }

        private void OnContinueClicked()
        {
            _uiService.HidePopup<DailyChallengeTutorialPresenter>();
            _uiService.HidePopup<HomePresenter>();
            _uiService.ShowPopup<DailyChallengePresenter>();
        }
    }
}