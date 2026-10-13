using General;
using General.EventDispatcher;
using IAP;
using SavedData;
using UI.General;
using UI.Settings;
using UnityEngine;

namespace RemoveAds
{
    public class RemoveAdsPresenter: BasePresenter<RemoveAdsView>
    {
        private IUIService _uiService;
        private IIAPService _iapService;
        private ISavedDataService _savedDataService;
        private IEventDispatcherService _eventDispatcherService;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _uiService = ServiceLocator.GetService<IUIService>();
            _iapService = ServiceLocator.GetService<IIAPService>();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            View.RemoveAdsButtonClicked += OnRemoveAdsButtonClicked;
            View.PlayWithAdsButtonButtonClicked += OnPlayWithAdsButtonClicked;
        }

        private void OnPlayWithAdsButtonClicked()
        {
            View.Hide();
            YoogoLabManager.ShowInterstitial();
            Debug.Log("IS Shown");
            //_uiService.ShowPopup<WinAnimPresenter>();
        }

        private void OnRemoveAdsButtonClicked()
        {
            _iapService.Purchase(ProductIds.NoAdsOnly, GiveReward);
        }
        
        private void GiveReward(bool success)
        {
            if (success)
            {
                var settingsModel = _savedDataService.GetModel<SettingsModel>();
                settingsModel.IsNoAds = true;
                _savedDataService.SaveData(settingsModel);
                _eventDispatcherService.Dispatch(new BannerVisibilityChangedSignal(false));
            }
            else
            {
                YoogoLabManager.ShowInterstitial();
                Debug.Log("IS Shown");
            }
            //_uiService.ShowPopup<WinAnimPresenter>();
            View.Hide();
        }
    }
}