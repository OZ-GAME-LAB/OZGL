namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 지형 생성과 타일 배치 설정을 보관합니다.
    /// </summary>
    public sealed class BoardMapSettings
    {
        public int totalNodeCount = 150;
        public float maxRadius = 25f;
        public float noiseScale = 0.15f;
        public float edgeFalloffStrength = 0.8f;
        public float coreRadius = 4f;
        public int treeClusterCount = 2;
        public int minTreeClusterSize = 1;
        public int maxTreeClusterSize = 3;
        public int rockClusterCount = 2;
        public int minRockClusterSize = 1;
        public int maxRockClusterSize = 4;
        public int waterClusterCount = 1;
        public int minWaterClusterSize = 1;
        public int maxWaterClusterSize = 5;
        public bool forceUnitAtStart = true;
        public int bossCount = 1;
        public int minBossDistance = 5;
        public int minBossDistanceFromStart = 4;
        public int maxBossDistanceFromStart = 999;
        public int shopCount = 3;
        public int minShopDistance = 3;
        public int minShopDistFromStart = 0;
        public int maxShopDistFromStart = 999;
        public int eliteCount = 3;
        public int minEliteDistance = 3;
        public int minEliteDistFromStart = 0;
        public int maxEliteDistFromStart = 999;
        public int eventCount = 8;
        public int minEventDistance = 2;
        public int minEventDistFromStart = 0;
        public int maxEventDistFromStart = 999;
        public int battleCount = 15;
        public int minBattleDistance = 1;
        public int minBattleDistFromStart = 0;
        public int maxBattleDistFromStart = 999;
        public int unitAcquisitionCount = 2;
        public int minUnitAcquisitionDistance = 4;
        public int minUnitAcquisitionDistFromStart = 2;
        public int maxUnitAcquisitionDistFromStart = 999;
        public bool hasTreePrefabs;
        public bool hasRockPrefabs;
        public bool hasWaterPrefabs;
    }
}
