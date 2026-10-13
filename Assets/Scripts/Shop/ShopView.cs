using UI.General;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ShopView: BaseView
    {
        [SerializeField] private Transform shopNoAdsPack;
        [SerializeField] private Transform noAdsOnly;
        [SerializeField] private CoinFlyAnimator coinFlyAnimator;
        [SerializeField] private Button closeButton;
        
        private void Start()
        {
            closeButton.onClick.AddListener(Hide);
        }

        public void PlayCoinFly(int totalCoins)
        {
            coinFlyAnimator.Play(shopNoAdsPack.position, totalCoins, Hide);
        }

        public void SetCoinCount(int value)
        {
            coinFlyAnimator.SetCoinCount(value);
        }
    }
}