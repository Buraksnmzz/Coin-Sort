using System;
using System.Collections.Generic;
using Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    public class LanguageSelectView:BaseView
    {
        [SerializeField] private Button backButton;
        [SerializeField] private LanguageButton languageButton;
        [SerializeField] private Transform languageButtonsContainer;
        private readonly List<LanguageButton> _languageButtons = new();

        public event Action<SystemLanguage> LanguageButtonCLicked;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnBackClicked()
        {
            Hide();
        }
        
        public void InitializeLanguageButtons(List<LocaleInfo> languages, SystemLanguage currentLanguage)
        {
            ClearButtons();

            foreach (var languageInfo in languages)
            {
                var languageButtonInstance = Instantiate(languageButton, languageButtonsContainer);

                languageButtonInstance.Initialize(languageInfo, languageInfo.Language == currentLanguage);
                languageButtonInstance.OnClicked += ()=>OnLanguageButtonClicked(languageInfo.Language);
                _languageButtons.Add(languageButtonInstance);
            }
        }

        private void OnLanguageButtonClicked(SystemLanguage language)
        {
            foreach (var button in _languageButtons)
            {
                button.SetSelected(button.Language == language);
            }
            LanguageButtonCLicked?.Invoke(language);
        }

        private void ClearButtons()
        {
            foreach (var button in _languageButtons)
            {
                button.OnClicked = null;
                Destroy(button.gameObject);
            }
            _languageButtons.Clear();
        }
    }
}