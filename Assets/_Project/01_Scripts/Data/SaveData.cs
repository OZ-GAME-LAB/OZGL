using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public List<UnitStuff> units;                   // 기물 보유 현황
    public List<UnitSaveEntry> unitSaveEntries;     // 기물 배치 정보
    public List<RelicSaveEntry> relicSaveEntries;   // 보유 유물 정보

    public int lastChapter;                         // 진행중이던 챕터
    public int posX;                                // 포지션 X값
    public int posY;                                // 포지션 Y값
    public long playTime;                           // 플레이타임

    // 신규 유저 초기값
    public static SaveData CreateDefault()
    {
        return new SaveData
        {
            units = { },
            unitSaveEntries = { },
            lastChapter = 1,
            posX = 0,
            posY = 0,
            playTime = 0
        };
    }
}

[System.Serializable]
public class UnitStuff
{
    public int unitId;
    public int amount;
}

[System.Serializable]
public class UnitSaveEntry
{
    public int unitId;
    public int placeGrid;
}

[System.Serializable]
public class RelicSaveEntry
{
    public int relicId;
    public int relicIndex;
}
