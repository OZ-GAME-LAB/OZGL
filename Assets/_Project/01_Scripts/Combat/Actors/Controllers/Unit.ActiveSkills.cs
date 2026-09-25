using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Managers;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 액티브 스킬(SkillData.activeEffects) 실행기. 효과 노드마다 대상을 정해 실제 효과를 적용하고,
    /// 효과가 적용된 대상에만 그 노드의 연출 VFX(vfx cue)를 재생합니다(데미지가 회피되면 생략).
    /// 스킬에서 온 회복/스탯 변화는 라이브러리 VFX 대신 cue만 재생합니다(상태이상 지속 오라는 유지).
    /// 기획서(UnitData.xlsx 스킬 시트)의 특수 공식 4종은 스킬 ID로 분기합니다.
    /// </summary>
    public partial class Unit
    {
        private const int GenieWishSkillId = 51051;   // 세가지 소원: 첫 2회는 스택만, 3회째부터 데미지
        private const int LionTauntSkillId = 51061;   // 내가 겁쟁이라고!?: 보호막 = 최대HP × (최대HP / 현재HP)
        private const int PeterPanSkillId = 51141;    // 어른이 되기 싫었어: 시전 전 공격력·공격 간격 기준 상호 증가
        private const int TinkerBellSkillId = 51151;  // 광기의 집착: 무작위 상태이상 1종을 적과 자신에게
        private const int GenieWishStacksToFire = 3;

        private int _genieWishStacks;

        private void ExecuteActiveEffects(SkillData data, Unit currentTarget)
        {
            // 피터팬 기획 비고: 두 스탯을 올리기 전에 공격력과 공격 간격을 미리 받아와 기준으로 삼는다.
            float attackBefore = attackPoint;
            float intervalBefore = _skills.Count > 0 ? GetEffectiveCooldown(_skills[0]) : 1f;
            // 팅커벨: 적과 자신에게 "같은" 상태이상을 걸기 위해 한 번만 뽑는다.
            DebuffProfile? sharedRandomDebuff = null;
            // 제페토: "임의의 아군"과 "같은 대상"이 이어지도록 무작위 아군은 한 번만 뽑는다.
            Unit sharedRandomAlly = null;

            if (data.id == GenieWishSkillId) _genieWishStacks++;

            foreach (ActiveSkillEffectNode node in data.activeEffects)
            {
                if (node == null) continue;
                bool anyApplied = false;
                foreach (Unit target in ResolveEffectTargets(node.targetType, currentTarget, ref sharedRandomAlly))
                {
                    if (target == null || target.IsDead) continue;
                    if (!ApplyActiveEffect(data, node, target, attackBefore, intervalBefore, ref sharedRandomDebuff)) continue;
                    anyApplied = true;
                    PlayEffectCues(node, target, onCaster: false);
                }

                if (anyApplied) PlayEffectCues(node, this, onCaster: true);
            }
        }

        /// <summary>효과 노드 하나를 대상에게 적용합니다. 실제로 적용됐으면 true(연출 재생 기준).</summary>
        private bool ApplyActiveEffect(SkillData data, ActiveSkillEffectNode node, Unit target,
            float attackBefore, float intervalBefore, ref DebuffProfile? sharedRandomDebuff)
        {
            float value = node.value;
            float seconds = node.extraParam;
            switch (node.effectType)
            {
                case ActiveEffectType.Damage:
                    return ApplySkillDamage(target, attackPoint * value);
                case ActiveEffectType.TrueDamage:
                    if (data.id == GenieWishSkillId && _genieWishStacks < GenieWishStacksToFire) return false;
                    return ApplySkillDamage(target, attackPoint * value, ignoreDefense: true);
                case ActiveEffectType.DefenseScaleDamage:
                    return ApplySkillDamage(target, defensePoint * value);
                case ActiveEffectType.TargetHpScaleDamage:
                    return ApplySkillDamage(target, target.MaxHp * value);
                case ActiveEffectType.DotDamage:
                    // value = 초당 공격력 계수, extraParam = 지속시간(초)
                    target.ApplyDebuff(new DebuffProfile
                    {
                        type = DebuffType.DamageOverTime, duration = seconds,
                        magnitude = attackPoint * value, tickInterval = 1f
                    });
                    return seconds > 0f;
                case ActiveEffectType.Heal:
                    return target.Heal(target.MaxHp * value, playVfx: false);
                case ActiveEffectType.GrantShield:
                    float shield = data.id == LionTauntSkillId
                        ? target.MaxHp * (target.MaxHp / Mathf.Max(1f, target.CurrentHp))
                        : target.MaxHp * value;
                    return target.GrantShield(shield, seconds, seconds <= 0f);
                case ActiveEffectType.Stun:
                    target.ApplyDebuff(new DebuffProfile { type = DebuffType.Stun, duration = seconds });
                    return seconds > 0f;
                case ActiveEffectType.Silence:
                    target.ApplyDebuff(new DebuffProfile { type = DebuffType.Silence, duration = seconds });
                    return seconds > 0f;
                case ActiveEffectType.Taunt:
                    return target.ApplyTaunt(seconds);
                case ActiveEffectType.CleanseDebuff:
                    bool cleansed = false;
                    for (int i = 0; i < Mathf.Max(1, Mathf.RoundToInt(value)); i++) cleansed |= target.CleanseOneDebuff();
                    return cleansed;
                case ActiveEffectType.CooldownRecovery:
                    return target.RecoverSkillCooldown(value);
                case ActiveEffectType.StealStat:
                    // 대상 공격력을 value만큼 낮추고(그을림) 같은 비율만큼 자신의 공격력을 올린다(extraParam초).
                    if (node.statType != EffectStatType.Attack || seconds <= 0f) return false;
                    target.ApplyDebuff(new DebuffProfile { type = DebuffType.AttackDown, duration = seconds, magnitude = value });
                    ApplyStatEffect(EffectStatType.Attack, value * 100f, EffectOperation.Add, seconds, false, playVfx: false);
                    return true;
                case ActiveEffectType.StatModifier:
                    return ApplyStatModifierNode(data, node, target, attackBefore, intervalBefore, ref sharedRandomDebuff);
                default:
                    // ReviveSelf 등 현재 아군 액티브 스킬 데이터에서 쓰이지 않는 유형
                    return false;
            }
        }

        private bool ApplyStatModifierNode(SkillData data, ActiveSkillEffectNode node, Unit target,
            float attackBefore, float intervalBefore, ref DebuffProfile? sharedRandomDebuff)
        {
            if (data.id == TinkerBellSkillId)
            {
                if (sharedRandomDebuff == null)
                {
                    DebuffProfile rolled = CreateRandomDebuffProfile();
                    rolled.duration = node.extraParam;
                    sharedRandomDebuff = rolled;
                }
                target.ApplyDebuff(sharedRandomDebuff.Value);
                return true;
            }

            if (data.id == PeterPanSkillId)
            {
                // 공격속도(공격 간격) 증가 = 시전 전 공격력 × 0.2 (%), 공격력 증가 = (1.0 - 시전 전 공격 간격) × 50 (%, 최소 0)
                float percent = node.statType == EffectStatType.AttackInterval
                    ? attackBefore * Mathf.Abs(node.value)
                    : Mathf.Max(0f, (1f - intervalBefore) * node.value);
                if (percent <= 0f) return false;
                return target.ApplyStatEffect(node.statType, percent, EffectOperation.Add, 0f, true, playVfx: false);
            }

            if (node.statType == EffectStatType.Unknown) return false;
            // value = 비율(0.2 → +20%), extraParam = 지속시간(초, 0이면 전투 끝까지)
            return target.ApplyStatEffect(node.statType, node.value * 100f, EffectOperation.Add,
                node.extraParam, node.extraParam <= 0f, playVfx: false);
        }

        private void PlayEffectCues(ActiveSkillEffectNode node, Unit vfxTarget, bool onCaster)
        {
            if (node.vfx == null) return;
            foreach (SkillVfxCue cue in node.vfx)
            {
                if (cue.onCaster == onCaster) StartCoroutine(PlaySkillVfxCue(cue, vfxTarget));
            }
        }

        private IEnumerator PlaySkillVfxCue(SkillVfxCue cue, Unit vfxTarget)
        {
            if (cue.delay > 0f) yield return new WaitForSeconds(cue.delay);
            if (vfxTarget == null || vfxTarget.IsDead) yield break;
            _presenter.PlaySkillVfx(cue.address, vfxTarget.transform, cue.scale, cue.attachSeconds, cue.anchor);
        }

        private List<Unit> ResolveEffectTargets(ActiveEffectTarget targetType, Unit currentTarget, ref Unit sharedRandomAlly)
        {
            var result = new List<Unit>();
            switch (targetType)
            {
                case ActiveEffectTarget.Self:
                    result.Add(this);
                    break;
                case ActiveEffectTarget.AllAllies:
                case ActiveEffectTarget.WorstHpAlly:
                case ActiveEffectTarget.WorstHpAllies:
                case ActiveEffectTarget.RandomAlly:
                    // "아군"은 시전자 팀 기준. 적은 한 명뿐이라 적의 아군은 자신이고,
                    // 전투 세션이 없으면(테스트 씬·EditMode) 시전자만 아군으로 취급합니다.
                    List<Unit> allies = team == Team.Ally && CombatManager.Instance?.Facade != null
                        ? GetAliveAlliesForSkill()
                        : new List<Unit>();
                    if (allies.Count == 0) allies.Add(this);
                    if (targetType == ActiveEffectTarget.AllAllies) result.AddRange(allies);
                    else if (targetType == ActiveEffectTarget.RandomAlly)
                    {
                        if (sharedRandomAlly == null || sharedRandomAlly.IsDead)
                        {
                            List<Unit> picked = CombatTargetSelector.SelectRandom(allies, 1, _random);
                            sharedRandomAlly = picked.Count > 0 ? picked[0] : null;
                        }
                        if (sharedRandomAlly != null) result.Add(sharedRandomAlly);
                    }
                    else result.AddRange(CombatTargetSelector.SelectWorstHp(allies, targetType == ActiveEffectTarget.WorstHpAlly ? 1 : 2));
                    break;
                default:
                    // CurrentTarget / AllEnemies: 적은 한 명이라 현재 대상과 같다.
                    if (currentTarget != null && !currentTarget.IsDead) result.Add(currentTarget);
                    break;
            }
            return result;
        }
    }
}
