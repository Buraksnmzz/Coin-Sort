using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using SavedData;
using Sound;
using UI.General;
using UI.Settings;
using UI.Shop;
using UnityEngine;

namespace Shop
{
    public class GetCoinsPresenter : BasePresenter<GetCoinsView>
    {
        ISavedDataService _savedDataService;
        IEventDispatcherService _eventDispatcherService;
        ISoundService _soundService;
        IHapticService _hapticService;
        IAdsService _adsService;
        IUIService _uiService;
        private Transform _buttonTransform;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _eventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            _soundService = ServiceLocator.GetService<ISoundService>();
            _eventDispatcherService.AddListener<RewardGivenSignal>(OnRewardGiven);
            _eventDispatcherService.AddListener<ShopRewardClosedSignal>(OnShopRewardClosed);
            _hapticService = ServiceLocator.GetService<IHapticService>();
            _adsService = ServiceLocator.GetService<IAdsService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.RewardedVideoButtonClicked += OnRewardedVideoButtonClicked;
        }

        private void OnRewardedVideoButtonClicked()
        {
            _adsService.GetReward(CallbackReward);
        }

        private void CallbackReward(bool success)
        {
            if (success)
            {
                var configModel = _savedDataService.GetModel<RemoteConfigModel>();
                var collectibleModel = _savedDataService.GetModel<CollectibleModel>();
                _soundService.PlaySound(ClipName.ShopPurchase);
                _hapticService.HapticLow();
                collectibleModel.TotalCoins += configModel.ShopCoinVideoReward;
                _savedDataService.SaveData(collectibleModel);
                _eventDispatcherService.Dispatch(new CoinChangedSignal());
                View.PlayCoinFly(collectibleModel.TotalCoins, View.rewardedVideoButton.transform);
            }
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _eventDispatcherService.AddListener<CoinChangedSignal>(OnCoinChanged);
            View.SetCoinCount(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }

        public override void ViewHidden()
        {
            if (_eventDispatcherService != null)
                _eventDispatcherService.RemoveListener<CoinChangedSignal>(OnCoinChanged);

            base.ViewHidden();
        }

        public override void Cleanup()
        {
            if (View != null)
                View.RewardedVideoButtonClicked -= OnRewardedVideoButtonClicked;

            if (_eventDispatcherService != null)
            {
                _eventDispatcherService.RemoveListener<RewardGivenSignal>(OnRewardGiven);
                _eventDispatcherService.RemoveListener<ShopRewardClosedSignal>(OnShopRewardClosed);
                _eventDispatcherService.RemoveListener<CoinChangedSignal>(OnCoinChanged);
            }

            base.Cleanup();
        }

        private void OnCoinChanged(CoinChangedSignal _)
        {
            View.SetCoinCount(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }

        private void OnRewardGiven(RewardGivenSignal rewardGivenSignal)
        {
            _soundService.PlaySound(ClipName.ShopPurchase);
            _hapticService.HapticLow();
            _buttonTransform = rewardGivenSignal.ButtonTransform;
        }

        private void OnShopRewardClosed(ShopRewardClosedSignal shopRewardClosedSignal)
        {
            View.PlayCoinFly(_savedDataService.GetModel<CollectibleModel>().TotalCoins, _buttonTransform);
        }
    }
}