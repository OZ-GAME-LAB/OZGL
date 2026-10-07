using UnityEngine;

namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 맵 생성기가 배치할 장애물과 상호작용 타일의 구성 규칙입니다.
    /// 맵 크기나 렌더링 연출처럼 씬 오브젝트에 종속적인 값은 포함하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBoardMapGenerationProfile", menuName = "OZGL/Board/Map Generation Profile")]
    public sealed class BoardMapGenerationProfile : ScriptableObject
    {
        [Header("Start Sequence")]
        [Tooltip("시작 타일의 3면을 바위로 막고, 남은 1면에 유닛 획득 타일을 배치합니다.")]
        [SerializeField] private bool forceUnitAtStart = true;

        [Header("Tree Clusters")]
        [Min(0)] [SerializeField] private int treeClusterCount = 2;
        [Min(1)] [SerializeField] private int minTreeClusterSize = 1;
        [Min(1)] [SerializeField] private int maxTreeClusterSize = 3;

        [Header("Rock Clusters")]
        [Min(0)] [SerializeField] private int rockClusterCount = 2;
        [Min(1)] [SerializeField] private int minRockClusterSize = 1;
        [Min(1)] [SerializeField] private int maxRockClusterSize = 4;

        [Header("Water Clusters")]
        [Min(0)] [SerializeField] private int waterClusterCount = 1;
        [Min(1)] [SerializeField] private int minWaterClusterSize = 1;
        [Min(1)] [SerializeField] private int maxWaterClusterSize = 5;

        [Header("Shop Tiles")]
        [Min(0)] [SerializeField] private int shopCount = 3;
        [Min(0)] [SerializeField] private int minShopDistance = 3;
        [Min(0)] [SerializeField] private int minShopDistanceFromStart;
        [Min(0)] [SerializeField] private int maxShopDistanceFromStart = 999;

        [Header("Event Tiles")]
        [Min(0)] [SerializeField] private int eventCount = 8;
        [Min(0)] [SerializeField] private int minEventDistance = 2;
        [Min(0)] [SerializeField] private int minEventDistanceFromStart;
        [Min(0)] [SerializeField] private int maxEventDistanceFromStart = 999;

        [Header("Battle Tiles")]
        [Min(0)] [SerializeField] private int battleCount = 15;
        [Min(0)] [SerializeField] private int minBattleDistance = 1;
        [Min(0)] [SerializeField] private int minBattleDistanceFromStart;
        [Min(0)] [SerializeField] private int maxBattleDistanceFromStart = 999;

        [Header("Unit Acquisition Tiles")]
        [Min(0)] [SerializeField] private int unitAcquisitionCount = 2;
        [Min(0)] [SerializeField] private int minUnitAcquisitionDistance = 4;
        [Min(0)] [SerializeField] private int minUnitAcquisitionDistanceFromStart = 2;
        [Min(0)] [SerializeField] private int maxUnitAcquisitionDistanceFromStart = 999;

        public void ApplyTo(BoardMapSettings settings)
        {
            settings.forceUnitAtStart = forceUnitAtStart;

            settings.treeClusterCount = treeClusterCount;
            settings.minTreeClusterSize = minTreeClusterSize;
            settings.maxTreeClusterSize = maxTreeClusterSize;
            settings.rockClusterCount = rockClusterCount;
            settings.minRockClusterSize = minRockClusterSize;
            settings.maxRockClusterSize = maxRockClusterSize;
            settings.waterClusterCount = waterClusterCount;
            settings.minWaterClusterSize = minWaterClusterSize;
            settings.maxWaterClusterSize = maxWaterClusterSize;

            settings.shopCount = shopCount;
            settings.minShopDistance = minShopDistance;
            settings.minShopDistFromStart = minShopDistanceFromStart;
            settings.maxShopDistFromStart = maxShopDistanceFromStart;
            settings.eventCount = eventCount;
            settings.minEventDistance = minEventDistance;
            settings.minEventDistFromStart = minEventDistanceFromStart;
            settings.maxEventDistFromStart = maxEventDistanceFromStart;
            settings.battleCount = battleCount;
            settings.minBattleDistance = minBattleDistance;
            settings.minBattleDistFromStart = minBattleDistanceFromStart;
            settings.maxBattleDistFromStart = maxBattleDistanceFromStart;
            settings.unitAcquisitionCount = unitAcquisitionCount;
            settings.minUnitAcquisitionDistance = minUnitAcquisitionDistance;
            settings.minUnitAcquisitionDistFromStart = minUnitAcquisitionDistanceFromStart;
            settings.maxUnitAcquisitionDistFromStart = maxUnitAcquisitionDistanceFromStart;
        }
    }
}
