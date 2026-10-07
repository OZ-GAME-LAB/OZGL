using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using UnityEngine;

namespace OzGameLab01.Tests.EditMode
{
    public class EnemySpeciesTests
    {
        [Test]
        public void EveryTierRowHasSpeciesWithLoadablePrefab()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("EnemyData");
            Assert.IsNotNull(jsonFile, "EnemyData.json을 Resources에서 찾을 수 없습니다.");

            foreach (MonsterData monster in JsonConvert.DeserializeObject<MonsterDataList>(jsonFile.text).monsterList)
            {
                Assert.IsNotEmpty(monster.species, $"{monster.type} 계층에 등장할 적 종이 없습니다.");
                foreach (MonsterSpecies species in monster.species)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(species.name), $"{monster.type}: 종 이름 없음");
                    GameObject prefab = Resources.Load<GameObject>(species.prefabAddress);
                    Assert.IsNotNull(prefab, $"{monster.type} {species.name}: 프리팹 없음 ({species.prefabAddress})");
                    Assert.IsNotNull(prefab.GetComponent<Unit>(), $"{monster.type} {species.name}: Unit 컴포넌트 없음");
                }
            }
        }

        [Test]
        public void ResourcesFallbackCatalogCarriesSpecies()
        {
            // 부팅 없이 진입(씬 직접 실행)하면 RuntimeContent가 이 Resources 경로로 카탈로그를 만든다.
            ContentCatalog catalog = ResourcesContentLoader.Load();
            Assert.That(catalog.EnemyCount, Is.GreaterThan(0));
            foreach (MonsterData monster in catalog.Enemies)
                Assert.IsNotEmpty(monster.species, $"폴백 카탈로그의 적 {monster.id}({monster.type})에 종 목록이 없습니다.");
        }

        [Test]
        public void EnemyPreparationReturnsDetachedSpecies()
        {
            var catalog = new ContentCatalog(
                new[] { new UnitData { id = 1, skillIds = new List<int> { 1 } } },
                new[] { new MonsterData { id = 1, type = MonsterType.normal, skillIds = new List<int> { 1 },
                    species = new List<MonsterSpecies> { new MonsterSpecies { name = "까마귀", prefabAddress = "A" } } } },
                new[] { new SkillData { id = 1 } },
                Array.Empty<SynergyData>(), Array.Empty<RelicData>());
            var cache = new EnemyPreparationCache();

            MonsterData first = cache.Prepare(1, catalog.GetEnemy(1), 0, 1f, 42, Array.Empty<UnitData>());
            first.species[0].name = "변경됨";
            first.species.Add(new MonsterSpecies { name = "추가됨" });
            MonsterData second = cache.Prepare(1, catalog.GetEnemy(1), 0, 1f, 42, Array.Empty<UnitData>());

            Assert.That(second.species.Count, Is.EqualTo(1));
            Assert.That(second.species[0].name, Is.EqualTo("까마귀"));
        }
    }
}
