using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge.Award
{
    public class AwardMonth : MonoBehaviour
    {
        [SerializeField] private Image awardImageActive;
        [SerializeField] private Image awardImageInactive;
        [SerializeField] private Image awardImageCompleted;
        [SerializeField] private TextMeshProUGUI monthName;

        public void SetMonth(AwardMonthModel model, AwardMonthSpriteConfig spriteConfig)
        {
            SetImageSprite(awardImageActive, spriteConfig != null ? spriteConfig.GetActiveSprite(model.Month) : null);
            SetImageSprite(awardImageCompleted, spriteConfig != null ? spriteConfig.GetCompletedSprite(model.Month) : null);

            SetImageActive(awardImageActive, model.State == AwardState.Active);
            SetImageActive(awardImageInactive, model.State == AwardState.Inactive);
            SetImageActive(awardImageCompleted, model.State == AwardState.Completed);

            if (monthName != null)
                monthName.text = !string.IsNullOrEmpty(model.MonthName)
                    ? model.MonthName
                    : model.Date.ToString("MMMM");
        }

        private static void SetImageSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
        }

        private static void SetImageActive(Image image, bool isActive)
        {
            if (image != null)
                image.gameObject.SetActive(isActive);
        }
    }

    public enum AwardState
    {
        Active,
        Inactive,
        Completed
    }
}