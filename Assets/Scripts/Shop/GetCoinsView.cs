using System;
using UI.General;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class GetCoinsView : BaseView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] public Button rewardedVideoButton;
        [SerializeField] public CoinFlyAnimator coinFlyAnimator;

        public event Action RewardedVideoButtonClicked;
        private void Start()
        {
            rewardedVideoButton.onClick.AddListener(()=> RewardedVideoButtonClicked?.Invoke());
            closeButton.onClick.AddListener(Hide);
        }

        protected override void OnShown()
        {
            base.OnShown();
            
        }

        public void SetCoinCount(int value)
        {
            coinFlyAnimator.SetCoinCount(value);
        }

        public void PlayCoinFly(int totalCoins, Transform buttonTransform)
        {
            coinFlyAnimator.Play(buttonTransform.position, totalCoins);
        }
    }
}