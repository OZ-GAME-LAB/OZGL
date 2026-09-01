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
        /// 플레이어가 "턴 종료" 버튼을 눌러 지금까지 끝낸 턴 수입니다.
        /// 행동력을 모두 소모한 것만으로는 증가하지 않습니다.
        /// </summary>
        public static int TurnCount { get; private set; }

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
        /// 보드에서 발생한 전투 정보를 저장합니다.
        /// </summary>
        public static void BeginBattle(
            Vector2Int battlePosition,
            bool isBossBattle)
        {
            EnsureActiveRun();

            CurrentBattlePosition = battlePosition;
            HasCurrentBattle = true;
            IsBossBattle = isBossBattle;

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
            if (!HasCurrentBattle)
            {
                return;
            }

            if (!IsBossBattle)
            {
                _completedBattlePositions.Add(CurrentBattlePosition);
            }

            HasCurrentBattle = false;
            IsBossBattle = false;
        }

        /// <summary>
        /// 해당 Battle 타일을 이미 완료했는지 확인합니다.
        /// </summary>
        public static bool IsBattleCompleted(Vector2Int position)
        {
            return _completedBattlePositions.Contains(position);
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

            TurnCount = 0;

            _completedBattlePositions.Clear();
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