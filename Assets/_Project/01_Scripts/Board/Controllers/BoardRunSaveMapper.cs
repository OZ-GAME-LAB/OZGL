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
            var result = new BoardRunSaveData
            {
                hasActiveRun = source.hasActiveRun,
                mapSeed = source.mapSeed,
                hasPlayerPosition = source.hasPlayerPosition,
                playerPositionX = source.playerPositionX,
                playerPositionY = source.playerPositionY,
                hasCurrentBattle = source.hasCurrentBattle,
                currentBattlePositionX = source.currentBattlePositionX,
                currentBattlePositionY = source.currentBattlePositionY,
                isBossBattle = source.isBossBattle,
                isEliteBattle = source.isEliteBattle,
                isBossDefeated = source.isBossDefeated,
                hasRolledThisTurn = source.hasRolledThisTurn,
                rolledDiceValue = source.rolledDiceValue,
                remainingDiceValue = source.remainingDiceValue,
                unusedActionPoints = source.unusedActionPoints,
                turnCount = source.turnCount,
                defeatedElitesCount = source.defeatedElitesCount,

            };
            if (source.completedBattlePositions != null)
            {
                foreach (var position in source.completedBattlePositions)
                {
                    if (position != null) { result.completedBattlePositions.Add(new BoardPositionSaveEntry { x = position.x, y = position.y }); }
                }
            }
            if (source.consumedSpecialTilePositions != null)
            {
                foreach (var position in source.consumedSpecialTilePositions)
                {
                    if (position != null) { result.consumedSpecialTilePositions.Add(new BoardPositionSaveEntry { x = position.x, y = position.y }); }
                }
            }
            return result;
        }
        public static BoardRunSnapshot FromSave(BoardRunSaveData source)
        {
            if (source == null) { return null; }
            var result = new BoardRunSnapshot
            {
                hasActiveRun = source.hasActiveRun,
                mapSeed = source.mapSeed,
                hasPlayerPosition = source.hasPlayerPosition,
                playerPositionX = source.playerPositionX,
                playerPositionY = source.playerPositionY,
                hasCurrentBattle = source.hasCurrentBattle,
                currentBattlePositionX = source.currentBattlePositionX,
                currentBattlePositionY = source.currentBattlePositionY,
                isBossBattle = source.isBossBattle,
                isEliteBattle = source.isEliteBattle,
                isBossDefeated = source.isBossDefeated,
                hasRolledThisTurn = source.hasRolledThisTurn,
                rolledDiceValue = source.rolledDiceValue,
                remainingDiceValue = source.remainingDiceValue,
                unusedActionPoints = source.unusedActionPoints,
                turnCount = source.turnCount,
                defeatedElitesCount = source.defeatedElitesCount,
            };
            if (source.completedBattlePositions != null)
            {
                foreach (var position in source.completedBattlePositions)
                {
                    if (position != null) { result.completedBattlePositions.Add(new BoardRunPosition { x = position.x, y = position.y }); }
                }
            }
            if (source.consumedSpecialTilePositions != null)
            {
                foreach (var position in source.consumedSpecialTilePositions)
                {
                    if (position != null) { result.consumedSpecialTilePositions.Add(new BoardRunPosition { x = position.x, y = position.y }); }
                }
            }
            return result;
        }
    }
}
