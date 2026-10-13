using System;
using UnityEngine;
using UnityEngine.UI;

namespace RemoveAds
{
    public class RemoveAdsView: BaseView
    {
        [SerializeField] private Button removeAdsButton;
        [SerializeField] private Button playWithAdsButton;

        public event Action RemoveAdsButtonClicked;
        public event Action PlayWithAdsButtonButtonClicked;
        
        private void Start()
        {
            removeAdsButton.onClick.AddListener(()=>RemoveAdsButtonClicked?.Invoke());
            playWithAdsButton.onClick.AddListener(()=>PlayWithAdsButtonButtonClicked?.Invoke());
        }
    }
}