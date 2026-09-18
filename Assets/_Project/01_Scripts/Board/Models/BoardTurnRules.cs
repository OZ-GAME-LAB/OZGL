namespace OzGameLab01.Board.Models
{
    public enum BoardTimeOfDay
    {
        Day,    // 아침
        Noon,   // 점심
        Night   // 저녁
    }

    public static class BoardTurnRules
    {
        public static BoardTimeOfDay GetTimeOfDay(
            int turnCount,
            int morningTurns,
            int lunchTurns,
            int eveningTurns)
        {
            morningTurns = NormalizeDuration(morningTurns);
            lunchTurns = NormalizeDuration(lunchTurns);
            eveningTurns = NormalizeDuration(eveningTurns);

            int cycleLength = morningTurns + lunchTurns + eveningTurns;
            if (cycleLength <= 0)
            {
                return BoardTimeOfDay.Day;
            }

            // TurnCount 0이 사용자에게 표시되는 첫 번째 턴입니다.
            int cycleTurn = turnCount % cycleLength;
            if (cycleTurn < 0)
            {
                cycleTurn += cycleLength;
            }

            if (cycleTurn < morningTurns)
            {
                return BoardTimeOfDay.Day;
            }

            if (cycleTurn < morningTurns + lunchTurns)
            {
                return BoardTimeOfDay.Noon;
            }

            return BoardTimeOfDay.Night;
        }

        public static bool ChangesPhase(
            int turnCount,
            int morningTurns,
            int lunchTurns,
            int eveningTurns)
        {
            if (turnCount <= 0)
            {
                return false;
            }

            BoardTimeOfDay previous = GetTimeOfDay(
                turnCount - 1,
                morningTurns,
                lunchTurns,
                eveningTurns);

            BoardTimeOfDay current = GetTimeOfDay(
                turnCount,
                morningTurns,
                lunchTurns,
                eveningTurns);

            return previous != current;
        }

        public static bool IsNight(
            int turnCount,
            int morningTurns,
            int lunchTurns,
            int eveningTurns)
        {
            return GetTimeOfDay(
                turnCount,
                morningTurns,
                lunchTurns,
                eveningTurns) == BoardTimeOfDay.Night;
        }

        public static int TurnsUntilPhase(
            int turnCount,
            int morningTurns,
            int lunchTurns,
            int eveningTurns)
        {
            morningTurns = NormalizeDuration(morningTurns);
            lunchTurns = NormalizeDuration(lunchTurns);
            eveningTurns = NormalizeDuration(eveningTurns);

            int cycleLength = morningTurns + lunchTurns + eveningTurns;
            if (cycleLength <= 0)
            {
                return 0;
            }

            int cycleTurn = turnCount % cycleLength;
            if (cycleTurn < 0)
            {
                cycleTurn += cycleLength;
            }

            if (cycleTurn < morningTurns)
            {
                return morningTurns - cycleTurn;
            }

            int lunchEnd = morningTurns + lunchTurns;
            if (cycleTurn < lunchEnd)
            {
                return lunchEnd - cycleTurn;
            }

            return cycleLength - cycleTurn;
        }

        public static int DisplayTurn(int turnCount)
        {
            return turnCount + 1;
        }

        public static float ClockAngle(int turnCount)
        {
            return DisplayTurn(turnCount) * -30f;
        }

        public static bool CanOpenRoll(
            bool hasDice,
            bool rolled,
            bool moving,
            int remainingDice)
        {
            return hasDice && !rolled && !moving && remainingDice <= 0;
        }

        private static int NormalizeDuration(int duration)
        {
            return duration > 0 ? duration : 0;
        }
    }
}