using General;
using UI.Settings;
using UI.Shop;

namespace Shop
{
    public class ShopNoAdsOnlyButton: ShopPurchaseButton
    {
        protected override void GiveReward(bool success)
        {
            if (success)
            {
                var settingsModel = SavedDataService.GetModel<SettingsModel>();
                settingsModel.IsNoAds = true;
                var shopRewardData = new ShopRewardData
                {
                    IsNoAds = true,
                    RewardType = RewardType.NoAds,
                };
                UIService.ShowPopup<ShopRewardPresenter, ShopRewardData>(shopRewardData);
                SavedDataService.SaveData(settingsModel);
                EventDispatcherService.Dispatch(new RewardGivenSignal(transform));
                EventDispatcherService.Dispatch(new BannerVisibilityChangedSignal(false));
            }
        }

        public override void SetRewardValue()
        {
            
        }
    }
}