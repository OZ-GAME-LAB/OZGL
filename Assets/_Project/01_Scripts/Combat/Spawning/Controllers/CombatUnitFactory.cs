using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// AllySpawner에서 분리된 아군/적 유닛 Instantiate + Configure 책임을 담당합니다.
    /// 스폰 직후 시각 바인딩(CombatUnitViewBinder)과 활성화(SetActive) 순서는 호출부가
    /// 그대로 소유합니다 — 이 클래스는 "설정이 끝난 Unit 컴포넌트를 만드는" 역할만 합니다.
    /// </summary>
    public static class CombatUnitFactory
    {
        /// <summary>
        /// 유닛의 Anchors/Ground(발밑)가 스폰 슬롯 위치에 오도록 유닛을 옮깁니다.
        /// 크기 조정(보스 스케일 등)이 끝난 뒤에 호출해야 합니다. 앵커가 없으면 그대로 둡니다.
        /// </summary>
        public static void AlignGroundToSlot(Unit unit, Transform slot)
        {
            if (unit == null || slot == null) return;
            Transform ground = unit.transform.Find(UnitPresenter.GroundAnchorPath);
            if (ground == null) return;
            unit.transform.position += slot.position - ground.position;
        }

        public static Unit CreateAlly(GameObject prefab, Transform parent, string instanceName, UnitData data)
        {
            GameObject instance = Object.Instantiate(prefab, parent, false);
            return ConfigureAlly(instance, instanceName, data);
        }

        /// <summary>
        /// Addressables에서 준비된 아군 인스턴스에 슬롯 Transform과 전투 데이터를 적용합니다.
        /// </summary>
        public static Unit ConfigureAlly(GameObject instance, string instanceName, UnitData data)
        {
            if (instance == null)
            {
                Debug.LogError("[CombatUnitFactory] 설정할 아군 인스턴스가 없습니다.");
                return null;
            }

            instance.name = instanceName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Unit unit = instance.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError($"[CombatUnitFactory] '{instance.name}' 루트에 Unit 컴포넌트가 없습니다.", instance);
                Object.Destroy(instance);
                return null;
            }

            unit.Configure(data);
            return unit;
        }

        public static Unit CreateEnemy(GameObject prefab, Transform parent, MonsterData data)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Unit unit = instance.GetComponent<Unit>();
            if (unit != null && data != null)
            {
                unit.ConfigureEnemy(data);
            }

            return unit;
        }
    }
}
