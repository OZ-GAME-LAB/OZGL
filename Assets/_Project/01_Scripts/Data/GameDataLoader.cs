using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using OzGameLab01.Data;
using OzGameLab01.Combat;

/// <summary>
/// 05_Data/Resources의 JSON을 읽어 역직렬화하는 임시 로더입니다.
/// GameDB/Addressables 파이프라인(DataManager)과는 별도로, ContentCatalog/Repository
/// 계층(Docs/BACKEND_DATA_ARCHITECTURE.md)이 만들어지기 전까지 RuntimeDataManager가
/// 쓰는 진입점입니다. 몬스터는 이미 있는 MonsterRosterData.ParseMonsterList를 그대로
/// 재사용해 파싱 로직을 이중으로 두지 않습니다.
/// </summary>
public static class GameDataLoader
{
    public static List<UnitData> LoadUnits()
    {
        string json = ReadResourceText("UnitData");
        if (json == null)
        {
            return null;
        }

        return JsonConvert.DeserializeObject<UnitDataList>(json)?.GetList();
    }

    public static List<SynergyData> LoadSynergies()
    {
        string json = ReadResourceText("SynergyData");
        if (json == null)
        {
            return null;
        }

        return JsonConvert.DeserializeObject<SynergyDataList>(json)?.GetList();
    }

    public static List<MonsterData> LoadEnemies()
    {
        string json = ReadResourceText("EnemyData");
        if (json == null)
        {
            return null;
        }

        return MonsterRosterData.ParseMonsterList(json);
    }

    private static string ReadResourceText(string resourceName)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourceName);
        if (asset == null)
        {
            Debug.LogWarning($"[GameDataLoader] Resources/{resourceName}.json을 찾을 수 없습니다.");
            return null;
        }

        return asset.text;
    }
}
