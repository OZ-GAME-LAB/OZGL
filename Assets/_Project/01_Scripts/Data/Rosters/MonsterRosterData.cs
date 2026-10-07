using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 적 유닛 id별 스탯을 담은 공유 데이터입니다. UnitRosterData와 같은 방식(SO 기반, id 매칭)으로
    /// MonsterData를 등록해, CombatManager가 프리팹 하드코딩 대신 이 데이터를 참조해 적을 구성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterRosterData", menuName = "Combat/Monster Roster Data")]
    public class MonsterRosterData : ScriptableObject
    {
        [Tooltip("적 유닛 id별 스탯/스프라이트 주소.")]
        [FormerlySerializedAs("monsterStats")]
        [SerializeField] private List<MonsterData> _monsterStats = new List<MonsterData>();

        [Tooltip("MonsterData.skillIds가 참조하는 스킬 정의 테이블. SkillData.id와 매칭.")]
        [FormerlySerializedAs("skillDefinitions")]
        [SerializeField] private List<SkillData> _skillDefinitions = new List<SkillData>();

        public IReadOnlyList<MonsterData> MonsterStats => _monsterStats;
        public IReadOnlyList<SkillData> SkillDefinitions => _skillDefinitions;

        /// <summary>
        /// 씬에 로드된 MonsterRosterData asset. Unit이 MonsterData.skillIds를 SkillData로
        /// 풀어낼 때 참조합니다. GameDB/어드레서블 이전 전까지의 임시 조회 경로입니다.
        /// </summary>
        public static MonsterRosterData Active { get; private set; }

        private void OnEnable()
        {
            Active = this;
            RosterDataLoader.LoadMonster(ref _monsterStats, this);
        }

        /// <summary>
        /// 몬스터 JSON 텍스트를 MonsterData 목록으로 역직렬화합니다. OnEnable()에서 분리해둔
        /// 순수 함수라 Resources/에셋 로드 없이도 EditMode 테스트로 검증할 수 있습니다.
        /// </summary>
        public static List<MonsterData> ParseMonsterList(string json)
        {
            return JsonDataParser.ParseOptional<MonsterData, MonsterDataList>(json);
        }

        public MonsterData GetById(int id) => RosterDataRules.FindFirst(_monsterStats, id, data => data.id);

        public SkillData GetSkill(int id) => RosterDataRules.FindFirst(_skillDefinitions, id, skill => skill.id);
    }
}
