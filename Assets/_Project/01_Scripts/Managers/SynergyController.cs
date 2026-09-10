using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// CombatManager에서 분리된 시너지 집계/발동/표시 책임을 담당합니다.
    /// Inspector 참조는 CombatManager가 그대로 들고 있고, 이 클래스는 그 값을
    /// 생성자로 전달받아 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요).
    /// </summary>
    public class SynergyController
    {
        private readonly UnitRosterData rosterData;
        private readonly Transform synergyPanelRoot;
        private readonly SynergyItemView synergyItemTemplate;
        private readonly Color synergyActiveColor;
        private readonly Color synergyInactiveColor;
        private readonly Object logContext;

        private Dictionary<int, List<SynergyDefinition>> _unitTraitsById;
        private Dictionary<SynergyDefinition, int> _traitCounts;

        public SynergyController(
            UnitRosterData rosterData,
            Transform synergyPanelRoot,
            SynergyItemView synergyItemTemplate,
            Color synergyActiveColor,
            Color synergyInactiveColor,
            Object logContext)
        {
            this.rosterData = rosterData;
            this.synergyPanelRoot = synergyPanelRoot;
            this.synergyItemTemplate = synergyItemTemplate;
            this.synergyActiveColor = synergyActiveColor;
            this.synergyInactiveColor = synergyInactiveColor;
            this.logContext = logContext;
        }

        /// <summary>
        /// 유닛 id별 시너지 트레이트를 읽어 둡니다. UnitRosterData.UnitTraits(수동 목록,
        /// 로스터 준비 화면 전용)가 아니라 실제로 전투에 등장하는 유닛들의 jobType/tribeType으로
        /// 직접 계산합니다 — 유닛이 UnitRosterData의 placeholder든 실제 DB(JSON)든 동일하게 동작합니다.
        /// </summary>
        public void BuildUnitTraitLookup(Dictionary<int, UnitData> unitDataById)
        {
            _unitTraitsById = new Dictionary<int, List<SynergyDefinition>>();
            if (rosterData == null || unitDataById == null)
            {
                return;
            }

            UnitRosterData.RegisterActive(rosterData, logContext);

            foreach (KeyValuePair<int, UnitData> kvp in unitDataById)
            {
                List<SynergyDefinition> traits = new List<SynergyDefinition>();

                SynergyDefinition jobTrait = rosterData.GetJobTrait(kvp.Value.jobType);
                if (jobTrait != null)
                {
                    traits.Add(jobTrait);
                }

                SynergyDefinition tribeTrait = rosterData.GetTribeTrait(kvp.Value.tribeType);
                if (tribeTrait != null)
                {
                    traits.Add(tribeTrait);
                }

                _unitTraitsById[kvp.Key] = traits;
            }
        }

        public void ApplySynergies(Dictionary<CombatManager.SlotKey, int> spawnedFormation, Unit[,] slotUnits)
        {
            // 팀 전체에서 각 트레이트를 보유한 유닛 수를 센다 (시너지 발동 여부 판정용).
            // 인스펙터 폴백 편성이 아니라 실제로 스폰된 편성(spawnedFormation)을 기준으로 삼아야
            // 배치 화면에서 넘어온 편성에도 시너지가 정상 반영된다.
            _traitCounts = SynergyPanelUtility.CountTraits(spawnedFormation.Values, _unitTraitsById);

            // 발동된 시너지의 보너스는 해당 트레이트를 실제로 보유한 유닛에게만 적용한다.
            foreach (KeyValuePair<CombatManager.SlotKey, int> kvp in spawnedFormation)
            {
                Unit unit = slotUnits[kvp.Key.column, (int)kvp.Key.row];
                if (unit == null || !_unitTraitsById.TryGetValue(kvp.Value, out List<SynergyDefinition> traits) || traits == null)
                {
                    continue;
                }

                foreach (SynergyDefinition definition in traits)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    _traitCounts.TryGetValue(definition, out int count);
                    if (definition.TryGetActiveTier(count, out SynergyDefinition.Tier tier))
                    {
                        unit.ApplySynergyBonus(tier.hpMultiplier, tier.attackMultiplier);
                    }
                }
            }
        }

        /// <summary>
        /// 보유 중인(카운트 1 이상) 시너지를 패널에 표시합니다.
        /// 발동 중인 시너지와 아직 발동하지 않은 시너지를 색상으로 구분합니다.
        /// </summary>
        public void PopulateSynergyPanel()
        {
            if (rosterData == null || synergyPanelRoot == null || synergyItemTemplate == null)
            {
                return;
            }

            for (int i = synergyPanelRoot.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(synergyPanelRoot.GetChild(i).gameObject);
            }

            List<SynergyPanelUtility.DisplayItem> displayItems =
                SynergyPanelUtility.BuildDisplayItems(rosterData.SynergyDefinitions, _traitCounts);

            foreach (SynergyPanelUtility.DisplayItem displayItem in displayItems)
            {
                SynergyItemView item = Object.Instantiate(synergyItemTemplate, synergyPanelRoot);
                item.gameObject.SetActive(true);
                item.SetTitle(displayItem.Definition.DisplayName);
                item.SetStackText(displayItem.StackText);
                item.SetBackgroundColor(displayItem.IsActive ? synergyActiveColor : synergyInactiveColor);
            }
        }
    }
}
