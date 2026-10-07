using OzGameLab01.Board.Models;
using OzGameLab01.Data;

namespace OzGameLab01.Board.Controllers
{
    /// <summary>
    /// 기존 저장 및 복원 API를 이동 모델의 저장소에 연결합니다.
    /// </summary>
    public sealed class BoardRunMovementState : IBoardMovementState
    {
        public int RemainingDiceValue => BoardRunData.RemainingDiceValue;

        public void SetRemainingDiceValue(int value)
        {
            BoardRunData.SetRemainingDiceValue(value);
        }
    }
}
