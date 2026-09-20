namespace OzGameLab01.Data
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
        FixedDamage,
        ExtraDamageOnStatus,
        StatusEffect,
        UseSkillTwoTimes,
        NoSkillStatBuff,
        DecreaseDmgDefense,
        Revive,
        CleanseDebuffs,
        DebuffImmunity,
        DebuffDurationModifier,
        CooldownModifier,
        NullifyNextSkill,
        SynergyModifier
    }

    /// <summary>
    /// StatModifier가 변경하는 런타임 능력치입니다.
    /// 효과 데이터에 statType이 없는 기존 JSON은 Unknown으로 역직렬화되며,
    /// 해당 효과는 종류별 캐시에는 보관되지만 특정 능력치 합산에는 사용되지 않습니다.
    /// </summary>
    public enum EffectStatType
    {
        Unknown,
        MaxHealth,
        Attack,
        Defense,
        AttackInterval,
        CriticalMultiplier,
        CriticalChance,
        DodgeChance,
        Lifesteal,
        RecoveryAmount,
        CurrentDiceValue,
        TurnRecovery
    }

    /// <summary>
    /// 능력치 효과의 계산 연산입니다.
    /// </summary>
    public enum EffectOperation
    {
        Add,
        Multiply
    }

    /// <summary>
    /// 트리거 하나 + 효과 하나를 묶은 데이터. 패시브 스킬/유물 모두 이 구조로 표현할 예정입니다.
    /// 패시브와 액티브 스킬 effects[]가 공유하며, 실제 실행기는 각 발동 경로가 소유합니다.
    /// </summary>
    [System.Serializable]
    public struct EffectInstance
    {
        public TriggerType trigger;
        public float triggerParam;
        public EffectTarget target;
        public EffectType effect;
        public EffectStatType statType;
        public EffectOperation operation;
        public float effectParam;
        public float effectSecondaryParam;
        public bool effectParamIsPercent;
        public float chance;
        public bool once;
        public float durationSeconds;
        public bool untilBattleEnd;
        public float tickInterval;
        // Zero in legacy serialized structs means the previous default of one target.
        public int targetCount;
    }
}
