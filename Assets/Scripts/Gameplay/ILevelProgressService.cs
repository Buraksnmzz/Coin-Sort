using General;

namespace Gameplay
{
    public interface ILevelProgressService : IService
    {
        int CurrentLevel { get; }
        int CurrentBoardLevel { get; }
        float Progress { get; }
        int GetBoardIndex();
        bool InitializeBoard(int boardIndex, int highestTier);
        bool RegisterCoinCreated(int tier);
    }
}
