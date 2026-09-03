using OzGameLab01.Managers;
using UnityEngine;


namespace OzGameLab01.Test
{
    public class UnitPlaceTestManager : MonoBehaviour
    {
        [SerializeField]
        private UnitPlaceManager unitPlaceSystemPanelPrefab;
        private UnitPlaceManager _unitPlaceSystemPanel;
        [SerializeField]
        private Canvas mapCanvas;

        private void Awake()
        {
            _unitPlaceSystemPanel = Instantiate(unitPlaceSystemPanelPrefab,mapCanvas.transform,false);
            _unitPlaceSystemPanel.transform.localPosition = Vector3.zero;
            _unitPlaceSystemPanel.gameObject.SetActive(false);
        }

        public void OnUnitPlaceSystem()
        {
            _unitPlaceSystemPanel.gameObject.SetActive(true);
        }
    }
}