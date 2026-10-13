using System;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge
{
    public class DailyChallengeUnlockView : BaseView
    {
        [SerializeField] private Button playButton;

        public event Action PlayButtonClicked;

        private void Start()
        {
            playButton.onClick.AddListener(() => PlayButtonClicked?.Invoke());
        }

        protected override void OnShown()
        {
            base.OnShown();
            StartAutoLoopButtonAnimation();
        }
    }
}