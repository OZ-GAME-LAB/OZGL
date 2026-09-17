using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Board.Models
{
    // 보드 런 상태와 전투 완료 규칙
    public sealed class BoardRunState
    {
        private readonly HashSet<Vector2Int> _completedBattlePositions = new();
        private readonly HashSet<Vector2Int> _consumedSpecialTilePositions = new();
        public bool HasActiveRun { get; private set; }
        public int MapSeed { get; private set; }
        public Vector2Int PlayerPosition { get; private set; }
        public bool HasPlayerPosition { get; private set; }
        public Vector2Int CurrentBattlePosition { get; private set; }
        public bool HasCurrentBattle { get; private set; }
        public bool IsBossBattle { get; private set; }
        public bool IsBossDefeated { get; private set; }
        public int UnusedActionPoints { get; private set; }
        public int RemainingDiceValue { get; private set; }
        public void SetRemainingDiceValue(int value)
        {
            RemainingDiceValue = Mathf.Max(0, value);
        }
        public int TurnCount { get; private set; }
        public int DefeatedElitesCount { get; private set; }
        public bool HasObjective { get; private set; }
        public Vector2Int ObjectivePosition { get; private set; }
        public bool IsEliteBattle { get; private set; }
        public event System.Action OnBattleCompleted; // 전투 종료 알림 이벤트
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
                isBossDefeated = IsBossDefeated,
                remainingDiceValue = RemainingDiceValue,
                unusedActionPoints = UnusedActionPoints,
                turnCount = TurnCount,
                defeatedElitesCount = DefeatedElitesCount
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
            IsBossDefeated = saveData.isBossDefeated;
            // 기존 저장 파일의 누락 필드 기본값 0 및 음수 보정
            SetRemainingDiceValue(saveData.remainingDiceValue);
            UnusedActionPoints = Mathf.Max(0, saveData.unusedActionPoints);
            TurnCount = Mathf.Max(0, saveData.turnCount);
            DefeatedElitesCount = Mathf.Max(0, saveData.defeatedElitesCount);

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
            return true;
        }
        public void SavePlayerPosition(Vector2Int position)
        {

            PlayerPosition = position;
            HasPlayerPosition = true;
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
        public void BeginBattle(Vector2Int battlePosition, bool isBossBattle, bool isEliteBattle = false)
        {
            CurrentBattlePosition = battlePosition;
            HasCurrentBattle = true;
            IsBossBattle = isBossBattle;
            IsEliteBattle = isEliteBattle; // 엘리트전 여부 기록
            SavePlayerPosition(battlePosition);
        }
        public void CompleteCurrentBattle()
        {
            if (!HasCurrentBattle) return;

            ConsumeSpecialTile(CurrentBattlePosition);

            if (!IsBossBattle) 
            {
                _completedBattlePositions.Add(CurrentBattlePosition);
            }
            else 
            {
                IsBossDefeated = true; // [추가] 보스 처치 플래그 설정
            }

            if (IsEliteBattle) DefeatedElitesCount++; // 엘리트전 카운트 증가!

            // 일반 전투에서는 목표를 유지하고, 목표 전투가 끝났을 때만 해제합니다.
            if (IsEliteBattle || IsBossBattle ||
                (HasObjective && ObjectivePosition == CurrentBattlePosition))
            {
                ClearObjective();
            }
            
            HasCurrentBattle = false;
            IsBossBattle = false;
            IsEliteBattle = false;

            OnBattleCompleted?.Invoke(); // 목표 매니저 알림 이벤트!
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
        public void AdvanceTurn()
        {

            TurnCount++;
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

            RemainingDiceValue = 0;
            UnusedActionPoints = 0;
            TurnCount = 0;

            _completedBattlePositions.Clear();
            _consumedSpecialTilePositions.Clear();

            DefeatedElitesCount = 0;
            ClearObjective();

            // [추가] 이전 런의 씬 객체가 남긴 전투 완료 구독 정보 초기화
            OnBattleCompleted = null;
        }
    }
}
