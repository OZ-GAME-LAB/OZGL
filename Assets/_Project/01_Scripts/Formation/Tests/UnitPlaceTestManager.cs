using UnityEngine;

namespace OzGameLab01.Test
{
    /// <summary>
    /// 테스트 씬에서 유닛 배치 화면을 생성하고 열어줍니다.
    /// </summary>
    public class UnitPlaceTestManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("새로 분리한 UnitPlacementView 프리팹")]
        private GameObject unitPlaceSystemPanelPrefab;

        [SerializeField]
        [Tooltip("유닛 배치 화면이 생성될 Canvas")]
        private Canvas mapCanvas;

        private GameObject unitPlaceSystemPanel;

        private void Awake()
        {
            if (unitPlaceSystemPanelPrefab == null)
            {
                Debug.LogError(
                    "[UnitPlaceTestManager] 유닛 배치 화면 프리팹이 연결되지 않았습니다.",
                    this);

                return;
            }

            if (mapCanvas == null)
            {
                Debug.LogError(
                    "[UnitPlaceTestManager] 유닛 배치 화면이 생성될 Canvas가 연결되지 않았습니다.",
                    this);

                return;
            }

            unitPlaceSystemPanel = Instantiate(
                unitPlaceSystemPanelPrefab,
                mapCanvas.transform,
                false);

            unitPlaceSystemPanel.transform.SetAsLastSibling();
            unitPlaceSystemPanel.SetActive(false);
        }

        /// <summary>
        /// 생성된 유닛 배치 화면을 엽니다.
        /// </summary>
        public void OnUnitPlaceSystem()
        {
            if (unitPlaceSystemPanel == null)
            {
                Debug.LogWarning(
                    "[UnitPlaceTestManager] 생성된 유닛 배치 화면이 없습니다.",
                    this);

                return;
            }

            unitPlaceSystemPanel.transform.SetAsLastSibling();
            unitPlaceSystemPanel.SetActive(true);
        }
    }
}