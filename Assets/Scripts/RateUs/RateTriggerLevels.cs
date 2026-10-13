using System;
using System.Linq;

namespace RateUs
{
    public class RateTriggerLevels
    {
        public int[] TriggerLevels;

        public int FirstTriggerLevel => TriggerLevels != null && TriggerLevels.Length > 0
            ? TriggerLevels[0]
            : 0;

        public static RateTriggerLevels FromCommaSeparatedString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new RateTriggerLevels
                {
                    TriggerLevels = Array.Empty<int>()
                };
            }

            var parts = value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p =>
                {
                    int.TryParse(p.Trim(), out var result);
                    return result;
                })
                .Where(v => v > 0)
                .ToArray();

            return new RateTriggerLevels
            {
                TriggerLevels = parts
            };
        }

        public int? GetNextTriggerLevel(int completedTriggerCount)
        {
            if (TriggerLevels == null || TriggerLevels.Length == 0)
                return null;

            if (completedTriggerCount < 0 || completedTriggerCount >= TriggerLevels.Length)
                return null;

            return TriggerLevels[completedTriggerCount];
        }
    }
}