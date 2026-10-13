using UnityEngine;
using System;
using UI.Settings;
using UnityEngine.UI;

public class SettingsView : BaseView
{
    
    [SerializeField] private Button soundButton;
    [SerializeField] private Button hapticButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private Button termsOfServiceButton;
    [SerializeField] private Button contactUsButton;
    [SerializeField] private Button removeAdsButton;
    [SerializeField] private Button restorePurchaseButton;
    [SerializeField] private Button languageButton;
    [SerializeField] private LegalTextView legalTextView;
    
    public SettingsCard soundCard;
    public SettingsCard hapticCard;

    public event Action OnSoundToggled;
    public event Action OnHapticToggled;
    public event Action RemoveAdsClicked;
    public event Action RestorePurchaseClicked;
    public event Action LanguageClicked;

    protected override void Awake()
    {
        base.Awake();
        soundButton.onClick.AddListener(OnSoundClicked);
        hapticButton.onClick.AddListener(OnHapticClicked);
        backButton.onClick.AddListener(OnBackClicked);
        privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);
        termsOfServiceButton.onClick.AddListener(OnTermsOfServiceClicked);
        contactUsButton.onClick.AddListener(OnContactUsClicked);
        removeAdsButton.onClick.AddListener(OnRemoveAdsClicked);
        restorePurchaseButton.onClick.AddListener(OnRestorePurchaseClicked);
        languageButton.onClick.AddListener(OnLanguageClicked);
    }

    private void OnLanguageClicked()
    {
        LanguageClicked?.Invoke();
    }

    public void SetNoAdsView(bool isPurchased)
    {
        removeAdsButton.gameObject.SetActive(!isPurchased);

#if UNITY_IOS
        restorePurchaseButton.gameObject.SetActive(true);
#else
        restorePurchaseButton.gameObject.SetActive(false);
#endif

    }

    private void OnRestorePurchaseClicked()
    {
        RestorePurchaseClicked?.Invoke();
    }

    private void OnRemoveAdsClicked()
    {
        RemoveAdsClicked?.Invoke();
    }

    private void OnContactUsClicked()
    {
        Application.OpenURL("mailto:contact@yoogalab.com");
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

    private void OnBackClicked()
    {
        Hide();
    }

    private void OnHapticClicked()
    {
        OnHapticToggled?.Invoke();
    }

    private void OnSoundClicked()
    {
        OnSoundToggled?.Invoke();
    }
}
