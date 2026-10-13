using SavedData;
using UnityEngine;

namespace UI.Settings
{
    public class SettingsModel: IModel
    {
        public bool IsHapticOn { get; set; }
        public bool IsSoundOn { get; set; }
        public bool IsMusicOn { get; set; }
        public bool IsNoAds { get; set; }
        public SystemLanguage CurrentLanguage { get; set; } = SystemLanguage.Unknown;
    }
}