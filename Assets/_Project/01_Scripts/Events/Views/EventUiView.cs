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
        private readonly List<EventActionButtonView> _actionButtons = new();

        private void Awake()
        {
            HideAction();
            SetInteractionEnabled(true);
        }

        public void SetTitle(string title)
        {
            eventTitleText.text = title ?? string.Empty;
        }

        public void SetDescription(string description)
        {
            eventDescriptionText.text = description ?? string.Empty;
        }

        public void ShowChoices(List<EventChoice> choices, Action<int> onChoiceSelected)
        {
            ClearChoices();
            HideAction();

            choiceRoot.gameObject.SetActive(true);

            for (int i = 0; i < choices.Count; i++) 
            {
                EventChoiceButtonView choiceButton = Instantiate(choiceButtonPrefab, choiceRoot);

                choiceButton.Bind(i, choices[i], onChoiceSelected);
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

        public void ShowAction(List<EventChoice> choices, Action<int> onActionClicked)
        {
            ClearChoices();
            ClearAction();

            actionRoot.gameObject.SetActive(true);

            for (int i = 0; i < choices.Count; i++)
            {
                EventActionButtonView actionButton = Instantiate(actionButtonPrefab, actionRoot);

                actionButton.Bind(i, choices[i], onActionClicked);
                _actionButtons.Add(actionButton);
            }
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

        private void ClearAction()
        {
            if (_actionButtons == null)
            {
                return;
            }

            for(int i=0; i< _actionButtons.Count; i++)
            {
                Destroy(_actionButtons[i].gameObject);
            }
            _actionButtons.Clear();// = null;
        }
    }
}