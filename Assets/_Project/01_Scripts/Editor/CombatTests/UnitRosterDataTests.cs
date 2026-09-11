using System.Collections.Generic;
using NUnit.Framework;
using OzGameLab01.Combat;
using OzGameLab01.Data;

namespace OzGameLab01.Tests.EditMode
{
    public class UnitRosterDataTests
    {
        [Test]
        public void ParseUnitList_ValidJson_ReturnsUnitData()
        {
            string json = @"{
                ""unitList"": [
                    { ""id"": 100, ""name"": ""앨리스"", ""jobType"": ""Assasin"", ""tribeType"": ""Human"", ""skillIds"": [900, 910] }
                ]
            }";

            List<UnitData> result = UnitRosterData.ParseUnitList(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(100, result[0].id);
            Assert.AreEqual("앨리스", result[0].name);
            Assert.AreEqual(UnitTypeJob.Assasin, result[0].jobType);
            Assert.AreEqual(UnitTypeTribe.Human, result[0].tribeType);
            CollectionAssert.AreEqual(new[] { 900, 910 }, result[0].skillIds);
        }

        [Test]
        public void ParseUnitList_MissingUnitListKey_ReturnsNull()
        {
            string json = @"{ ""somethingElse"": [] }";

            List<UnitData> result = UnitRosterData.ParseUnitList(json);

            Assert.IsNull(result);
        }

        [Test]
        public void ParseUnitList_MalformedJson_ThrowsJsonException()
        {
            string json = "{ not valid json";

            Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => UnitRosterData.ParseUnitList(json));
        }
    }
}
