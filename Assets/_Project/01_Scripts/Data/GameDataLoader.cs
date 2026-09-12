using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 05_Data/Resources의 JSON을 읽어 역직렬화하는 임시 로더입니다.
/// Unit/Enemy 스탯은 이미 UnitRosterData/MonsterRosterData가 각자의 JSON을 직접 파싱해
/// 캐싱하므로 여기서는 다루지 않습니다(RuntimeDataManager가 그 두 asset을 그대로 참조).
/// Synergy와 유닛 액티브 스킬(UnitSkillData)은 아직 대응하는 라이브 로스터가 없어 이
/// 로더로 직접 읽습니다.
/// </summary>
public static class GameDataLoader
{
    public static List<SynergyData> LoadSynergies()
    {
        TextAsset asset = Resources.Load<TextAsset>("SynergyData");
        if (asset == null)
        {
            Debug.LogWarning("[GameDataLoader] Resources/SynergyData.json을 찾을 수 없습니다.");
            return null;
        }

        return JsonConvert.DeserializeObject<SynergyDataList>(asset.text)?.GetList();
    }

    /// <summary>
    /// 엑셀 스킬 시트(액티브 스킬 이름/효과 텍스트)를 damage/cooldown/debuff 하나로 단순화해
    /// 담은 임시 데이터입니다. 수치는 전부 기능 검증용 placeholder이며 실제 밸런스는 아직
    /// 정해지지 않았습니다 — Docs/Database/UnitData.xlsx 스킬 시트 참고.
    /// </summary>
    public static List<SkillData> LoadUnitSkills()
    {
        TextAsset asset = Resources.Load<TextAsset>("UnitSkillData");
        if (asset == null)
        {
            Debug.LogWarning("[GameDataLoader] Resources/UnitSkillData.json을 찾을 수 없습니다.");
            return null;
        }

        return JsonConvert.DeserializeObject<SkillDataList>(asset.text)?.GetList();
    }
}
