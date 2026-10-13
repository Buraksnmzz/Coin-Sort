using System;

namespace DailyChallenge
{
    [Serializable]
    public class DailyChallengeDay
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public bool Completed { get; set; }
        public bool Active { get; set; }

        public DateTime Date => new DateTime(Year, Month, Day);
        public int DayNumber => Day;
    }
}