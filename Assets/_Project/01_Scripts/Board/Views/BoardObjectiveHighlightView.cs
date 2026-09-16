using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// 목표 하이라이트의 생성과 제거를 담당합니다.
    /// </summary>
    public sealed class BoardObjectiveHighlightView
    {
        private GameObject _highlight;

        public void Show(GameObject prefab, Transform target, float height)
        {
            if (prefab == null || target == null)
            {
                return;
            }
            Clear();
            _highlight = Object.Instantiate(prefab, target);
            _highlight.transform.localPosition = Vector3.up * height;
        }

        public void Clear()
        {
            if (_highlight != null)
            {
                Object.Destroy(_highlight);
                _highlight = null;
            }
        }
    }
}
