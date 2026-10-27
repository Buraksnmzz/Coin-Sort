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
        BoardState RestoreBoardState(BoardState generatedBoardState);
        void SaveBoardState(BoardState boardState);
        bool RegisterCoinCreated(int tier);
        void Flush();
    }
}
