using OzGameLab01.Managers;
using UnityEngine;


namespace OzGameLab01.Test
{
    public class UnitPlaceTestManager : MonoBehaviour
    {
        [SerializeField]
        private UnitPlaceManager unitPlaceSystemPanelPrefab;
        private UnitPlaceManager unitPlaceSystemPanel;
        [SerializeField]
        private Canvas mapCanvas;

        private void Awake()
        {
            unitPlaceSystemPanel = Instantiate(unitPlaceSystemPanelPrefab,mapCanvas.transform,false);
            unitPlaceSystemPanel.transform.localPosition = Vector3.zero;
            unitPlaceSystemPanel.gameObject.SetActive(false);
        }

        public void OnUnitPlaceSystem()
        {
            unitPlaceSystemPanel.gameObject.SetActive(true);
        }
    }
}