using OzGameLab01.Save;

/// <summary>
/// 저장 시스템 전반을 관리하는, 게임 부팅 후 계속 살아있는 매니저입니다. 외부 호출은
/// <see cref="SaveFacade"/>가, 실제 상태는 <see cref="SaveState"/>가 각각 전담합니다.
/// 이 클래스는 애플리케이션 일시정지/종료 시점의 자동 저장(Unity 라이프사이클 콜백은
/// MonoBehaviour에서만 받을 수 있음)과 부팅 시 로드만 담당합니다.
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    private SaveFacade _facade;
    public SaveFacade Facade => _facade ??= CreateFacade();

    private SaveFacade CreateFacade()
    {
        SaveFacade facade = new SaveFacade();
        SystemBus.Register(facade);
        return facade;
    }

    protected override void Awake()
    {
        if (Instance != this) { Destroy(gameObject); return; }
        base.Awake();
        Facade.Load();
    }

    private void OnDestroy()
    {
        if (_facade != null) SystemBus.Unregister(_facade);
    }

    // 게임 일시중지 시 저장
    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // [추가] 일시정지 저장 전에 최신 런 상태를 저장 객체에 반영
            Facade.CaptureCurrentRun();
            await Facade.SaveAsync();
        }
    }

    // 게임 꺼질 때 저장
    private async void OnApplicationQuit()
    {
        // [추가] 종료 저장 전에 최신 런 상태를 저장 객체에 반영
        Facade.CaptureCurrentRun();
        await Facade.SaveAsync();
    }
}
