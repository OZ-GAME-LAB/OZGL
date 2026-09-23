using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 기존 Resources 주소의 JSON을 읽고 공용 역직렬화기에 전달합니다.
    /// </summary>
    public static class GameDataLoader
    {
        public static List<SynergyData> LoadSynergies()
        {
            return LoadList<SynergyData, SynergyDataList>("SynergyData");
        }

        public static List<SkillData> LoadUnitSkills()
        {
            return LoadList<SkillData, SkillDataList>("UnitSkillData");
        }

        public static List<SkillData> LoadSkills()
        {
            return LoadList<SkillData, SkillDataList>("SkillData");
        }

        public static List<RelicData> LoadRelics()
        {
            return LoadList<RelicData, RelicDataList>("RelicData");
        }

        private static List<T> LoadList<T, TList>(string path) where T : IIdentifiable where TList : IDataList<T>
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogWarning("[GameDataLoader] Resources/" + path + ".json을 찾을 수 없습니다.");
                return null;
            }
            return JsonDataParser.Parse<T, TList>(asset.text);
        }
    }
}
