namespace OzGameLab01.Dice
{
    /// <summary>
    /// 주사위 턴 상태를 보관하는 순수 데이터 클래스입니다.
    /// </summary>
    public class DiceState
    {
        public bool HasRolledThisTurn { get; private set; }

        public void MarkRolled()
        {
            HasRolledThisTurn = true;
        }

        public void ResetTurn()
        {
            HasRolledThisTurn = false;
        }
    }
}
