using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using OzGameLab01.Data;
using UnityEngine;

namespace OzGameLab01.Tests.EditMode
{
    public class ExcelJsonDeserializationTests
    {
        [Test]
        public void TempUnitData_ExcelFixture_DeserializesAllRows()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("TempUnitData");
            Assert.IsNotNull(jsonFile, "TempUnitData.json을 Resources에서 찾을 수 없습니다.");

            UnitDataList data = JsonConvert.DeserializeObject<UnitDataList>(jsonFile.text);

            Assert.IsNotNull(data);
            Assert.IsNotNull(data.unitList);
            Assert.AreEqual(21, data.unitList.Count);
            Assert.AreEqual(100, data.unitList[0].id);
            Assert.AreEqual(120, data.unitList[20].id);
            Assert.AreEqual(92f, data.unitList[0].healthPoint);
            Assert.AreEqual(10.3f, data.unitList[0].attackPoint);
            Assert.AreEqual(UnitTypeJob.Assasin, data.unitList[0].jobType);
            Assert.AreEqual(UnitTypeTribe.Human, data.unitList[0].tribeType);
        }

        [Test]
        public void TempUnitData_ExcelFixture_HasRuntimeDefaultsForOptionalFields()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("TempUnitData");
            Assert.IsNotNull(jsonFile);

            List<UnitData> units = JsonConvert.DeserializeObject<UnitDataList>(jsonFile.text).unitList;

            Assert.IsNotNull(units);
            Assert.IsNotNull(units[0].skillIds);
            Assert.IsNotNull(units[0].passiveEffects);
            Assert.AreEqual(1f, units[0].basicAttackCooldown);
        }
    }
}
