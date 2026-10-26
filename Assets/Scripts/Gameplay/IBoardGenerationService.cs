using General;

namespace Gameplay
{
    public interface IBoardGenerationService : IService
    {
        BoardState CreateBoard(int boardIndex, int currentLevel);
    }
}
