using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using UnityEngine;

namespace OzGameLab01.Tests.EditMode
{
    public class EnemySpeciesDataTests
    {
        [Test]
        public void EverySpeciesPrefabLoadsWithUnit()
        {
            foreach (EnemySpeciesData species in LoadSpecies())
            {
                GameObject prefab = Resources.Load<GameObject>(species.prefabAddress);
                Assert.IsNotNull(prefab, $"{species.id} {species.name}: 프리팹 없음 ({species.prefabAddress})");
                Assert.IsNotNull(prefab.GetComponent<Unit>(), $"{species.id} {species.name}: Unit 컴포넌트 없음");
                Assert.IsFalse(string.IsNullOrWhiteSpace(species.name), $"{species.id}: 이름 없음");
            }
        }

        [Test]
        public void EveryMonsterTierHasAtLeastOneSpecies()
        {
            List<EnemySpeciesData> species = LoadSpecies();
            TextAsset monsterJson = Resources.Load<TextAsset>("EnemyData");
            Assert.IsNotNull(monsterJson, "EnemyData.json을 Resources에서 찾을 수 없습니다.");

            foreach (MonsterData monster in JsonConvert.DeserializeObject<MonsterDataList>(monsterJson.text).monsterList)
            {
                Assert.IsTrue(species.Exists(s => s.tiers != null && s.tiers.Contains(monster.type)),
                    $"{monster.type} 계층에 등장할 적 종이 없습니다.");
            }
        }

        private static List<EnemySpeciesData> LoadSpecies()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("EnemySpeciesData");
            Assert.IsNotNull(jsonFile, "EnemySpeciesData.json을 Resources에서 찾을 수 없습니다.");
            List<EnemySpeciesData> species = JsonConvert.DeserializeObject<EnemySpeciesDataList>(jsonFile.text).speciesList;
            Assert.IsNotNull(species);
            Assert.IsNotEmpty(species);
            return species;
        }
    }
}
