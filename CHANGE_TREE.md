# 변경 트리

기준 브랜치: `fix/unit_tile_interaction`  
기준 커밋: `776f1e5` (`2026-09-06`)

```text
fix/unit_tile_interaction
|
├─ [Feat] 유닛 획득 타일 맵 설정 및 생성 범위 추가  (a0cb692)
│  ├─ MapObjectiveManager
│  │  └─ 목표 타일 생성 최대 거리(`maxSpawnDistance`) 제한
│  └─ 02_MainGame
│     ├─ 유닛 획득 타일 생성 수·거리 설정
│     ├─ 시작 지점 유닛 배치 설정
│     └─ UnitRosterData 및 이벤트 UI 참조 연결
|
├─ [Feat] 유닛 획득 타일 보상 및 편성 반영  (a7d0312)
│  ├─ BoardSceneController
│  │  └─ UnitAcquisition 타일 도착 시 로스터에서 무작위 유닛을 획득해 인벤토리에 추가
│  └─ UnitFormationController
│     └─ 인벤토리의 유닛 추가 이벤트를 구독해 편성 목록을 즉시 갱신
|
└─ [Refactor] 전투 UI 제어 로직 분리  (776f1e5)
   ├─ CombatSceneController
   │  └─ 승패 판정·보드 복귀만 담당하고 전투 결과 이벤트 발행
   ├─ BattleUIController (신규)
   │  ├─ 전투 타이머와 배속 제어
   │  ├─ 설정·항복 UI 처리
   │  └─ 승패 결과 및 임시 보상 UI 처리
   └─ 03_Combat
      └─ BattleUIController와 EventSystem을 씬에 연결
```

## 기능 흐름

```text
맵 생성 설정
  → UnitAcquisition 타일 생성
  → 플레이어가 타일 도착
  → 무작위 유닛을 PlayerInventoryManager에 추가
  → UnitFormationController가 OnUnitAdded를 수신
  → 편성 목록과 유닛 수 갱신

전투 종료
  → CombatSceneController가 승패 판정
  → BattleUIController가 결과 이벤트 수신
  → 승리: 보상 선택 → 결과 화면 / 패배: 결과 화면
  → 보드 씬 복귀
```
