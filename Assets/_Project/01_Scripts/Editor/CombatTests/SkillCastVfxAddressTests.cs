using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;

namespace OzGameLab01.Tests.EditMode
{
    public class SkillCastVfxAddressTests
    {
        // UnitData.xlsx 스킬 시트 이펙트 명세: 아군 12종 액티브 스킬은 각자 *_Activate_Skill_EF를 시전 VFX로 쓴다.
        private static readonly Dictionary<int, string> ExpectedCastVfx = new Dictionary<int, string>
        {
            { 51001, "Alice" }, { 51011, "White_Rabbit" }, { 51021, "Cheshire_Cat" }, { 51051, "Genie" },
            { 51061, "Lion" }, { 51081, "Scarecrow" }, { 51091, "Dorothy" }, { 51101, "Little_Mermaid" },
            { 51111, "Snow White" }, { 51141, "Peter_Pan" }, { 51151, "Tinker_Bell" }, { 51201, "Zepeto" },
        };

        [Test]
        public void AllyActiveSkillsHaveRegisteredCastVfxAddress()
        {
            List<SkillData> skills = GameDataLoader.LoadSkills();
            Assert.That(skills, Is.Not.Null);
            HashSet<string> addresses = CollectAddresses();

            foreach (KeyValuePair<int, string> expected in ExpectedCastVfx)
            {
                SkillData skill = skills.Find(s => s.id == expected.Key);
                Assert.That(skill, Is.Not.Null, expected.Key.ToString());
                Assert.That(skill.castVfxAddress,
                    Is.EqualTo($"VFX/Skill/Cast/{expected.Value}_Activate_Skill_EF"), expected.Key.ToString());
                Assert.That(addresses.Contains(skill.castVfxAddress), Is.True, skill.castVfxAddress);
            }
        }

        [Test]
        public void ActiveSkillEffectVfxCuesAreRegistered()
        {
            List<SkillData> skills = GameDataLoader.LoadSkills();
            Assert.That(skills, Is.Not.Null);
            HashSet<string> addresses = CollectAddresses();

            int cueCount = 0;
            foreach (SkillData skill in skills)
            {
                if (skill.activeEffects == null) continue;
                foreach (ActiveSkillEffectNode node in skill.activeEffects)
                {
                    if (node?.vfx == null) continue;
                    foreach (SkillVfxCue cue in node.vfx)
                    {
                        cueCount++;
                        Assert.That(addresses.Contains(cue.address), Is.True, $"{skill.id}: {cue.address}");
                    }
                }
            }

            // 아군 12종 매핑(Docs/VFX_ANIMATION_INVENTORY.md 7장) 기준 연출 VFX 개수.
            Assert.That(cueCount, Is.EqualTo(18));
        }

        private static HashSet<string> CollectAddresses()
        {
            var addresses = new HashSet<string>();
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
            {
                if (group == null) continue;
                foreach (var entry in group.entries) addresses.Add(entry.address);
            }
            return addresses;
        }
    }
}
