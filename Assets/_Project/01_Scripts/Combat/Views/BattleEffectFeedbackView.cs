using System.Collections.Generic;
using OzGameLab01.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI.Battle
{
    /// <summary>
    /// Bounded, non-interactive combat feedback. Uses battle time so pause/speed controls
    /// also control the feedback. Attached before startup effects; no prefab wiring needed.
    /// </summary>
    public sealed class BattleEffectFeedbackView : MonoBehaviour
    {
        private const int MaxEntries = 5;
        private const int MaxMarkers = 12;
        private const float EntryLifetime = 7f;
        private const float MarkerLifetime = 1.6f;

        private sealed class Entry
        {
            public CombatFeedback Feedback;
            public readonly HashSet<int> Targets = new();
            public float Remaining;
            public int Frame;
        }

        private sealed class Marker
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Text Label;
            public Image Border;
            public RectTransform Anchor;
            public float Remaining;
        }

        private readonly List<Entry> _entries = new();
        private readonly List<Marker> _markers = new();
        private readonly List<Text> _rows = new();
        private RectTransform _root;
        private GameObject _panel;
        private Font _font;

        public static BattleEffectFeedbackView Create(BattleMainView mainView)
        {
            var existing = mainView.GetComponentInChildren<BattleEffectFeedbackView>(true);
            if (existing != null) return existing;
            var go = new GameObject("BattleEffectFeedback", typeof(RectTransform));
            go.transform.SetParent(mainView.transform, false);
            var view = go.AddComponent<BattleEffectFeedbackView>();
            view.Initialize();
            return view;
        }

        private void Initialize()
        {
            _root = (RectTransform)transform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;
            // Existing TMP assets contain Latin glyphs only. Use the platform's Korean font
            // without changing global TMP settings or any existing UI materials.
            _font = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 22);

            var panel = CreateRect("RecentEffects", _root, new Vector2(520, 270));
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(24, -110);
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.055f, 0.09f, 0.86f);
            background.raycastTarget = false;
            _panel = panel.gameObject;
            for (int i = 0; i < MaxEntries; i++)
            {
                Text row = CreateText("EffectRow", panel, new Vector2(490, 48), 20);
                row.rectTransform.anchorMin = row.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                row.rectTransform.pivot = new Vector2(0.5f, 1f);
                row.rectTransform.anchoredPosition = new Vector2(0, -10 - i * 50);
                row.alignment = TextAnchor.MiddleLeft;
                _rows.Add(row);
            }
            _panel.SetActive(false);
        }

        public void Show(CombatFeedback feedback)
        {
            // Group startup effects applied to several allies in the same frame, without
            // merging successive casts or losing the source of different effects.
            Entry entry = _entries.Find(e => e.Frame == Time.frameCount &&
                e.Feedback.Kind == feedback.Kind && e.Feedback.Source == feedback.Source &&
                e.Feedback.Detail == feedback.Detail);
            if (entry == null)
            {
                entry = new Entry { Feedback = feedback, Frame = Time.frameCount };
                _entries.Insert(0, entry);
                if (_entries.Count > MaxEntries) _entries.RemoveAt(MaxEntries);
            }
            if (feedback.Target != null) entry.Targets.Add(feedback.Target.GetInstanceID());
            entry.Remaining = EntryLifetime;
            ShowMarker(feedback);
            RefreshRows();
        }

        private void ShowMarker(CombatFeedback feedback)
        {
            RectTransform anchor = feedback.Target != null ? feedback.Target.CombatAnchor : null;
            if (anchor == null) return;
            Marker marker = _markers.Find(m => m.Remaining > 0 && m.Anchor == anchor);
            bool sameTarget = marker != null;
            marker ??= _markers.Find(m => m.Remaining <= 0);
            if (marker == null && _markers.Count < MaxMarkers)
            {
                var root = CreateRect("EffectTarget", _root, new Vector2(190, 60));
                var border = root.gameObject.AddComponent<Image>();
                border.raycastTarget = false;
                var group = root.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                Text label = CreateText("EffectLabel", root, new Vector2(300, 70), 20);
                label.rectTransform.anchoredPosition = new Vector2(0, 60);
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.95f);
                marker = new Marker { Root = root, Group = group, Label = label, Border = border };
                _markers.Add(marker);
            }
            if (marker == null) return;
            string caption = $"[{feedback.Category}] {feedback.Source}";
            // Keep at most two captions when startup buffs hit the same unit together.
            marker.Label.text = sameTarget && !marker.Label.text.Contains(caption)
                ? marker.Label.text.Split('\n')[0] + "\n" + caption : caption;
            marker.Label.color = feedback.Color;
            marker.Border.color = new Color(feedback.Color.r, feedback.Color.g, feedback.Color.b, 0.22f);
            marker.Anchor = anchor;
            marker.Remaining = MarkerLifetime;
            marker.Group.alpha = 1;
            marker.Root.gameObject.SetActive(true);
            PositionMarker(marker);
        }

        private void Update()
        {
            bool expired = false;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                _entries[i].Remaining -= Time.deltaTime;
                if (_entries[i].Remaining <= 0)
                {
                    _entries.RemoveAt(i);
                    expired = true;
                }
            }
            if (expired) RefreshRows();
            for (int i = 0; i < _entries.Count; i++)
            {
                Color color = _rows[i].color;
                color.a = Mathf.Clamp01(_entries[i].Remaining);
                _rows[i].color = color;
            }
            foreach (Marker marker in _markers)
            {
                if (marker.Remaining <= 0) continue;
                marker.Remaining -= Time.deltaTime;
                if (marker.Remaining <= 0 || marker.Anchor == null)
                {
                    marker.Remaining = 0;
                    marker.Root.gameObject.SetActive(false);
                    continue;
                }
                marker.Group.alpha = Mathf.Clamp01(marker.Remaining / 0.4f);
                PositionMarker(marker);
            }
        }

        private void PositionMarker(Marker marker)
        {
            Vector3 position = _root.InverseTransformPoint(marker.Anchor.TransformPoint(
                new Vector3(marker.Anchor.rect.center.x, marker.Anchor.rect.yMin + 25, 0)));
            position.y += (1 - marker.Remaining / MarkerLifetime) * 15;
            marker.Root.localPosition = position;
        }

        private void RefreshRows()
        {
            _panel.SetActive(_entries.Count > 0);
            ((RectTransform)_panel.transform).sizeDelta = new Vector2(520, _entries.Count * 50 + 20);
            for (int i = 0; i < _rows.Count; i++)
            {
                Text row = _rows[i];
                row.gameObject.SetActive(i < _entries.Count);
                if (i >= _entries.Count) continue;
                Entry entry = _entries[i];
                CombatFeedback feedback = entry.Feedback;
                string target = feedback.Kind == CombatFeedbackKind.Skill ? "" :
                    entry.Targets.Count > 1 ? $" · {entry.Targets.Count}명" :
                    feedback.Target != null ? $" · {feedback.Target.DisplayName ?? feedback.Target.name}" : "";
                row.text = $"[{feedback.Category}] {feedback.Source} · {feedback.Detail}{target}";
                Color color = feedback.Color;
                color.a = Mathf.Clamp01(entry.Remaining);
                row.color = color;
            }
        }

        private Text CreateText(string objectName, Transform parent, Vector2 size, int fontSize)
        {
            var rect = CreateRect(objectName, parent, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private void OnDestroy()
        {
            if (_font == null) return;
            if (Application.isPlaying) Destroy(_font);
            else DestroyImmediate(_font);
        }
    }
}
