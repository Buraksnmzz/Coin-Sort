using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "CoinSpriteConfig", menuName = "Gameplay/Coin Sprite Config")]
    public class CoinSpriteConfig : ScriptableObject
    {
        [SerializeField] private Sprite[] sprites;

        public Sprite GetSprite(int tier)
        {
            return sprites[(tier - 1) % sprites.Length];
        }
    }
}
