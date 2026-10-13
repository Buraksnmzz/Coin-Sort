using Collectible;
using GameConfig;
using General;
using General.EventDispatcher;
using IAP;
using JetBrains.Annotations;
using SavedData;
using TMPro;
using UI.General;
using UI.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public class ShopPurchaseButton : MonoBehaviour
    {
        public string productId;
        [SerializeField] private Button purchaseButton;
        [SerializeField][CanBeNull] private TextMeshProUGUI purchaseValueText;
        [SerializeField][CanBeNull] private TextMeshProUGUI coinRewardText;
        [SerializeField][CanBeNull] private TextMeshProUGUI discountText;
        [SerializeField][CanBeNull] private GameObject popularObject;
        [SerializeField][CanBeNull] private GameObject discountObject;

        private IIAPService _iapService;
        protected IEventDispatcherService EventDispatcherService;
        protected ISavedDataService SavedDataService;
        protected IUIService UIService;

        private void Awake()
        {
            _iapService = ServiceLocator.GetService<IIAPService>();
            SavedDataService = ServiceLocator.GetService<ISavedDataService>();
            EventDispatcherService = ServiceLocator.GetService<IEventDispatcherService>();
            UIService = ServiceLocator.GetService<IUIService>();
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
        }

        private void Start()
        {
            SetPurchaseValue();
            SetRewardValue();
        }

        private void SetPurchaseValue()
        {
            if (string.IsNullOrEmpty(productId)) return;

            var localized = _iapService.GetLocalizedPrice(productId);
            if (!string.IsNullOrEmpty(localized))
            {
                purchaseButton.interactable = true;
                purchaseValueText?.SetText(localized);
                return;
            }
            purchaseButton.interactable = false;
            purchaseValueText?.SetText("--");
        }

        public virtual void SetRewardValue()
        {
            if (string.IsNullOrEmpty(productId)) return;

            var reward = GetProductReward();
            if (coinRewardText != null)
                coinRewardText.SetText(reward.Coins.ToString());
        }

        private void OnPurchaseClicked()
        {
            _iapService.Purchase(productId, GiveReward);
        }

        protected virtual void GiveReward(bool success)
        {
            if (success)
            {
                var reward = GetProductReward();
                var collectibleModel = SavedDataService.GetModel<CollectibleModel>();
                collectibleModel.TotalCoins += reward.Coins;
                var shopRewardData = new ShopRewardData
                {
                    CoinReward = reward.Coins,
                    RewardType = RewardType.Coin,
                };
                UIService.ShowPopup<ShopRewardPresenter, ShopRewardData>(shopRewardData);
                SavedDataService.SaveData(collectibleModel);
                EventDispatcherService.Dispatch(new CoinChangedSignal());
                EventDispatcherService.Dispatch(new RewardGivenSignal(transform));
            }
        }

        protected CatalogProduct GetProductReward()
        {
            var configModel = SavedDataService.GetModel<RemoteConfigModel>();
            return ProductRewardResolver.Resolve(configModel, productId);
        }
    }
}