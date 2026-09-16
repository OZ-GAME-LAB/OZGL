using System.Collections.Generic;
using OzGameLab01.Effects.Models;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Effects.Views
{
    /// <summary>
    /// 전달받은 시너지 표시 목록으로 기존 패널을 갱신합니다.
    /// </summary>
    public sealed class SynergyPanelView
    {
        private readonly Transform _root;
        private readonly SynergyItemView _template;
        private readonly Color _activeColor;
        private readonly Color _inactiveColor;

        public bool IsAvailable => _root != null && _template != null;

        public SynergyPanelView(Transform root, SynergyItemView template, Color activeColor, Color inactiveColor)
        {
            _root = root;
            _template = template;
            _activeColor = activeColor;
            _inactiveColor = inactiveColor;
        }

        public void Render(IEnumerable<SynergyPanelUtility.DisplayItem> items)
        {
            if (!IsAvailable)
            {
                return;
            }
            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(_root.GetChild(i).gameObject);
            }
            foreach (SynergyPanelUtility.DisplayItem display in items)
            {
                SynergyItemView item = Object.Instantiate(_template, _root);
                item.gameObject.SetActive(true);
                item.SetTitle(display.Definition.DisplayName);
                item.SetStackText(display.StackText);
                item.SetBackgroundColor(display.IsActive ? _activeColor : _inactiveColor);
            }
        }
    }
}
