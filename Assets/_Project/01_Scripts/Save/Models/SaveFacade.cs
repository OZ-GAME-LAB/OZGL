using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.Player;

namespace OzGameLab01.Save
{
    /// <summary>
    /// Save 시스템 외부(TitleSceneController/BoardSceneController 등)가 호출하는
    /// 유일한 진입점입니다. 실제 상태는 <see cref="SaveState"/>가 들고 있고, 이
    /// 클래스는 파일 I/O와 런 생명주기 오케스트레이션을 담당합니다.
    /// </summary>
    public class SaveFacade
    {
        private readonly SaveState _state = new SaveState();
        private readonly object _ioLock = new object();

        public SaveData CurrentData => _state.CurrentData;

        // [추가] Continue 버튼 활성화에 사용할 유효한 런 저장 여부
        public bool HasContinueData =>
            _state.CurrentData != null &&
            _state.CurrentData.boardRun != null &&
            _state.CurrentData.boardRun.hasActiveRun &&
            _state.CurrentData.boardRun.mapSeed > 0;

        // 정상 세이브 경로
        private string savePath => Path.Combine(Application.persistentDataPath, "save.json");
        // 저장 도중 임시 파일 경로
        private string tempPath => Path.Combine(Application.persistentDataPath, "save_tmp.json");
        // 세이브 백업 파일 경로
        private string backUpPath => Path.Combine(Application.persistentDataPath, "save_backUp.json");

        // 데이터가 수정됐을 때 플래그 켜기
        public void MarkAsDirty()
        {
            _state.IsDirty = true;
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
                        _state.CurrentData = data;
                        // [추가] 이전 버전 저장 파일의 null 목록을 현재 구조에 맞게 보정
                        NormalizeSaveData(_state.CurrentData);
                        _state.IsDirty = false;
                        Debug.Log("[SaveFacade] 세이브 파일 로드 성공");
                        return;
                    }
                }

                // 2. 원본 파손 시 백업 파일 검사
                if (File.Exists(backUpPath))
                {
                    Debug.LogWarning("[SaveFacade] 원본 파일 로드 실패. 백업 파일로 복구.");
                    if (TryDeserialize(backUpPath, out SaveData backupData))
                    {
                        _state.CurrentData = backupData;
                        // [추가] 백업 파일도 현재 저장 구조에 맞게 보정
                        NormalizeSaveData(_state.CurrentData);
                        _state.IsDirty = true;    // 원본 재작성을 위해
                        return;
                    }
                }

                // 3. 신규 플레이
                Debug.Log("[SaveFacade] 기존 데이터가 없어 기본 데이터로 시작");
                _state.CurrentData = SaveData.CreateDefault();
                _state.IsDirty = true;
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

            PlayerInventoryManager inventoryManager = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
            if (inventoryManager != null)
            {
                inventoryManager.Facade.ClearInventory();
            }

