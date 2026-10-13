using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeTutorialView : BaseView
    {
        [SerializeField] private Transform[] tutorialObjects;
        [SerializeField] private Button continueButton;
        [SerializeField] private float stepDelay = 0.25f;
        [SerializeField] private float scaleDuration = 0.5f;

        public event Action ContinueClicked;

        private Sequence _animationSequence;
        private Vector3[] _tutorialObjectOriginalScales;
        private Vector3 _continueButtonOriginalScale;

        protected override void Awake()
        {
            base.Awake();
            CacheOriginalScales();
        }

        private void Start()
        {
            if (continueButton != null)
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
            PlayIntroAnimation();
        }

        protected override void OnDestroy()
        {
            _animationSequence?.Kill();
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueButtonClicked);

            base.OnDestroy();
        }

        private void OnContinueButtonClicked()
        {
            ContinueClicked?.Invoke();
        }

        private void CacheOriginalScales()
        {
            if (tutorialObjects != null)
            {
                _tutorialObjectOriginalScales = new Vector3[tutorialObjects.Length];
                for (var i = 0; i < tutorialObjects.Length; i++)
                {
                    _tutorialObjectOriginalScales[i] = tutorialObjects[i] != null
                        ? tutorialObjects[i].localScale
                        : Vector3.one;
                }
            }

            _continueButtonOriginalScale = continueButton != null ? continueButton.transform.localScale : Vector3.one;
        }

        private void PrepareVisuals()
        {
            if (tutorialObjects != null)
            {
                for (var i = 0; i < tutorialObjects.Length; i++)
                {
                    if (tutorialObjects[i] == null)
                        continue;

                    tutorialObjects[i].localScale = Vector3.zero;
                }
            }

            if (continueButton != null)
            {
                continueButton.transform.localScale = Vector3.zero;
                continueButton.interactable = false;
            }
        }

        private void PlayIntroAnimation()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();

            if (tutorialObjects != null)
            {
                for (var i = 0; i < tutorialObjects.Length; i++)
                {
                    var tutorialObject = tutorialObjects[i];
                    if (tutorialObject == null)
                        continue;

                    var targetScale = i < _tutorialObjectOriginalScales.Length
                        ? _tutorialObjectOriginalScales[i]
                        : Vector3.one;

                    _animationSequence.Insert(i * stepDelay,
                        tutorialObject.DOScale(targetScale, scaleDuration).SetEase(Ease.OutBack));
                }
            }

            var continueStartTime = tutorialObjects != null ? tutorialObjects.Length * stepDelay : 0f;
            if (continueButton != null)
            {
                _animationSequence.Insert(continueStartTime,
                    continueButton.transform.DOScale(_continueButtonOriginalScale, scaleDuration)
                        .SetEase(Ease.OutBack)
                        .OnStart(() => continueButton.interactable = true));
            }

            _animationSequence.OnComplete(StartAutoLoopButtonAnimation);
        }
    }
}