using General;
using General.EventDispatcher;
using UI.General;
using UI.Shop;

namespace Shop
{
    public class ShopRewardPresenter: BasePresenterWithData<ShopRewardView, ShopRewardData>
    {
        IEventDispatcherService _eventDispatcher;
        IUIService _uiService;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _eventDispatcher = ServiceLocator.GetService<IEventDispatcherService>();
            _uiService = ServiceLocator.GetService<IUIService>();
            View.OkClicked += OnOkClicked;
        }

        private void OnOkClicked()
        {
            _uiService.HidePopup<ShopRewardPresenter>();
            _eventDispatcher.Dispatch(new ShopRewardClosedSignal(Data.RewardType == RewardType.NoAds));
        }

        protected override void OnDataSet()
        {
            base.OnDataSet();
            View.HideAllRewards();
            if (Data.RewardType == RewardType.NoAds)
                View.ShowNoAds();
            else if (Data.RewardType == RewardType.Pack)
                View.ShowPack(Data.CoinReward);
            else if (Data.RewardType == RewardType.Coin)
                View.ShowCoin(Data.CoinReward);
        }
    }
}