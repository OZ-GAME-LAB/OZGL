namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// 이동 모델이 사용하는 잔여 눈금 저장소를 정의합니다.
    /// </summary>
    public interface IBoardMovementState
    {
        int RemainingDiceValue { get; }
        void SetRemainingDiceValue(int value);
    }
}
