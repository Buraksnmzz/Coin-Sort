using General;

namespace Gameplay
{
    public interface IDynamicDifficultyService : IService
    {
        bool IsActive { get; }
        float CurrentDifficulty { get; }
        HardnessDealConfig GetDealConfig();
        void BeginAttempt();
        void RegisterProgress(float progress);
        void RegisterWin(int boardIndex);
        void RegisterLoss();
        void UpdateEasyPhase(BoardState boardState);
    }
}
