using System.Collections.Generic;
using OzGameLab01.Combat;

/// <summary>
/// 실제 스킬/몬스터 기획이 아직 준비되지 않은 부분을 메우는 임시 데이터입니다.
/// 값을 UnitRosterData/MonsterRosterData의 Inspector 필드 대신 이 스크립트 한 곳에 모아둬서,
/// 나중에 실제 데이터로 교체할 때 이 파일과 주입 지점(각 asset의 OnEnable)만 정리하면 됩니다.
/// </summary>
public static class TempRosterSeed
{
    /// <summary>
    /// 실제 21종 유닛(UnitJSON)이 아직 고유 액티브 스킬을 갖지 못해 임시로 부여하는 공용 기본공격.
    /// TempUnitData.json(21종 임시 유닛)의 0번 스킬로도 재사용합니다.
    /// </summary>
    public const int UnitBasicAttackSkillId = 900;

    /// <summary>
    /// TempUnitData.json의 21종 임시 유닛이 4개씩 순환으로 나눠 갖는, 도트/기절/그을림/침묵
    /// 디버프를 하나씩 검증하기 위한 임시 액티브 스킬 id입니다.
    /// </summary>
    public const int DotSkillId = 910;
    public const int StunSkillId = 911;
    public const int AttackDownSkillId = 912;
    public const int SilenceSkillId = 913;

    public static SkillData CreateUnitBasicAttackSkill()
    {
        return new SkillData
        {
            id = UnitBasicAttackSkillId,
            name = "Temp_Basic",
            damage = 10f,
            cooldown = 1f,
        };
    }

    public static SkillData CreateDotSkill()
    {
        return new SkillData
        {
            id = DotSkillId,
            name = "Temp_DoT",
            damage = 8f,
            cooldown = 4f,
            debuff = new DebuffProfile { type = DebuffType.DamageOverTime, duration = 4f, magnitude = 5f, tickInterval = 1f },
        };
    }

    public static SkillData CreateStunSkill()
    {
        return new SkillData
        {
            id = StunSkillId,
            name = "Temp_Stun",
            damage = 6f,
            cooldown = 5f,
            debuff = new DebuffProfile { type = DebuffType.Stun, duration = 1.5f },
        };
    }

    public static SkillData CreateAttackDownSkill()
    {
        return new SkillData
        {
            id = AttackDownSkillId,
            name = "Temp_AttackDown",
            damage = 8f,
            cooldown = 5f,
            debuff = new DebuffProfile { type = DebuffType.AttackDown, duration = 4f, magnitude = 0.3f },
        };
    }

    public static SkillData CreateSilenceSkill()
    {
        return new SkillData
        {
            id = SilenceSkillId,
            name = "Temp_Silence",
            damage = 6f,
            cooldown = 6f,
            debuff = new DebuffProfile { type = DebuffType.Silence, duration = 3f },
        };
    }

    /// <summary>
    /// UnitRosterData.OnEnable()이 skillDefinitions에 주입하는 전체 임시 스킬 목록입니다.
    /// </summary>
    public static List<SkillData> CreateAllTempUnitSkills()
    {
        return new List<SkillData>
        {
            CreateUnitBasicAttackSkill(),
            CreateDotSkill(),
            CreateStunSkill(),
            CreateAttackDownSkill(),
            CreateSilenceSkill(),
        };
    }
}
