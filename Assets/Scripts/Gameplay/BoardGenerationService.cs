using GameConfig;
using General;
using SavedData;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class BoardGenerationService : IBoardGenerationService
    {
        private const string BoardConfigResourcePath = "Gameplay/BoardConfig";
        private const string BoardInitialLayoutConfigResourcePath = "Gameplay/BoardInitialLayoutConfig";
        private const string LevelConfigResourcePath = "Gameplay/LevelConfig";

        private readonly BoardConfig _boardConfig;
        private readonly BoardInitialLayoutConfig _boardInitialLayoutConfig;
        private readonly LevelConfig _levelConfig;
        private readonly RemoteConfigModel _remoteConfigModel;

        public BoardGenerationService()
        {
            _boardConfig = Resources.Load<BoardConfig>(BoardConfigResourcePath);
            _boardInitialLayoutConfig = Resources.Load<BoardInitialLayoutConfig>(BoardInitialLayoutConfigResourcePath);
            _levelConfig = Resources.Load<LevelConfig>(LevelConfigResourcePath);
            _remoteConfigModel = ServiceLocator.GetService<ISavedDataService>().GetModel<RemoteConfigModel>();
        }

        public BoardState CreateBoard(int boardIndex, int currentLevel)
        {
            var slots = CreateSlots(boardIndex, currentLevel);
            var slotDefinitions = GetSlotDefinitions(boardIndex);
            var tierOffset = GetTierOffset(boardIndex);

            for (var slotIndex = 0; slotIndex < slotDefinitions.Length; slotIndex++)
            {
                var slotState = slots[slotIndex];
                slots[slotIndex] = new SlotState(
                    slotState.UnlockCoinCost,
                    slotState.AutomaticUnlockLevel,
                    slotState.Type,
                    CreateCoins(slotDefinitions[slotIndex], tierOffset));
            }

            return new BoardState(boardIndex, slots);
        }

        private SlotState[] CreateSlots(int boardIndex, int currentLevel)
        {
            var slots = new SlotState[_boardConfig.SlotCount];
            var initialUnlockedSlotCount = boardIndex == 1
                ? _boardConfig.BoardOneInitialUnlockedSlotCount
                : _boardConfig.SubsequentBoardsInitialUnlockedSlotCount;
            var slotUnlockLevels = _levelConfig.GetSlotUnlockLevels(boardIndex);
            var unlockedSlotCount = GetUnlockedSlotCount(initialUnlockedSlotCount, slotUnlockLevels, currentLevel);

            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var automaticUnlockLevel = GetAutomaticUnlockLevel(slotIndex, initialUnlockedSlotCount, slotUnlockLevels);
                var slotType = GetSlotType(slotIndex, unlockedSlotCount);
                var unlockCoinCost = slotType == SlotType.Unlockable ? _remoteConfigModel.UnlockableSlotCoinCost : 0;

                slots[slotIndex] = new SlotState(
                    unlockCoinCost,
                    automaticUnlockLevel,
                    slotType,
                    new List<CoinState>());
            }

            return slots;
        }

        private int GetUnlockedSlotCount(int initialUnlockedSlotCount, IReadOnlyList<int> slotUnlockLevels, int currentLevel)
        {
            var unlockedSlotCount = initialUnlockedSlotCount;

            foreach (var unlockLevel in slotUnlockLevels)
            {
                if (currentLevel >= unlockLevel)
                {
                    unlockedSlotCount++;
                }
            }

            return unlockedSlotCount;
        }

        private SlotType GetSlotType(int slotIndex, int unlockedSlotCount)
        {
            if (slotIndex == _boardConfig.RentableSlotIndex)
            {
                return SlotType.Rentable;
            }

            if (slotIndex < unlockedSlotCount)
            {
                return SlotType.Unlocked;
            }

            return slotIndex == unlockedSlotCount
                ? SlotType.Unlockable
                : SlotType.Locked;
        }

        private static int GetAutomaticUnlockLevel(
            int slotIndex,
            int initialUnlockedSlotCount,
            IReadOnlyList<int> slotUnlockLevels)
        {
            var unlockLevelIndex = slotIndex - initialUnlockedSlotCount;

            return unlockLevelIndex >= 0 && unlockLevelIndex < slotUnlockLevels.Count
                ? slotUnlockLevels[unlockLevelIndex]
                : 0;
        }

        private SlotCoinStackDefinition[] GetSlotDefinitions(int boardIndex)
        {
            if (boardIndex == 1)
            {
                return _boardInitialLayoutConfig.BoardOneSlots;
            }

            return _boardInitialLayoutConfig.GetTemplate(GetTemplateIndex(boardIndex)).Slots;
        }

        private static int GetTemplateIndex(int boardIndex)
        {
            if (boardIndex <= 5)
            {
                return boardIndex - 2;
            }

            return boardIndex % 2 == 0 ? 2 : 3;
        }

        private static int GetTierOffset(int boardIndex)
        {
            return boardIndex == 1 ? 0 : (boardIndex - 2) * 9;
        }

        private static List<CoinState> CreateCoins(SlotCoinStackDefinition slotDefinition, int tierOffset)
        {
            var coins = new List<CoinState>();

            foreach (var coinStack in slotDefinition.CoinStacks)
            {
                var coinType = coinStack.IsHidden ? CoinType.Hidden : CoinType.Standard;
                var tier = coinStack.Tier + tierOffset;

                for (var coinIndex = 0; coinIndex < coinStack.Count; coinIndex++)
                {
                    coins.Add(new CoinState(tier, coinType));
                }
            }

            return coins;
        }
    }
}
