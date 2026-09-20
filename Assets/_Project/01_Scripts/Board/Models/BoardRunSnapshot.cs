using System.Collections.Generic;
namespace OzGameLab01.Board.Models
{
    // 보드 상태 전달용 스냅샷
    public sealed class BoardRunSnapshot
    {
        public bool hasActiveRun;
        public int mapSeed;
        public bool hasPlayerPosition;
        public int playerPositionX;
        public int playerPositionY;
        public bool hasCurrentBattle;
        public int currentBattlePositionX;
        public int currentBattlePositionY;
        public bool isBossBattle;
        public bool isEliteBattle;
        public bool isBossDefeated;
        public bool hasRolledThisTurn;
        public int rolledDiceValue;
        public int remainingDiceValue;
        public int unusedActionPoints;
        public int turnCount;
        public int defeatedElitesCount;
        public List<BoardRunPosition> completedBattlePositions = new List<BoardRunPosition>();
        public List<BoardRunPosition> consumedSpecialTilePositions = new List<BoardRunPosition>();
    }
    // 보드 좌표 전달 값
    public sealed class BoardRunPosition
    {
        public int x;
        public int y;
    }
}
