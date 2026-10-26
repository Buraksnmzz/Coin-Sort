using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class Slot : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Transform coinParent;
        [SerializeField] private GameObject unlockedRoot;
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private GameObject unlockableRoot;
        [SerializeField] private GameObject rentableRoot;
        [SerializeField] private TextMeshProUGUI unlockCoinCostText;
        [SerializeField] private TextMeshProUGUI automaticUnlockLevelText;

        public event Action<Slot> Clicked;

        private void Awake()
        {
            button.onClick.AddListener(OnClicked);
        }

        public void Initialize(SlotState slotState)
        {
            ClearCoins();

            unlockedRoot.SetActive(slotState.Type == SlotType.Unlocked);
            lockedRoot.SetActive(slotState.Type == SlotType.Locked);
            unlockableRoot.SetActive(slotState.Type == SlotType.Unlockable);
            rentableRoot.SetActive(slotState.Type == SlotType.Rentable);

            if (slotState.Type == SlotType.Unlockable)
            {
                unlockCoinCostText.SetText("{0}", slotState.UnlockCoinCost);
                automaticUnlockLevelText.SetText("{0}", slotState.AutomaticUnlockLevel);
            }
        }

        public Transform GetNextCoinParent()
        {
            return coinParent;
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this);
        }

        private void ClearCoins()
        {
            for (var childIndex = coinParent.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(coinParent.GetChild(childIndex).gameObject);
            }
        }
    }
}
