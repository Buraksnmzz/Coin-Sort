using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeDayView : MonoBehaviour
    {
        public event Action<int> Clicked;

        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private GameObject completedRoot;
        [SerializeField] private GameObject inactiveRoot;
        [SerializeField] private GameObject emptyRoot;
        [SerializeField] private Color normalTextColor = new Color(0.471f, 0.337f, 0.145f, 1f);
        [SerializeField] private Color selectedTextColor = Color.white;
        [SerializeField] private Color completedTextColor = Color.clear;
        [SerializeField] private Color futureTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private float completedPulseScale = 1.6f;
        [SerializeField] private float completedPulseDuration = 0.6f;

        private int _dayNumber;
        private Tween _completedPulseTween;
        private Vector3 _completedRootInitialScale = Vector3.one;

        private void Awake()
        {
            if (completedRoot != null)
            {
                _completedRootInitialScale = completedRoot.transform.localScale;
            }

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            _completedPulseTween?.Kill();

            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public void SetEmpty()
        {
            _dayNumber = 0;
            if (dayText != null)
            {
                dayText.text = string.Empty;
                dayText.gameObject.SetActive(false);
            }
            if (button != null)
                button.interactable = false;
            SetActive(selectedRoot, false);
            SetActive(completedRoot, false);
            SetActive(inactiveRoot, false);
            SetActive(emptyRoot, true);
        }

        public void SetDay(DailyChallengeDay day, bool isSelected)
        {
            _dayNumber = day.DayNumber;
            if (dayText != null)
            {
                dayText.text = day.DayNumber.ToString();
                dayText.gameObject.SetActive(!day.Completed);
                dayText.color = GetTextColor(day.Active, day.Completed, isSelected);
            }
            if (button != null)
                button.interactable = day.Active && !day.Completed;
            SetActive(selectedRoot, isSelected);
            SetActive(completedRoot, day.Completed);
            SetActive(inactiveRoot, !day.Active);
            SetActive(emptyRoot, false);
        }

        public void SetDay(DailyChallengeDayModel day)
        {
            if (day == null || day.Day <= 0)
            {
                SetEmpty();
                return;
            }

            _dayNumber = day.Day;
            if (dayText != null)
            {
                dayText.text = day.Day.ToString();
                dayText.gameObject.SetActive(!day.Completed);
                dayText.color = GetTextColor(day.Active, day.Completed, day.Selected);
            }
            if (button != null)
                button.interactable = day.Active && !day.Completed;
            SetActive(selectedRoot, day.Selected);
            SetActive(completedRoot, day.Completed);
            SetActive(inactiveRoot, !day.Active);
            SetActive(emptyRoot, false);
        }

        public bool IsDay(int day)
        {
            return _dayNumber == day;
        }

        public void PlayCompletedPulse(Action onComplete)
        {
            if (completedRoot == null || !completedRoot.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            _completedPulseTween?.Kill();
            completedRoot.transform.localScale = _completedRootInitialScale;
            _completedPulseTween = DOTween.Sequence()
                .Append(completedRoot.transform.DOScale(_completedRootInitialScale * completedPulseScale, completedPulseDuration).SetEase(Ease.OutBack).SetDelay(0.2f))
                .Append(completedRoot.transform.DOScale(_completedRootInitialScale, completedPulseDuration).SetEase(Ease.InOutSine))
                .OnComplete(() => onComplete?.Invoke());
        }

        private void OnButtonClicked()
        {
            if (_dayNumber <= 0)
                return;

            Clicked?.Invoke(_dayNumber);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }
        }

        private Color GetTextColor(bool active, bool completed, bool selected)
        {
            if (completed)
                return completedTextColor;
            if (selected)
                return selectedTextColor;
            if (!active)
                return futureTextColor;
            return normalTextColor;
        }
    }
}