using General;
using System;
using System.Collections.Generic;
using DailyChallenge.Award;

namespace DailyChallenge
{
    public interface IDailyChallengeService : IService
    {
        int InstallYear { get; }
        int InstallMonth { get; }
        int CurrentYear { get; }
        int CurrentMonth { get; }
        DateTime CurrentMonthDate { get; }
        DateTime DisplayedMonthDate { get; }
        DateTime FirstAvailableMonthDate { get; }
        DateTime LastAvailableMonthDate { get; }
        bool HasActiveDailyChallengeGame { get; }

        IReadOnlyList<DailyChallengeDayModel> GetCalendarGrid();
        IReadOnlyList<AwardMonthModel> GetAwardMonths();
        IReadOnlyList<DailyChallengeDay> GetDisplayedMonthDays();
        DailyChallengeDay GetDay(int day);
        DailyChallengeDay GetDay(DateTime date);
        DailyChallengeDay GetSelectedDay();
        DateTime? GetSelectedDate();
        DateTime? GetPlayedDate();
        string GetPlayedDateText();
        string GetPlayedMonthYearText();
        int GetSelectedLevelId();
        int GetPlayedLevelId();
        int GetPlayedAttemptCount();
        int GetLevelId(DateTime date);
        string GetDisplayedMonthText();
        IReadOnlyList<string> GetAbbreviatedWeekdayNames();
        AwardState GetDisplayedMonthAwardState();
        int GetCompletedCountInDisplayedMonth();
        int GetActiveCountInDisplayedMonth();
        int GetTotalCountInDisplayedMonth();
        bool CanNavigateToPreviousMonth();
        bool CanNavigateToNextMonth();
        void NavigateToPreviousMonth();
        void NavigateToNextMonth();
        void SetDisplayedMonth(int year, int month);
        bool SelectDay(int day);
        bool SelectDate(DateTime date);
        bool SelectLatestPlayableUncompletedDayFromCurrentDate();
        bool SelectLatestPlayableUncompletedDay();
        bool SelectFirstPlayableUncompletedDay();
        void StartSelectedDayGame();
        void IncreasePlayedAttemptCount();
        void ResetPlayedAttemptCount();
        void ClearActiveDailyChallengeGame();
        void CompletePlayedDay();
        void CompleteSelectedDay();
        void ResetAllDays();
        void CompleteAllActiveDays();
    }
}