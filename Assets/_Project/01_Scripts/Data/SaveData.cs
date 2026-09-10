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

    // [추가] New Game과 Continue에서 사용하는 런 전용 저장 데이터
    public BoardRunSaveData boardRun;

    // 신규 유저 초기값
    public static SaveData CreateDefault()
    {
        return new SaveData
        {
            //units = { },
            // [수정] 기본 저장 데이터 생성 시 null 컬렉션 초기화 예외 방지
            units = new List<UnitStuff>(),
            //unitSaveEntries = { },
            // [수정] 편성 및 유물 목록을 항상 사용할 수 있도록 명시적으로 생성
            unitSaveEntries = new List<UnitSaveEntry>(),
            relicSaveEntries = new List<RelicSaveEntry>(),
            lastChapter = 1,
            posX = 0,
            posY = 0,
            //playTime = 0
            // [수정] 런 전용 저장 데이터를 함께 초기화할 수 있도록 구분자 추가
            playTime = 0,
            boardRun = null
        };
    }
}

/// <summary>
/// 한 번의 게임 진행에만 속하는 맵, 위치, 전투 및 편성 상태
/// </summary>
[System.Serializable]
public class BoardRunSaveData
{
    public bool hasActiveRun;
    public int mapSeed;
    public bool hasPlayerPosition;
    public int playerPositionX; // 플레이어가 마지막으로 위치한 보드 X 좌표
    public int playerPositionY; // 플레이어가 마지막으로 위치한 보드 Y 좌표입니다.
    public bool hasCurrentBattle;
    public int currentBattlePositionX; // 현재 전투가 발생한 보드 X 좌표
    public int currentBattlePositionY; // 현재 전투가 발생한 보드 Y 좌표
    public bool isBossBattle;
    public bool isEliteBattle;
    public bool isBossDefeated;
    public int unusedActionPoints; // 턴 종료시 남은 행동력
    public int turnCount;
    public int defeatedElitesCount;
    public List<BoardPositionSaveEntry> completedBattlePositions = new List<BoardPositionSaveEntry>(); // 전투 완료 진입 불가 
    public List<BoardPositionSaveEntry> consumedSpecialTilePositions = new List<BoardPositionSaveEntry>(); // 발동이 끝난 일회성 특수 타일
    // 전투 슬롯 순서대로 저장한 유닛 ID 목록 / 빈 슬롯은 -1
    public List<int> battleFormationUnitIds = new List<int>();

    // 서포트 슬롯 순서대로 저장한 유닛 ID 목록 / 빈 슬롯은 -1
    public List<int> supportFormationUnitIds = new List<int>();
}

/// <summary>
/// JsonUtility로 Vector2Int 좌표를 저장하기 위한 직렬화 항목
/// </summary>
[System.Serializable]
public class BoardPositionSaveEntry
{
    public int x;
    public int y;
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
