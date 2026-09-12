using System.Collections.Generic;
using UnityEngine;

namespace OzGameLab01.Data
{
    /// <summary>
    /// 한 번의 게임 진행 중 유지되어야 하는 보드 데이터를 관리합니다.
    ///
    /// 일반 전투 씬으로 이동해도 맵과 플레이어 위치를 복원할 수 있도록
    /// 맵 Seed, 플레이어 좌표, 완료한 전투 타일을 보관합니다.
    ///
    /// 게임 종료 후 결과 씬에서 타이틀로 돌아갈 때 Clear()를 호출하여
    /// 모든 진행 데이터를 초기화합니다.
    /// </summary>
    public static class BoardRunData
    {
        private static readonly HashSet<Vector2Int> _completedBattlePositions = new();
        private static readonly HashSet<Vector2Int> _consumedSpecialTilePositions = new();

        /// <summary>
        /// 현재 진행 중인 게임이 존재하는지 나타냅니다.
        /// </summary>
        public static bool HasActiveRun { get; private set; }

        /// <summary>
        /// 동일한 보드 맵을 다시 생성하기 위한 Seed입니다.
        /// </summary>
        public static int MapSeed { get; private set; }

        /// <summary>
        /// 보드에서 플레이어가 마지막으로 도착한 좌표입니다.
        /// </summary>
        public static Vector2Int PlayerPosition { get; private set; }

        /// <summary>
        /// 저장된 플레이어 좌표가 존재하는지 나타냅니다.
        /// 시작 좌표 (0, 0)과 저장 여부를 구분하기 위해 사용합니다.
        /// </summary>
        public static bool HasPlayerPosition { get; private set; }

        /// <summary>
        /// 현재 진행 중인 전투가 발생한 보드 좌표입니다.
        /// </summary>
        public static Vector2Int CurrentBattlePosition { get; private set; }

        /// <summary>
        /// 현재 전투 정보가 저장되어 있는지 나타냅니다.
        /// </summary>
        public static bool HasCurrentBattle { get; private set; }

        /// <summary>
        /// 현재 전투가 보스전인지 나타냅니다.
        /// </summary>
        public static bool IsBossBattle { get; private set; }

        /// <summary>
        /// 보스가 처치되었는지 여부를 나타냅니다. (임시 엔딩 확인용)
        /// </summary>
        public static bool IsBossDefeated { get; private set; }

        /// <summary>
        /// 가장 최근 턴 종료 시 사용하지 않고 남은 행동력입니다.
        /// 현재 보드에서는 주사위 눈금을 행동력으로 사용합니다.
        /// </summary>
        public static int UnusedActionPoints { get; private set; }

        /// <summary>
        /// 현재 이동에 사용할 수 있는 남은 주사위 눈금을 반환합니다.
        /// </summary>
        public static int RemainingDiceValue { get; private set; }

        /// <summary>
        /// 주사위 획득, 이동 및 턴 종료 후 남은 눈금을 런 상태에 반영합니다.
        /// </summary>
        public static void SetRemainingDiceValue(int value)
        {
            RemainingDiceValue = Mathf.Max(0, value);
        }

        /// <summary>
        /// 플레이어가 턴 종료 버튼을 눌러 완료한 누적 턴 수입니다.
        /// 시간 및 밤 시스템에서 경과 턴을 계산하는 데 사용합니다.
        /// </summary>
        public static int TurnCount { get; private set; }

        /// <summary>
        /// 플레이어가 중간보스(엘리트 타일)를 몇이나 처치했는지 알기위한 값입니다.
        /// 순차적으로 엘리트타일과 보스타일을 생성하여 절차적 생성을 통해 엘리트타일과 보스 타일을
        /// 생성할 때 대비 진행하던 방향의 역방향으로 플레이어가 진행하는것을 방지하고 플레이어의 이동동선을 자연스럽게 유도하는데 사용됩니다.
        /// </summary>
        public static int DefeatedElitesCount { get; private set; }
        public static bool IsEliteBattle { get; private set; }
        public static event System.Action OnBattleCompleted; // 전투 종료 알림 이벤트

        /// <summary>
        /// 새로운 게임 진행 데이터를 생성합니다.
        /// 타이틀 화면에서 게임을 새로 시작할 때 호출합니다.
        /// </summary>
        public static void BeginNewRun()
        {
            Clear();

            MapSeed = Random.Range(1, int.MaxValue);
            HasActiveRun = true;

            Debug.Log($"[BoardRunData] 새로운 게임 진행을 시작합니다. Map Seed: {MapSeed}");
        }

        /// <summary>
        /// 현재 런 상태를 Continue용 직렬화 데이터로 변환합니다.
        /// </summary>
        public static BoardRunSaveData CreateSaveData()
        {
            BoardRunSaveData saveData = new BoardRunSaveData
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
                saveData.completedBattlePositions.Add(new BoardPositionSaveEntry
                {
                    x = position.x,
                    y = position.y
                });
            }

            foreach (Vector2Int position in _consumedSpecialTilePositions)
            {
                saveData.consumedSpecialTilePositions.Add(new BoardPositionSaveEntry
                {
                    x = position.x,
                    y = position.y
                });
            }

            return saveData;
        }

