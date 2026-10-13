using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DailyChallenge.Award
{
    public class AwardsView : BaseView
    {
        [SerializeField] private AwardMonth awardMonthPrefab;
        [SerializeField] private AwardYearSeparator awardYearSeparatorPrefab;
        [SerializeField] private GridLayoutGroup awardMonthGridPrefab;
        [SerializeField] private Transform awardContent;
        [SerializeField] private AwardMonthSpriteConfig awardMonthSpriteConfig;
        [SerializeField] private ScrollRect awardsScrollRect;
        [SerializeField] private Button backButton;

        private readonly List<GameObject> _awardItems = new List<GameObject>();

        private void Start()
        {
            backButton.onClick.AddListener(Hide);
        }

        public void SetAwards(IReadOnlyList<AwardMonthModel> awards)
        {
            ClearAwards();

            if (awardContent == null || awardMonthPrefab == null || awards == null)
                return;

            var previousYear = 0;
            var currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            RectTransform currentMonthTarget = null;
            GridLayoutGroup currentMonthGrid = null;
            foreach (var award in awards)
            {
                if (award.Year != previousYear)
                {
                    if (previousYear != 0)
                        CreateYearSeparator(award.Year);

                    currentMonthGrid = CreateMonthGrid();
                    previousYear = award.Year;
                }

                if (currentMonthGrid == null)
                    continue;

                var awardMonth = Instantiate(awardMonthPrefab, currentMonthGrid.transform);
                awardMonth.SetMonth(award, awardMonthSpriteConfig);

                if (award.Date == currentMonthDate)
                    currentMonthTarget = awardMonth.transform as RectTransform;
            }

            if (currentMonthTarget != null)
                StartCoroutine(ScrollToCurrentMonth(currentMonthTarget));
        }

        protected override void OnDestroy()
        {
            ClearAwards();
            base.OnDestroy();
        }

        private void CreateYearSeparator(int year)
        {
            if (awardYearSeparatorPrefab == null)
                return;

            var separator = Instantiate(awardYearSeparatorPrefab, awardContent);
            separator.SetYear(year);
            _awardItems.Add(separator.gameObject);
        }

        private void ClearAwards()
        {
            foreach (var item in _awardItems)
            {
                if (item != null)
                    Destroy(item);
            }

            _awardItems.Clear();
        }

        private GridLayoutGroup CreateMonthGrid()
        {
            if (awardMonthGridPrefab == null)
                return null;

            var monthGrid = Instantiate(awardMonthGridPrefab, awardContent);
            _awardItems.Add(monthGrid.gameObject);
            return monthGrid;
        }

        private IEnumerator ScrollToCurrentMonth(RectTransform currentMonthTarget)
        {
            yield return null;

            if (currentMonthTarget == null)
                yield break;

            var scrollRect = awardsScrollRect != null ? awardsScrollRect : GetComponentInChildren<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null)
                yield break;

            var viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.transform as RectTransform;
            if (viewport == null)
                yield break;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            Canvas.ForceUpdateCanvases();

            var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, currentMonthTarget);
            var offsetToViewportTop = viewport.rect.yMax - targetBounds.max.y;
            var maxScroll = Mathf.Max(0f, scrollRect.content.rect.height - viewport.rect.height);
            var targetY = Mathf.Clamp(scrollRect.content.anchoredPosition.y + offsetToViewportTop, 0f, maxScroll);
            scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, targetY);
            scrollRect.velocity = Vector2.zero;
        }
    }
}