using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OzGameLab01.UI
{
    public class EventUiView : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescriptionText;

        [Header("Interaction")]
        [SerializeField] private Transform choiceRoot;
        [SerializeField] private Transform actionRoot;

        [Header("Runtime Prefabs")]
        [SerializeField] private EventChoiceButtonView choiceButtonPrefab;
        [SerializeField] private EventActionButtonView actionButtonPrefab;

        [Header("Input")]
        [SerializeField] private CanvasGroup eventViewCanvasGroup;

        private readonly List<EventChoiceButtonView> _choiceButtons = new();
        private EventActionButtonView _actionButton;

        private void Awake()
        {
            HideAction();
            SetInteractionEnabled(true);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            ResetView();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetTitle(string title)
        {
            eventTitleText.text = title ?? string.Empty;
        }

        public void SetDescription(string description)
        {
            eventDescriptionText.text = description ?? string.Empty;
        }

        public void ShowChoices(IReadOnlyList<EventChoiceDisplayData> choices, Action<string> onChoiceSelected)
        {
            ClearChoices();
            HideAction();

            choiceRoot.gameObject.SetActive(true);

            foreach (EventChoiceDisplayData choiceData in choices)
            {
                EventChoiceButtonView choiceButton = Instantiate(choiceButtonPrefab, choiceRoot);

                choiceButton.Bind(choiceData, onChoiceSelected);
                _choiceButtons.Add(choiceButton);
            }
        }

        public void ClearChoices()
        {
            foreach (EventChoiceButtonView choiceButton in _choiceButtons)
            {
                if (choiceButton != null)
                {
                    Destroy(choiceButton.gameObject);
                }
            }

            _choiceButtons.Clear();

            if (choiceRoot != null)
            {
                choiceRoot.gameObject.SetActive(false);
            }
        }

        public void ShowAction(string label, Action onActionClicked)
        {
            ClearChoices();
            ClearAction();

            actionRoot.gameObject.SetActive(true);

            _actionButton = Instantiate(actionButtonPrefab, actionRoot);
            _actionButton.Bind(label, onActionClicked);
        }

        public void HideAction()
        {
            ClearAction();

            if (actionRoot != null)
            {
                actionRoot.gameObject.SetActive(false);
            }
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            eventViewCanvasGroup.interactable = isEnabled;
            eventViewCanvasGroup.blocksRaycasts = isEnabled;
        }

        public void ResetView()
        {
            SetTitle(string.Empty);
            SetDescription(string.Empty);

            ClearChoices();
            HideAction();

            SetInteractionEnabled(true);
        }

        private void ClearAction()
        {
            if (_actionButton == null)
            {
                return;
            }

            Destroy(_actionButton.gameObject);
            _actionButton = null;
        }
    }
}