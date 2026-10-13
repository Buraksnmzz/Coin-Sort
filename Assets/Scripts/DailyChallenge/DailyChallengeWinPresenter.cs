using Gameplay;
using General;
using Localization;
using Services;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengeWinPresenter : BasePresenter<DailyChallengeWinView>
    {
        private IDailyChallengeService _dailyChallengeService;
        private IUIService _uiService;
        private ILocalizationService _localizationService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            View.ContinueClicked += OnContinueClicked;
        }

        public override void ViewShown()
        {
            base.ViewShown();
            var dateText =
                _localizationService.GetLocalizedString(LocalizationStrings.YouHaveCompletedTheDailyChallengeFor);
            View.SetDateText(dateText + " " + _dailyChallengeService.GetPlayedDateText());
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
            _uiService.HidePopup<GameplayPresenter>(false);
            _uiService.HidePopup<DailyChallengeWinPresenter>();
            var dailyChallengePresenter = _uiService.ShowPopup<DailyChallengePresenter>();
            dailyChallengePresenter?.PlayCompletedDayRewardFlow();
        }
    }
}