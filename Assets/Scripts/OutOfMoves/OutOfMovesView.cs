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

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnCloseButtonClicked()
        {
            CloseButtonClicked?.Invoke();
        }
    }
}