using System;
using System.Collections.Generic;
using OzGameLab01.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    [Serializable]
    public sealed class TutorialButtonTarget
    {
        [SerializeField] private string key;
        [SerializeField] private Button button;

        public string Key => key;
        public Button Button => button;
    }

    [Serializable]
    public sealed class TutorialOutlineTarget
    {
        [SerializeField] private string key;
        [SerializeField] private TutorialOutlinePulseView highlightView;

        public string Key => key;
        public TutorialOutlinePulseView HighlightView => highlightView;
    }

    [DisallowMultipleComponent]
    public sealed class TutorialTargetRegistry : MonoBehaviour
    {
        [Header("Button Targets")]
        [SerializeField] private List<TutorialButtonTarget> buttonTargets = new();

        [Header("Outline Targets")]
        [SerializeField] private List<TutorialOutlineTarget> outlineTargets = new();

        public bool TryGetButton(string key,out Button button)
        {
            button = null;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            for (int i = 0; i < buttonTargets.Count; i++)
            {
                TutorialButtonTarget target = buttonTargets[i];

                if (target == null ||
                    !string.Equals(target.Key,key,StringComparison.Ordinal))
                    continue;

                button = target.Button;
                return button != null;
            }

            return false;
        }

        public bool TryGetOutline(
            string key,
            out TutorialOutlinePulseView highlightView)
        {
            highlightView = null;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            for (int i = 0; i < outlineTargets.Count; i++)
            {
                TutorialOutlineTarget target = outlineTargets[i];

                if (target == null ||
                    !string.Equals(target.Key,key,StringComparison.Ordinal))
                    continue;

                highlightView = target.HighlightView;
                return highlightView != null;
            }

            return false;
        }
    }
}
