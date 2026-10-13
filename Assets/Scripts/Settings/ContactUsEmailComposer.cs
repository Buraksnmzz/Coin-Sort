using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Settings
{
    public static class ContactUsEmailComposer
    {
        private const string ContactEmailAddress = "contact@yoogalab.com";
        private const string Separator = "----------------------";

        public static string CreateMailtoUrl()
        {
            var subject = BuildSubject();
            return $"mailto:{ContactEmailAddress}?subject={UnityWebRequest.EscapeURL(subject)}";
        }

        private static string BuildSubject()
        {
            var builder = new StringBuilder();
            builder.AppendLine(Separator);
            builder.AppendLine("Name:Loving_Colors");
            builder.AppendLine($"Version:{FormatValue(Application.version)}");
            builder.AppendLine($"Country:{FormatValue(GetCountryCode())}");
            builder.AppendLine($"Language:{FormatValue(Application.systemLanguage.ToString())}");
            builder.AppendLine($"Device:{FormatValue(SystemInfo.deviceModel)}");
            builder.AppendLine($"OS:{FormatValue(SystemInfo.operatingSystem)}");
            builder.AppendLine($"Screen:{FormatValue(GetScreenInfo())}");
            builder.Append($"ID:{FormatValue(SystemInfo.deviceUniqueIdentifier)}");
            return builder.ToString();
        }

        private static string GetCountryCode()
        {
            return RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }

        private static string GetScreenInfo()
        {
            var resolution = Screen.currentResolution;
            var shorterSide = Mathf.Min(resolution.width, resolution.height);
            var longerSide = Mathf.Max(resolution.width, resolution.height);
            var refreshRate = resolution.refreshRateRatio.value.ToString("0.##", CultureInfo.InvariantCulture);
            return $"{shorterSide} x {longerSide} @ {refreshRate}Hz";
        }

        private static string FormatValue(string value)
        {
            return $" {value.Replace(" ", " ")}";
        }
    }
}