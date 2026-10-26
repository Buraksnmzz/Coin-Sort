using UnityEngine;

namespace Gameplay
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private Slot[] slots;
        [SerializeField] private CoinBase coinPrefab;
        [SerializeField] private CoinSpriteConfig coinSpriteConfig;

        public void Initialize(BoardState boardState)
        {
            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                var slot = slots[slotIndex];
                var slotState = boardState.Slots[slotIndex];

                slot.Initialize(slotState);

                foreach (var coinState in slotState.Coins)
                {
                    var coin = Instantiate(coinPrefab, slot.GetNextCoinParent());
                    coin.Initialize(coinState, coinSpriteConfig);
                }
            }
        }
    }
}
