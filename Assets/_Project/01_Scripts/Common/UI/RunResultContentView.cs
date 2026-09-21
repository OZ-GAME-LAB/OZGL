using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    /// <summary>결과 아이템의 생성과 정리, 스크롤 레이아웃을 담당합니다. 게임 데이터는 외부에서 전달합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class RunResultContentView : MonoBehaviour
    {
        #region Nested Types

        /// <summary>아이콘을 표시할 결과 영역입니다.</summary>
        public enum IconSection { Synergies, Units, Relics }

        [Serializable]
        public sealed class IconList
        {
            public IconSection section;
            public ScrollRect scroll;
            public LayoutElement contentMinimumSize;
        }

        #endregion

        [SerializeField] private ResultStatItemView statItemPrefab;
        [SerializeField] private ResultIconItemView iconItemPrefab;
        [SerializeField] private ScrollRect statisticsScroll;
        [SerializeField] private IconList[] iconLists = Array.Empty<IconList>();

        #region Properties

        /// <summary>동적으로 생성할 통계 아이템 프리팹입니다.</summary>
        public ResultStatItemView StatItemPrefab => statItemPrefab;

        /// <summary>동적으로 생성할 아이콘 아이템 프리팹입니다.</summary>
        public ResultIconItemView IconItemPrefab => iconItemPrefab;

        /// <summary>왼쪽 통계 영역의 세로 스크롤입니다.</summary>
        public ScrollRect StatisticsScroll => statisticsScroll;

        #endregion

        #region Unity Lifecycle

        private void OnEnable() => RefreshLayout();

        private void LateUpdate() => UpdateMinimumWidths();

        #endregion

        #region Public API

        /// <summary>통계 프리팹을 생성하고 항목명과 값을 설정합니다. 기존 아이템은 유지합니다.</summary>
        public ResultStatItemView CreateStatItem(string label, string value)
        {
            if (statItemPrefab == null || statisticsScroll == null || statisticsScroll.content == null) return null;
            ResultStatItemView item = Instantiate(statItemPrefab, statisticsScroll.content, false);
            item.SetData(label, value);
            item.gameObject.SetActive(true);
            LayoutRebuilder.MarkLayoutForRebuild(statisticsScroll.content);
            return item;
        }

        /// <summary>지정한 영역에 아이콘 프리팹을 생성합니다. 기존 아이템은 유지합니다.</summary>
        public ResultIconItemView CreateIconItem(IconSection section, Sprite sprite)
        {
            ScrollRect scroll = GetIconScroll(section);
            if (iconItemPrefab == null || scroll == null || scroll.content == null) return null;
            ResultIconItemView item = Instantiate(iconItemPrefab, scroll.content, false);
            item.SetIcon(sprite);
            item.gameObject.SetActive(true);
            LayoutRebuilder.MarkLayoutForRebuild(scroll.content);
            return item;
        }

        /// <summary>지정한 아이콘 영역의 스크롤을 반환합니다.</summary>
        public ScrollRect GetIconScroll(IconSection section)
        {
            foreach (IconList list in iconLists)
                if (list != null && list.section == section) return list.scroll;
            return null;
        }

        /// <summary>씬에 배치한 샘플을 포함하여 통계 영역의 모든 아이템을 제거합니다.</summary>
        public void ClearStatItems() => ClearItems(statisticsScroll);

        /// <summary>씬에 배치한 샘플을 포함하여 지정한 아이콘 영역의 모든 아이템을 제거합니다.</summary>
        public void ClearIconItems(IconSection section) => ClearItems(GetIconScroll(section));

        /// <summary>모든 결과 아이템을 제거합니다. 새 결과를 채우기 전에 호출합니다.</summary>
        public void ClearAllItems()
        {
            ClearStatItems();
            foreach (IconList list in iconLists)
                if (list != null) ClearItems(list.scroll);
        }

        /// <summary>아이템을 모두 채운 뒤 레이아웃을 즉시 갱신합니다. 연출 재생 전에 호출합니다.</summary>
        public void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            UpdateMinimumWidths();
            Rebuild(statisticsScroll);
            foreach (IconList list in iconLists)
                if (list != null) Rebuild(list.scroll);
        }

        /// <summary>통계는 맨 위로, 아이콘 목록은 맨 왼쪽으로 이동합니다.</summary>
        public void ResetScrollPositions()
        {
            RefreshLayout();
            ResetScroll(statisticsScroll);
            foreach (IconList list in iconLists)
                if (list != null) ResetScroll(list.scroll);
        }

        /// <summary>연출 중에는 스크롤을 잠그고, 완료 후에는 드래그와 휠 입력을 허용합니다.</summary>
        public void SetScrollInteraction(bool enabled)
        {
            SetScrollEnabled(statisticsScroll, enabled);
            foreach (IconList list in iconLists)
                if (list != null) SetScrollEnabled(list.scroll, enabled);
        }

        #endregion

        #region Private Methods

        private void UpdateMinimumWidths()
        {
            foreach (IconList list in iconLists)
            {
                if (list == null || list.scroll == null || list.scroll.viewport == null || list.contentMinimumSize == null) continue;
                float width = list.scroll.viewport.rect.width;
                if (!Mathf.Approximately(list.contentMinimumSize.minWidth, width))
                    list.contentMinimumSize.minWidth = width;
            }
        }

        private void ClearItems(ScrollRect scroll)
        {
            if (scroll == null || scroll.content == null) return;
            for (int i = scroll.content.childCount - 1; i >= 0; i--)
            {
                GameObject item = scroll.content.GetChild(i).gameObject;
                item.SetActive(false);
                item.transform.SetParent(null, false);
                Destroy(item);
            }
            Rebuild(scroll);
            ResetScroll(scroll);
        }

        private static void Rebuild(ScrollRect scroll)
        {
            if (scroll != null && scroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        }

        private static void ResetScroll(ScrollRect scroll)
        {
            if (scroll == null) return;
            scroll.StopMovement();
            scroll.normalizedPosition = new Vector2(0f, 1f);
        }

        private static void SetScrollEnabled(ScrollRect scroll, bool enabled)
        {
            if (scroll == null) return;
            scroll.StopMovement();
            scroll.enabled = enabled;
        }

        #endregion
    }
}
