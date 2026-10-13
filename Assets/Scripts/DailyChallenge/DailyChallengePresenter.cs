using DailyChallenge.Award;
using Gameplay;
using General;
using Home;
using UI.General;

namespace DailyChallenge
{
    public class DailyChallengePresenter : BasePresenter<DailyChallengeView>
    {
        private IDailyChallengeService _dailyChallengeService;
        private IUIService _uiService;
        private bool _shouldShowCompletedReward;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _dailyChallengeService = ServiceLocator.GetService<IDailyChallengeService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.PreviousMonthClicked += OnPreviousMonthClicked;
            View.NextMonthClicked += OnNextMonthClicked;
            View.PlayClicked += OnPlayClicked;
            View.AwardsClicked += OnAwardsClicked;
            View.DayClicked += OnDayClicked;
            View.DebugCompleteDayClicked += OnDebugCompleteDayClicked;
            View.DebugResetAllDaysClicked += OnDebugResetAllDaysClicked;
            View.DebugCompleteAllDaysClicked += OnDebugCompleteAllDaysClicked;
            View.BackButtonClicked += OnBackButtonClicked;
            View.CompletedDayPulseCompleted += OnCompletedDayPulseCompleted;
        }

        private void OnAwardsClicked()
        {
            _uiService.ShowPopup<AwardsPresenter>();
        }

        private void OnBackButtonClicked()
        {
            _dailyChallengeService.SelectLatestPlayableUncompletedDayFromCurrentDate();
            _uiService.HidePopup<DailyChallengePresenter>();
            _uiService.ShowPopup<HomePresenter>();
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _dailyChallengeService.SelectLatestPlayableUncompletedDayFromCurrentDate();
            RefreshView();
        }

        public override void Cleanup()
        {
            if (View != null)
            {
                View.PreviousMonthClicked -= OnPreviousMonthClicked;
                View.NextMonthClicked -= OnNextMonthClicked;
                View.PlayClicked -= OnPlayClicked;
                View.DayClicked -= OnDayClicked;
                View.DebugCompleteDayClicked -= OnDebugCompleteDayClicked;
                View.DebugResetAllDaysClicked -= OnDebugResetAllDaysClicked;
                View.DebugCompleteAllDaysClicked -= OnDebugCompleteAllDaysClicked;
                View.AwardsClicked -= OnAwardsClicked;
                View.BackButtonClicked -= OnBackButtonClicked;
                View.CompletedDayPulseCompleted -= OnCompletedDayPulseCompleted;
            }

            base.Cleanup();
        }

        private void OnPreviousMonthClicked()
        {
            _dailyChallengeService.NavigateToPreviousMonth();
            RefreshView();
        }

        private void OnNextMonthClicked()
        {
            _dailyChallengeService.NavigateToNextMonth();
            RefreshView();
        }

        private void OnDayClicked(int day)
        {
            _dailyChallengeService.SelectDay(day);
            RefreshView();
        }

        private void OnPlayClicked()
        {
            var selectedDay = _dailyChallengeService.GetSelectedDay();
            if (selectedDay == null || !selectedDay.Active)
                return;

            _dailyChallengeService.StartSelectedDayGame();
            _uiService.HidePopup<DailyChallengePresenter>();
            _uiService.ShowPopup<GameplayPresenter>();
        }

        private void OnDebugCompleteDayClicked()
        {
            _dailyChallengeService.CompleteSelectedDay();
            _dailyChallengeService.SelectLatestPlayableUncompletedDay();
            RefreshView();
        }

        private void OnDebugResetAllDaysClicked()
        {
            _dailyChallengeService.ResetAllDays();
            _dailyChallengeService.SelectLatestPlayableUncompletedDay();
            RefreshView();
        }

        private void OnDebugCompleteAllDaysClicked()
        {
            _dailyChallengeService.CompleteAllActiveDays();
            _dailyChallengeService.SelectLatestPlayableUncompletedDay();
            RefreshView();
        }

        private void RefreshView()
        {
            var selectedDay = _dailyChallengeService.GetSelectedDay();

            View.SetMonthText(_dailyChallengeService.GetDisplayedMonthText());
            View.SetWeekdayHeaderLabels(_dailyChallengeService.GetAbbreviatedWeekdayNames());
            View.SetNavigationButtons(
                _dailyChallengeService.CanNavigateToPreviousMonth(),
                _dailyChallengeService.CanNavigateToNextMonth());
            View.SetProgress(
                _dailyChallengeService.GetCompletedCountInDisplayedMonth(),
                _dailyChallengeService.GetTotalCountInDisplayedMonth());
            View.SetAward(_dailyChallengeService.DisplayedMonthDate.Month, _dailyChallengeService.GetDisplayedMonthAwardState());
            View.SetPlayButton(selectedDay != null && selectedDay.Active && !selectedDay.Completed);
            View.SetDays(_dailyChallengeService.GetCalendarGrid());
        }

        public void PlayCompletedDayRewardFlow()
        {
            var playedDate = _dailyChallengeService.GetPlayedDate();
            if (!playedDate.HasValue)
                return;

            _dailyChallengeService.SetDisplayedMonth(playedDate.Value.Year, playedDate.Value.Month);
            var completedCount = _dailyChallengeService.GetCompletedCountInDisplayedMonth();
            var totalCount = _dailyChallengeService.GetTotalCountInDisplayedMonth();
            _shouldShowCompletedReward = totalCount > 0 && completedCount >= totalCount;
            RefreshView();
            View.PlayCompletedDayPulse(playedDate.Value.Day);
        }

        private void OnCompletedDayPulseCompleted()
        {
            if (!_shouldShowCompletedReward)
                return;

            _shouldShowCompletedReward = false;
            _uiService.ShowPopup<DailyChallengeReward1Presenter>();
        }
    }
}