using OzGameLab01.Managers;
using UnityEngine;


namespace OzGameLab01.Test
{
    public class UnitPlaceTestManager : MonoBehaviour
    {
        //[SerializeField]
        //private UnitPlaceManager unitPlaceSystemPanelPrefab;
        //private UnitPlaceManager unitPlaceSystemPanel;

        [SerializeField]
        [Tooltip("새로 분리한 UnitPlacementView 프리팹")]
        private GameObject unitPlaceSystemPanelPrefab;

        [SerializeField]
        [Tooltip("유닛 배치 UI가 생성될 Canvas")]
        private Canvas mapCanvas;

        private GameObject unitPlaceSystemPanel;

        private void Awake()
        {
            if (unitPlaceSystemPanelPrefab == null)
            {
                Debug.LogError(
                    "[UnitPlaceTestManager] 유닛 배치 UI 프리팹이 연결되지 않았습니다.",
                    this);

                return;
            }

            if (mapCanvas == null)
            {
                Debug.LogError(
                    "[UnitPlaceTestManager] 유닛 배치 UI가 생성될 Canvas가 연결되지 않았습니다.",
                    this);

                return;
            }
            unitPlaceSystemPanel = Instantiate(unitPlaceSystemPanelPrefab, mapCanvas.transform, false);
            unitPlaceSystemPanel.transform.SetAsLastSibling();
            unitPlaceSystemPanel.SetActive(false);

            //unitPlaceSystemPanel = Instantiate(unitPlaceSystemPanelPrefab,mapCanvas.transform,false);
            //unitPlaceSystemPanel.transform.localPosition = Vector3.zero;
            //unitPlaceSystemPanel.gameObject.SetActive(false);
        }

        public void OnUnitPlaceSystem()
        {
            if (unitPlaceSystemPanel == null)
            {
                Debug.LogWarning(
                    "[UnitPlaceTestManager] 생성된 유닛 배치 UI가 없습니다.",
                    this);

                return;
            }

            unitPlaceSystemPanel.transform.SetAsLastSibling();
            unitPlaceSystemPanel.SetActive(true);
            //unitPlaceSystemPanel.gameObject.SetActive(true);
        }
    }
}