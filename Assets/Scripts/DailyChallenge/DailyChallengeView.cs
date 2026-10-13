using System;
using System.Collections.Generic;
using DailyChallenge.Award;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeView : BaseView
    {
        public event Action PreviousMonthClicked;
        public event Action NextMonthClicked;
        public event Action PlayClicked;
        public event Action AwardsClicked;
        public event Action<int> DayClicked;
        public event Action DebugCompleteDayClicked;
        public event Action DebugResetAllDaysClicked;
        public event Action DebugCompleteAllDaysClicked;
        public event Action BackButtonClicked;
        public event Action CompletedDayPulseCompleted;

        [SerializeField] private Button previousMonthButton;
        [SerializeField] private Button nextMonthButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button awardsButton;
        [SerializeField] private Button debugCompleteDayButton;
        [SerializeField] private Button debugResetAllDaysButton;
        [SerializeField] private Button debugCompleteAllDaysButton;
        [SerializeField] private TextMeshProUGUI monthText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Transform dayContainer;
        [SerializeField] private DailyChallengeDayView dayPrefab;
        [SerializeField] private TMP_Text[] weekdayHeaderLabels;
        [SerializeField] private Image awardImageActive;
        [SerializeField] private Image awardImageInactive;
        [SerializeField] private Image awardImageCompleted;
        [SerializeField] private RectTransform rewardParent;
        [SerializeField] private AwardMonthSpriteConfig awardMonthSpriteConfig;

        private readonly List<DailyChallengeDayView> _dayViews = new List<DailyChallengeDayView>();

        private void Start()
        {
            previousMonthButton.onClick.AddListener(() => PreviousMonthClicked?.Invoke());
            nextMonthButton.onClick.AddListener(() => NextMonthClicked?.Invoke());
            playButton.onClick.AddListener(() => PlayClicked?.Invoke());
            awardsButton.onClick.AddListener(() => AwardsClicked?.Invoke());
            debugCompleteDayButton.onClick.AddListener(() => DebugCompleteDayClicked?.Invoke());
            debugResetAllDaysButton.onClick.AddListener(() => DebugResetAllDaysClicked?.Invoke());
            debugCompleteAllDaysButton.onClick.AddListener(() => DebugCompleteAllDaysClicked?.Invoke());
            backButton.onClick.AddListener(() => BackButtonClicked?.Invoke());
        }

        protected override void OnShown()
        {
            base.OnShown();
            StartAutoLoopButtonAnimation();
        }

        public void SetMonthText(string value)
        {
            monthText.text = value;
        }

        public void SetWeekdayHeaderLabels(IReadOnlyList<string> weekdayLabels)
        {
            if (weekdayHeaderLabels == null || weekdayLabels == null)
                return;

            var count = Mathf.Min(weekdayHeaderLabels.Length, weekdayLabels.Count);
            for (var index = 0; index < count; index++)
            {
                if (weekdayHeaderLabels[index] != null)
                    weekdayHeaderLabels[index].text = weekdayLabels[index];
            }
        }

        public void SetNavigationButtons(bool canGoPrevious, bool canGoNext)
        {
            previousMonthButton.interactable = canGoPrevious;
            nextMonthButton.interactable = canGoNext;
        }

        public void SetPlayButton(bool isInteractable)
        {
            playButton.gameObject.SetActive(isInteractable);
            if (isInteractable)
            {
                rewardParent.sizeDelta = new Vector2(400, 400);
            }
            else
            {
                rewardParent.sizeDelta = new Vector2(540, 540);
            }
        }

        public void SetProgress(int completedCount, int totalCount)
        {
            progressText.text = completedCount + "/" + totalCount;
        }

        public void SetAward(int month, AwardState state)
        {
            SetImageSprite(awardImageActive, awardMonthSpriteConfig != null ? awardMonthSpriteConfig.GetActiveSprite(month) : null);
            SetImageSprite(awardImageCompleted, awardMonthSpriteConfig != null ? awardMonthSpriteConfig.GetCompletedSprite(month) : null);

            SetImageActive(awardImageActive, state == AwardState.Active);
            SetImageActive(awardImageInactive, state == AwardState.Inactive);
            SetImageActive(awardImageCompleted, state == AwardState.Completed);
        }

        public void SetDays(IReadOnlyList<DailyChallengeDayModel> days)
        {
            ClearDays();

            if (dayPrefab == null || dayContainer == null)
                return;

            foreach (var day in days)
            {
                var dayView = Instantiate(dayPrefab, dayContainer);
                dayView.Clicked += OnDayClicked;
                dayView.SetDay(day);
                _dayViews.Add(dayView);
            }
        }

        public void PlayCompletedDayPulse(int day)
        {
            foreach (var dayView in _dayViews)
            {
                if (dayView == null || !dayView.IsDay(day))
                    continue;

                dayView.PlayCompletedPulse(() => CompletedDayPulseCompleted?.Invoke());
                return;
            }

            CompletedDayPulseCompleted?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (previousMonthButton != null)
                previousMonthButton.onClick.RemoveAllListeners();
            if (nextMonthButton != null)
                nextMonthButton.onClick.RemoveAllListeners();
            if (playButton != null)
                playButton.onClick.RemoveAllListeners();
            if (debugCompleteDayButton != null)
                debugCompleteDayButton.onClick.RemoveAllListeners();
            if (debugResetAllDaysButton != null)
                debugResetAllDaysButton.onClick.RemoveAllListeners();
            if (debugCompleteAllDaysButton != null)
                debugCompleteAllDaysButton.onClick.RemoveAllListeners();
            if (awardsButton != null)
                awardsButton.onClick.RemoveAllListeners();
            if (backButton != null)
                backButton.onClick.RemoveAllListeners();

            ClearDays();

            base.OnDestroy();
        }

        private void ClearDays()
        {
            foreach (var dayView in _dayViews)
            {
                if (dayView == null)
                    continue;

                dayView.Clicked -= OnDayClicked;
                Destroy(dayView.gameObject);
            }

            _dayViews.Clear();
        }

        private void OnDayClicked(int day)
        {
            DayClicked?.Invoke(day);
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
        }

        private static void SetImageActive(Image image, bool isActive)
        {
            if (image != null)
                image.gameObject.SetActive(isActive);
        }
    }
}