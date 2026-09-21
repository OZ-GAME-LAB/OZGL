using UnityEngine;

namespace OzGameLab01.UI.Battle
{
    [DisallowMultipleComponent]
    public sealed class CombatMapView : MonoBehaviour
    {
        [Header("World Spawn Points — 편성 인덱스 0~8 순서")]
        [SerializeField] private Transform[] allySpawnPoints = new Transform[9];
        [SerializeField] private Transform enemySpawnPoint;

        /// <summary>
        /// 편성 인덱스에 해당하는 월드 스폰 지점을 반환합니다.
        /// </summary>
        public bool TryGetAllySpawnPoint(int placementIndex, out Transform spawnPoint)
        {
            spawnPoint = null;

            if (allySpawnPoints == null ||
                placementIndex < 0 ||
                placementIndex >= allySpawnPoints.Length)
            {
                return false;
            }

            spawnPoint = allySpawnPoints[placementIndex];
            return spawnPoint != null;
        }

        /// <summary>
        /// 적의 월드 스폰 지점을 반환합니다.
        /// </summary>
        public bool TryGetEnemySpawnPoint(out Transform spawnPoint)
        {
            spawnPoint = enemySpawnPoint;
            return spawnPoint != null;
        }
    }
}
