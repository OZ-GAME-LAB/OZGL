using UnityEngine;
using OzGameLab01.Board.Models;
using OzGameLab01.Board.Controllers;
namespace OzGameLab01.Data
{
    // 기존 외부 API와 보드 상태 및 저장 변환의 연결
    public static class BoardRunData
    {
        private static readonly BoardRunState _state = new BoardRunState();
        public static bool HasActiveRun => _state.HasActiveRun;
        public static int MapSeed => _state.MapSeed;
        public static Vector2Int PlayerPosition => _state.PlayerPosition;
        public static bool HasPlayerPosition => _state.HasPlayerPosition;
        public static Vector2Int CurrentBattlePosition => _state.CurrentBattlePosition;
        public static bool HasCurrentBattle => _state.HasCurrentBattle;
        public static bool IsBossBattle => _state.IsBossBattle;
        public static bool IsBossDefeated => _state.IsBossDefeated;
        public static int UnusedActionPoints => _state.UnusedActionPoints;
        public static int RemainingDiceValue => _state.RemainingDiceValue;
        public static int TurnCount => _state.TurnCount;
        public static int DefeatedElitesCount => _state.DefeatedElitesCount;
        public static bool IsEliteBattle => _state.IsEliteBattle;
        public static event System.Action OnBattleCompleted
        {
            add { _state.OnBattleCompleted += value; }
            remove { _state.OnBattleCompleted -= value; }
        }
        public static void BeginNewRun() { _state.BeginNewRun(Random.Range(1, int.MaxValue)); }
        public static void EnsureActiveRun() { if (!HasActiveRun) { BeginNewRun(); } }
        public static void SetRemainingDiceValue(int value) { _state.SetRemainingDiceValue(value); }
        public static void SavePlayerPosition(Vector2Int position) { EnsureActiveRun(); _state.SavePlayerPosition(position); }
        public static void SaveUnusedActionPoints(int actionPoints) { EnsureActiveRun(); _state.SaveUnusedActionPoints(actionPoints); }
        public static void BeginBattle(Vector2Int battlePosition, bool isBossBattle, bool isEliteBattle = false) { EnsureActiveRun(); _state.BeginBattle(battlePosition, isBossBattle, isEliteBattle); }
        public static void CompleteCurrentBattle() { _state.CompleteCurrentBattle(); }
        public static bool IsBattleCompleted(Vector2Int position) { return _state.IsBattleCompleted(position); }
        public static void ConsumeSpecialTile(Vector2Int position) { EnsureActiveRun(); _state.ConsumeSpecialTile(position); }
        public static bool IsSpecialTileConsumed(Vector2Int position) { return _state.IsSpecialTileConsumed(position); }
        public static void AdvanceTurn() { EnsureActiveRun(); _state.AdvanceTurn(); }
        public static void Clear() { _state.Clear(); }
        public static BoardRunSaveData CreateSaveData() { return BoardRunSaveMapper.ToSave(_state.Capture()); }
        public static bool RestoreFromSaveData(BoardRunSaveData saveData) { return _state.Restore(BoardRunSaveMapper.FromSave(saveData)); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart() { Clear(); }
    }
}