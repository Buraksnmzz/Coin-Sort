using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeWinView : BaseView
    {
        public event Action ContinueClicked;

        [SerializeField] private Button continueButton;
        [SerializeField] private Transform headerImage;
        [SerializeField] private Transform middleImage;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private float stepDelay = 0.25f;
        [SerializeField] private float scaleDuration = 0.45f;
        [SerializeField] private float textFadeDuration = 0.35f;
        [SerializeField] private float headerStartScale = 2f;

        private Sequence _animationSequence;
        private CanvasGroup _headerCanvasGroup;

        private void Start()
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        public override void Show()
        {
            base.Show();
            PrepareVisuals();
        }

        protected override void OnShown()
        {
            base.OnShown();
            AnimateVisuals();
        }

        protected override void OnDestroy()
        {
            _animationSequence?.Kill();

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            }

            base.OnDestroy();
        }

        public void SetDateText(string text)
        {
            dateText.text = text;
        }

        private void OnContinueButtonClicked()
        {
            ContinueClicked?.Invoke();
        }

        private void AnimateVisuals()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();

            _animationSequence.Insert(0f, headerImage.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));

            if (_headerCanvasGroup != null)
            {
                _animationSequence.Insert(0f, _headerCanvasGroup.DOFade(1f, scaleDuration).SetEase(Ease.Linear));
            }

            _animationSequence.Insert(stepDelay, middleImage.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
            _animationSequence.Insert(stepDelay * 2f, dateText.DOFade(1f, textFadeDuration).SetEase(Ease.Linear));
            _animationSequence.Insert(stepDelay * 3f, continueButton.transform.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));
        }

        private void PrepareVisuals()
        {
            _headerCanvasGroup = GetOrAddCanvasGroup(headerImage);

            headerImage.localScale = Vector3.one * headerStartScale;
            middleImage.localScale = Vector3.zero;
            continueButton.transform.localScale = Vector3.zero;
            dateText.alpha = 0f;

            if (_headerCanvasGroup != null)
            {
                _headerCanvasGroup.alpha = 0f;
            }
        }

        private static CanvasGroup GetOrAddCanvasGroup(Component target)
        {
            if (target == null)
            {
                return null;
            }

            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }
    }
}