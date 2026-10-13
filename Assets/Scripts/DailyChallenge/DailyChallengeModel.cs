using System;
using System.Collections.Generic;
using SavedData;

namespace DailyChallenge
{
    [Serializable]
    public class DailyChallengeModel : IModel
    {
        public int InstallYear { get; set; }
        public int InstallMonth { get; set; }
        public int DisplayedYear { get; set; }
        public int DisplayedMonth { get; set; }
        public int SelectedYear { get; set; }
        public int SelectedMonth { get; set; }
        public int SelectedDay { get; set; }
        public int PlayedYear { get; set; }
        public int PlayedMonth { get; set; }
        public int PlayedDay { get; set; }
        public int PlayedAttemptCount { get; set; }
        public List<string> CompletedDays { get; set; } = new List<string>();
    }
}