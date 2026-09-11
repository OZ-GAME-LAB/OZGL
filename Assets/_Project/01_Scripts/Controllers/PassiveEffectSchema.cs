namespace OzGameLab01.Combat
{
    /// <summary>
    /// 패시브 스킬/유물 효과의 발동 조건. Docs/PASSIVE_TRIGGER_EFFECT_SCHEMA.md 참고.
    /// </summary>
    public enum TriggerType
    {
        Always,
        OnBattleStart,
        OnBattleEnd,
        OnNightStart,
        OnSelfDeath,
        OnAllyDeath,
        OnFirstAllyDeath,
        OnAllySkillUsed,
        OnAttackCount,
        OnAllyHealed,
        OnAllyBuffed,
        OnAllyShielded,
        OnHpBelowThreshold,
        OnEnemySkillUsed
    }

    /// <summary>
    /// 효과가 적용되는 대상.
    /// </summary>
    public enum EffectTarget
    {
        Self,
        TriggeringUnit,
        AllAllies,
        FrontRow,
        MidRow,
        BackRow,
        SupportRow,
        RandomAlly,
        WorstHpAlly,
        Enemy
    }

    /// <summary>
    /// 트리거가 발생했을 때 실제로 일어나는 효과의 종류.
    /// </summary>
    public enum EffectType
    {
        StatModifier,
        DealDamage,
        Heal,
        GrantShield,
        Revive,
        CleanseDebuffs,
        DebuffImmunity,
        DebuffDurationModifier,
        CooldownModifier,
        NullifyNextSkill,
        SynergyModifier
    }

    /// <summary>
    /// 트리거 하나 + 효과 하나를 묶은 데이터. 패시브 스킬/유물 모두 이 구조로 표현할 예정입니다.
    /// 실제 적용 로직(Apply)은 각 EffectType이 필요로 하는 메커니즘(보호막/회복/부활 등)이
    /// 아직 없어서 이번에는 스키마와 발행 지점만 준비합니다.
    /// </summary>
    [System.Serializable]
    public struct EffectInstance
    {
        public TriggerType trigger;
        public float triggerParam;
        public EffectTarget target;
        public EffectType effect;
        public float effectParam;
        public float chance;
        public bool once;
    }
}
