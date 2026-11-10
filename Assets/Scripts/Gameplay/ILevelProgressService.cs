using General;

namespace Gameplay
{
    public interface ILevelProgressService : IService
    {
        int CurrentLevel { get; }
        int CurrentBoardLevel { get; }
        float Progress { get; }
        int GetBoardIndex();
        void SetDebugBoard(int boardIndex);
        bool InitializeBoard(int boardIndex, int highestTier);
        BoardState RestoreBoardState(BoardState generatedBoardState);
        void SaveBoardState(BoardState boardState);
        void ResetLevelProgress();
        bool RegisterCoinCreated(int tier);
        void RegisterHighestTierCoinsDestroyed(int tier, int count);
        void Flush();
    }
}
