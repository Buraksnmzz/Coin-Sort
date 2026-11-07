using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    [RequireComponent(typeof(LayoutElement))]
    public class CoinBase : MonoBehaviour
    {
        [SerializeField] private Image coinImage;
        [SerializeField] private GameObject hiddenVisual;
        [SerializeField] private GameObject hiddenGroupIndicatorVisual;
        [SerializeField] private GameObject rainbowVisual;
        [SerializeField] private GameObject stickyVisual;
        [SerializeField] private GameObject icedVisual;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private TextMeshProUGUI icedCountdownText;
        [SerializeField] private LayoutElement _layoutElement;

        public int Tier { get; private set; }
        public CoinType Type { get; private set; }
        public bool IsHiddenGroupCenter { get; private set; }
        public int ObstacleGroupId { get; private set; }
        public int IcedCountdown { get; private set; }

        public void Initialize(CoinState coinState, CoinSpriteConfig coinSpriteConfig)
        {
            Tier = coinState.Tier;
            Type = coinState.Type;
            IsHiddenGroupCenter = coinState.IsHiddenGroupCenter;
            ObstacleGroupId = coinState.ObstacleGroupId;
            IcedCountdown = coinState.IcedCountdown;
            SetTierVisuals(coinSpriteConfig);

            hiddenVisual.SetActive(coinState.Type == CoinType.Hidden);
            hiddenGroupIndicatorVisual.SetActive(coinState.Type == CoinType.Hidden && coinState.IsHiddenGroupCenter);
            rainbowVisual.SetActive(coinState.Type == CoinType.Rainbow);
            stickyVisual.SetActive(coinState.Type == CoinType.Sticky);
            icedVisual.SetActive(coinState.Type == CoinType.Iced);
            icedCountdownText.SetText("{0}", IcedCountdown);
        }

        public void RevealHidden()
        {
            Type = CoinType.Standard;
            IsHiddenGroupCenter = false;
            hiddenVisual.SetActive(false);
            hiddenGroupIndicatorVisual.SetActive(false);
        }

        public void BreakIce()
        {
            Type = CoinType.Standard;
            IcedCountdown = 0;
            icedVisual.SetActive(false);
        }

        public void SetIcedCountdown(int countdown)
        {
            IcedCountdown = countdown;
            icedCountdownText.SetText("{0}", countdown);
        }

        public void ConvertStickyToStandard()
        {
            Type = CoinType.Standard;
            ObstacleGroupId = 0;
            stickyVisual.SetActive(false);
        }

        public void TransformRainbow(int tier, CoinSpriteConfig coinSpriteConfig)
        {
            Tier = tier;
            Type = CoinType.Standard;
            SetTierVisuals(coinSpriteConfig);
            rainbowVisual.SetActive(false);
        }

        private void SetTierVisuals(CoinSpriteConfig coinSpriteConfig)
        {
            coinImage.sprite = coinSpriteConfig.GetSprite(Tier);
            tierText.SetText("{0}", Tier);
        }

        public void SetIgnoredByLayout(bool isIgnored)
        {
            _layoutElement.ignoreLayout = isIgnored;
        }

        public Tween MoveTo(Vector3 position, float duration)
        {
            return transform.DOMove(position, duration);
        }
    }
}
