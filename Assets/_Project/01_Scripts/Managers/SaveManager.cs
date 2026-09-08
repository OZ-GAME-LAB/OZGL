using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Managers;

public class SaveManager : Singleton<SaveManager>
{

    // 런타임에서 참조, 수정할 객체
    public SaveData currentData { get; private set; }

    private bool _isDirty = false;
    private readonly object _ioLock = new object();
    // [추가] Continue 직후 메인보드의 로스터가 준비될 때 인벤토리를 복원하기 위한 대기 상태
    private bool _isInventoryRestorePending;

    // [추가] Continue 버튼 활성화에 사용할 유효한 런 저장 여부
    public bool HasContinueData =>
        currentData != null &&
        currentData.boardRun != null &&
        currentData.boardRun.hasActiveRun &&
        currentData.boardRun.mapSeed > 0;

    // 정상 세이브 경로
    private string savePath => Path.Combine(Application.persistentDataPath, "save.json");
    // 저장 도중 임시 파일 경로
    private string tempPath => Path.Combine(Application.persistentDataPath, "save_tmp.json");
    // 세이브 백업 파일 경로
    private string backUpPath => Path.Combine(Application.persistentDataPath, "save_backUp.json");

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    // 데이터가 수정됐을 때 플래그 켜기
    public void MarkAsDirty()
    {
        _isDirty = true;
    }

    // 데이터 로드
    public void Load()
    {
        lock (_ioLock)
        {
            // 1. 원본 파일 검사
            if (File.Exists(savePath))
            {
                if (TryDeserialize(savePath, out SaveData data))
                {
                    currentData = data;
                    // [추가] 이전 버전 저장 파일의 null 목록을 현재 구조에 맞게 보정
                    NormalizeSaveData(currentData);
                    _isDirty = false;
                    Debug.Log("[SaveManager] 세이브 파일 로드 성공");
                    return;
                }
            }

            // 2. 원본 파손 시 백업 파일 검사
            if (File.Exists(backUpPath))
            {
                Debug.LogWarning("[SaveManager] 원본 파일 로드 실패. 백업 파일로 복구.");
                if (TryDeserialize(backUpPath, out SaveData backupData))
                {
                    currentData = backupData;
                    // [추가] 백업 파일도 현재 저장 구조에 맞게 보정
                    NormalizeSaveData(currentData);
                    _isDirty = true;    // 원본 재작성을 위해
                    return;
                }
            }

            // 3. 신규 플레이
            Debug.Log("[SaveManager] 기존 데이터가 없어 기본 데이터로 시작");
            currentData = SaveData.CreateDefault();
            _isDirty = true;
        }
    }

    /// <summary>
    /// New Game용 런 상태를 만들고 이전 맵, 위치 및 편성 데이터를 초기화합니다.
    /// </summary>
    public void BeginNewRun()
    {
        BoardRunData.BeginNewRun();
        UnitFormationCombatLink.ClearSavedFormation();
        ResetLegacyBoardTransitionState();
        ResetPersistentRunManagers();

        PlayerInventoryManager inventoryManager = FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        if (inventoryManager != null)
        {
            inventoryManager.ClearInventory();
        }

        currentData = SaveData.CreateDefault();
        CaptureCurrentRun();
        _isInventoryRestorePending = false;
    }

    /// <summary>
    /// 저장 파일의 맵, 위치 및 편성 데이터를 Continue 런타임 상태에 적용합니다.
    /// </summary>
    /// <returns></returns>
    public bool RestoreCurrentRun()
    {
        if (!HasContinueData || !BoardRunData.RestoreFromSaveData(currentData.boardRun))
        {
            return false;
        }

        UnitFormationCombatLink.RestoreFormationFromSaveData(currentData.boardRun);

        PlayerInventoryManager inventoryManager = FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        if (inventoryManager != null)
        {
            inventoryManager.RestoreFromSave(currentData.units);
            _isInventoryRestorePending = false;
        }
        else
        {
            _isInventoryRestorePending = true;
        }

        return true;
    }

    /// <summary>
    /// 보스전 완료처럼 런이 끝난 경우 Continue 저장과 런타임 상태를 함께 초기화합니다.
    /// </summary>
    public void ClearCurrentRun()
    {
        BoardRunData.Clear();
        UnitFormationCombatLink.ClearSavedFormation();
        ResetLegacyBoardTransitionState();
        ResetPersistentRunManagers();

        PlayerInventoryManager inventoryManager = FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        if (inventoryManager != null)
        {
            inventoryManager.ClearInventory();
        }

        if (currentData == null)
        {
            currentData = SaveData.CreateDefault();
        }

        currentData.boardRun = null;
        currentData.units.Clear();
        currentData.unitSaveEntries.Clear();
        currentData.relicSaveEntries.Clear();
        _isInventoryRestorePending = false;
        MarkAsDirty();
    }

    /// <summary>
    /// 메인보드 씬의 PlayerInventoryManager가 준비된 뒤 대기 중인 인벤토리를 복원합니다.
    /// </summary>
    public void RestorePendingInventory(PlayerInventoryManager inventoryManager)
    {
        if (!_isInventoryRestorePending || inventoryManager == null || currentData == null)
        {
            return;
        }

        inventoryManager.RestoreFromSave(currentData.units);
        _isInventoryRestorePending = false;
    }

