using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SettingsCard : MonoBehaviour
{
    [SerializeField] private Vector2 onPosition = new Vector2(33, 0);
    [SerializeField] private Vector2 offPosition = new Vector2(-33, 0);
    [SerializeField] private float tweenDuration = 1f;
    [SerializeField] private Image toggleBackImage;
    [SerializeField] private Sprite toggleOnSprite;
    [SerializeField] private Sprite toggleOffSprite;
    [SerializeField] private RectTransform handle;
    private bool _isOn;

    
    public void SetState(bool isOn, bool animate = true)
    {
        _isOn = isOn;
        ApplyVisualState(animate);
    }
        
    private void ApplyVisualState(bool animate)
    {
        var target = _isOn ? onPosition : offPosition;
        if (animate)
        {
            handle.DOAnchorPos(target, tweenDuration).OnComplete(() =>
            {
                toggleBackImage.sprite = _isOn ? toggleOnSprite : toggleOffSprite;
            });
        }
        else
        {
            handle.anchoredPosition = target;
            toggleBackImage.sprite = _isOn ? toggleOnSprite : toggleOffSprite;
        }
    }
}