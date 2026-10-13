using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    public class LegalTextView: MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
    
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private RectTransform bodyRect;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            HideImmediate();
        }

        public void Show(string title, TextAsset content)
        {
            if (root != null) root.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = content != null ? content.text : "";
            RebuildContentHeight();
        }
        
        private void RebuildContentHeight()
        {
            if (bodyText == null || contentRect == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bodyRect);
            Canvas.ForceUpdateCanvases();
            float preferred = bodyText.preferredHeight;
            if (preferred < 1f) 
                preferred = bodyRect.sizeDelta.y; 

            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferred);
            bodyRect.anchoredPosition = Vector2.zero;
        }

        private void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void HideImmediate()
        {
            if (root != null) root.SetActive(false);
        }
    }
}