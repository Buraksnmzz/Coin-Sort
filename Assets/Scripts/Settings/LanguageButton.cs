using System;
using DG.Tweening;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    public class LanguageButton: MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI languageText;
        [SerializeField] private GameObject selectedIndicator;

        public SystemLanguage Language { get; private set; }
        public Action OnClicked;

        public void Initialize(LocaleInfo localeInfo, bool isSelected)
        {
            Language = localeInfo.Language;
            languageText.text = localeInfo.DisplayName;

            SetSelected(isSelected);

            button.onClick.AddListener(() =>
            {
                OnClicked?.Invoke();
            });
        }

        public void SetSelected(bool selected)
        {
            selectedIndicator.SetActive(selected);
            if (selected)
            {
                selectedIndicator.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5);
            }
        }

        private void OnDestroy()
        {
            button.onClick.RemoveAllListeners();
        }
    }
}