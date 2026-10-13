using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Core.Scripts.Services;
using General;
using Sound;
using UI.General;
using Sequence = DG.Tweening.Sequence;

public abstract class BaseView : MonoBehaviour, IView
{
    [SerializeField] protected Image backgroundImage;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private PopupAnimationType animationType = PopupAnimationType.Fade;
    [SerializeField] protected RectTransform panel;
    [SerializeField] private float fadeEndValue = 0.985f;
    [SerializeField] private bool shouldMoveBackgroundToParent = false;
    [SerializeField] private Button autoLoopButton;
    [SerializeField] private float autoLoopButtonScaleMultiplier = 1.08f;
    [SerializeField] private float autoLoopButtonScaleDuration = 0.8f;

    private bool _isVisible;
    protected Sequence _currentAnimation;
    private Tween _autoLoopButtonTween;
    private Vector2 _originalPanelPosition;
    private Vector3 _originalPanelScale;
    private float _moveOffset;
    private RectTransform _backgroundRectTransform;
    private bool _isBackgroundAttachedToCanvas;
    private Transform _backgroundOriginalParent;
    private int _backgroundOriginalSiblingIndex;
    private Transform _backgroundsRoot;
    private CanvasGroup _panelCanvasGroup;
    private ISoundService _soundService;
    private IHapticService _hapticService;
    private List<Button> _subscribedButtons;
    private PopupAnimationType? _nextShowAnimationOverride;
    private PopupAnimationType? _nextHideAnimationOverride;
    private bool? _nextShowShouldAnimate;
    private bool? _nextHideShouldAnimate;
    private Vector3 _autoLoopButtonOriginalScale;
    private bool _hasAutoLoopButtonOriginalScale;

    protected virtual void Awake()
    {
        if (backgroundImage != null)
        {
            _backgroundRectTransform = backgroundImage.rectTransform;
        }

        if (panel != null)
        {
            _originalPanelPosition = panel.anchoredPosition;
            _originalPanelScale = panel.localScale;

            var rect = panel.rect;
            _moveOffset = rect.height;

            _panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }
            _panelCanvasGroup.interactable = false;
            _panelCanvasGroup.blocksRaycasts = false;
            panel.anchoredPosition = _originalPanelPosition;
            panel.localScale = _originalPanelScale;
            _panelCanvasGroup.alpha = 1f;
        }

