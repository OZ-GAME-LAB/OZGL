using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.Test
{

    public class ChoiceEventTestManager : MonoBehaviour
    {
        [SerializeField]
        private ChoiceEventManager choiceEventPrefab;
        [SerializeField]
        private Canvas mapCanvas;

        private ChoiceEventManager _choiceEventPanel;

        private void Awake()
        {
            _choiceEventPanel = Instantiate(choiceEventPrefab, mapCanvas.transform, false);
            _choiceEventPanel.transform.localPosition = Vector3.zero;
            _choiceEventPanel.gameObject.SetActive(false);
        }

        public void OnChoiceEventSystem()
        {
            _choiceEventPanel.gameObject.SetActive(true);
            _choiceEventPanel.RandomEventOpenTest();
        }
    }
}