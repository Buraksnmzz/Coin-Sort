using System;
using System.Collections.Generic;
using DG.Tweening;
using General;
using Sound;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.General
{
    public class CoinFlyAnimator : MonoBehaviour
    {
        [SerializeField] private Transform coinHolder;
        [SerializeField] private Transform targetCoinIcon;
        [SerializeField] private TextMeshProUGUI coinCountText;
        [SerializeField] private RectTransform coinPrefab;
        [SerializeField] private ParticleSystem coinParticle;
        [SerializeField] private int coinIconCount = 10;
        [SerializeField] private float holderIntroDuration = 0.35f;
        [SerializeField] private float holderIntroYOffset = 300f;
        [SerializeField] private float iconScaleDuration = 0.35f;
        [SerializeField] private float iconMoveDuration = 0.6f;
        [SerializeField] private float iconStagger = 0.04f;
        [SerializeField] private float iconRandomOffset = 3f;
        [SerializeField] private float targetPunchScale = 0.2f;
        [SerializeField] private float targetPunchDuration = 0.08f;
        [SerializeField] private bool playParticleOnFirstIconMoved = true;
        [SerializeField] private bool punchTargetOnIconMoved = true;
        private readonly ClipName coinCreatedClip = ClipName.CoinCreate;
        private readonly ClipName iconMovedClip = ClipName.CoinIncrease;

        public RectTransform parentTransform;

        public event Action CoinCreated;
        public event Action IconMoved;
        public event Action Completed;

        private readonly List<RectTransform> _activeIcons = new List<RectTransform>();
        private Sequence _coinSequence;
        private Tween _holderIntroTween;
        private Action _onCompleted;
        private Vector3 _holderInitialLocalPosition;
        private bool _hasHolderInitialLocalPosition;
        private bool _isPlaying;
        private int _finalCoinCount;
        private ISoundService _soundService;
        private IHapticService _hapticService;

        private int _remainingIcons;

        private void Awake()
        {
            TryResolveServices();
            CacheHolderPosition();
        }

        private void OnDestroy()
        {
            Complete(false);
            _holderIntroTween?.Kill();
        }

        public void SetCoinCount(int value)
        {
            if (coinCountText != null)
                coinCountText.text = value.ToString();
        }

        public void SetParentTransform(RectTransform value)
        {
            parentTransform = value;
        }

        public void PrepareHolderIntro()
        {
            if (coinHolder == null)
                return;

            CacheHolderPosition();
            coinHolder.localPosition = _holderInitialLocalPosition + Vector3.up * holderIntroYOffset;
        }

        public void PlayHolderIntro(float delay = 0f)
        {
            if (coinHolder == null)
                return;

            CacheHolderPosition();
            _holderIntroTween?.Kill();
            _holderIntroTween = coinHolder.DOLocalMove(_holderInitialLocalPosition, holderIntroDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack);
        }

        public void Play(Vector3 startPosition, int finalCoinCount, Action onCompleted = null)
        {
            Complete(false);

            if (coinIconCount <= 0 || coinPrefab == null || targetCoinIcon == null)
            {
                SetCoinCount(finalCoinCount);
                onCompleted?.Invoke();
                Completed?.Invoke();
                return;
            }

            _isPlaying = true;
            _finalCoinCount = finalCoinCount;
            _onCompleted = onCompleted;

            for (var index = 0; index < coinIconCount; index++)
            {
                var icon = Instantiate(coinPrefab, parentTransform);
                _activeIcons.Add(icon);
                icon.position = startPosition + new Vector3(
                    Random.Range(-iconRandomOffset, iconRandomOffset),
                    Random.Range(-iconRandomOffset, iconRandomOffset),
                    0f);
                icon.localScale = Vector3.zero;
            }

            CoinCreated?.Invoke();
            PlayCoinCreatedFeedback();
            _coinSequence = DOTween.Sequence();
            _remainingIcons = _activeIcons.Count;

            foreach (var icon in _activeIcons.ToArray())
            {
                var startTime = iconStagger * _activeIcons.IndexOf(icon);
                _coinSequence.Insert(startTime,
                    icon.DOScale(Vector3.one, iconScaleDuration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() => MoveIconToTarget(icon)));
            }
        }

        public void Complete(bool invokeCompleted = true)
        {
            if (!_isPlaying)
                return;

            _coinSequence?.Kill();
            _coinSequence = null;

            for (var index = 0; index < _activeIcons.Count; index++)
            {
                var icon = _activeIcons[index];
                if (icon == null)
                    continue;

                icon.DOKill();
                Destroy(icon.gameObject);
            }

            _activeIcons.Clear();
            FinalizeAnimation(invokeCompleted);
        }

        private void MoveIconToTarget(RectTransform icon)
        {
            if (icon == null || targetCoinIcon == null)
                return;

            icon.DOMove(targetCoinIcon.position, iconMoveDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => OnIconReachedTarget(icon));
        }

        private void OnIconReachedTarget(RectTransform icon)
        {
            IconMoved?.Invoke();
            PlayIconMovedFeedback();

            if (_activeIcons.Remove(icon))
                _remainingIcons--;

            Destroy(icon.gameObject);

            if (playParticleOnFirstIconMoved && _remainingIcons == coinIconCount - 1 && coinParticle != null)
                coinParticle.Play();

            if (_remainingIcons <= 0)
                FinalizeAnimation(true);
        }

        private void FinalizeAnimation(bool invokeCompleted)
        {
            if (!_isPlaying)
                return;

            _isPlaying = false;
            _coinSequence = null;
            SetCoinCount(_finalCoinCount);
            var onCompleted = _onCompleted;
            _onCompleted = null;

            if (!invokeCompleted)
                return;

            onCompleted?.Invoke();
            Completed?.Invoke();
        }

        private void CacheHolderPosition()
        {
            if (_hasHolderInitialLocalPosition || coinHolder == null)
                return;

            _holderInitialLocalPosition = coinHolder.localPosition;
            _hasHolderInitialLocalPosition = true;
        }

        private void PlayCoinCreatedFeedback()
        {
            _soundService?.PlaySound(coinCreatedClip);
        }

        private void PlayIconMovedFeedback()
        {
            if (punchTargetOnIconMoved && targetCoinIcon != null)
            {
                targetCoinIcon.DOComplete();
                targetCoinIcon.DOPunchScale(Vector3.one * targetPunchScale, targetPunchDuration);
            }

            _soundService?.PlaySound(iconMovedClip);
            _hapticService?.HapticMin();
        }

        private void TryResolveServices()
        {
            try
            {
                _soundService = ServiceLocator.GetService<ISoundService>();
            }
            catch
            {
                _soundService = null;
            }

            try
            {
                _hapticService = ServiceLocator.GetService<IHapticService>();
            }
            catch
            {
                _hapticService = null;
            }
        }
    }
}