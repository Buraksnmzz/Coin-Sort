
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DailyChallenge.Award;
using General;
using Localization;
using SavedData;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace DailyChallenge
{
    public class DailyChallengeService : IDailyChallengeService
    {
        private readonly ISavedDataService _savedDataService;
        private readonly ILocalizationService _localizationService;
        private readonly DailyChallengeModel _model;
        private bool _hasActiveDailyChallengeGame;

        public int InstallYear => _model.InstallYear;
        public int InstallMonth => _model.InstallMonth;
        public int CurrentYear => DateTime.Today.Year;
        public int CurrentMonth => DateTime.Today.Month;
        public DateTime CurrentMonthDate => new DateTime(CurrentYear, CurrentMonth, 1);
        public DateTime DisplayedMonthDate => new DateTime(_model.DisplayedYear, _model.DisplayedMonth, 1);
        public DateTime FirstAvailableMonthDate => new DateTime(InstallYear - 1, 1, 1);
        public DateTime LastAvailableMonthDate => CurrentMonthDate;
        public bool HasActiveDailyChallengeGame => _hasActiveDailyChallengeGame;

        public DailyChallengeService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            _model = _savedDataService.LoadData<DailyChallengeModel>();
            EnsureInitialized();
        }

        public IReadOnlyList<DailyChallengeDay> GetDisplayedMonthDays()
        {
            var days = new List<DailyChallengeDay>();
            var daysInMonth = DateTime.DaysInMonth(_model.DisplayedYear, _model.DisplayedMonth);
            for (var day = 1; day <= daysInMonth; day++)
            {
                days.Add(GetDay(new DateTime(_model.DisplayedYear, _model.DisplayedMonth, day)));
            }

            return days;
        }

        public IReadOnlyList<AwardMonthModel> GetAwardMonths()
        {
            var awards = new List<AwardMonthModel>();
            var monthDate = FirstAvailableMonthDate;
            var lastAwardMonthDate = new DateTime(CurrentYear, 12, 1);

            while (monthDate <= lastAwardMonthDate)
            {
                awards.Add(new AwardMonthModel
                {
                    Year = monthDate.Year,
                    Month = monthDate.Month,
                    MonthName = GetMonthName(monthDate),
                    State = GetAwardState(monthDate)
                });

                monthDate = monthDate.AddMonths(1);
            }

            return awards;
        }

        public DailyChallengeDay GetDay(int day)
        {
            var daysInMonth = DateTime.DaysInMonth(_model.DisplayedYear, _model.DisplayedMonth);
            if (day < 1 || day > daysInMonth)
                return null;

            return GetDay(new DateTime(_model.DisplayedYear, _model.DisplayedMonth, day));
        }

        public DailyChallengeDay GetDay(DateTime date)
        {
            var normalizedDate = date.Date;
            return new DailyChallengeDay
            {
                Year = normalizedDate.Year,
                Month = normalizedDate.Month,
                Day = normalizedDate.Day,
                Completed = IsCompleted(normalizedDate),
                Active = IsActive(normalizedDate)
            };
        }

        public IReadOnlyList<DailyChallengeDayModel> GetCalendarGrid()
        {
            var grid = new List<DailyChallengeDayModel>();
            var firstDay = DisplayedMonthDate;
            var firstDayOffset = (int)firstDay.DayOfWeek;
            var days = GetDisplayedMonthDays();
            var selectedDay = GetSelectedDay();
            var totalCells = firstDayOffset + days.Count;
            var rows = Mathf.CeilToInt(totalCells / 7f);
            totalCells = rows * 7;

            for (var cell = 0; cell < totalCells; cell++)
            {
                if (cell < firstDayOffset || cell >= firstDayOffset + days.Count)
                {
                    grid.Add(new DailyChallengeDayModel());
                    continue;
                }

                var day = days[cell - firstDayOffset];
                var isSelected = selectedDay != null
                                 && !selectedDay.Completed
                                 && selectedDay.Year == day.Year
                                 && selectedDay.Month == day.Month
                                 && selectedDay.Day == day.Day;
                grid.Add(new DailyChallengeDayModel
                {
                    Day = day.DayNumber,
                    Completed = day.Completed,
                    Active = day.Active,
                    Selected = isSelected
                });
            }

            return grid;
        }

        public DailyChallengeDay GetSelectedDay()
        {
            var selectedDate = GetSelectedDate();
            return selectedDate.HasValue ? GetDay(selectedDate.Value) : null;
        }

        public DateTime? GetSelectedDate()
        {
            if (_model.SelectedYear <= 0 || _model.SelectedMonth <= 0 || _model.SelectedDay <= 0)
                return null;

            return new DateTime(_model.SelectedYear, _model.SelectedMonth, _model.SelectedDay);
        }

        public DateTime? GetPlayedDate()
        {
            if (_model.PlayedYear <= 0 || _model.PlayedMonth <= 0 || _model.PlayedDay <= 0)
                return null;

            return new DateTime(_model.PlayedYear, _model.PlayedMonth, _model.PlayedDay);
        }

        public string GetPlayedDateText()
        {
            var playedDate = GetPlayedDate();
            return playedDate.HasValue ? playedDate.Value.ToString("dd/MM/yyyy") : string.Empty;
        }

        public string GetPlayedMonthYearText()
        {
            var playedDate = GetPlayedDate();
            return playedDate.HasValue
                ? playedDate.Value.ToString("MMMM yyyy", GetCurrentCulture())
                : string.Empty;
        }

        public int GetSelectedLevelId()
        {
            var selectedDate = GetSelectedDate();
            return selectedDate.HasValue ? GetLevelId(selectedDate.Value) : 1;
        }

        public int GetPlayedLevelId()
        {
            var playedDate = GetPlayedDate();
            return playedDate.HasValue ? GetLevelId(playedDate.Value) : GetSelectedLevelId();
        }

        public int GetPlayedAttemptCount()
        {
            return Mathf.Max(0, _model.PlayedAttemptCount);
        }

        public int GetLevelId(DateTime date)
        {
            return date.DayOfYear;
        }

        public string GetDisplayedMonthText()
        {
            return DisplayedMonthDate.ToString("MMMM yyyy", GetCurrentCulture());
        }

        public IReadOnlyList<string> GetAbbreviatedWeekdayNames()
        {
            return GetCurrentCulture().DateTimeFormat.AbbreviatedDayNames;
        }

        public AwardState GetDisplayedMonthAwardState()
        {
            return GetAwardState(DisplayedMonthDate);
        }

        public int GetCompletedCountInDisplayedMonth()
        {
            var completedCount = 0;
            foreach (var day in GetDisplayedMonthDays())
            {
                if (day.Active && day.Completed)
                    completedCount++;
            }

            return completedCount;
        }

        public int GetActiveCountInDisplayedMonth()
        {
            var activeCount = 0;
            foreach (var day in GetDisplayedMonthDays())
            {
                if (day.Active)
                    activeCount++;
            }

            return activeCount;
        }

        public int GetTotalCountInDisplayedMonth()
        {
            return DateTime.DaysInMonth(_model.DisplayedYear, _model.DisplayedMonth);
        }

        public bool CanNavigateToPreviousMonth()
        {
            return DisplayedMonthDate.AddMonths(-1) >= FirstAvailableMonthDate;
        }

        public bool CanNavigateToNextMonth()
        {
            return DisplayedMonthDate.AddMonths(1) <= LastAvailableMonthDate;
        }

        public void NavigateToPreviousMonth()
        {
            if (!CanNavigateToPreviousMonth())
                return;

            SetDisplayedMonth(DisplayedMonthDate.AddMonths(-1));
        }

        public void NavigateToNextMonth()
        {
            if (!CanNavigateToNextMonth())
                return;

            SetDisplayedMonth(DisplayedMonthDate.AddMonths(1));
        }

        public void SetDisplayedMonth(int year, int month)
        {
            SetDisplayedMonth(new DateTime(year, month, 1));
        }

        public bool SelectDay(int day)
        {
            var monthDays = DateTime.DaysInMonth(_model.DisplayedYear, _model.DisplayedMonth);
            if (day < 1 || day > monthDays)
                return false;

            return SelectDate(new DateTime(_model.DisplayedYear, _model.DisplayedMonth, day));
        }

        public bool SelectDate(DateTime date)
        {
            var day = GetDay(date);
            if (day == null || !day.Active || day.Completed)
                return false;

            _model.SelectedYear = day.Year;
            _model.SelectedMonth = day.Month;
            _model.SelectedDay = day.Day;
            _savedDataService.SaveData(_model);
            return true;
        }

        public bool SelectLatestPlayableUncompletedDayFromCurrentDate()
        {
            var monthDate = CurrentMonthDate;
            while (monthDate >= FirstAvailableMonthDate)
            {
                SetDisplayedMonthWithoutAutoSelection(monthDate);
                var days = GetDisplayedMonthDays();
                for (var index = days.Count - 1; index >= 0; index--)
                {
                    var day = days[index];
                    if (!day.Active || day.Completed)
                        continue;

                    return SelectDate(day.Date);
                }

                monthDate = monthDate.AddMonths(-1);
            }

            SetDisplayedMonth(CurrentMonthDate);
            ClearSelectedDay();
            return false;
        }

        public bool SelectLatestPlayableUncompletedDay()
        {
            var days = GetDisplayedMonthDays();
            for (var index = days.Count - 1; index >= 0; index--)
            {
                var day = days[index];
                if (!day.Active || day.Completed)
                    continue;

                return SelectDate(day.Date);
            }

            ClearSelectedDay();
            return false;
        }

        public bool SelectFirstPlayableUncompletedDay()
        {
            var currentMonth = FirstAvailableMonthDate;
            while (currentMonth <= CurrentMonthDate)
            {
                SetDisplayedMonth(currentMonth);
                var days = GetDisplayedMonthDays();
                foreach (var day in days)
                {
                    if (!day.Active || day.Completed)
                        continue;

                    return SelectDate(day.Date);
                }

                currentMonth = currentMonth.AddMonths(1);
            }

            SetDisplayedMonth(CurrentMonthDate);
            return false;
        }

        public void StartSelectedDayGame()
        {
            var selectedDate = GetSelectedDate();
            if (!selectedDate.HasValue)
                return;

            _model.PlayedYear = selectedDate.Value.Year;
            _model.PlayedMonth = selectedDate.Value.Month;
            _model.PlayedDay = selectedDate.Value.Day;
            _model.PlayedAttemptCount = 0;
            _hasActiveDailyChallengeGame = true;
            _savedDataService.SaveData(_model);
        }

        public void IncreasePlayedAttemptCount()
        {
            _model.PlayedAttemptCount++;
            _savedDataService.SaveData(_model);
        }

        public void ResetPlayedAttemptCount()
        {
            if (_model.PlayedAttemptCount == 0)
                return;

            _model.PlayedAttemptCount = 0;
            _savedDataService.SaveData(_model);
        }

        public void ClearActiveDailyChallengeGame()
        {
            _hasActiveDailyChallengeGame = false;
        }

        public void CompletePlayedDay()
        {
            var playedDate = GetPlayedDate();
            if (!playedDate.HasValue)
                return;

            CompleteDay(playedDate.Value);
            ResetPlayedAttemptCount();
            ClearActiveDailyChallengeGame();
        }

        public void CompleteSelectedDay()
        {
            var selectedDate = GetSelectedDate();
            if (!selectedDate.HasValue)
                return;

            CompleteDay(selectedDate.Value);
        }

        public void ResetAllDays()
        {
            foreach (var day in GetDisplayedMonthDays())
            {
                _model.CompletedDays.Remove(ToKey(day.Date));
            }

            _savedDataService.SaveData(_model);
        }

        public void CompleteAllActiveDays()
        {
            foreach (var day in GetDisplayedMonthDays())
            {
                if (day.Active)
                    AddCompleted(day.Date);
            }

            _savedDataService.SaveData(_model);
        }

        private void EnsureInitialized()
        {
            var today = DateTime.Today;
            if (_model.InstallYear <= 0 || _model.InstallMonth <= 0)
            {
                var installMonthDate = GetInstallMonthDate();
                _model.InstallYear = installMonthDate.Year;
                _model.InstallMonth = installMonthDate.Month;
            }

            if (_model.DisplayedYear <= 0 || _model.DisplayedMonth <= 0)
            {
                _model.DisplayedYear = today.Year;
                _model.DisplayedMonth = today.Month;
            }

            if (_model.CompletedDays == null)
            {
                _model.CompletedDays = new List<string>();
            }

            SetDisplayedMonth(new DateTime(_model.DisplayedYear, _model.DisplayedMonth, 1));
        }

        private void SetDisplayedMonth(DateTime monthDate)
        {
            SetDisplayedMonthWithoutAutoSelection(monthDate);
            SelectLatestPlayableUncompletedDayInCurrentMonth();
            _savedDataService.SaveData(_model);
        }

        private void SetDisplayedMonthWithoutAutoSelection(DateTime monthDate)
        {
            var clampedMonth = ClampMonth(monthDate);
            _model.DisplayedYear = clampedMonth.Year;
            _model.DisplayedMonth = clampedMonth.Month;
            _savedDataService.SaveData(_model);
        }

        private void SelectLatestPlayableUncompletedDayInCurrentMonth()
        {
            var days = GetDisplayedMonthDays();
            for (var index = days.Count - 1; index >= 0; index--)
            {
                var day = days[index];
                if (!day.Active || day.Completed)
                    continue;

                _model.SelectedYear = day.Year;
                _model.SelectedMonth = day.Month;
                _model.SelectedDay = day.Day;
                return;
            }

            ClearSelectedDay();
        }

        private void ClearSelectedDay()
        {
            _model.SelectedYear = 0;
            _model.SelectedMonth = 0;
            _model.SelectedDay = 0;
            _savedDataService.SaveData(_model);
        }

        private DateTime ClampMonth(DateTime monthDate)
        {
            var normalizedMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
            if (normalizedMonth < FirstAvailableMonthDate)
                return FirstAvailableMonthDate;
            if (normalizedMonth > LastAvailableMonthDate)
                return LastAvailableMonthDate;
            return normalizedMonth;
        }

        private bool IsActive(DateTime date)
        {
            var normalizedDate = date.Date;
            return normalizedDate >= FirstAvailableMonthDate && normalizedDate <= DateTime.Today;
        }

        private static DateTime GetInstallMonthDate()
        {
            var today = DateTime.Today;
            try
            {
                var persistentDataDirectory = new DirectoryInfo(Application.persistentDataPath);
                if (persistentDataDirectory.Exists && persistentDataDirectory.CreationTime.Date <= today)
                {
                    return new DateTime(persistentDataDirectory.CreationTime.Year, persistentDataDirectory.CreationTime.Month, 1);
                }
            }
            catch
            {
            }

            return new DateTime(today.Year, today.Month, 1);
        }

        private bool IsCompleted(DateTime date)
        {
            return _model.CompletedDays.Contains(ToKey(date));
        }

        private CultureInfo GetCurrentCulture()
        {
            var selectedLanguage = _localizationService != null
                ? _localizationService.GetCurrentLanguage()
                : Application.systemLanguage;

            var locale = LocalizationSettings.AvailableLocales.GetLocale(selectedLanguage);
            return locale != null && locale.Identifier.CultureInfo != null
                ? locale.Identifier.CultureInfo
                : CultureInfo.CurrentCulture;
        }

        private string GetMonthName(DateTime monthDate)
        {
            return monthDate.ToString("MMMM", GetCurrentCulture());
        }

        private AwardState GetAwardState(DateTime monthDate)
        {
            if (monthDate > CurrentMonthDate)
                return AwardState.Inactive;

            if (IsMonthCompleted(monthDate))
                return AwardState.Completed;

            return HasAnyCompletedDayInMonth(monthDate) ? AwardState.Active : AwardState.Inactive;
        }

        private bool IsMonthCompleted(DateTime monthDate)
        {
            var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(monthDate.Year, monthDate.Month, day);
                if (!IsCompleted(date))
                    return false;
            }

            return monthDate <= CurrentMonthDate;
        }

        private bool HasAnyCompletedDayInMonth(DateTime monthDate)
        {
            var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(monthDate.Year, monthDate.Month, day);
                if (date > DateTime.Today)
                    break;

                if (IsCompleted(date))
                    return true;
            }

            return false;
        }

        private void CompleteDay(DateTime date)
        {
            AddCompleted(date);
            _savedDataService.SaveData(_model);
        }

        private void AddCompleted(DateTime date)
        {
            var key = ToKey(date);
            if (!_model.CompletedDays.Contains(key))
            {
                _model.CompletedDays.Add(key);
            }
        }

        private static string ToKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

    }
}