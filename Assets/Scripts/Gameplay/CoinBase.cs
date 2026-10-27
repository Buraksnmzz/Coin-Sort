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
        [SerializeField] private GameObject rainbowVisual;
        [SerializeField] private GameObject stickyVisual;
        [SerializeField] private GameObject icedVisual;
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private LayoutElement _layoutElement;

        public int Tier { get; private set; }
        public CoinType Type { get; private set; }

        public void Initialize(CoinState coinState, CoinSpriteConfig coinSpriteConfig)
        {
            Tier = coinState.Tier;
            Type = coinState.Type;
            coinImage.sprite = coinSpriteConfig.GetSprite(coinState.Tier);
            tierText.SetText("{0}", coinState.Tier);

            hiddenVisual.SetActive(coinState.Type == CoinType.Hidden);
            rainbowVisual.SetActive(coinState.Type == CoinType.Rainbow);
            stickyVisual.SetActive(coinState.Type == CoinType.Sticky);
            icedVisual.SetActive(coinState.Type == CoinType.Iced);
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