            _state.CurrentData = SaveData.CreateDefault();
            CaptureCurrentRun();
            _state.IsInventoryRestorePending = false;
        }

        /// <summary>
        /// 저장 파일의 맵, 위치 및 편성 데이터를 Continue 런타임 상태에 적용합니다.
        /// </summary>
        public bool RestoreCurrentRun()
        {
            if (!HasContinueData || !BoardRunData.RestoreFromSaveData(_state.CurrentData.boardRun))
            {
                return false;
            }

            UnitFormationCombatLink.RestoreFormationFromSaveData(_state.CurrentData.boardRun);

            PlayerInventoryManager inventoryManager = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
            if (inventoryManager != null)
            {
                inventoryManager.Facade.RestoreFromSave(_state.CurrentData.units);
                _state.IsInventoryRestorePending = false;
            }
            else
            {
                _state.IsInventoryRestorePending = true;
            }

            // [추가] 유닛 인벤토리와 마찬가지로 보유 유물도 Continue 시 복원합니다.
            // RelicManager는 Singleton이라 씬에 없어도 Instance 접근 시 자동 생성됩니다.
            RelicManager.Instance.RestoreFromSave(_state.CurrentData.relicSaveEntries);

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

            PlayerInventoryManager inventoryManager = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
            if (inventoryManager != null)
            {
                inventoryManager.Facade.ClearInventory();
            }

            if (_state.CurrentData == null)
            {
                _state.CurrentData = SaveData.CreateDefault();
            }

            _state.CurrentData.boardRun = null;
            _state.CurrentData.units.Clear();
            _state.CurrentData.unitSaveEntries.Clear();
            _state.CurrentData.relicSaveEntries.Clear();
            _state.IsInventoryRestorePending = false;
            MarkAsDirty();
        }

        /// <summary>
        /// 메인보드 씬의 PlayerInventoryManager가 준비된 뒤 대기 중인 인벤토리를 복원합니다.
        /// </summary>
        public void RestorePendingInventory(PlayerFacade facade)
        {
            if (!_state.IsInventoryRestorePending || facade == null || _state.CurrentData == null)
            {
                return;
            }

            facade.RestoreFromSave(_state.CurrentData.units);
            _state.IsInventoryRestorePending = false;
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

            if (_state.CurrentData == null)
            {
                _state.CurrentData = SaveData.CreateDefault();
            }

            BoardRunSaveData boardRunSaveData = BoardRunData.CreateSaveData();
            UnitFormationCombatLink.WriteFormationToSaveData(boardRunSaveData);
            _state.CurrentData.boardRun = boardRunSaveData;

            PlayerInventoryManager inventoryManager = UnityEngine.Object.FindFirstObjectByType<PlayerInventoryManager>(FindObjectsInactive.Include);
            if (inventoryManager != null)
            {
                CaptureInventory(inventoryManager.Facade);
            }

            CaptureRelics();

            MarkAsDirty();
        }

        // 비동기 파일 저장
        public async Task<bool> SaveAsync()
        {
            // [추가] Unity API를 사용하는 저장 경로는 메인 스레드에서 미리 확정
            string resolvedSavePath = savePath;
            string resolvedTempPath = tempPath;
            string resolvedBackUpPath = backUpPath;

            // [수정] 백그라운드 저장 작업과 동일한 확정 경로 사용
            if (!_state.IsDirty && File.Exists(resolvedSavePath)) return true;

            string json = JsonUtility.ToJson(_state.CurrentData, true);

            return await Task.Run(() =>
            {
                lock (_ioLock)
                {
                    try
                    {
                        // 1. 임시 파일에 쓰기
                        // [수정] Task.Run 내부에서는 Unity API를 호출하지 않고 확정된 경로만 사용
                        File.WriteAllText(resolvedTempPath, json, Encoding.UTF8);

                        // 2. 파일 교체 (Temp -> Save, 기존 Save -> Backup)
                        // [수정] 저장 파일 검사에도 메인 스레드에서 확정한 경로 사용
                        if (File.Exists(resolvedSavePath))
                        {
                            // [수정] 임시, 원본, 백업 경로를 순수 문자열로 전달
                            File.Replace(resolvedTempPath, resolvedSavePath, resolvedBackUpPath);
                        }
                        else
                        {
                            // [수정] 최초 저장도 확정된 경로만 사용
                            File.Move(resolvedTempPath, resolvedSavePath);
                        }

                        _state.IsDirty = false;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveFacade] 파일 저장 실패: {ex.Message}");
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
                Debug.LogError($"[SaveFacade] 파싱 실패 ({path}): {ex.Message}");
                result = null;
                return false;
            }
        }

        /// <summary>
        /// 구버전 또는 일부 필드가 비어 있는 저장 데이터의 컬렉션을 안전하게 초기화합니다.
        /// </summary>
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
                data.boardRun.consumedSpecialTilePositions ??= new List<BoardPositionSaveEntry>();
                data.boardRun.battleFormationUnitIds ??= new List<int>();
                data.boardRun.supportFormationUnitIds ??= new List<int>();
            }
        }

        /// <summary>
        /// 현재 보유 유닛을 ID와 수량으로 저장합니다.
        /// </summary>
        private void CaptureInventory(PlayerFacade facade)
        {
            _state.CurrentData.units.Clear();
            Dictionary<int, int> unitCounts = new Dictionary<int, int>();

            foreach (UnitData unitData in facade.OwnedUnits)
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
                _state.CurrentData.units.Add(new UnitStuff
                {
                    unitId = unitCount.Key,
                    amount = unitCount.Value
                });
            }
        }

        /// <summary>
        /// 현재 보유 유물을 저장합니다. RelicManager는 Singleton이라 씬에 없어도
        /// Instance 접근 시 자동 생성되므로(보유 유물 0개) null 체크가 필요 없습니다.
        /// </summary>
        private void CaptureRelics()
        {
            _state.CurrentData.relicSaveEntries.Clear();

            int index = 0;
            foreach (RelicRuntimeInstance relic in RelicManager.Instance.OwnedRelics)
            {
                if (relic?.Data == null)
                {
                    continue;
                }

                _state.CurrentData.relicSaveEntries.Add(new RelicSaveEntry
                {
                    relicId = relic.Data.id,
                    relicIndex = index
                });
                index++;
            }
        }

        /// <summary>
        /// New Game과 런 종료 시 레거시 보드 씬 전환 데이터를 초기화합니다.
        /// </summary>
        private static void ResetLegacyBoardTransitionState()
        {
            SceneTransitioner.MapTileIndex = 0;
            SceneTransitioner.BoardReturnPosition = Vector2Int.zero;
        }

        // [추가] 설정 데이터를 제외하고 씬 전환 후에도 유지되는 런 전용 매니저 상태를 초기화합니다.
        private static void ResetPersistentRunManagers()
        {
            Time.timeScale = 1f;

            DiceManager diceManager = UnityEngine.Object.FindFirstObjectByType<DiceManager>(FindObjectsInactive.Include);
            if (diceManager != null && diceManager.Facade != null)
            {
                diceManager.Facade.ResetRunState();
            }

            RelicManager relicManager = UnityEngine.Object.FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
            if (relicManager != null)
            {
                relicManager.ClearRunState();
            }
        }
    }
}
