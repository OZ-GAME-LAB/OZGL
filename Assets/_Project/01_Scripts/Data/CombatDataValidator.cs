using OzGameLab01.Data;
using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 전투 진입 전에 공유 데이터의 구조를 검증합니다.
    /// 검증은 실패를 조용히 삼키지 않고, 문제가 있는 asset과 항목을 로그로 남깁니다.
    /// </summary>
    public static class CombatDataValidator
    {
        public static bool ValidateRoster(UnitRosterData roster, Object context = null)
        {
            if (roster == null)
            {
                Debug.LogError("[CombatDataValidator] UnitRosterData가 할당되지 않았습니다.", context);
                return false;
            }

            bool valid = true;
            HashSet<int> unitIds = new HashSet<int>();
            foreach (UnitData unit in roster.UnitStats)
            {
                if (!ValidateUnitData(unit, context))
                {
                    valid = false;
                    continue;
                }

                if (!unitIds.Add(unit.id))
                {
                    Debug.LogError($"[CombatDataValidator] UnitData ID가 중복되었습니다: {unit.id}", context);
                    valid = false;
                }
            }

            HashSet<int> traitEntryIds = new HashSet<int>();
            foreach (UnitRosterData.UnitTraitEntry entry in roster.UnitTraits)
            {
                if (!traitEntryIds.Add(entry.id))
                {
                    Debug.LogError($"[CombatDataValidator] UnitTraitEntry ID가 중복되었습니다: {entry.id}", context);
                    valid = false;
                }

                if (!unitIds.Contains(entry.id))
                {
                    Debug.LogError($"[CombatDataValidator] 트레이트가 존재하지 않는 유닛 ID를 참조합니다: {entry.id}", context);
                    valid = false;
                }

                if (entry.traits == null)
                {
                    Debug.LogError($"[CombatDataValidator] 유닛 트레이트 목록이 null입니다: {entry.id}", context);
                    valid = false;
                }
            }

            HashSet<SynergyTrait> definitionTraits = new HashSet<SynergyTrait>();
            foreach (SynergyDefinition definition in roster.SynergyDefinitions)
            {
                if (definition == null || !definition.ValidateConfiguration(context))
                {
                    valid = false;
                    continue;
                }

                if (!definitionTraits.Add(definition.Trait))
                {
                    Debug.LogError(
                        $"[CombatDataValidator] 같은 트레이트의 시너지 정의가 중복되었습니다: {definition.Trait.name}",
                        context);
                    valid = false;
                }
            }

            return valid;
        }

        public static bool ValidateUnitData(UnitData unit, Object context = null)
        {
            if (unit == null)
            {
                Debug.LogError("[CombatDataValidator] UnitData 항목이 null입니다.", context);
                return false;
            }

            bool valid = true;
            if (unit.id < 0)
            {
                Debug.LogError($"[CombatDataValidator] 유닛 ID가 음수입니다: {unit.id}", context);
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(unit.name))
            {
                Debug.LogError($"[CombatDataValidator] 이름이 비어 있는 유닛이 있습니다. ID: {unit.id}", context);
                valid = false;
            }

            if (unit.healthPoint <= 0 || unit.attackPoint <= 0)
            {
                Debug.LogError($"[CombatDataValidator] 유닛의 HP/공격력은 0보다 커야 합니다. ID: {unit.id}", context);
                valid = false;
            }

            if (unit.basicAttackCooldown <= 0f || unit.skillCooldown < 0)
            {
                Debug.LogError($"[CombatDataValidator] 유닛의 공격 쿨다운 값이 올바르지 않습니다. ID: {unit.id}", context);
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(unit.spriteAddress))
            {
                Debug.LogWarning($"[CombatDataValidator] 유닛 Sprite 리소스 주소가 비어 있습니다. ID: {unit.id}", context);
            }

            return valid;
        }

        public static bool ValidateReward(BattleRewardData reward, Object context = null)
        {
            if (string.IsNullOrWhiteSpace(reward.description))
            {
                Debug.LogWarning("[CombatDataValidator] 보상 설명이 비어 있습니다.", context);
            }

            switch (reward.kind)
            {
                case BattleRewardKind.Experience:
                    if (reward.amount <= 0f)
                    {
                        Debug.LogError("[CombatDataValidator] 경험치 보상 값은 0보다 커야 합니다.", context);
                        return false;
                    }
                    break;
                case BattleRewardKind.Relic:
                case BattleRewardKind.Unit:
                    if (reward.targetId < 0)
                    {
                        Debug.LogError($"[CombatDataValidator] 보상 대상 ID가 올바르지 않습니다: {reward.targetId}", context);
                        return false;
                    }
                    break;
                default:
                    Debug.LogError($"[CombatDataValidator] 지원하지 않는 보상 유형입니다: {reward.kind}", context);
                    return false;
            }

            return true;
        }
    }
}
