using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OzGameLab01.Save
{
    public enum SaveLoadSource { None, Primary, Backup }

    public readonly struct SaveLoadResult
    {
        public SaveData Data { get; }
        public SaveLoadSource Source { get; }
        public SaveLoadResult(SaveData data, SaveLoadSource source) { Data = data; Source = source; }
    }

    /// <summary>
    /// SaveData를 디스크에 읽고 쓰는 파일 I/O 전담 클래스입니다. 원본/임시/백업 파일
    /// 교체와 역직렬화만 책임지고, 런 생명주기 조율이나 다른 시스템 상태는 알지 못합니다.
    /// </summary>
    public sealed class SaveFileStore
    {
        private readonly object _ioLock = new object();
        private readonly string _savePath;
        private readonly string _tempPath;
        private readonly string _backUpPath;
        // 초기화 이전에 시작한 비동기 저장 작업의 재기록 방지
        private int _writeGeneration;

        public SaveFileStore(string persistentDataPath)
        {
            _savePath = Path.Combine(persistentDataPath, "save.json");
            _tempPath = Path.Combine(persistentDataPath, "save_tmp.json");
            _backUpPath = Path.Combine(persistentDataPath, "save_backUp.json");
        }

        public bool SaveFileExists => File.Exists(_savePath);

        /// <summary>
        /// 원본 파일을 먼저 시도하고, 손상 시 백업 파일로 복구를 시도합니다.
        /// </summary>
        public SaveLoadResult Load()
        {
            lock (_ioLock)
            {
                if (File.Exists(_savePath) && TryDeserialize(_savePath, out SaveData data))
                {
                    return new SaveLoadResult(data, SaveLoadSource.Primary);
                }

                if (File.Exists(_backUpPath) && TryDeserialize(_backUpPath, out SaveData backupData))
                {
                    return new SaveLoadResult(backupData, SaveLoadSource.Backup);
                }

                return new SaveLoadResult(null, SaveLoadSource.None);
            }
        }

        /// <summary>
        /// 임시 파일에 쓴 뒤 원본/백업 파일과 교체하는 방식으로 저장합니다.
        /// </summary>
        public async Task<bool> SaveAsync(SaveData data)
        {
            int writeGeneration;
            lock (_ioLock)
            {
                writeGeneration = _writeGeneration;
            }

            // Unity API를 사용하는 경로는 메인 스레드에서 미리 확정
            string resolvedSavePath = _savePath;
            string resolvedTempPath = _tempPath;
            string resolvedBackUpPath = _backUpPath;
            string json = JsonUtility.ToJson(data, true);

            return await Task.Run(() =>
            {
                lock (_ioLock)
                {
                    if (writeGeneration != _writeGeneration)
                    {
                        return false;
                    }

                    try
                    {
                        File.WriteAllText(resolvedTempPath, json, Encoding.UTF8);

                        if (File.Exists(resolvedSavePath))
                        {
                            File.Replace(resolvedTempPath, resolvedSavePath, resolvedBackUpPath);
                        }
                        else
                        {
                            File.Move(resolvedTempPath, resolvedSavePath);
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveFileStore] 파일 저장 실패: {ex.Message}");
                        return false;
                    }
                }
            });
        }

        /// <summary>
        /// 공장 초기화를 위해 모든 세이브 파일을 삭제합니다.
        /// </summary>
        public bool DeleteAll()
        {
            lock (_ioLock)
            {
                _writeGeneration++;
                try
                {
                    DeleteIfExists(_savePath);
                    DeleteIfExists(_tempPath);
                    DeleteIfExists(_backUpPath);

                    Debug.Log("[SaveFileStore] 모든 세이브 파일 삭제 완료");
                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[SaveFileStore] 세이브 파일 삭제 실패: {exception.Message}");
                    return false;
                }
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path) == false)
            {
                return;
            }

            File.Delete(path);
        }

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
                Debug.LogError($"[SaveFileStore] 파싱 실패 ({path}): {ex.Message}");
                result = null;
                return false;
            }
        }
    }
}
