using General;
using Localization;
using Services;
using Sound;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeReward1Presenter : BasePresenter<DailyChallengeReward1View>
    {
        private IDailyChallengeService _dailyChallengeService;
        private IUIService _uiService;
        private ISoundService _soundService;
        private ILocalizationService _localizationService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            View.ContinueClicked += OnContinueClicked;
            View.CompletedImageAnimationStarted += OnCompletedImageAnimationStarted;
            _soundService = ServiceLocator.GetService<ISoundService>();
        }

        private void OnCompletedImageAnimationStarted()
        {
            _soundService.PlaySound(ClipName.DailyChallengeReward);
        }

        public override void ViewShown()
        {
            base.ViewShown();
            var rewardText =
                _localizationService.GetLocalizedString(LocalizationStrings.YouHaveCompletedAllTheDailyChallengesFor);
            var monthYearText = _dailyChallengeService.GetPlayedMonthYearText();
            View.SetCompletedText(rewardText + " " + monthYearText);
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.ContinueClicked -= OnContinueClicked;
            }

            base.Cleanup();
        }

        private void OnContinueClicked()
        {
            _uiService.HidePopup<DailyChallengeReward1Presenter>();
            _uiService.ShowPopup<DailyChallengeReward2Presenter>();
        }
    }
}