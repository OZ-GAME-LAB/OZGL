namespace OzGameLab01.Save
{
    /// <summary>
    /// 저장 시스템의 런타임 상태(현재 로드된 데이터, 저장 필요 여부)를 보관하는
    /// 순수 데이터 클래스입니다.
    /// </summary>
    public class SaveState
    {
        public SaveData CurrentData { get; set; }
        public bool IsDirty { get; set; }

        /// <summary>
        /// Continue 직후 메인보드의 로스터가 준비될 때 인벤토리를 복원하기 위한 대기 상태
        /// </summary>
        public bool IsInventoryRestorePending { get; set; }
    }
}
