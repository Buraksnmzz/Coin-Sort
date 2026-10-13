using General.EventDispatcher;
using UnityEngine;

namespace UI.Shop
{
    public class RewardGivenSignal : ISignal
    {
        public Transform ButtonTransform; 
        public RewardGivenSignal(Transform buttonTransform)
        {
            ButtonTransform = buttonTransform;
        }
    }
}