using System;
using DailyChallenge.Award;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeReward1View : BaseView
    {
        [SerializeField] private Image headerImage;
        [SerializeField] private Image completedImage;
        [SerializeField] private TextMeshProUGUI completedText;
        [SerializeField] private Button continueButton;
        [SerializeField] private AwardMonthSpriteConfig awardMonthSpriteConfig;
        [SerializeField] private ParticleSystem particle;
        [SerializeField] private float stepDelay = 0.4f;
        [SerializeField] private float durations = 0.6f;
        [SerializeField] private float headerStartScale = 2f;

        public event Action ContinueClicked;
        public event Action CompletedImageAnimationStarted;
        private Sequence _animationSequence;

        private void Start()
        {
            continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
        }

        public void SetCompletedText(string text)
        {
            completedText.text = text;
        }

        public override void Show()
        {
            base.Show();
            SetRewardSprite();
            PrepareVisuals();
        }

        protected override void OnShown()
        {
            base.OnShown();
            AnimateVisuals();
        }

        private void AnimateVisuals()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();
            _animationSequence.Insert(0f, headerImage.transform.DOScale(1, durations).SetEase(Ease.OutBack));
            _animationSequence.Insert(0f, headerImage.DOFade(1f, durations).SetEase(Ease.Linear));
            _animationSequence.Insert(stepDelay, completedImage.transform.DOScale(1, durations * 2).SetEase(Ease.OutBack).OnStart(() =>
            {
                CompletedImageAnimationStarted?.Invoke();
                particle.Play();
            }));
            _animationSequence.Insert(stepDelay * 2f, completedText.DOFade(1f, durations).SetEase(Ease.Linear));
            _animationSequence.Insert(stepDelay * 3f, continueButton.transform.DOScale(1, durations).SetEase(Ease.OutBack));
        }

        protected override void OnDestroy()
        {
            _animationSequence?.Kill();
            if (continueButton != null)
                continueButton.onClick.RemoveAllListeners();

            base.OnDestroy();
        }

        private void PrepareVisuals()
        {
            headerImage.transform.localScale = Vector3.one * headerStartScale;
            var headerColor = headerImage.color;
            headerColor.a = 0f;
            headerImage.color = headerColor;
            completedImage.transform.localScale = Vector3.zero;
            completedText.alpha = 0f;
            continueButton.transform.localScale = Vector3.zero;
        }

        private void SetRewardSprite()
        {
            var rewardSprite = awardMonthSpriteConfig.GetCompletedSprite(DateTime.Today.Month);
            if (rewardSprite != null)
                completedImage.sprite = rewardSprite;
        }
    }
}