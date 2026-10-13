using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.RateUs
{
    public class RateUsView: BaseView
    {
        [SerializeField] private Button[] starButtons;
        [SerializeField] private Image[] starImagesGrey;
        [SerializeField] private Image[] starImagesYellow;
        [SerializeField] private Button closeButton;

        public event Action<int> OnStarsClicked;

        public override void Show()
        {
            base.Show();
            SetupStars();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        protected override void OnShown()
        {
            base.OnShown();
            AnimateStars();
        }

        private void OnCloseClicked()
        {
            Hide();
        }

        private void SetupStars()
        {
            for (var i = 0; i < starButtons.Length; i++)
            {
                var rating = i + 1;
                starButtons[i].onClick.RemoveAllListeners();
                starButtons[i].onClick.AddListener(() => OnStarClicked(rating));
                starImagesYellow[i].transform.localScale = Vector3.zero;
            }
        }

        private void AnimateStars()
        {
            for (var i = 0; i < starButtons.Length; i++)
            {
                starImagesYellow[i].transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
            }
            
            DOVirtual.DelayedCall(1f, () =>
            {
                for (var i = 0; i < starButtons.Length; i++)
                {
                    starImagesYellow[i].transform.DOScale(Vector3.zero, 0.2f);
                }
            });
        }

        private void OnStarClicked(int rating)
        {
            OnStarsClicked?.Invoke(rating);
            Hide();
        }

        public override void Hide()
        {
            base.Hide();
            closeButton.onClick.RemoveAllListeners();
            foreach (var starButton in starButtons)
            {
                starButton.onClick.RemoveAllListeners();
            }
        }
    }
}