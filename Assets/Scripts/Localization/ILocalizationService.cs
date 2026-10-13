using System.Collections.Generic;
using General;
using Services;
using UnityEngine;

namespace Localization
{
    public interface ILocalizationService: IService
    {
        void SetLanguage(SystemLanguage language);
        SystemLanguage GetCurrentLanguage();
        string GetLocalizedString(string key);
        string GetLocalizedString(string key, params object[] args);
        List<LocaleInfo> GetAvailableLanguages();
    }
}