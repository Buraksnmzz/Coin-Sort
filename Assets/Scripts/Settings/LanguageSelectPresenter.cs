using General;
using Localization;
using UI.General;
using UnityEngine;

namespace UI.Settings
{
    public class LanguageSelectPresenter: BasePresenter<LanguageSelectView>
    {
        ILocalizationService _localizationService;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            View.LanguageButtonCLicked += OnLanguageClicked;
        }

        private void OnLanguageClicked(SystemLanguage language)
        {
            _localizationService.SetLanguage(language);
        }

        public override void ViewShown()
        {
            base.ViewShown();
            View.InitializeLanguageButtons(_localizationService.GetAvailableLanguages(),
                _localizationService.GetCurrentLanguage());
        }
    }
}