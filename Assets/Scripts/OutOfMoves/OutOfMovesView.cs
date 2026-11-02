using System;
using UnityEngine;
using UnityEngine.UI;

namespace OutOfMoves
{
    public class OutOfMovesView: BaseView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button continueButton;

        public event Action CloseButtonClicked;
        public event Action ContinueButtonClicked;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        private void OnCloseButtonClicked()
        {
            CloseButtonClicked?.Invoke();
        }

        private void OnContinueButtonClicked()
        {
            ContinueButtonClicked?.Invoke();
        }
    }
}