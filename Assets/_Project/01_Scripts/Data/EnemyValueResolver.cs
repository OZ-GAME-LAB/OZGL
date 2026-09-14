namespace OzGameLab01.Data
{
    /// <summary>
    /// Docs/Database/EnemyData.xlsx의 "Value" 성장 규칙(턴 진행에 따라 체력/공격력/방어력에
    /// 곱해지는 배율)을 계산합니다. 원본 엑셀 주석에 "★★확정아님★★" 표시가 있는 미확정 규칙을
    /// 그대로 코드화한 것이라, 밸런스가 바뀌면 이 클래스만 고치면 되도록 분리해뒀습니다.
    ///
    /// 현재 Unit.ConfigureEnemy(MonsterData)는 healthPoint만 실제로 읽으므로, 이 리졸버도
    /// 체력 배율 계산만 제공합니다. 공격력/치명타/회피율은 MonsterData에 값은 있어도 전투
    /// 로직이 아직 읽지 않는 죽은 필드라 배율을 만들어봐야 아무 효과가 없습니다
    /// (Docs/COMBAT_REFACTOR_TASKS.md 21번 참고).
    /// </summary>
    public static class EnemyValueResolver
    {
        // EnemyData.xlsx normalEnemy/nightEnemy 시트 Turn1 행의 Value 값.
        private const float BaseValue = 0.7f;

        // 원본 셀 주석: "낮에는 0.07씩 증가, 밤에는 0.12씩 증가. 오버턴(15턴 이후)에는 0.3씩 증가."
        private const float DayRatePerTurn = 0.07f;
        private const float NightRatePerTurn = 0.12f;
        private const float OverturnRatePerTurn = 0.3f;
        private const int OverturnStartTurn = 15;

        /// <summary>
        /// 현재 턴 기준 체력 배율을 계산합니다.
        /// </summary>
        /// <param name="turnCount">BoardRunData.TurnCount — 누적 완료 턴 수.</param>
        /// <param name="nightInterval">BoardSceneController._nightInterval과 반드시 같은 값이어야
        /// 낮/밤 판정이 실제 보드와 일치합니다. 씬을 벗어나면 이 값을 직접 조회할 수 없어 호출부에서
        /// 상수로 전달합니다 — 보드 씬의 인스펙터 값이 바뀌면 호출부도 같이 바꿔야 합니다.</param>
        /// <param name="defeatedElites">BoardRunData.DefeatedElitesCount — 중간보스 처치 수.</param>
        public static float GetHealthMultiplier(int turnCount, int nightInterval, int defeatedElites)
        {
            if (turnCount <= 0)
            {
                return BaseValue;
            }

            int overturnTurns = turnCount > OverturnStartTurn ? turnCount - OverturnStartTurn : 0;
            int preOverturnTurns = turnCount - overturnTurns;

            float preOverturnGrowth = 0f;
            for (int turn = 1; turn <= preOverturnTurns; turn++)
            {
                preOverturnGrowth += IsNightTurn(turn, nightInterval) ? NightRatePerTurn : DayRatePerTurn;
            }

            if (defeatedElites > 0)
            {
                // 원본 주석: "중간보스를 처치하고 다시 낮이 되면 1 증가하고, 오버턴 동안
                // 증가한 스탯은 사라짐." — 오버턴 누적분을 버리고 처치 횟수만큼만 고정 가산.
                return BaseValue + preOverturnGrowth + defeatedElites;
            }

            float overturnGrowth = overturnTurns * OverturnRatePerTurn;
            return BaseValue + preOverturnGrowth + overturnGrowth;
        }

        /// <summary>
        /// BoardSceneController.EndTurn()의 낮/밤 판정(phaseIndex = TurnCount / nightInterval,
        /// isNight = phaseIndex % 2 == 1)과 같은 결과가 나오도록 턴 단위로 재계산한 버전입니다.
        /// </summary>
        private static bool IsNightTurn(int turn, int nightInterval)
        {
            if (nightInterval <= 0)
            {
                return false;
            }

            int phase = (turn - 1) / nightInterval;
            return phase % 2 == 1;
        }
    }
}
