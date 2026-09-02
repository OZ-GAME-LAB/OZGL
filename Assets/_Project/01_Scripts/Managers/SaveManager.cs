using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class SaveManager : Singleton<SaveManager>
{

    // 런타임에서 참조, 수정할 객체
    public SaveData currentData { get; private set; }

    private bool _isDirty = false;
    private readonly object _ioLock = new object();

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
                    _isDirty = false;
                    Debug.Log("<color=green>[SaveManager] 세이브 파일 로드 성공</color>");
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

    // 비동기 파일 저장
    public async Task<bool> SaveAsync()
    {
        if (!_isDirty && File.Exists(savePath)) return true;

        string json = JsonUtility.ToJson(currentData, true);

        return await Task.Run(() =>
        {
            lock (_ioLock)
            {
                try
                {
                    // 1. 임시 파일에 쓰기
                    File.WriteAllText(tempPath, json, Encoding.UTF8);

                    // 2. 파일 교체 (Temp -> Save, 기존 Save -> Backup)
                    if (File.Exists(savePath))
                    {
                        File.Replace(tempPath, savePath, backUpPath);
                    }
                    else
                    {
                        // 최초 생성 시
                        File.Move(tempPath, savePath);
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

    // 게임 일시중지 시 저장
    private async void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            await SaveAsync();
        }
    }

    // 게임 꺼질 때 저장
    private async void OnApplicationQuit()
    {
        await SaveAsync();
    }
}
