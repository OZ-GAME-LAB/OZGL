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

            // 실제 몬스터 데이터베이스(JSON)를 로드해 monsterStats를 캐싱합니다.
            // EnemyData.xlsx의 BaseStat(Turn1 기준) 수치를 담고 있으며, 턴 진행에 따른
            // 성장(Value 배율)은 EnemyValueResolver가 별도로 계산해 적용합니다.
            TextAsset jsonFile = Resources.Load<TextAsset>("EnemyData");
            if (jsonFile == null)
            {
                Debug.LogWarning("[MonsterRosterData] 05_Data/Resources/EnemyData.json을 찾을 수 없어 monsterStats가 마지막으로 저장된 값 그대로 유지됩니다.", this);
                return;
            }

            List<MonsterData> parsed;
            try
            {
                parsed = ParseMonsterList(jsonFile.text);
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                Debug.LogWarning($"[MonsterRosterData] EnemyData.json 파싱에 실패해 monsterStats가 마지막으로 저장된 값 그대로 유지됩니다. ({e.Message})", this);
                return;
            }

            if (parsed == null)
            {
                Debug.LogWarning("[MonsterRosterData] EnemyData.json에 monsterList가 없어 monsterStats가 마지막으로 저장된 값 그대로 유지됩니다.", this);
                return;
            }

            monsterStats = parsed;
        }

        /// <summary>
        /// 몬스터 JSON 텍스트를 MonsterData 목록으로 역직렬화합니다. OnEnable()에서 분리해둔
        /// 순수 함수라 Resources/에셋 로드 없이도 EditMode 테스트로 검증할 수 있습니다.
        /// </summary>
        public static List<MonsterData> ParseMonsterList(string json)
        {
            MonsterDataList list = Newtonsoft.Json.JsonConvert.DeserializeObject<MonsterDataList>(json);
            return list?.monsterList;
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