    /// <summary>
    /// 현재 런타임 상태를 저장 객체에 복사하고 저장 필요 상태로 표시합니다.
    /// </summary>
    public void CaptureCurrentRun()
    {
        // 타이틀만 실행한 상태에서 기존 Continue 저장을 빈 런으로 덮어쓰지 않음
        if (!BoardRunData.HasActiveRun)
        {
            return;
        }

        if (currentData == null)
        {
            currentData = SaveData.CreateDefault();
        }

        BoardRunSaveData boardRunSaveData = BoardRunData.CreateSaveData();
        UnitFormationCombatLink.WriteFormationToSaveData(boardRunSaveData);
        currentData.boardRun = boardRunSaveData;

        PlayerInventoryManager inventoryManager = FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
        if (inventoryManager != null)
        {
            CaptureInventory(inventoryManager);
        }

        MarkAsDirty();
    }

    // 비동기 파일 저장
    public async Task<bool> SaveAsync()
    {
        // [추가] Unity API를 사용하는 저장 경로는 메인 스레드에서 미리 확정
        string resolvedSavePath = savePath;
        string resolvedTempPath = tempPath;
        string resolvedBackUpPath = backUpPath;

        //if (!_isDirty && File.Exists(savePath)) return true;
        // [수정] 백그라운드 저장 작업과 동일한 확정 경로 사용
        if (!_isDirty && File.Exists(resolvedSavePath)) return true;

        string json = JsonUtility.ToJson(currentData, true);

        return await Task.Run(() =>
        {
            lock (_ioLock)
            {
                try
                {
                    // 1. 임시 파일에 쓰기
                    //File.WriteAllText(tempPath, json, Encoding.UTF8);
                    // [수정] Task.Run 내부에서는 Unity API를 호출하지 않고 확정된 경로만 사용
                    File.WriteAllText(resolvedTempPath, json, Encoding.UTF8);

                    // 2. 파일 교체 (Temp -> Save, 기존 Save -> Backup)
                    //if (File.Exists(savePath))
                    // [수정] 저장 파일 검사에도 메인 스레드에서 확정한 경로 사용
                    if (File.Exists(resolvedSavePath))
                    {
                        //File.Replace(tempPath, savePath, backUpPath);
                        // [수정] 임시, 원본, 백업 경로를 순수 문자열로 전달
                        File.Replace(resolvedTempPath, resolvedSavePath, resolvedBackUpPath);
                    }
                    else
                    {
                        // 최초 생성 시
                        //File.Move(tempPath, savePath);
                        // [수정] 최초 저장도 확정된 경로만 사용
                        File.Move(resolvedTempPath, resolvedSavePath);
                    }

                    _isDirty = false;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] 파일 저장 실패: {ex.Message}");
                    return false;
                }
            }
        });
    }

    // 저장된 데이터 역직렬화 시도
    private bool TryDeserialize(string path, out SaveData result)
    {
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            result = JsonUtility.FromJson<SaveData>(json);
            return result != null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 파싱 실패 ({path}): {ex.Message}");
            result = null;
            return false;
        }
    }

    /// <summary>
    /// 구버전 또는 일부 필드가 비어 있는 저장 데이터의 컬렉션을 안전하게 초기화합니다.
    /// </summary>
    /// <param name="data"></param>
    private static void NormalizeSaveData(SaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.units ??= new List<UnitStuff>();
        data.unitSaveEntries ??= new List<UnitSaveEntry>();
        data.relicSaveEntries ??= new List<RelicSaveEntry>();

        if (data.boardRun != null)
        {
            data.boardRun.completedBattlePositions ??= new List<BoardPositionSaveEntry>();
            data.boardRun.battleFormationUnitIds ??= new List<int>();
            data.boardRun.supportFormationUnitIds ??= new List<int>();
        }
    }

    /// <summary>
    /// 현재 보유 유닛을 ID와 수량으로 저장합니다.
    /// </summary>
    private void CaptureInventory(PlayerInventoryManager inventoryManager)
    {
        currentData.units.Clear();
        Dictionary<int, int> unitCounts = new Dictionary<int, int>();

        foreach (UnitData unitData in inventoryManager.OwnedUnits)
        {
            if (unitData == null)
            {
                continue;
            }

            unitCounts.TryGetValue(unitData.id, out int amount);
            unitCounts[unitData.id] = amount + 1;
        }

        foreach (KeyValuePair<int, int> unitCount in unitCounts)
        {
            currentData.units.Add(new UnitStuff
            {
                unitId = unitCount.Key,
                amount = unitCount.Value
            });
        }
    }

    /// <summary>
    /// New Game과 런 종료 시 레거시 보드 씬 전환 데이터를 초기화합니다.
    /// </summary>
    private static void ResetLegacyBoardTransitionState()
    {
        SceneTransitioner.MapTileIndex = 0;
        SceneTransitioner.BoardReturnPosition = Vector2Int.zero;
        SceneTransitioner.AllyFormationSlots = null;
    }

    // [추가] 설정 데이터를 제외하고 씬 전환 후에도 유지되는 런 전용 매니저 상태를 초기화합니다.
    private static void ResetPersistentRunManagers()
    {
        Time.timeScale = 1f;

        DiceManager diceManager = UnityEngine.Object.FindFirstObjectByType<DiceManager>(FindObjectsInactive.Include);
        if (diceManager != null)
        {
            diceManager.ResetRunState();
        }

        RelicManager relicManager = UnityEngine.Object.FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (relicManager != null)
        {
            relicManager.ClearRunState();
        }
    }

    // 게임 일시중지 시 저장
    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // [추가] 일시정지 저장 전에 최신 런 상태를 저장 객체에 반영
            CaptureCurrentRun();
            await SaveAsync();
        }
    }

    // 게임 꺼질 때 저장
    private async void OnApplicationQuit()
    {
        // [추가] 종료 저장 전에 최신 런 상태를 저장 객체에 반영
        CaptureCurrentRun();
        await SaveAsync();
    }
}
