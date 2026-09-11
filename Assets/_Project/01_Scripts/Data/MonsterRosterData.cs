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

        [Tooltip("MonsterData.skillIds가 참조하는 스킬 정의 테이블. SkillData.id와 매칭.")]
        [SerializeField] private List<SkillData> skillDefinitions = new List<SkillData>();

        public IReadOnlyList<MonsterData> MonsterStats => monsterStats;

        /// <summary>
        /// 씬에 로드된 MonsterRosterData asset. Unit이 MonsterData.skillIds를 SkillData로
        /// 풀어낼 때 참조합니다. GameDB/어드레서블 이전 전까지의 임시 조회 경로입니다.
        /// </summary>
        public static MonsterRosterData Active { get; private set; }

        private void OnEnable()
        {
            Active = this;

            // 실제 몬스터 스키마(EnemyData.xlsx)가 턴 스케일링 커브뿐이라 아직 로스터 형태가 아닙니다.
            // 그 전까지 쓸 임시 데이터를 TempRosterSeed 한 곳에서 가져와 채웁니다.
            monsterStats = TempRosterSeed.CreateMonsterRoster();
        }

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

        public SkillData GetSkill(int id)
        {
            foreach (SkillData skill in skillDefinitions)
            {
                if (skill != null && skill.id == id)
                {
                    return skill;
                }
            }

            return null;
        }
    }
}
