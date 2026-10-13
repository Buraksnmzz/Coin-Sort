using System;

namespace DailyChallenge.Award
{
    [Serializable]
    public class AwardMonthModel
    {
        public int Year;
        public int Month;
        public string MonthName;
        public AwardState State;

        public DateTime Date => new DateTime(Year, Month, 1);
    }
}