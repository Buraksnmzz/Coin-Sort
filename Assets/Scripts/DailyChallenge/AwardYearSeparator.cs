using TMPro;
using UnityEngine;

namespace DailyChallenge.Award
{
    public class AwardYearSeparator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI yearText;

        public void SetYear(int year)
        {
            if (yearText != null)
                yearText.text = year.ToString();
        }
    }
}