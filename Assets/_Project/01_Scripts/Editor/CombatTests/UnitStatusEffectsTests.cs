using NUnit.Framework;
using OzGameLab01.Combat;

namespace OzGameLab01.Tests.EditMode
{
    public class UnitStatusEffectsTests
    {
        [Test]
        public void Apply_Stun_SetsIsStunnedUntilExpiry()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.Stun, duration = 1f });

            Assert.IsTrue(status.IsStunned);

            status.Tick(0.6f, null);
            Assert.IsTrue(status.IsStunned);

            status.Tick(0.5f, null);
            Assert.IsFalse(status.IsStunned);
        }

        [Test]
        public void Apply_Silence_SetsIsSilencedUntilExpiry()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.Silence, duration = 0.5f });

            Assert.IsTrue(status.IsSilenced);

            status.Tick(0.6f, null);
            Assert.IsFalse(status.IsSilenced);
        }

        [Test]
        public void Apply_AttackDown_ReducesAttackMultiplier()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.AttackDown, duration = 1f, magnitude = 0.3f });

            Assert.AreEqual(0.7f, status.AttackMultiplier, 0.0001f);

            status.Tick(1.1f, null);
            Assert.AreEqual(1f, status.AttackMultiplier, 0.0001f);
        }

        [Test]
        public void Apply_DamageOverTime_TicksAtInterval()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.DamageOverTime, duration = 3f, magnitude = 5f, tickInterval = 1f });

            float totalDamage = 0f;
            status.Tick(0.9f, dmg => totalDamage += dmg);
            Assert.AreEqual(0f, totalDamage, 0.0001f);

            status.Tick(0.2f, dmg => totalDamage += dmg);
            Assert.AreEqual(5f, totalDamage, 0.0001f);

            status.Tick(1f, dmg => totalDamage += dmg);
            Assert.AreEqual(10f, totalDamage, 0.0001f);
        }

        [Test]
        public void Apply_SameTypeTwice_RefreshesDurationInsteadOfStacking()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.AttackDown, duration = 1f, magnitude = 0.2f });
            status.Apply(new DebuffProfile { type = DebuffType.AttackDown, duration = 1f, magnitude = 0.5f });

            Assert.AreEqual(0.5f, status.AttackMultiplier, 0.0001f);

            status.Tick(1.1f, null);
            Assert.AreEqual(1f, status.AttackMultiplier, 0.0001f);
        }

        [Test]
        public void Apply_NoneType_IsIgnored()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.None, duration = 5f });

            Assert.IsNull(status.IndicatorColor);
        }

        [Test]
        public void IndicatorColor_IsNull_WhenNoActiveDebuffs()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            Assert.IsNull(status.IndicatorColor);
        }

        [Test]
        public void IndicatorColor_PrioritizesStunOverOthers()
        {
            UnitStatusEffects status = new UnitStatusEffects();
            status.Apply(new DebuffProfile { type = DebuffType.DamageOverTime, duration = 1f, magnitude = 1f, tickInterval = 1f });
            status.Apply(new DebuffProfile { type = DebuffType.Stun, duration = 1f });

            UnityEngine.Color stunColor = new UnityEngine.Color(0.55f, 0.6f, 1f);
            Assert.AreEqual(stunColor, status.IndicatorColor.Value);
        }
    }
}
