using OzGameLab01.Player;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// 게임 중 플레이어가 획득한 유닛 인벤토리를 관리하는, 게임 부팅 후 계속 살아있는
    /// 싱글톤 매니저입니다. 전투 씬으로 전환되어도 이 데이터는 절대 파괴되지 않습니다.
    /// 외부 호출은 <see cref="PlayerFacade"/>가, 실제 상태는 <see cref="PlayerState"/>가
    /// 각각 전담합니다.
    /// </summary>
    public class PlayerInventoryManager : Singleton<PlayerInventoryManager>
    {
        private PlayerFacade _facade;
        public PlayerFacade Facade => _facade ??= CreateFacade();

        private PlayerFacade CreateFacade()
        {
            PlayerFacade facade = new PlayerFacade();
            SystemBus.Register(facade);
            return facade;
        }

        protected override void Awake()
        {
            if (Instance != this) { Destroy(gameObject); return; }
            base.Awake();
            // [추가] Continue에서 보류된 인벤토리를 로스터 참조가 준비된 시점에 복원
            SaveManager.Instance.Facade.RestorePendingInventory(Facade);
        }

        private void OnDestroy()
        {
            if (_facade != null) SystemBus.Unregister(_facade);
        }
    }
}