        /// <summary>
        /// Continue가 선택한 저장 데이터로 정적 런 상태를 복원합니다.
        /// </summary>
        public static bool RestoreFromSaveData(BoardRunSaveData saveData)
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
                foreach (BoardPositionSaveEntry position in saveData.completedBattlePositions)
                {
                    if (position != null)
                    {
                        Vector2Int completedPosition = new Vector2Int(position.x, position.y);
                        _completedBattlePositions.Add(completedPosition);

                        // 구버전 저장 데이터의 완료 전투도 일회성 특수 타일로 마이그레이션합니다.
                        _consumedSpecialTilePositions.Add(completedPosition);
                    }
                }
            }

            if (saveData.consumedSpecialTilePositions != null)
            {
                foreach (BoardPositionSaveEntry position in saveData.consumedSpecialTilePositions)
                {
                    if (position != null)
                    {
                        _consumedSpecialTilePositions.Add(new Vector2Int(position.x, position.y));
                    }
                }
            }

            Debug.Log($"[BoardRunData] 저장된 게임 진행을 복원했습니다. Map Seed: {MapSeed}");
            return true;
        }

        /// <summary>
        /// 활성화된 게임 진행 데이터가 없다면 새로 생성합니다.
        ///
        /// 보드 씬을 직접 실행하는 테스트 상황에서도
        /// 정상적인 맵 Seed가 존재하도록 보장합니다.
        /// </summary>
        public static void EnsureActiveRun()
        {
            if (HasActiveRun)
            {
                return;
            }

            BeginNewRun();
        }

        /// <summary>
        /// 플레이어가 마지막으로 도착한 보드 좌표를 저장합니다.
        /// </summary>
        public static void SavePlayerPosition(Vector2Int position)
        {
            EnsureActiveRun();

            PlayerPosition = position;
            HasPlayerPosition = true;

            Debug.Log($"[BoardRunData] 플레이어 위치를 저장했습니다. Position: {position}");
        }

        /// <summary>
        /// 턴 종료 시 사용하지 않은 행동력을 저장합니다.
        /// </summary>
        public static void SaveUnusedActionPoints(int actionPoints)
        {
            EnsureActiveRun();

            UnusedActionPoints = Mathf.Max(0, actionPoints);

            Debug.Log(
                $"[BoardRunData] 남은 행동력 저장: " +
                $"{UnusedActionPoints}");
        }

        /// <summary>
        /// 보드에서 발생한 전투 정보를 저장합니다.
        /// </summary>
        public static void BeginBattle(Vector2Int battlePosition, bool isBossBattle, bool isEliteBattle = false)
        {
            EnsureActiveRun();
            CurrentBattlePosition = battlePosition;
            HasCurrentBattle = true;
            IsBossBattle = isBossBattle;
            IsEliteBattle = isEliteBattle; // 엘리트전 여부 기록
            SavePlayerPosition(battlePosition);

            Debug.Log(
                $"[BoardRunData] 전투 정보를 저장했습니다. " +
                $"Position: {battlePosition}, Boss: {isBossBattle}");
        }

        /// <summary>
        /// 현재 일반 전투를 완료 처리합니다.
        ///
        /// 보스전은 결과 씬으로 이동하므로
        /// 일반 Battle 타일 완료 목록에는 등록하지 않습니다.
        /// </summary>
        public static void CompleteCurrentBattle()
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
                UnityEngine.Debug.Log("<color=yellow>=========================================</color>");
                UnityEngine.Debug.Log("<color=green><b>보스처치 게임종료</b></color>");
                UnityEngine.Debug.Log("<color=yellow>=========================================</color>");
            }

            if (IsEliteBattle) DefeatedElitesCount++; // 엘리트전 카운트 증가!
            
            HasCurrentBattle = false;
            IsBossBattle = false;
            IsEliteBattle = false;

            OnBattleCompleted?.Invoke(); // 목표 매니저 알림 이벤트!
        }

        /// <summary>
        /// 해당 Battle 타일을 이미 완료했는지 확인합니다.
        /// </summary>
        public static bool IsBattleCompleted(Vector2Int position)
        {
            return _completedBattlePositions.Contains(position) ||
                   _consumedSpecialTilePositions.Contains(position);
        }

        /// <summary>
        /// 효과 처리가 끝난 일회성 특수 타일의 좌표를 현재 런에 기록합니다.
        /// </summary>
        public static void ConsumeSpecialTile(Vector2Int position)
        {
            EnsureActiveRun();

            if (_consumedSpecialTilePositions.Add(position))
            {
                Debug.Log($"[BoardRunData] 특수 타일 소모 완료. Position: {position}");
            }
        }

        /// <summary>
        /// 해당 좌표의 일회성 특수 타일이 이미 발동을 마쳤는지 확인합니다.
        /// </summary>
        public static bool IsSpecialTileConsumed(Vector2Int position)
        {
            return _consumedSpecialTilePositions.Contains(position);
        }

        /// <summary>
        /// 플레이어가 "턴 종료" 버튼을 눌렀을 때 호출합니다.
        /// 누적 턴 수를 1 증가시킵니다.
        /// </summary>
        public static void AdvanceTurn()
        {
            EnsureActiveRun();

            TurnCount++;

            Debug.Log($"[BoardRunData] 턴 종료. TurnCount: {TurnCount}");
        }

        /// <summary>
        /// 현재 게임 진행 데이터를 모두 초기화합니다.
        ///
        /// 보스전 종료 후 결과 씬에서 타이틀로 이동할 때 호출합니다.
        /// </summary>
        public static void Clear()
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

            // [추가] 이전 런의 씬 객체가 남긴 전투 완료 구독 정보 초기화
            OnBattleCompleted = null;
        }

        /// <summary>
        /// Enter Play Mode Options에서 Domain Reload가 꺼져 있어도
        /// 이전 플레이의 정적 데이터가 남지 않도록 초기화합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart()
        {
            Clear();
        }
    }
}
