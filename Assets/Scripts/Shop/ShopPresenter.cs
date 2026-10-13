using Collectible;
using General;
using General.EventDispatcher;
using SavedData;
using Sound;
using UI.General;
using UI.Shop;
using UnityEngine;

namespace Shop
{
    public class ShopPresenter : BasePresenter<ShopView>
    {
        ISavedDataService _savedDataService;
        IEventDispatcherService _eventDispatcherService;
        ISoundService _soundService;
        IHapticService _hapticService;
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
        }

        private void OnShopRewardClosed(ShopRewardClosedSignal shopRewardClosedSignal)
        {
            if (shopRewardClosedSignal.isNoAdsOnly)
            {
                View.Hide();
                return;
            }

            View.PlayCoinFly(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
        }

        private void OnRewardGiven(RewardGivenSignal rewardGivenSignal)
        {
            _soundService.PlaySound(ClipName.ShopPurchase);
            _hapticService.HapticLow();
        }

        public override void ViewShown()
        {
            base.ViewShown();
            _eventDispatcherService.AddListener<CoinChangedSignal>(OnCoinChanged);
            View.SetCoinCount(_savedDataService.GetModel<CollectibleModel>().TotalCoins);
            YoogoLabManager.HideBanner();
        }

        public override void ViewHidden()
        {
            if (_eventDispatcherService != null)
                _eventDispatcherService.RemoveListener<CoinChangedSignal>(OnCoinChanged);

            base.ViewHidden();
        }

        public override void Cleanup()
        {
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
    }
}