# 변경 트리

대상 브랜치: `feat/add_tutorial`  
기준 커밋: `2b91abc` (`[Add] 보드 명암 셰이더 및 시간대 표현용 레이어 추가`)

```text
feat/add_tutorial
|
├─ [Feat] 주사위 턴 상태 저장 및 복원  (5cdc696)
│  └─ Assets/_Project/01_Scripts
│     ├─ Board
│     │  ├─ Contracts
│     │  │  └─ BoardDiceMessages.cs
│     │  │     └─ 굴림 여부와 최초 굴림값을 보드 주사위 스냅샷에 추가
│     │  ├─ Controllers
│     │  │  ├─ BoardDiceRequestAdapter.cs
│     │  │  │  └─ 보드 런 데이터와 주사위 요청 상태를 동기화
│     │  │  ├─ BoardRunData.cs
│     │  │  │  └─ 턴별 주사위 상태 조회·기록 API 추가
│     │  │  └─ BoardRunSaveMapper.cs
│     │  │     └─ 신규 주사위 상태의 저장·복원 매핑 추가
│     │  └─ Models
│     │     ├─ BoardRunSnapshot.cs
│     │     │  └─ 굴림 여부와 최초 굴림값 필드 추가
│     │     └─ BoardRunState.cs
│     │        └─ 굴림값 기록, 턴 종료 초기화, 구버전 세이브 호환 복원
│     ├─ Dice/Models/DiceModel.cs
│     │  └─ 로컬 주사위 상태와 보드 상태를 함께 검사해 중복 굴림 방지
│     └─ Save/Models/SaveData.cs
│        └─ 턴별 주사위 상태 저장 필드 추가
|
├─ [Feat] 메인 게임 턴 UI 및 시간대 연출 개선  (ca583cb)
│  └─ Assets/_Project/01_Scripts/Board
│     ├─ Models/BoardTurnRules.cs
│     │  ├─ 아침·점심·저녁 구간을 개별 턴 수로 계산
│     │  └─ 시간대 전환 및 다음 전환까지 남은 턴 계산
│     └─ Controllers
│        ├─ BoardSceneController.cs
│        │  ├─ 시간대별 턴 설정과 정오 이벤트 추가
│        │  └─ 턴 진행 시 시간대 오버레이·HUD 갱신
│        └─ BoardUIController.cs
│           ├─ 주사위 굴림·이동 상태에 따른 행동력 피드백 갱신
│           ├─ 턴 시작 시 주사위 창 자동 표시 흐름 정리
│           └─ 낮·밤 전환 시 시계 회전 애니메이션과 정오 안내 추가
|
└─ [Modify] 메인 게임 씬 UI 구성 갱신  (05f5fc5)
   └─ Assets/_Project/03_Scenes/02_MainGame.unity/02_MainGame.unity
      ├─ 기존 ReadyUI를 Refactor/ReadyUI 프리팹으로 교체
      ├─ UnitAcquirePopup 프리팹 배치 및 UI 참조 재연결
      ├─ 아침 8턴·점심 2턴·저녁 5턴 설정 적용
      └─ 자동 주사위 UI와 시계 애니메이션 직렬화 값 적용
```

## 기능 흐름

```text
주사위 굴림
  → 최초 굴림값과 잔여 행동력을 BoardRunState에 기록
  → 이동할 때 잔여 행동력 갱신
  → ReadyUI의 턴 종료 버튼에 행동력 상태 표시
  → 저장 시 턴별 주사위 상태 보존
  → 다음 턴 시작 시 주사위 상태 초기화

턴 종료
  → 아침·점심·저녁 구간 계산
  → 시간대 오버레이와 남은 턴 HUD 갱신
  → 시간대 전환 안내 표시
  → 낮·밤 전환 시 시계 애니메이션 재생
  → 다음 턴 주사위 창 자동 표시
```
