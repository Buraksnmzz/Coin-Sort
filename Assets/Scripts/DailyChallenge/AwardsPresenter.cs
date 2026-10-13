using DailyChallenge;
using General;
using UI.General;

namespace DailyChallenge.Award
{
    public class AwardsPresenter : BasePresenter<AwardsView>
    {
        private IDailyChallengeService _dailyChallengeService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
        }

        public override void ViewShown()
        {
            base.ViewShown();
            View.SetAwards(_dailyChallengeService.GetAwardMonths());
        }
    }
}