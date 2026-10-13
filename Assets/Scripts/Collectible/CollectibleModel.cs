using SavedData;

namespace Collectible
{
    public class CollectibleModel : IModel
    {
        public int TotalCoins = 100;
        public int TotalHints = 3;
        public int TotalXp = 0;
        public int CurrentBadgeIndex = 0;
        public int TotalPins = 0;
        public int TotalSuperPins = 0;
        public bool HasFreePin;
        public bool HasFreeSuperPin;
    }
}