using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Gameplay/Board Config")]
    public class BoardConfig : ScriptableObject
    {
        [SerializeField] private int slotCount = 15;
        [SerializeField] private int slotCapacity = 10;
        [SerializeField] private int rentableSlotIndex = 14;
        [SerializeField] private int rentableSlotDurationSeconds = 60;
        [SerializeField] private int boardOneInitialUnlockedSlotCount = 6;
        [SerializeField] private int subsequentBoardsInitialUnlockedSlotCount = 7;
        [SerializeField] private int firstBoardChangeLevel = 28;
        [SerializeField] private int boardChangeLevelInterval = 24;

        public int SlotCount => slotCount;
        public int SlotCapacity => slotCapacity;
        public int RentableSlotIndex => rentableSlotIndex;
        public int RentableSlotDurationSeconds => rentableSlotDurationSeconds;
        public int BoardOneInitialUnlockedSlotCount => boardOneInitialUnlockedSlotCount;
        public int SubsequentBoardsInitialUnlockedSlotCount => subsequentBoardsInitialUnlockedSlotCount;
        public int FirstBoardChangeLevel => firstBoardChangeLevel;
        public int BoardChangeLevelInterval => boardChangeLevelInterval;
    }
}
