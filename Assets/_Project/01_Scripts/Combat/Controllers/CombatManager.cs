namespace OzGameLab01.Combat
{
    /// <summary>
    /// 전투 시스템 전반을 관리하는, 게임 부팅 후 계속 살아있는 매니저입니다.
    /// "이번 전투" 하나에 대한 씬 소속 데이터는 <see cref="CombatSession"/>이 따로
    /// 들고 있고, 이 클래스는 항상 존재하는 <see cref="CombatFacade"/>를 노출하는 것과
    /// 슬롯 좌표계 타입 정의(외부에서 여전히 많이 참조함) 외에는 아무 일도 하지 않습니다.
    /// </summary>
    public class CombatManager : Singleton<CombatManager>
    {
        public enum SlotRow { Front, Mid, Back }

        [System.Serializable]
        public struct SlotKey : System.IEquatable<SlotKey>
        {
            public int column;
            public SlotRow row;

            public bool Equals(SlotKey other) => column == other.column && row == other.row;
            public override bool Equals(object obj) => obj is SlotKey other && Equals(other);
            public override int GetHashCode() => (column, row).GetHashCode();
        }

        private CombatFacade _facade;
        public CombatFacade Facade => _facade ??= CreateFacade();

        private CombatFacade CreateFacade()
        {
            CombatFacade facade = new CombatFacade();
            SystemBus.Register(facade);
            return facade;
        }

        private void OnDestroy()
        {
            if (_facade != null) SystemBus.Unregister(_facade);
        }
    }
}
