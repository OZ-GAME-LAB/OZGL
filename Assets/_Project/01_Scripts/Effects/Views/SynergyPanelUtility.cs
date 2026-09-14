using System.Collections.Generic;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 시너지 트레이트 집계와 표시용 정렬/필터링을 담당하는 공용 로직입니다.
    /// CombatManager 쪽 SynergyController와 UnitFormationController가 각자
    /// 거의 동일한 계산(트레이트 카운트→정렬→발동 여부/표시 텍스트)을 따로 구현하고
    /// 있던 것을 이 곳으로 통합합니다. 실제 패널 UI를 Instantiate/Destroy하는 방식은
    /// 두 화면의 씬 구성이 달라(템플릿을 패널 안에 두는지 여부 등) 각자 유지합니다.
    /// </summary>
    public static class SynergyPanelUtility
    {
        public readonly struct DisplayItem
        {
            public readonly SynergyDefinition Definition;
            public readonly bool IsActive;
            public readonly string StackText;

            public DisplayItem(SynergyDefinition definition, bool isActive, string stackText)
            {
                Definition = definition;
                IsActive = isActive;
                StackText = stackText;
            }
        }

        /// <summary>
        /// 유닛 id 목록을 트레이트 테이블과 대조해 트레이트(=SynergyDefinition)별 보유 수를 센다.
        /// </summary>
        public static Dictionary<SynergyDefinition, int> CountTraits(
            IEnumerable<int> unitIds,
            Dictionary<int, List<SynergyDefinition>> traitsById)
        {
            Dictionary<SynergyDefinition, int> traitCounts = new Dictionary<SynergyDefinition, int>();

            foreach (int unitId in unitIds)
            {
                if (!traitsById.TryGetValue(unitId, out List<SynergyDefinition> traits) || traits == null)
                {
                    continue;
                }

                foreach (SynergyDefinition trait in traits)
                {
                    if (trait == null)
                    {
                        continue;
                    }

                    traitCounts.TryGetValue(trait, out int count);
                    traitCounts[trait] = count + 1;
                }
            }

            return traitCounts;
        }

        /// <summary>
        /// 보유 중인(카운트 1 이상) 시너지를, 발동 수가 높은 순으로 정렬해 표시용 목록으로 만든다.
        /// </summary>
        public static List<DisplayItem> BuildDisplayItems(
            IReadOnlyList<SynergyDefinition> definitions,
            Dictionary<SynergyDefinition, int> traitCounts)
        {
            List<SynergyDefinition> sortedDefinitions = new List<SynergyDefinition>(definitions);
            sortedDefinitions.Sort((a, b) => GetTraitCount(traitCounts, b).CompareTo(GetTraitCount(traitCounts, a)));

            List<DisplayItem> items = new List<DisplayItem>();
            foreach (SynergyDefinition definition in sortedDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                int count = GetTraitCount(traitCounts, definition);
                if (count <= 0)
                {
                    continue;
                }

                bool isActive = definition.TryGetActiveTier(count, out _);
                string stackText = definition.TryGetNextThreshold(count, out int nextThreshold)
                    ? $"{count}/{nextThreshold}"
                    : count.ToString();

                items.Add(new DisplayItem(definition, isActive, stackText));
            }

            return items;
        }

        private static int GetTraitCount(Dictionary<SynergyDefinition, int> traitCounts, SynergyDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            traitCounts.TryGetValue(definition, out int count);
            return count;
        }
    }
}
