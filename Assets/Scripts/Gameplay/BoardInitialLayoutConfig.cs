using System;
using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(fileName = "BoardInitialLayoutConfig", menuName = "Gameplay/Board Initial Layout Config")]
    public class BoardInitialLayoutConfig : ScriptableObject
    {
        [SerializeField] private SlotCoinStackDefinition[] boardOneSlots;
        [SerializeField] private BoardTemplateDefinition[] boardTemplates;

        public SlotCoinStackDefinition[] BoardOneSlots => boardOneSlots;

        public BoardTemplateDefinition GetTemplate(int templateIndex)
        {
            return boardTemplates[templateIndex];
        }
    }

    [Serializable]
    public class BoardTemplateDefinition
    {
        [SerializeField] private SlotCoinStackDefinition[] slots;

        public SlotCoinStackDefinition[] Slots => slots;
    }

    [Serializable]
    public class SlotCoinStackDefinition
    {
        [SerializeField] private CoinStackDefinition[] coinStacks;

        public CoinStackDefinition[] CoinStacks => coinStacks;
    }

    [Serializable]
    public class CoinStackDefinition
    {
        [SerializeField] private int tier;
        [SerializeField] private int count;
        [SerializeField] private bool isHidden;

        public int Tier => tier;
        public int Count => count;
        public bool IsHidden => isHidden;
    }
}
