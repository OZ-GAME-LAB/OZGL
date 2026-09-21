using System;
using OzGameLab01.Board.Contracts;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Controllers;
using OzGameLab01.Data;

namespace OzGameLab01.Board.Controllers
{
    /// <summary>보드 상태 소유자 이전 전까지 기존 BoardPlayerController API를 연결하는 어댑터.</summary>
    public sealed class BoardDiceRequestAdapter : IDisposable
    {
        private readonly IDisposable _query;
        private readonly IDisposable _apply;
        public BoardDiceRequestAdapter(MessageBus bus)
        {
            _query = bus.Handle<BoardDiceStateRequested, BoardDiceSnapshot>(_ =>
            {
                BoardPlayerController player = BoardPlayerController.Instance;

                return player == null
                    ? default
                    : new BoardDiceSnapshot(
                        available: true,
                        moving: player.IsMoving,
                        remaining: BoardRunData.RemainingDiceValue,
                        hasRolledThisTurn: BoardRunData.HasRolledThisTurn,
                        rolledDiceValue: BoardRunData.RolledDiceValue);
            });

            _apply = bus.Handle<BoardDiceValueRequested, bool>(request =>
            {
                BoardPlayerController player = BoardPlayerController.Instance;

                if (player == null ||
                    player.IsMoving ||
                    BoardRunData.HasRolledThisTurn ||
                    BoardRunData.RemainingDiceValue > 0)
                {
                    return false;
                }

                // 최초 굴림값과 잔여 행동력을 동시에 기록합니다.
                BoardRunData.RecordDiceRoll(request.Value);

                // 실제 행동력은 즉시 반영하되 HUD는 주사위 연출 완료 후 갱신합니다.
                player.SetCurrentDiceValue(request.Value, refreshHud: false);

                return true;
            });
        }
        public void Dispose() { _apply.Dispose(); _query.Dispose(); }
    }
}
