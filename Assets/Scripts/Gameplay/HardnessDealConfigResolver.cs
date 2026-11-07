using Newtonsoft.Json.Linq;

namespace Gameplay
{
    public enum HardnessDealType
    {
        None,
        Hard,
        AutoMerge,
        Magic,
        Smart
    }

    public readonly struct HardnessDealConfig
    {
        public HardnessDealType DealType { get; }
        public float Value { get; }

        public HardnessDealConfig(HardnessDealType dealType, float value)
        {
            DealType = dealType;
            Value = value;
        }
    }

    public static class HardnessDealConfigResolver
    {
        public static HardnessDealConfig Resolve(string rawJson, int level)
        {
            var levels = JObject.Parse(rawJson)["levels"];

            foreach (var levelConfig in levels)
            {
                if (levelConfig.Value<int>("level_id") != level)
                {
                    continue;
                }

                if (TryGetDeal(levelConfig, "hard_deal_a", HardnessDealType.Hard, out var config) ||
                    TryGetDeal(levelConfig, "auto_merge_a", HardnessDealType.AutoMerge, out config) ||
                    TryGetDeal(levelConfig, "magic_deal_a", HardnessDealType.Magic, out config) ||
                    TryGetDeal(levelConfig, "smart_deal_a", HardnessDealType.Smart, out config))
                {
                    return config;
                }
            }

            return new HardnessDealConfig(HardnessDealType.None, 0f);
        }

        private static bool TryGetDeal(JToken levelConfig, string propertyName, HardnessDealType dealType,
            out HardnessDealConfig config)
        {
            var valueToken = levelConfig[propertyName];

            if (valueToken == null)
            {
                config = default;
                return false;
            }

            config = new HardnessDealConfig(dealType, valueToken.Value<float>());
            return true;
        }
    }
}
