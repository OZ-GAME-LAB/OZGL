using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// AllySpawner에서 분리된 유닛 프리팹 리소스 로드 책임을 담당합니다.
    /// spriteAddress 기반 Resources.Load를 우선 시도하고, 실패하면 폴백 프리팹을 쓰는
    /// 정책을 한 곳에 모읍니다.
    /// </summary>
    public static class UnitPrefabProvider
    {
        public static GameObject GetAllyPrefab(UnitData data, GameObject fallbackTemplate)
        {
            GameObject prefab = null;
            if (data != null && !string.IsNullOrEmpty(data.spriteAddress))
            {
                prefab = Resources.Load<GameObject>($"Characters/{data.spriteAddress}");
            }

            if (prefab == null)
            {
                if (data != null && !string.IsNullOrEmpty(data.spriteAddress))
                {
                    Debug.LogWarning($"[UnitPrefabProvider] '{data.spriteAddress}' 캐릭터 프리팹이 " +
                        $"Resources/Characters에 없어 폴백 프리팹으로 스폰합니다. (유닛 ID: {data.id})");
                }

                prefab = fallbackTemplate;
            }

            if (prefab == null)
            {
                Debug.LogError($"[UnitPrefabProvider] 아군 프리팹을 찾을 수 없습니다. " +
                    $"(spriteAddress: {data?.spriteAddress})");
            }

            return prefab;
        }

        public static GameObject GetEnemyPrefab(string resourceName)
        {
            return Resources.Load<GameObject>(resourceName);
        }
    }
}
