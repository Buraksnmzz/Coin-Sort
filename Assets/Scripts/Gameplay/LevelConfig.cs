using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Gameplay/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private int[] boardOneSlotUnlockLevels = { 2, 3, 5, 11, 15, 18, 22, 27 };
        [SerializeField] private int[] boardTwoSlotUnlockLevels = { 29, 34, 37, 40, 43, 46, 52 };
        [SerializeField] private int[] boardThreeSlotUnlockLevels = { 53, 58, 62, 64, 68, 71, 76 };
        [SerializeField] private int loopStartLevel = 77;
        [SerializeField] private int levelLoopLength = 144;

        public int[] BoardOneSlotUnlockLevels => boardOneSlotUnlockLevels;
        public int[] BoardTwoSlotUnlockLevels => boardTwoSlotUnlockLevels;
        public int[] BoardThreeSlotUnlockLevels => boardThreeSlotUnlockLevels;
        public int LoopStartLevel => loopStartLevel;
        public int LevelLoopLength => levelLoopLength;

        public int[] GetSlotUnlockLevels(int boardIndex)
        {
            if (boardIndex == 1)
            {
                return boardOneSlotUnlockLevels;
            }

            if (boardIndex == 2)
            {
                return boardTwoSlotUnlockLevels;
            }

            var levelOffset = (boardIndex - 3) * 24;
            var unlockLevels = new int[boardThreeSlotUnlockLevels.Length];

            for (var index = 0; index < unlockLevels.Length; index++)
            {
                unlockLevels[index] = boardThreeSlotUnlockLevels[index] + levelOffset;
            }

            return unlockLevels;
        }
    }
}
