using System;
using UnityEngine;
using UnityEngine.UI;

namespace OutOfMoves
{
    public class RestartView: BaseView
    {
        [SerializeField] private Button restartButton;

        public event Action RestartButtonClicked;

        protected override void Awake()
        {
            base.Awake();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        private void OnRestartButtonClicked()
        {
            RestartButtonClicked?.Invoke();
        }
    }
}