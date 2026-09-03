using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace OzGameLab01.UI
{
    public class EventChoiceUI : MonoBehaviour
    {
        [SerializeField]
        private Image choiceImg;
        [SerializeField]
        private TextMeshProUGUI choiceDialog;
        
        public int eventChoiceIndex;

        public event Action<int> OnChoice;
        public bool SetEventChoiceUI(EventChoice choiceSO)
        {
            //choiceImg
            choiceDialog.text = choiceSO.ChoiceDialog;

            return true;
        }
        public void OnClick()
        {
            OnChoice?.Invoke(eventChoiceIndex);
        }
    }
}
