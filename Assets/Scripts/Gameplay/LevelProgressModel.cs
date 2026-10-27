using SavedData;

namespace Gameplay
{
    public class LevelProgressModel : IModel
    {
        public int CurrentLevel = 1;
        public float ProgressPercentage;
        public int HighestCreatedTier;
        public int CurrentBoardIndex;
        public int PhaseIndex;
    }
}
