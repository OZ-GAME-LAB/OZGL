using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 편성 배치 인덱스(0~8)를 CombatManager 전투 슬롯 좌표로 변환합니다.
    /// UnitPlaceScene의 행 우선 배치 순서와 전투 슬롯 좌표계의 차이를 한 곳에서 관리합니다.
    /// </summary>
    public static class FormationPlacementResolver
    {
        private const int SlotColumns = 3;
        // UnitPlaceScene 배치 그리드(인덱스 0-8, row-major: row=idx/3 위→아래, col=idx%3 왼쪽→오른쪽)를
        // CombatManager 슬롯으로 옮긴다. 오른쪽 열=Front, 가운데=Mid, 왼쪽=Back로 취급하고,
        // 배치 UI의 위쪽 행이 CombatManager의 높은 column 값이 되도록 상하 시각 순서를 그대로 보존한다.
        public static CombatManager.SlotKey PlacementIndexToSlotKey(int placementIndex)
        {
            int placeRow = placementIndex / SlotColumns;
            int placeCol = placementIndex % SlotColumns;

            return new CombatManager.SlotKey
            {
                column = (SlotColumns - 1) - placeRow,
                row = (CombatManager.SlotRow)((SlotColumns - 1) - placeCol)
            };
        }
    }
}
