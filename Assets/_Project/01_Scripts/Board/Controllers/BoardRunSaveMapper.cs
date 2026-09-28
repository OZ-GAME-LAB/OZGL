using OzGameLab01.Board.Models;
using OzGameLab01.Data;
using OzGameLab01.Save;
namespace OzGameLab01.Board.Controllers
{
    // 기존 저장 형식과 보드 스냅샷 사이의 변환
    public static class BoardRunSaveMapper
    {
        public static BoardRunSaveData ToSave(BoardRunSnapshot source)
        {
            if (source == null) { return null; }
            BoardRunSaveData result = new BoardRunSaveData
            {
                hasActiveRun = source.hasActiveRun,
                mapSeed = source.mapSeed,
                hasPlayerPosition = source.hasPlayerPosition,
                playerPositionX = source.playerPositionX,
                playerPositionY = source.playerPositionY,
                hasCurrentBattle = source.hasCurrentBattle,
                currentBattlePositionX = source.currentBattlePositionX,
                currentBattlePositionY = source.currentBattlePositionY,
                currentBattleMonsterId = source.currentBattleMonsterId,
                isBossBattle = source.isBossBattle,
                isEliteBattle = source.isEliteBattle,
                isNightEncounter = source.isNightEncounter,
                isBossDefeated = source.isBossDefeated,
                hasRolledThisTurn = source.hasRolledThisTurn,
                rolledDiceValue = source.rolledDiceValue,
                remainingDiceValue = source.remainingDiceValue,
                unusedActionPoints = source.unusedActionPoints,
                turnCount = source.turnCount,
                timeCycleStartTurn = source.timeCycleStartTurn,
                isMidBossActive = source.isMidBossActive,
                defeatedElitesCount = source.defeatedElitesCount,
                hasObjective = source.hasObjective,
                objectivePositionX = source.objectivePositionX,
                objectivePositionY = source.objectivePositionY,

                enemyGrowthValue = source.enemyGrowthValue,
                enemyOverturnValue = source.enemyOverturnValue,
                isInEnemyOverturn = source.isInEnemyOverturn,
                eliteDefeatedThisCycle = source.eliteDefeatedThisCycle,
            };
            if (source.completedBattlePositions != null)
            {
                foreach (BoardRunPosition position in source.completedBattlePositions)
                {
                    if (position != null) { result.completedBattlePositions.Add(new BoardPositionSaveEntry { x = position.x, y = position.y }); }
                }
            }
            if (source.consumedSpecialTilePositions != null)
            {
                foreach (BoardRunPosition position in source.consumedSpecialTilePositions)
                {
                    if (position != null) { result.consumedSpecialTilePositions.Add(new BoardPositionSaveEntry { x = position.x, y = position.y }); }
                }
            }
            if (source.visitedPositions != null)
            {
                foreach (BoardRunPosition position in source.visitedPositions)
                {
                    if (position != null) { result.visitedPositions.Add(new BoardPositionSaveEntry { x = position.x, y = position.y }); }
                }
            }
            if (source.battleUnitHealthEntries != null)
            {
                foreach (BattleUnitHealthSnapshot entry in source.battleUnitHealthEntries)
                {
                    if (entry == null) { continue; }
                    result.battleUnitHealthEntries.Add(new BattleUnitHealthSaveEntry
                    {
                        slotIndex = entry.slotIndex,
                        unitId = entry.unitId,
                        healthRate = entry.healthRate
                    });
                }
            }
            return result;
        }
        public static BoardRunSnapshot FromSave(BoardRunSaveData source)
        {
            if (source == null) { return null; }
            BoardRunSnapshot result = new BoardRunSnapshot
            {
                hasActiveRun = source.hasActiveRun,
                mapSeed = source.mapSeed,
                hasPlayerPosition = source.hasPlayerPosition,
                playerPositionX = source.playerPositionX,
                playerPositionY = source.playerPositionY,
                hasCurrentBattle = source.hasCurrentBattle,
                currentBattlePositionX = source.currentBattlePositionX,
                currentBattlePositionY = source.currentBattlePositionY,
                currentBattleMonsterId = source.currentBattleMonsterId,
                isBossBattle = source.isBossBattle,
                isEliteBattle = source.isEliteBattle,
                isNightEncounter = source.isNightEncounter,
                isBossDefeated = source.isBossDefeated,
                hasRolledThisTurn = source.hasRolledThisTurn,
                rolledDiceValue = source.rolledDiceValue,
                remainingDiceValue = source.remainingDiceValue,
                unusedActionPoints = source.unusedActionPoints,
                turnCount = source.turnCount,
                timeCycleStartTurn = source.timeCycleStartTurn,
                isMidBossActive = source.isMidBossActive,
                defeatedElitesCount = source.defeatedElitesCount,
                hasObjective = source.hasObjective,
                objectivePositionX = source.objectivePositionX,
                objectivePositionY = source.objectivePositionY,
                enemyGrowthValue = source.enemyGrowthValue,
                enemyOverturnValue = source.enemyOverturnValue,
                isInEnemyOverturn = source.isInEnemyOverturn,
                eliteDefeatedThisCycle = source.eliteDefeatedThisCycle,
            };
            if (source.completedBattlePositions != null)
            {
                foreach (BoardPositionSaveEntry position in source.completedBattlePositions)
                {
                    if (position != null) { result.completedBattlePositions.Add(new BoardRunPosition { x = position.x, y = position.y }); }
                }
            }
            if (source.consumedSpecialTilePositions != null)
            {
                foreach (BoardPositionSaveEntry position in source.consumedSpecialTilePositions)
                {
                    if (position != null) { result.consumedSpecialTilePositions.Add(new BoardRunPosition { x = position.x, y = position.y }); }
                }
            }
            if (source.visitedPositions != null)
            {
                foreach (BoardPositionSaveEntry position in source.visitedPositions)
                {
                    if (position != null) { result.visitedPositions.Add(new BoardRunPosition { x = position.x, y = position.y }); }
                }
            }
            if (source.battleUnitHealthEntries != null)
            {
                foreach (BattleUnitHealthSaveEntry entry in source.battleUnitHealthEntries)
                {
                    if (entry == null) { continue; }
                    result.battleUnitHealthEntries.Add(new BattleUnitHealthSnapshot
                    {
                        slotIndex = entry.slotIndex,
                        unitId = entry.unitId,
                        healthRate = entry.healthRate
                    });
                }
            }
            return result;
        }
    }
}
