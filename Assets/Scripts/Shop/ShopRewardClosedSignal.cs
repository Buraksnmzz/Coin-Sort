using General.EventDispatcher;

namespace Shop
{
    public class ShopRewardClosedSignal : ISignal
    {
        public bool isNoAdsOnly;

        public ShopRewardClosedSignal(bool isNoAdsOnly)
        {
            this.isNoAdsOnly = isNoAdsOnly;
        }
    }
}