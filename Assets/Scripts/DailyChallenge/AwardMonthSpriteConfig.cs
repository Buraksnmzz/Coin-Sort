using System;
using UnityEngine;

namespace DailyChallenge.Award
{
    [CreateAssetMenu(fileName = "AwardMonthSpriteConfig", menuName = "Daily Challenge/Award Month Sprite Config")]
    public class AwardMonthSpriteConfig : ScriptableObject
    {
        [SerializeField] private AwardMonthSprites[] monthSprites;

        public Sprite GetActiveSprite(int month)
        {
            var sprites = GetSprites(month);
            return sprites != null ? sprites.CompletedSprite : null;
        }

        public Sprite GetCompletedSprite(int month)
        {
            var sprites = GetSprites(month);
            return sprites != null ? sprites.CompletedSprite : null;
        }

        private AwardMonthSprites GetSprites(int month)
        {
            if (monthSprites == null)
                return null;

            foreach (var sprites in monthSprites)
            {
                if (sprites != null && sprites.Month == month)
                    return sprites;
            }

            return null;
        }
    }

    [Serializable]
    public class AwardMonthSprites
    {
        public int Month;
        public Sprite ActiveSprite;
        public Sprite CompletedSprite;
    }
}