namespace OzGameLab01.Board.Models
{
    // 누적 턴 기준 시간 전환 및 표시 값
    public static class BoardTurnRules
    {
        public static bool ChangesPhase(int turnCount, int interval) { return interval > 0 && turnCount % interval == 0; }
        public static bool IsNight(int turnCount, int interval) { return interval > 0 && (turnCount / interval) % 2 == 1; }
        public static int TurnsUntilPhase(int turnCount, int interval) { return interval > 0 ? interval - turnCount % interval : 0; }
        public static int DisplayTurn(int turnCount) { return turnCount + 1; }
        public static float ClockAngle(int turnCount) { return DisplayTurn(turnCount) * -30f; }
        public static bool CanOpenRoll(bool hasDice, bool rolled, bool moving, int remainingDice) { return hasDice && !rolled && !moving && remainingDice <= 0; }
    }
}
