using OzGameLab01.Data;

namespace OzGameLab01.Combat
{
    /// <summary>Per-unit skill state. Definition values are never modified for cooldown/bonuses.</summary>
    public sealed class UnitSkillRuntime
    {
        public SkillData data;
        public float timer;
        public float damageMultiplier = 1f;
        public float? cooldownOverride;
        public float Cooldown => cooldownOverride ?? data.cooldown;
    }
}
