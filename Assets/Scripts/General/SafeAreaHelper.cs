using General;
using General.EventDispatcher;
using SavedData;
using UI.Settings;
using UnityEngine;

public class SafeAreaHelper : MonoBehaviour
{
    private RectTransform _rectTransform;
    private bool _isBannerVisible;
    private IEventDispatcherService _eventDispatcherService;
    private ISavedDataService _savedDataService;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
        _savedDataService = ServiceLocator.GetService<ISavedDataService>();
        _eventDispatcherService.AddListener<BannerVisibilityChangedSignal>(OnBannerVisibilityChanged);
        _isBannerVisible = !_savedDataService.GetModel<SettingsModel>().IsNoAds;
        Refresh();
    }
        
    private void OnBannerVisibilityChanged(BannerVisibilityChangedSignal signal)
    {
        _isBannerVisible = signal.Visible;
        if(!_isBannerVisible)
            YoogoLabManager.HideBanner();
        Refresh();
    }

    private void Refresh()
    {
        var safeArea = Screen.safeArea;
        float screenHeight = Screen.height;
        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        var bannerHeightPercent = _isBannerVisible ? YoogoLabManager.GetBannerHeightPercent() : 0f;
        float bannerHeight = screenHeight * bannerHeightPercent;
        anchorMin.y = (safeArea.y + bannerHeight) / screenHeight;
        anchorMin.x /= Screen.width;
        anchorMax.x /= Screen.width;
        anchorMax.y = (safeArea.y + safeArea.height) / screenHeight;
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
}