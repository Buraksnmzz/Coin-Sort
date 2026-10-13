using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using General;
using Localization;
using SavedData;
using UI.Settings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Services
{
    public class LocaleInfo
    {
        public SystemLanguage Language { get; set; }
        public string DisplayName { get; set; }
        public Sprite Flag { get; set; }
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly ISavedDataService _savedDataService;
        private readonly SettingsModel _settingsModel;
        
        public LocalizationService()
        {
            _savedDataService = ServiceLocator.GetService<ISavedDataService>();
            _settingsModel = _savedDataService.LoadData<SettingsModel>();
            LocalizationSettings.InitializationOperation.Completed += OnLocalizationInitialized;
        }

        private void OnLocalizationInitialized(AsyncOperationHandle<LocalizationSettings> op)
        {
            SetLanguage(GetCurrentLanguage());
        }

        public List<LocaleInfo> GetAvailableLanguages()
        {
            return LocalizationSettings.AvailableLocales.Locales
                .Select(locale => new { locale, lang = GetSystemLanguageFromLocale(locale) })
                .Where(item => item.lang != SystemLanguage.Unknown)
                .Select(item => new LocaleInfo
                {
                    Language = item.lang,
                    DisplayName = GetDisplayName(item.locale),
                    Flag = Resources.Load<Sprite>($"Flags/{item.locale.Identifier.Code}")
                })
                .ToList();
        }

        private string GetDisplayName(Locale locale)
        {
            try
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(locale.Identifier.CultureInfo.NativeName);
            }
            catch
            {
                return locale.LocaleName;
            }
        }

        public void SetLanguage(SystemLanguage language)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(language);

            if (locale != null && LocalizationSettings.SelectedLocale != locale)
            {
                LocalizationSettings.SelectedLocale = locale;
                _settingsModel.CurrentLanguage = language;
                _savedDataService.SaveData(_settingsModel);
            }
            else if (locale == null)
            {
                Debug.LogError($"Locale not found for language: {language}.");
            }
        }

        public SystemLanguage GetCurrentLanguage()
        {
            if (_settingsModel !=null && _settingsModel.CurrentLanguage != SystemLanguage.Unknown)
            {
                return _settingsModel.CurrentLanguage;
            }
            
            var deviceLanguage = Application.systemLanguage;
            var availableLanguages = GetAvailableLanguages().Select(l => l.Language).ToList();
            
            return availableLanguages.Contains(deviceLanguage) ? deviceLanguage : SystemLanguage.English;
        }

        public string GetLocalizedString(string key)
        {
            if (!LocalizationSettings.InitializationOperation.IsDone) return key;

            var table = LocalizationSettings.StringDatabase.GetTable(LocalizationStrings.LocalizationTable);
            var entry = table?.GetEntry(key);
            return entry?.GetLocalizedString() ?? key;
        }

        public string GetLocalizedString(string key, params object[] args)
        {
            if (!LocalizationSettings.InitializationOperation.IsDone) return key;

            var table = LocalizationSettings.StringDatabase.GetTable(LocalizationStrings.LocalizationTable);
            var entry = table?.GetEntry(key);
            return entry?.GetLocalizedString(args) ?? key;
        }

        private SystemLanguage GetSystemLanguageFromLocale(Locale locale)
        {
            var cultureInfo = locale.Identifier.CultureInfo;
            if (cultureInfo != null && Enum.TryParse(cultureInfo.EnglishName, out SystemLanguage language))
            {
                return language;
            }
            return SystemLanguage.Unknown;
        }
    }
}