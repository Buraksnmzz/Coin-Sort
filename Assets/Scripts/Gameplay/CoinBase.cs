using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public class CoinBase : MonoBehaviour
    {
        [SerializeField] private Image coinImage;
        [SerializeField] private GameObject hiddenVisual;
        [SerializeField] private GameObject rainbowVisual;
        [SerializeField] private GameObject stickyVisual;
        [SerializeField] private GameObject icedVisual;
        [SerializeField] private TextMeshProUGUI tierText;

        public void Initialize(CoinState coinState, CoinSpriteConfig coinSpriteConfig)
        {
            coinImage.sprite = coinSpriteConfig.GetSprite(coinState.Tier);
            tierText.SetText("{0}", coinState.Tier);

            hiddenVisual.SetActive(coinState.Type == CoinType.Hidden);
            rainbowVisual.SetActive(coinState.Type == CoinType.Rainbow);
            stickyVisual.SetActive(coinState.Type == CoinType.Sticky);
            icedVisual.SetActive(coinState.Type == CoinType.Iced);
        }
    }
}
