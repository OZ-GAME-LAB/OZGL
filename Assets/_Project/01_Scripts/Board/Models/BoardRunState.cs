using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Board.Models
{
    // 보드 런 상태와 전투 완료 규칙
    public sealed class BoardRunState
    {
        private readonly HashSet<Vector2Int> _completedBattlePositions = new();
        private readonly HashSet<Vector2Int> _consumedSpecialTilePositions = new();
        private readonly HashSet<Vector2Int> _visitedPositions = new();
        public bool HasActiveRun { get; private set; }
        public int MapSeed { get; private set; }
        public Vector2Int PlayerPosition { get; private set; }
        public bool HasPlayerPosition { get; private set; }
        public Vector2Int CurrentBattlePosition { get; private set; }
        public bool HasCurrentBattle { get; private set; }
        public bool IsBossBattle { get; private set; }
        public bool IsNightEncounter { get; private set; }
        public bool IsBossDefeated { get; private set; }
        public int UnusedActionPoints { get; private set; }
        public bool HasRolledThisTurn { get; private set; }
        public int RolledDiceValue { get; private set; }
        public int RemainingDiceValue { get; private set; }

        public int TurnCount { get; private set; }
        public int TimeCycleStartTurn { get; private set; }
        public bool IsMidBossActive { get; private set; }
        public int DefeatedElitesCount { get; private set; }
        public bool HasObjective { get; private set; }
        public Vector2Int ObjectivePosition { get; private set; }
        public bool IsEliteBattle { get; private set; }
        public IReadOnlyCollection<Vector2Int> VisitedPositions => _visitedPositions;
        public event System.Action OnBattleCompleted; // 전투 종료 알림 이벤트
        public event System.Action OnMidBossDefeated;

        // 적 성장(HP/공격력/방어력에 곱하는 value) — 매 턴 종료 시점의 보드 낮/밤 상태로 누적.
        // 웨이브 마감(낮→밤→낮 한 사이클)까지 중간보스를 못 잡으면 오버턴으로 넘어가 성장이
        // 멈추고 별도 오버턴 값만 쌓인다. 상세: Docs/ENEMY_SCALING_DESIGN.md 4-3절.
        private const float EnemyGrowthValueBase = 0.7f;
        private const float EnemyDayValueIncrement = 0.07f;
        private const float EnemyNightValueIncrement = 0.12f;
        private const float EnemyOverturnValueIncrement = 0.3f;
        public float EnemyGrowthValue { get; private set; } = EnemyGrowthValueBase;
        public float EnemyOverturnValue { get; private set; }
        public bool IsInEnemyOverturn { get; private set; }
        private bool _eliteDefeatedThisCycle;

        public void SetRemainingDiceValue(int value)
        {
            RemainingDiceValue = Mathf.Max(0, value);
        }

        public void RecordDiceRoll(int value)
        {
            int normalizedValue = Mathf.Max(0, value);

            HasRolledThisTurn = normalizedValue > 0;
            RolledDiceValue = normalizedValue;
            RemainingDiceValue = normalizedValue;
        }

        private void ResetTurnDiceState()
        {
            HasRolledThisTurn = false;
            RolledDiceValue = 0;
            RemainingDiceValue = 0;
        }

        public void BeginNewRun(int seed)
        {
            Clear();

            MapSeed = seed;
            HasActiveRun = true;
        }
        public BoardRunSnapshot Capture()
        {
            BoardRunSnapshot saveData = new BoardRunSnapshot
            {
                hasActiveRun = HasActiveRun,
                mapSeed = MapSeed,
                hasPlayerPosition = HasPlayerPosition,
                playerPositionX = PlayerPosition.x,
                playerPositionY = PlayerPosition.y,
                hasCurrentBattle = HasCurrentBattle,
                currentBattlePositionX = CurrentBattlePosition.x,
                currentBattlePositionY = CurrentBattlePosition.y,
                isBossBattle = IsBossBattle,
                isEliteBattle = IsEliteBattle,
                isNightEncounter = IsNightEncounter,
                isBossDefeated = IsBossDefeated,

                hasRolledThisTurn = HasRolledThisTurn,
                rolledDiceValue = RolledDiceValue,
                remainingDiceValue = RemainingDiceValue,

                unusedActionPoints = UnusedActionPoints,
                turnCount = TurnCount,
   timeCycleStartTurn = TimeCycleStartTurn,
                isMidBossActive = IsMidBossActive,
                defeatedElitesCount = DefeatedElitesCount,
                hasObjective = HasObjective,
                objectivePositionX = ObjectivePosition.x,
                objectivePositionY = ObjectivePosition.y,
                enemyGrowthValue = EnemyGrowthValue,
                enemyOverturnValue = EnemyOverturnValue,
                isInEnemyOverturn = IsInEnemyOverturn,
                eliteDefeatedThisCycle = _eliteDefeatedThisCycle
            };

            foreach (Vector2Int position in _completedBattlePositions)
            {
                saveData.completedBattlePositions.Add(new BoardRunPosition
                {
                    x = position.x,
                    y = position.y
                });
            }

            foreach (Vector2Int position in _consumedSpecialTilePositions)
            {
                saveData.consumedSpecialTilePositions.Add(new BoardRunPosition
                {
                    x = position.x,
                    y = position.y
                });
            }

            foreach (Vector2Int position in _visitedPositions)
            {
                saveData.visitedPositions.Add(new BoardRunPosition
                {
                    x = position.x,
                    y = position.y
                });
            }

            return saveData;
        }
        public bool Restore(BoardRunSnapshot saveData)
        {
            if (saveData == null || !saveData.hasActiveRun || saveData.mapSeed <= 0)
            {
                return false;
            }

            Clear();

            HasActiveRun = true;
            MapSeed = saveData.mapSeed;
            HasPlayerPosition = saveData.hasPlayerPosition;
            PlayerPosition = new Vector2Int(saveData.playerPositionX, saveData.playerPositionY);
            HasCurrentBattle = saveData.hasCurrentBattle;
            CurrentBattlePosition = new Vector2Int(
                saveData.currentBattlePositionX,
                saveData.currentBattlePositionY);
            IsBossBattle = saveData.isBossBattle;
            IsEliteBattle = saveData.isEliteBattle;
            IsNightEncounter = saveData.isNightEncounter;
            IsBossDefeated = saveData.isBossDefeated;
            // 기존 저장 파일의 누락 필드 기본값 0 및 음수 보정
            int restoredRemaining = Mathf.Max(0, saveData.remainingDiceValue);
            int restoredRolledValue = Mathf.Max(0, saveData.rolledDiceValue);

            // 구버전 저장 파일에는 rolledDiceValue가 없으므로,
            // 잔여 행동력이라도 있으면 최소한 굴린 상태로 복원합니다.
            if (restoredRolledValue < restoredRemaining)
            {
                restoredRolledValue = restoredRemaining;
            }

            HasRolledThisTurn =
                saveData.hasRolledThisTurn ||
                restoredRolledValue > 0 ||
                restoredRemaining > 0;

            RolledDiceValue = HasRolledThisTurn
                ? restoredRolledValue
                : 0;

            SetRemainingDiceValue(restoredRemaining);

            UnusedActionPoints = Mathf.Max(0, saveData.unusedActionPoints);
            TurnCount = Mathf.Max(0, saveData.turnCount);
            TimeCycleStartTurn = Mathf.Clamp(saveData.timeCycleStartTurn, 0, TurnCount);
            IsMidBossActive = saveData.isMidBossActive;
            DefeatedElitesCount = Mathf.Max(0, saveData.defeatedElitesCount);
            HasObjective = saveData.hasObjective;
            ObjectivePosition = HasObjective
                ? new Vector2Int(saveData.objectivePositionX, saveData.objectivePositionY)
                : Vector2Int.zero;

            // 구버전 세이브(이 필드가 생기기 전)에는 enemyGrowthValue가 항상 기본값 0이다.
            // 실제 진행 중에는 이 값이 절대 0 이하로 내려가지 않으므로 0을 "필드 없음" 신호로 쓴다.
            if (saveData.enemyGrowthValue > 0f)
            {
                EnemyGrowthValue = saveData.enemyGrowthValue;
                EnemyOverturnValue = Mathf.Max(0f, saveData.enemyOverturnValue);
                IsInEnemyOverturn = saveData.isInEnemyOverturn;
                _eliteDefeatedThisCycle = saveData.eliteDefeatedThisCycle;
            }
            else
            {
                EnemyGrowthValue = EnemyGrowthSaveMigration.EstimateValue(TurnCount, DefeatedElitesCount);
                EnemyOverturnValue = 0f;
                IsInEnemyOverturn = false;
                _eliteDefeatedThisCycle = false;
            }

            if (saveData.completedBattlePositions != null)
            {
                foreach (BoardRunPosition position in saveData.completedBattlePositions)
                {
                    if (position != null)
                    {
                        Vector2Int completedPosition = new Vector2Int(position.x, position.y);
                        _completedBattlePositions.Add(completedPosition);

                        // 구버전 완료 전투의 일회성 타일 소비 기록 변환
                        _consumedSpecialTilePositions.Add(completedPosition);
                    }
                }
            }

            if (saveData.consumedSpecialTilePositions != null)
            {
                foreach (BoardRunPosition position in saveData.consumedSpecialTilePositions)
                {
                    if (position != null)
                    {
                        _consumedSpecialTilePositions.Add(new Vector2Int(position.x, position.y));
                    }
                }
            }

            if (saveData.visitedPositions != null)
            {
                foreach (BoardRunPosition position in saveData.visitedPositions)
                {
                    if (position != null)
                    {
                        _visitedPositions.Add(new Vector2Int(position.x, position.y));
                    }
                }
            }

            // 방문 기록이 없던 구버전 저장은 현재 위치부터 기록을 이어갑니다.
            if (_visitedPositions.Count == 0 && HasPlayerPosition)
            {
                _visitedPositions.Add(PlayerPosition);
            }
            return true;
        }
        public void SavePlayerPosition(Vector2Int position)
        {

            PlayerPosition = position;
            HasPlayerPosition = true;
            _visitedPositions.Add(position);
        }
        public void SaveUnusedActionPoints(int actionPoints)
        {

            UnusedActionPoints = Mathf.Max(0, actionPoints);
        }
        public void SaveObjectivePosition(Vector2Int position)
        {
            HasObjective = true;
            ObjectivePosition = position;
        }
        public void ClearObjective()
        {
            HasObjective = false;
            ObjectivePosition = Vector2Int.zero;
        }
 public void ActivateMidBoss()
        {
            IsMidBossActive = true;
        }
        public void BeginBattle(Vector2Int battlePosition, bool isBossBattle, bool isEliteBattle = false, bool isNightEncounter = false)
        {
            CurrentBattlePosition = battlePosition;
            HasCurrentBattle = true;
            IsBossBattle = isBossBattle;
            IsEliteBattle = isEliteBattle; // 엘리트전 여부 기록
            IsNightEncounter = isNightEncounter; // 전투 시작 시점의 보드 낮/밤 — 적 종류/스탯 선택에 사용
            SavePlayerPosition(battlePosition);
        }
        public void CompleteCurrentBattle()
        {
            if (!HasCurrentBattle) return;

            bool defeatedMidBoss = IsEliteBattle;

            ConsumeSpecialTile(CurrentBattlePosition);

            if (!IsBossBattle)
            {
                _completedBattlePositions.Add(CurrentBattlePosition);
            }
            else
            {
                IsBossDefeated = true; // [추가] 보스 처치 플래그 설정
            }

  if (defeatedMidBoss) // fix 브랜치의 새로운 조건문을 사용 (기존: IsEliteBattle)
            {
                DefeatedElitesCount++; // 엘리트전 카운트 증가!
                IsMidBossActive = false;
                TimeCycleStartTurn = TurnCount;
                // 중간보스 처치 시 오버턴 값은 사라지고, 성장값에 +1을 더한 뒤
                // 다음 턴부터 정상 낮/밤 성장으로 복귀한다.
                _eliteDefeatedThisCycle = true;
                IsInEnemyOverturn = false;
                EnemyOverturnValue = 0f;
                EnemyGrowthValue += 1f;
            }

            // 일반 전투에서는 목표를 유지하고, 목표 전투가 끝났을 때만 해제합니다.
            if (IsEliteBattle || IsBossBattle ||
                (HasObjective && ObjectivePosition == CurrentBattlePosition))
            {
                ClearObjective();
            }

            HasCurrentBattle = false;
            IsBossBattle = false;
            IsEliteBattle = false;
            IsNightEncounter = false;

            OnBattleCompleted?.Invoke(); // 목표 매니저 알림 이벤트!
            if (defeatedMidBoss)
            {
                OnMidBossDefeated?.Invoke();
            }
        }
        public bool IsBattleCompleted(Vector2Int position)
        {
            return _completedBattlePositions.Contains(position) ||
                   _consumedSpecialTilePositions.Contains(position);
        }
        public void ConsumeSpecialTile(Vector2Int position)
        {

            _consumedSpecialTilePositions.Add(position);
        }
        public bool IsSpecialTileConsumed(Vector2Int position)
        {
            return _consumedSpecialTilePositions.Contains(position);
        }
        public void AdvanceTurn(bool wasNightTurn)
        {

            TurnCount++;
            ResetTurnDiceState();

            if (IsInEnemyOverturn)
            {
                EnemyOverturnValue += EnemyOverturnValueIncrement;
            }
            else
            {
                EnemyGrowthValue += wasNightTurn ? EnemyNightValueIncrement : EnemyDayValueIncrement;
            }
        }

        /// <summary>
        /// 낮→밤→낮 한 사이클(웨이브)이 방금 끝났을 때 호출한다. 그 사이클 안에서 중간보스를
        /// 못 잡았으면 오버턴에 진입시킨다. 호출 시점은 <see cref="AdvanceTurn"/> 이후,
        /// 사이클이 실제로 끝난 턴에 한정된다 — 호출부(BoardSceneController)가 낮/밤 전환을
        /// 감지해서 판단한다.
        /// </summary>
        public void RegisterEnemyGrowthCycleBoundary()
        {
            if (!_eliteDefeatedThisCycle)
            {
                IsInEnemyOverturn = true;
            }
            _eliteDefeatedThisCycle = false;
        }
        public void Clear()
        {
            HasActiveRun = false;
            MapSeed = 0;

            PlayerPosition = Vector2Int.zero;
            HasPlayerPosition = false;

            CurrentBattlePosition = Vector2Int.zero;
            HasCurrentBattle = false;
            IsBossBattle = false;
            IsBossDefeated = false; // 보스 처치 상태 초기화
            // [추가] New Game에서 이전 엘리트 전투 상태가 남지 않도록 초기화
            IsEliteBattle = false;
            IsNightEncounter = false;

            HasRolledThisTurn = false;
            RolledDiceValue = 0;
            RemainingDiceValue = 0;

            UnusedActionPoints = 0;
            TurnCount = 0;
            TimeCycleStartTurn = 0;
            IsMidBossActive = false;

            _completedBattlePositions.Clear();
            _consumedSpecialTilePositions.Clear();
            _visitedPositions.Clear();

            DefeatedElitesCount = 0;
            ClearObjective();

            EnemyGrowthValue = EnemyGrowthValueBase;
            EnemyOverturnValue = 0f;
            IsInEnemyOverturn = false;
            _eliteDefeatedThisCycle = false;

            // [추가] 이전 런의 씬 객체가 남긴 전투 완료 구독 정보 초기화
            OnBattleCompleted = null;
            OnMidBossDefeated = null;
        }
    }
}
