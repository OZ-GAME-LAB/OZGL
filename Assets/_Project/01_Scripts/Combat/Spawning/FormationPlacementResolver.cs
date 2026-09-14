namespace OzGameLab01.Combat
{
    /// <summary>
    /// AllySpawner에서 분리된 편성 배치 인덱스(0~8) ↔ 전투 슬롯(SlotKey) 좌표 변환 책임을
    /// 담당합니다. UnitPlaceScene의 배치 그리드(row-major)와 CombatManager의 슬롯 좌표계가
    /// 서로 달라서 필요한 순수 변환 로직입니다.
    /// </summary>
    public static class FormationPlacementResolver
    {
        private const int SlotColumns = 3;
        private const int SlotRows = 3;

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

        /// <summary>
        /// Inspector 폴백 SlotKey를 CombatMainView.PlayerSlotViews의 0~8 인덱스로 역변환합니다.
        /// </summary>
        public static int SlotKeyToPlacementIndex(CombatManager.SlotKey slot)
        {
            int placeRow = (SlotColumns - 1) - slot.column;
            int placeCol = (SlotRows - 1) - (int)slot.row;
            return placeRow * SlotColumns + placeCol;
        }
    }
}