        RegisterButtonClickSounds();
        CacheAutoLoopButtonScale();
    }

    private void RegisterButtonClickSounds()
    {
        _hapticService = ServiceLocator.GetService<IHapticService>();
        _subscribedButtons = new List<Button>();
        try
        {
            _soundService = ServiceLocator.GetService<ISoundService>();
        }
        catch
        {
            _soundService = null;
        }

        if (_soundService == null) return;

        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            if (_subscribedButtons.Contains(btn)) continue;
            btn.onClick.AddListener(PlayButtonClickSound);
            _subscribedButtons.Add(btn);
        }
    }

    private void PlayButtonClickSound()
    {
        _soundService?.PlaySound(ClipName.ButtonClick);
        _hapticService.HapticMin();
    }

    /// <summary>
    /// Exclude a specific button from the automatic click sound behavior.
    /// Call this from derived views (e.g., in Start) for buttons that should remain silent.
    /// </summary>
    public void ExcludeButtonFromClickSound(Button btn)
    {
        if (btn == null || _subscribedButtons == null) return;
        try
        {
            btn.onClick.RemoveListener(PlayButtonClickSound);
        }
        catch { }
        _subscribedButtons.Remove(btn);
    }

    /// <summary>
    /// Exclude multiple buttons from automatic click sound.
    /// </summary>
    public void ExcludeButtonsFromClickSound(params Button[] buttons)
    {
        if (buttons == null) return;
        foreach (var b in buttons)
            ExcludeButtonFromClickSound(b);
    }

    public virtual void Show()
    {
        transform.SetAsLastSibling();
        if (_isVisible) return;

        _currentAnimation?.Kill();
        gameObject.SetActive(true);

        var effectiveAnimationType = ConsumeShowAnimationType();
        var shouldAnimate = ConsumeShowShouldAnimate();

        if (shouldMoveBackgroundToParent)
        {
            AttachBackgroundToCanvas();
            if (_backgroundRectTransform != null)
            {
                _backgroundRectTransform.SetAsLastSibling();
            }
        }

        if (!shouldAnimate)
        {
            ApplyShownState();
            OnShown();
            return;
        }

        PreparePanelForShow(effectiveAnimationType);

        _currentAnimation = DOTween.Sequence()
            .OnStart(() =>
            {
                if (backgroundImage != null)
                {
                    backgroundImage.raycastTarget = true;
                }
                if (_panelCanvasGroup != null)
                {
                    _panelCanvasGroup.interactable = false;
                    _panelCanvasGroup.blocksRaycasts = false;
                }
            });

        if (backgroundImage != null)
        {
            _currentAnimation.Join(backgroundImage.DOFade(fadeEndValue, animationDuration));
        }

        if (panel != null)
        {
            switch (effectiveAnimationType)
            {
                case PopupAnimationType.Fade:
                    if (_panelCanvasGroup != null)
                    {
                        _currentAnimation.Join(_panelCanvasGroup.DOFade(1f, animationDuration)
                            .SetEase(Ease.Linear));
                    }
                    break;

                case PopupAnimationType.MoveUp:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition, animationDuration)
                        .From(_originalPanelPosition + Vector2.down * _moveOffset)
                        .SetEase(Ease.OutBack));
                    break;

                case PopupAnimationType.MoveDown:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition, animationDuration)
                        .SetEase(Ease.OutBack));
                    break;

                case PopupAnimationType.MoveLeft:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition, animationDuration)
                        .SetEase(Ease.OutQuad));
                    break;

                case PopupAnimationType.MoveRight:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition, animationDuration)
                        .SetEase(Ease.OutQuad));
                    break;

                case PopupAnimationType.Scale:
                    _currentAnimation.Join(panel.DOScale(_originalPanelScale, animationDuration)
                        .SetEase(Ease.OutBack));
                    break;
            }
        }

        _currentAnimation.OnComplete(() =>
        {
            ApplyShownState();
            OnShown();
        });
    }

    public virtual void Hide()
    {
        if (!_isVisible && !gameObject.activeSelf) return;

        _currentAnimation?.Kill();

        var effectiveAnimationType = ConsumeHideAnimationType();
        var shouldAnimate = ConsumeHideShouldAnimate();

        if (!shouldAnimate)
        {
            ApplyHiddenState();
            OnHidden();
            return;
        }

        _currentAnimation = DOTween.Sequence()
            .OnStart(() =>
            {
                if (backgroundImage != null)
                {
                    backgroundImage.raycastTarget = false;
                }
                if (_panelCanvasGroup != null)
                {
                    _panelCanvasGroup.interactable = false;
                    _panelCanvasGroup.blocksRaycasts = false;
                }
            });

        if (backgroundImage != null)
        {
            _currentAnimation.Join(backgroundImage.DOFade(0, animationDuration));
        }

        if (panel != null)
        {
            switch (effectiveAnimationType)
            {
                case PopupAnimationType.Fade:
                    if (_panelCanvasGroup != null)
                    {
                        _currentAnimation.Join(_panelCanvasGroup.DOFade(0f, animationDuration)
                            .SetEase(Ease.Linear));
                    }
                    break;

                case PopupAnimationType.MoveUp:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition + Vector2.down * _moveOffset, animationDuration)
                        .SetEase(Ease.InBack));
                    break;

                case PopupAnimationType.MoveDown:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition + Vector2.up * _moveOffset, animationDuration)
                        .SetEase(Ease.InBack));
                    break;

                case PopupAnimationType.MoveLeft:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition + Vector2.right * _moveOffset, animationDuration)
                        .SetEase(Ease.InQuad));
                    break;

                case PopupAnimationType.MoveRight:
                    _currentAnimation.Join(panel.DOAnchorPos(_originalPanelPosition + Vector2.left * _moveOffset, animationDuration)
                        .SetEase(Ease.InQuad));
                    break;

                case PopupAnimationType.Scale:
                    _currentAnimation.Join(panel.DOScale(Vector3.zero, animationDuration)
                        .SetEase(Ease.InBack));
                    break;
            }
        }

        _currentAnimation.OnComplete(() =>
        {
            ApplyHiddenState();
            OnHidden();
        });
    }

    protected virtual void OnShown() { }

    protected virtual void OnHidden()
    {
        StopAutoLoopButtonAnimation();
    }

    protected virtual void OnDestroy()
    {
        _currentAnimation?.Kill();
        StopAutoLoopButtonAnimation();
        if (backgroundImage != null)
        {
            Destroy(backgroundImage.gameObject);
        }
        if (_subscribedButtons != null)
        {
            foreach (var btn in _subscribedButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveListener(PlayButtonClickSound);
            }
            _subscribedButtons.Clear();
            _subscribedButtons = null;
        }
    }

    public void SetNextShowAnimationOverride(PopupAnimationType animationOverride)
    {
        _nextShowAnimationOverride = animationOverride;
    }

    public void SetNextHideAnimationOverride(PopupAnimationType animationOverride)
    {
        _nextHideAnimationOverride = animationOverride;
    }

    public void SetNextShowShouldAnimate(bool shouldAnimate)
    {
        _nextShowShouldAnimate = shouldAnimate;
    }

    public void SetNextHideShouldAnimate(bool shouldAnimate)
    {
        _nextHideShouldAnimate = shouldAnimate;
    }

    private PopupAnimationType ConsumeShowAnimationType()
    {
        var resolvedAnimationType = _nextShowAnimationOverride ?? animationType;
        _nextShowAnimationOverride = null;
        return resolvedAnimationType;
    }

    private PopupAnimationType ConsumeHideAnimationType()
    {
        var resolvedAnimationType = _nextHideAnimationOverride ?? animationType;
        _nextHideAnimationOverride = null;
        return resolvedAnimationType;
    }

    private bool ConsumeShowShouldAnimate()
    {
        var shouldAnimate = _nextShowShouldAnimate ?? true;
        _nextShowShouldAnimate = null;
        return shouldAnimate;
    }

    private bool ConsumeHideShouldAnimate()
    {
        var shouldAnimate = _nextHideShouldAnimate ?? true;
        _nextHideShouldAnimate = null;
        return shouldAnimate;
    }

    private void ApplyShownState()
    {
        panel?.DOKill();

        if (backgroundImage != null)
        {
            var backgroundColor = backgroundImage.color;
            backgroundColor.a = fadeEndValue;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = true;
        }

        if (panel != null)
        {
            panel.anchoredPosition = _originalPanelPosition;
            panel.localScale = _originalPanelScale;
        }

        if (_panelCanvasGroup != null)
        {
            _panelCanvasGroup.alpha = 1f;
            _panelCanvasGroup.interactable = true;
            _panelCanvasGroup.blocksRaycasts = true;
        }

        _isVisible = true;
    }

    private void ApplyHiddenState()
    {
        panel?.DOKill();

        if (backgroundImage != null)
        {
            var backgroundColor = backgroundImage.color;
            backgroundColor.a = 0f;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
        }

        if (panel != null)
        {
            panel.anchoredPosition = _originalPanelPosition;
            panel.localScale = _originalPanelScale;
        }

        if (_panelCanvasGroup != null)
        {
            _panelCanvasGroup.alpha = 0f;
            _panelCanvasGroup.interactable = false;
            _panelCanvasGroup.blocksRaycasts = false;
        }

        _isVisible = false;
        gameObject.SetActive(false);
    }

    private void PreparePanelForShow(PopupAnimationType effectiveAnimationType)
    {
        if (panel == null)
        {
            return;
        }

        panel.DOKill();
        panel.anchoredPosition = _originalPanelPosition;
        panel.localScale = _originalPanelScale;

        if (_panelCanvasGroup != null)
        {
            _panelCanvasGroup.alpha = 1f;
        }

        switch (effectiveAnimationType)
        {
            case PopupAnimationType.Fade:
                if (_panelCanvasGroup != null)
                {
                    _panelCanvasGroup.alpha = 0f;
                }
                break;
            case PopupAnimationType.MoveUp:
                panel.anchoredPosition = _originalPanelPosition + Vector2.down * _moveOffset;
                break;
            case PopupAnimationType.MoveDown:
                panel.anchoredPosition = _originalPanelPosition + Vector2.up * _moveOffset;
                break;
            case PopupAnimationType.MoveLeft:
                panel.anchoredPosition = _originalPanelPosition + Vector2.right * _moveOffset;
                break;
            case PopupAnimationType.MoveRight:
                panel.anchoredPosition = _originalPanelPosition + Vector2.left * _moveOffset;
                break;
            case PopupAnimationType.Scale:
                panel.localScale = Vector3.zero;
                break;
        }
    }

    private void AttachBackgroundToCanvas()
    {
        if (_backgroundRectTransform == null)
        {
            return;
        }

        var canvas = backgroundImage.canvas;
        if (canvas == null)
        {
            return;
        }

        if (!_isBackgroundAttachedToCanvas)
        {
            _backgroundOriginalParent = _backgroundRectTransform.parent;
            _backgroundOriginalSiblingIndex = _backgroundRectTransform.GetSiblingIndex();
        }

        if (_backgroundsRoot == null)
        {
            var backgroundsTransform = canvas.transform.Find("Backgrounds");
            _backgroundsRoot = backgroundsTransform != null ? backgroundsTransform : canvas.transform;
        }

        _backgroundRectTransform.SetParent(_backgroundsRoot, false);
        _isBackgroundAttachedToCanvas = true;
    }

    protected void AssignAutoLoopButton(Button button)
    {
        if (autoLoopButton == button)
        {
            return;
        }

        StopAutoLoopButtonAnimation();
        autoLoopButton = button;
        CacheAutoLoopButtonScale();
    }

    protected void StartAutoLoopButtonAnimation()
    {
        StopAutoLoopButtonAnimation();

        if (autoLoopButton == null || !autoLoopButton.interactable)
        {
            return;
        }

        var buttonTransform = autoLoopButton.transform;
        CacheAutoLoopButtonScale();
        buttonTransform.localScale = _autoLoopButtonOriginalScale;
        _autoLoopButtonTween = buttonTransform
            .DOScale(_autoLoopButtonOriginalScale * autoLoopButtonScaleMultiplier, autoLoopButtonScaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    protected void StopAutoLoopButtonAnimation()
    {
        _autoLoopButtonTween?.Kill();
        _autoLoopButtonTween = null;

        if (autoLoopButton != null && _hasAutoLoopButtonOriginalScale)
        {
            autoLoopButton.transform.localScale = _autoLoopButtonOriginalScale;
        }
    }

    private void CacheAutoLoopButtonScale()
    {
        if (autoLoopButton == null)
        {
            _hasAutoLoopButtonOriginalScale = false;
            return;
        }

        var buttonScale = autoLoopButton.transform.localScale;
        if (buttonScale == Vector3.zero)
        {
            if (!_hasAutoLoopButtonOriginalScale)
            {
                _autoLoopButtonOriginalScale = Vector3.one;
                _hasAutoLoopButtonOriginalScale = true;
            }

            return;
        }

        _autoLoopButtonOriginalScale = buttonScale;
        _hasAutoLoopButtonOriginalScale = true;
    }
}
