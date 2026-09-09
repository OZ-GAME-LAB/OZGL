using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 적 유닛 id별 스탯을 담은 공유 데이터입니다. UnitRosterData와 같은 방식(SO 기반, id 매칭)으로
    /// MonsterData를 등록해, CombatManager가 프리팹 하드코딩 대신 이 데이터를 참조해 적을 구성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterRosterData", menuName = "Combat/Monster Roster Data")]
    public class MonsterRosterData : ScriptableObject
    {
        [Tooltip("적 유닛 id별 스탯/스프라이트 주소.")]
        [SerializeField] private List<MonsterData> monsterStats = new List<MonsterData>();

        public IReadOnlyList<MonsterData> MonsterStats => monsterStats;

        public MonsterData GetById(int id)
        {
            foreach (MonsterData data in monsterStats)
            {
                if (data != null && data.id == id)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
