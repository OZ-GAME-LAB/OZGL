using System.Collections.Generic;
using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 액티브 스킬 효과 적용 대상
    /// </summary>
    public enum ActiveEffectTarget
    {
        CurrentTarget,          // 현재 단일 타겟
        AllEnemies,             // 모든 적
        Self,                   // 시전자 본인
        AllAllies,              // 모든 아군
        WorstHpAlly,            // 가장 체력이 낮은 아군 1체
        WorstHpAllies,          // 체력이 낮은 아군 복수
        RandomAlly              // 무작위 아군 1체
    }

    /// <summary>
    /// 액티브 스킬 시전 시 즉시 발동하는 효과 유형
    /// </summary>
    public enum ActiveEffectType
    {
        Damage,                 // 공격력 계수 기반 일반 피해
        TrueDamage,             // 방어력 무시 고정 피해
        DefenseScaleDamage,     // 방어력 계수 기반 피해
        TargetHpScaleDamage,    // 대상 체력 비례 피해
        DotDamage,              // 지속(도트) 피해 부여
        Heal,                   // 체력 회복
        GrantShield,            // 보호막 부여
        Stun,                   // 기절 상태이상
        Silence,                // 침묵 상태이상
        Taunt,                  // 도발 상태이상
        CleanseDebuff,          // 상태이상 제거/해제
        ReviveSelf,             // 전투 중 1회 부활 상태 부여
        StatModifier,           // 버프/디버프 스탯 변경
        StealStat,              // 대상의 스탯을 감소시키고 감소량만큼 자신 증가
        CooldownRecovery        // 쿨타임 회복/감소
    }

    /// <summary>
    /// 액티브 스킬의 단일 세부 효과 정의 노드
    /// </summary>
    [System.Serializable]
    public class ActiveSkillEffectNode
    {
        public ActiveEffectTarget targetType;       // 효과 대상
        public ActiveEffectType effectType;         // 효과 유형
        public EffectStatType statType;             // 참조 또는 변경할 스탯

        public float value;                         // 기본 계수
        public float extraParam;                    // 보조 수치

        public List<SkillVfxCue> vfx = new();       // 이 효과의 연출 VFX (효과 대상마다 재생)
    }

    /// <summary>
    /// 액티브 스킬 효과 하나에 붙는 연출 VFX 정의. 효과 실행과 별개로 연출만 담당합니다.
    /// </summary>
    [System.Serializable]
    public class SkillVfxCue
    {
        public string address;                      // Addressables 주소
        public bool onCaster;                       // true면 효과 대상 대신 시전자 위치에 1회 재생
        public float delay;                         // 효과 적용 시점 기준 지연(초)
        public float scale;                         // 월드 스케일 (0이면 기본값)
        public float attachSeconds;                 // 0보다 크면 대상에 붙여 해당 시간 동안 유지(루프 VFX용)
    }
}
