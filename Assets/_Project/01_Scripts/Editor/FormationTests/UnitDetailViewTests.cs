using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Formation;
using OzGameLab01.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Tests.EditMode
{
    public class UnitDetailViewTests
    {
        private const string UnitViewPrefabPath = "Assets/_Project/02_Prefabs/UI/ReadyUi/UnitView.prefab";

        [Test]
        public void LoadUnit_BindsSevenStatsWithExpectedFormatting()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UnitViewPrefabPath);

            try
            {
                UnitDetailView detailView = root.GetComponentInChildren<UnitDetailView>(true);
                Assert.That(detailView, Is.Not.Null);

                var data = new UnitData
                {
                    name = "테스트 유닛",
                    healthPoint = 92f,
                    attackPoint = 10.3f,
                    defensePoint = 3.48f,
                    attackSpeed = 0.82f,
                    criticalRate = 15f,
                    criticalMult = 150f,
                    dodgeRate = 7.5f
                };

                detailView.LoadUnit(data, null);

                CombatInfoStatItemView[] statItems = GetPrivateField<CombatInfoStatItemView[]>(detailView, "statItems");
                Assert.That(statItems, Has.Length.EqualTo(7));
                Assert.That(GetValues(statItems), Is.EqualTo(new[]
                {
                    "92", "10.3", "3.48", "0.82", "15%", "150%", "7.5%"
                }));
                Assert.That(GetTitles(statItems), Is.EqualTo(new[]
                {
                    "체력", "공격력", "방어력", "공격 속도", "치명타 확률", "치명타 피해", "회피율"
                }));

                foreach (CombatInfoStatItemView item in statItems)
                    Assert.That(item.Icon, Is.Not.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void UnitIconMappings_CoverCurrentRuntimeUnitCatalog()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UnitViewPrefabPath);

            try
            {
                UnitFormationController controller = root.GetComponentInChildren<UnitFormationController>(true);
                Assert.That(controller, Is.Not.Null);

                MethodInfo getUnitIcon = typeof(UnitFormationController).GetMethod(
                    "GetUnitIcon",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(getUnitIcon, Is.Not.Null);

                int[] unitIds = { 100, 101, 102, 105, 106, 108, 109, 110, 111, 114, 115, 120 };
                var sprites = new HashSet<Sprite>();

                foreach (int unitId in unitIds)
                {
                    var data = new UnitData { id = unitId };
                    var sprite = getUnitIcon.Invoke(controller, new object[] { data }) as Sprite;
                    Assert.That(sprite, Is.Not.Null, $"UnitData.id={unitId} icon is not connected.");
                    sprites.Add(sprite);
                }

                Assert.That(sprites.Count, Is.EqualTo(unitIds.Length));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void FormationDetailData_ResolvesForEveryRuntimeUnit()
        {
            TextAsset unitJson = Resources.Load<TextAsset>("UnitData");
            Assert.That(unitJson, Is.Not.Null);

            List<UnitData> units = JsonDataParser.Parse<UnitData, UnitDataList>(unitJson.text);
            List<SkillData> skills = GameDataLoader.LoadSkills();
            List<SynergyData> synergies = GameDataLoader.LoadSynergies();
            var requiredIconAddresses = new HashSet<string>();

            Assert.That(units, Has.Count.EqualTo(12));
            foreach (UnitData unit in units)
            {
                UnitDetailData detail = UnitDetailDataResolver.Resolve(unit, skills, synergies);
                Assert.That(unit.flavorText, Is.Not.Empty, $"UnitData.id={unit.id} flavorText");
                Assert.That(detail.ActiveSkill, Is.Not.Null, $"UnitData.id={unit.id} active skill");
                Assert.That(detail.ActiveSkill.id, Is.EqualTo(unit.activeSkillId));
                Assert.That(detail.ActiveSkill.iconAddress, Is.Not.Empty);
                requiredIconAddresses.Add(detail.ActiveSkill.iconAddress);
                Assert.That(detail.PassiveSkill, Is.Not.Null, $"UnitData.id={unit.id} passive skill");
                Assert.That(detail.PassiveSkill.id, Is.EqualTo(unit.passiveSkillId));
                Assert.That(detail.PassiveSkill.iconAddress, Is.Not.Empty);
                requiredIconAddresses.Add(detail.PassiveSkill.iconAddress);
                Assert.That(detail.Synergies, Has.Count.EqualTo(2), $"UnitData.id={unit.id} synergies");

                if (unit.id == 100)
                    Assert.That(UnitDetailDataResolver.GetDisplayName(detail.Synergies[0]), Is.EqualTo("재간둥이"));
            }

            var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var iconGroup = addressableSettings.FindGroup("Icon");
            var registeredAddresses = iconGroup.entries.Select(entry => entry.address).ToHashSet();
            Assert.That(requiredIconAddresses, Has.Count.EqualTo(24));
            Assert.That(requiredIconAddresses.All(registeredAddresses.Contains), Is.True);
        }

        [Test]
        public void LoadUnit_BindsSynergiesSkillsAndFlavorWithoutPrefabPlaceholders()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UnitViewPrefabPath);

            try
            {
                UnitDetailView detailView = root.GetComponentInChildren<UnitDetailView>(true);
                Sprite skillIcon = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/ImportedAssets/UI_Resources/05. Icon/SKill/Active/alice_active.png");
                var data = new UnitData
                {
                    name = "앨리스",
                    flavorText = "앨리스 플레이버 텍스트"
                };
                var activeSkill = new SkillData { name = "액티브 스킬", description = "액티브 설명" };
                var passiveSkill = new SkillData { name = "패시브 스킬", description = "패시브 설명" };

                detailView.LoadUnit(
                    data,
                    null,
                    new[] { "재간둥이", "인간" },
                    activeSkill,
                    skillIcon,
                    passiveSkill,
                    skillIcon);

                Image[] skillIcons = GetPrivateField<Image[]>(detailView, "skillIcons");
                TMP_Text[] skillTexts = GetPrivateField<TMP_Text[]>(detailView, "skillDescriptionTexts");
                TMP_Text conceptText = GetPrivateField<TMP_Text>(detailView, "conceptDescriptionText");
                Transform synergyRoot = GetPrivateField<Transform>(detailView, "synergyBadgeRoot");
                InfoSynergyItemView[] badges = synergyRoot.GetComponentsInChildren<InfoSynergyItemView>(false);

                Assert.That(skillIcons.Select(image => image.sprite), Is.All.EqualTo(skillIcon));
                Assert.That(skillTexts[0].text, Does.Contain("액티브 스킬").And.Contain("액티브 설명"));
                Assert.That(skillTexts[1].text, Does.Contain("패시브 스킬").And.Contain("패시브 설명"));
                Assert.That(conceptText.text, Is.EqualTo(data.flavorText));
                Assert.That(badges.Select(badge => badge.Name), Is.EqualTo(new[] { "재간둥이", "인간" }));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private static string[] GetValues(CombatInfoStatItemView[] statItems)
        {
            var values = new string[statItems.Length];
            for (int index = 0; index < statItems.Length; index++)
                values[index] = statItems[index].Value;
            return values;
        }

        private static string[] GetTitles(CombatInfoStatItemView[] statItems)
        {
            var titles = new string[statItems.Length];
            for (int index = 0; index < statItems.Length; index++)
                titles[index] = statItems[index].DetailTitle;
            return titles;
        }
    }
}
