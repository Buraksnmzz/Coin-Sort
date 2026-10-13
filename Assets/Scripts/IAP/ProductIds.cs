using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace IAP
{
    // Central place to keep product id strings used across the project.
    // Replace the placeholder ids below with the real ids from your store/catalog.
    public static class ProductIds
    {
        public const string CoinPack1 = "dropmarble_pack_1";
        public const string CoinPack2 = "dropmarble_pack_2";
        public const string CoinPack3 = "dropmarble_pack_3";
        public const string CoinPack4 = "dropmarble_pack_4";
        public const string CoinPack5 = "dropmarble_pack_5";
        public const string NoAdsOnly = "dropmarble_noads";
        public const string NoAdsPack = "dropmarble_noadspack"; 

        public static readonly Dictionary<string, ProductType> ProductTypeMap = new Dictionary<string, ProductType>
        {
            { CoinPack1, ProductType.Consumable },
            { CoinPack2, ProductType.Consumable },
            { CoinPack3, ProductType.Consumable },
            { CoinPack4, ProductType.Consumable },
            { CoinPack5, ProductType.Consumable },
            { NoAdsPack, ProductType.NonConsumable },
            { NoAdsOnly, ProductType.NonConsumable }
        };
    }
}