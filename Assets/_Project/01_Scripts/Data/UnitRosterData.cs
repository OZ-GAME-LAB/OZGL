using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 유닛·스킬·시너지를 한 곳에 모아 전투 씬/로스터 준비 화면/인벤토리(획득·보상·세이브)에
    /// 뿌려주는 허브 데이터입니다. 이 asset 하나만 참조하면 되도록 통일합니다.
    ///
    /// UnitStats는 더 이상 손으로 채워두는 placeholder가 아니라, 실제 유닛 데이터베이스(JSON)를
    /// 읽어 캐싱해두는 값입니다. 지금은 TempUnitData.json(임시, 실제 스킬 기획 전)을 읽고,
    /// 나중에 실제 UnitJSON으로 전환할 때는 OnEnable()의 로드 경로만 바꾸면 됩니다.
    ///
    /// 아군은 공용 프리팹 하나(CombatManager.allyTemplatePrefab)를 Instantiate한 뒤
    /// UnitStats의 값으로 Unit.Configure()를 호출해 생성합니다. id별 프리팹은 더 이상 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitRosterData", menuName = "Combat/Unit Roster Data")]
    public class UnitRosterData : ScriptableObject
    {
        [System.Serializable]
        public struct JobTraitEntry
        {
            public UnitTypeJob job;
            public SynergyDefinition trait;
        }

        [System.Serializable]
        public struct TribeTraitEntry
        {
            public UnitTypeTribe tribe;
            public SynergyDefinition trait;
        }

        [Tooltip("유닛 데이터베이스(JSON)에서 로드해 캐싱한 값입니다. 직접 편집하지 마세요 — OnEnable()에서 덮어씁니다.")]
        [SerializeField] private List<UnitData> unitStats = new List<UnitData>();

        [Tooltip("UnitData.skillIds가 참조하는 스킬 정의 테이블. SkillData.id와 매칭.")]
        [SerializeField] private List<SkillData> skillDefinitions = new List<SkillData>();

        [Tooltip("UnitData.jobType별 시너지 트레이트. 실제 유닛 데이터베이스(JSON) 기반 전투에서 사용.")]
        [SerializeField] private List<JobTraitEntry> jobTraits = new List<JobTraitEntry>();

        [Tooltip("UnitData.tribeType별 시너지 트레이트. 실제 유닛 데이터베이스(JSON) 기반 전투에서 사용.")]
        [SerializeField] private List<TribeTraitEntry> tribeTraits = new List<TribeTraitEntry>();

        [Tooltip("트레이트 조합으로 발동 가능한 시너지 목록.")]
        [SerializeField] private List<SynergyDefinition> synergyDefinitions = new List<SynergyDefinition>();

        public IReadOnlyList<UnitData> UnitStats => unitStats;
        public IReadOnlyList<SynergyDefinition> SynergyDefinitions => synergyDefinitions;

        public SynergyDefinition GetJobTrait(UnitTypeJob job)
        {
            foreach (JobTraitEntry entry in jobTraits)
            {
                if (entry.job == job)
                {
                    return entry.trait;
                }
            }

            return null;
        }

        public SynergyDefinition GetTribeTrait(UnitTypeTribe tribe)
        {
            foreach (TribeTraitEntry entry in tribeTraits)
            {
                if (entry.tribe == tribe)
                {
                    return entry.trait;
                }
            }

            return null;
        }

        /// <summary>
        /// 현재 전투에서 활성화된 로스터(RegisterActive로 등록됨). Unit이 UnitData.skillIds를
        /// SkillData로 풀어낼 때 참조합니다. GameDB/어드레서블 이전 전까지의 임시 조회 경로입니다.
        /// </summary>
        public static UnitRosterData Active => _activeInstance;

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

        private void OnEnable()
        {
            // TempUnitData.json 테스트 유닛이 아직 실제 스킬 기획을 갖지 못해
            // 임시로 부여하는 공용 기본공격 + 디버프 4종 검증용 액티브를 TempRosterSeed에서 채웁니다.
            List<SkillData> tempSkills = TempRosterSeed.CreateAllTempUnitSkills();
            HashSet<int> tempSkillIds = new HashSet<int>(tempSkills.ConvertAll(skill => skill.id));
            skillDefinitions.RemoveAll(skill => skill != null && tempSkillIds.Contains(skill.id));
            skillDefinitions.AddRange(tempSkills);

            // 유닛 데이터베이스(JSON)를 로드해 UnitStats를 캐싱합니다.
            // 지금은 TempUnitData.json(임시)이고, 나중에 실제 UnitJSON으로 바뀌어도 이 asset을
            // 참조하는 CombatManager/PlayerInventoryManager 등은 그대로 둘 수 있습니다.
            TextAsset jsonFile = Resources.Load<TextAsset>("Data/TempUnitData");
            if (jsonFile != null)
            {
                UnitDataList list = Newtonsoft.Json.JsonConvert.DeserializeObject<UnitDataList>(jsonFile.text);
                if (list?.unitList != null)
                {
                    unitStats = list.unitList;
                }
            }
        }

        private void OnValidate()
        {
            CombatDataValidator.ValidateRoster(this, this);
        }

        private static UnitRosterData _activeInstance;

        /// <summary>
        /// CombatManager와 UnitFormationController가 각자 들고 있는 rosterData 참조가
        /// 실제로 같은 asset을 가리키는지 확인합니다. 이번 플레이 세션에서 처음 등록된
        /// asset을 기준으로, 이후 다른 asset이 등록되면 에러 로그를 남깁니다.
        /// </summary>
        public static void RegisterActive(UnitRosterData data, Object context)
        {
            if (data == null)
            {
                return;
            }

            if (_activeInstance == null)
            {
                _activeInstance = data;
                return;
            }

            if (_activeInstance != data)
            {
                Debug.LogError(
                    $"[UnitRosterData] 서로 다른 로스터 asset이 쓰이고 있습니다: '{_activeInstance.name}' vs '{data.name}'. " +
                    "CombatManager와 UnitFormationController의 rosterData 참조가 같은 asset을 가리키는지 확인하세요.",
                    context);
            }
        }
    }
}
