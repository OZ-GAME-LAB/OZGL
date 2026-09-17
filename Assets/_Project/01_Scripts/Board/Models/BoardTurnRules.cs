namespace OzGameLab01.Board.Models
{
    public enum BoardTimeOfDay
    {
        Day,
        Noon,
        Night
    }

    // 누적 턴 기준 시간 전환 및 표시 값
    public static class BoardTurnRules
    {
        public static bool ChangesPhase(int turnCount, int interval) { return interval > 0 && turnCount % interval == 0; }
        public static bool IsNight(int turnCount, int interval) { return interval > 0 && (turnCount / interval) % 2 == 1; }
        public static BoardTimeOfDay GetTimeOfDay(int turnCount, int interval)
        {
            if (interval <= 0)
            {
                return BoardTimeOfDay.Day;
            }

            // 기존 낮/밤 게임 규칙은 유지하고, 낮 구간의 후반부를 점심으로 세분화합니다.
            // 기본 interval 3 기준: 낮 2턴 -> 점심 1턴 -> 밤 3턴.
            if (IsNight(turnCount, interval))
            {
                return BoardTimeOfDay.Night;
            }

            int phaseTurn = turnCount % interval;
            if (phaseTurn < 0)
            {
                phaseTurn += interval;
            }

            int noonStartTurn = (interval + 1) / 2;
            return phaseTurn >= noonStartTurn
                ? BoardTimeOfDay.Noon
                : BoardTimeOfDay.Day;
        }
        public static int TurnsUntilPhase(int turnCount, int interval) { return interval > 0 ? interval - turnCount % interval : 0; }
        public static int DisplayTurn(int turnCount) { return turnCount + 1; }
        public static float ClockAngle(int turnCount) { return DisplayTurn(turnCount) * -30f; }
        public static bool CanOpenRoll(bool hasDice, bool rolled, bool moving, int remainingDice) { return hasDice && !rolled && !moving && remainingDice <= 0; }
    }
}
