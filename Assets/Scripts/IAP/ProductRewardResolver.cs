using GameConfig;

namespace IAP
{
    public static class ProductRewardResolver
    {
        public static CatalogProduct Resolve(RemoteConfigModel configModel, string productId)
        {

            if (productId == ProductIds.CoinPack1)
            {
                return new CatalogProduct { Coins = configModel.ShopCoinReward1 };
            }
            
            return new CatalogProduct();
        }
    }

    public class CatalogProduct
    {
        public int Coins;
    }
}