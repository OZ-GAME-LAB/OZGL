using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Resources에 남아 있는 적 프리팹 로드 책임을 담당합니다.
    /// </summary>
    public static class UnitPrefabProvider
    {
        public static GameObject GetEnemyPrefab(string resourceName)
        {
            return Resources.Load<GameObject>(resourceName);
        }
    }
}
