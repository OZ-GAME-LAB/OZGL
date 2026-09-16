using System;
using OzGameLab01.Board.Contracts;
using OzGameLab01.Common.Messaging;
using OzGameLab01.Controllers;

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
                return player == null ? default : new BoardDiceSnapshot(true, player.IsMoving, player.CurrentDiceValue);
            });
            _apply = bus.Handle<BoardDiceValueRequested, bool>(request =>
            {
                BoardPlayerController player = BoardPlayerController.Instance;
                if (player == null || player.IsMoving || player.CurrentDiceValue > 0) return false;
                player.CurrentDiceValue = request.Value;
                return true;
            });
        }
        public void Dispose() { _apply.Dispose(); _query.Dispose(); }
    }
}
