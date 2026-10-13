using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ShopRewardView: BaseView
    {
        [SerializeField] private GameObject packReward;
        [SerializeField] private GameObject noAdsReward;
        [SerializeField] private GameObject coinReward;
        [SerializeField] TextMeshProUGUI packCoinAmountText;
        [SerializeField] TextMeshProUGUI coinOnlyAmountText;
        [SerializeField] Button okButton;
        
        public event Action OkClicked;

        private void Start()
        {
            okButton.onClick.AddListener(()=>OkClicked?.Invoke());
        }

        public override void Show()
        {
            base.Show();
        }

        public void HideAllRewards()
        {
            packReward.SetActive(false);
            noAdsReward.SetActive(false);
            coinReward.SetActive(false);
        }

        public void ShowNoAds()
        {
            noAdsReward.SetActive(true);
        }

        public void ShowPack(int dataCoinReward)
        {
            packReward.SetActive(true);
            packCoinAmountText.text = dataCoinReward.ToString();
        }

        public void ShowCoin(int dataCoinReward)
        {
            coinReward.SetActive(true);
            coinOnlyAmountText.text = dataCoinReward.ToString();
        }
    }
}