using General;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeUnlockPresenter: BasePresenter<DailyChallengeUnlockView>
    {
        private IUIService _uiService;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.PlayButtonClicked += OnPlayButtonClicked;
        }

        private void OnPlayButtonClicked()
        {
            View.Hide();
            _uiService.ShowPopup<DailyChallengeTutorialPresenter>();
        }
    }
}