using TMPro;
using UI.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Settings
{
    public class AboutView : BaseView
    {
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button termsOfServiceButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button contactUsButton;
        [SerializeField] private LegalTextView legalTextView;
        [SerializeField] private TextMeshProUGUI versionText;
        

        private void Start()
        {
            privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);
            termsOfServiceButton.onClick.AddListener(OnTermsOfServiceClicked);
            contactUsButton.onClick.AddListener(OnContactUsClicked);
            closeButton.onClick.AddListener(Hide);
            versionText.text = $"Version {Application.version}";
        }

        private void OnContactUsClicked()
        {
            Application.OpenURL(ContactUsEmailComposer.CreateMailtoUrl());
        }

        private void OnTermsOfServiceClicked()
        {
            var asset = Resources.Load<TextAsset>("Legal/TermsOfService");
            if (legalTextView != null)
            {
                legalTextView.Show("Terms of Service", asset);
            }
        }

        private void OnPrivacyPolicyClicked()
        {
            var asset = Resources.Load<TextAsset>("Legal/PrivacyPolicy");
            if (legalTextView != null)
            {
                legalTextView.Show("Privacy Policy", asset);
            }
        }
    }
}