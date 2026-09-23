namespace OzGameLab01.Board.Models
{
    /// <summary>
    /// EnemyGrowthValue/EnemyOverturnValue 필드가 생기기 전에 저장된 세이브를 불러올 때,
    /// 턴 수·중간보스 처치 수만으로 근사치를 계산해 채워주는 1회성 마이그레이션 도구입니다.
    /// 게임이 실제로 진행되는 동안은 BoardRunState가 매 턴 낮/밤을 그대로 누적하므로
    /// 이 계산은 쓰이지 않습니다(2026-09-23, 웨이브 내부 낮/밤 구분을 보드의 실제 낮/밤
    /// 시계로 대체하면서 이 근사식은 구버전 세이브 전용으로 남았습니다).
    /// </summary>
    public static class EnemyGrowthSaveMigration
    {
        private const int WaveLength = 15;
        private const float ValueBase = 0.7f;
        private const float DayValueIncrement = 0.07f;
        private const float NightValueIncrement = 0.12f;
        private const float OverturnValueIncrement = 0.3f;
        private const int DayResidueCutoff = 10;

        public static float EstimateValue(int turnCount, int defeatedElites)
        {
            int wavesOnTime = System.Math.Min(defeatedElites, turnCount / WaveLength);
            int deadlineTurnCount = (wavesOnTime + 1) * WaveLength;
            int frozenTurnCount = System.Math.Min(turnCount, deadlineTurnCount - 1);
            float value = ComputeGrowthValue(frozenTurnCount);
            if (turnCount < deadlineTurnCount) return value;
            int overturnTurns = turnCount - deadlineTurnCount;
            return value + OverturnValueIncrement * overturnTurns;
        }

        private static float ComputeGrowthValue(int turnCount)
        {
            float value = ValueBase;
            int sheetTurn = turnCount + 1;
            for (int previousSheetTurn = 1; previousSheetTurn < sheetTurn; previousSheetTurn++)
            {
                int residue = previousSheetTurn % WaveLength;
                value += residue <= DayResidueCutoff ? DayValueIncrement : NightValueIncrement;
                if (residue == 0) value += 1f;
            }
            return value;
        }
    }
}
