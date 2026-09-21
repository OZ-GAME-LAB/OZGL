using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Creates combat units at the world slots supplied by CombatMapView.
    /// </summary>
    public class AllySpawner : IDisposable
    {
        private readonly CombatMapView _battleMapView;
        private readonly string _enemyPrefabResourceName;
        private readonly float _enemyScale;
        private readonly MonsterData _enemyMonsterData;
        private readonly AllyUnitCombatHUDView _allyHudPrefab;
        private readonly List<AsyncOperationHandle<GameObject>> _allyPrefabHandles =
            new List<AsyncOperationHandle<GameObject>>();
        private bool _isDisposed;

        public AllySpawner(
            CombatMapView battleMapView,
            string enemyPrefabResourceName,
            float enemyScale,
            MonsterData enemyMonsterData,
            AllyUnitCombatHUDView allyHudPrefab)
        {
            _battleMapView = battleMapView;
            _enemyPrefabResourceName = enemyPrefabResourceName;
            _enemyScale = enemyScale;
            _enemyMonsterData = enemyMonsterData;
            _allyHudPrefab = allyHudPrefab;
        }

        /// <summary>
        /// 편성 인덱스별 프리팹을 병렬로 로드한 뒤 원래 인덱스의 전투 슬롯에 생성합니다.
        /// </summary>
        public async Task<Dictionary<CombatManager.SlotKey, int>> SpawnAlliesAsync(
            Unit[,] slotUnits,
            UnitData[] placedAllyFormation)
        {
            Dictionary<CombatManager.SlotKey, int> spawnedFormation =
                new Dictionary<CombatManager.SlotKey, int>();

            if (_battleMapView == null || placedAllyFormation == null)
            {
                Debug.LogError("[AllySpawner] BattleMap 또는 편성 데이터가 없습니다.");
                return spawnedFormation;
            }

            List<Task<AllySpawnRequest>> loadTasks = new List<Task<AllySpawnRequest>>();
            for (int placementIndex = 0; placementIndex < placedAllyFormation.Length; placementIndex++)
            {
                UnitData data = placedAllyFormation[placementIndex];
                if (data == null)
                {
                    continue;
                }

                if (!_battleMapView.TryGetAllySpawnPoint(placementIndex, out Transform spawnPoint))
                {
                    Debug.LogError($"[AllySpawner] Missing world slot for formation index {placementIndex}.");
                    continue;
                }

                loadTasks.Add(LoadAllyAsync(placementIndex, data, spawnPoint));
            }

            AllySpawnRequest[] requests = await Task.WhenAll(loadTasks);
            if (_isDisposed)
            {
                ReleaseLoadedHandles(requests);
                return spawnedFormation;
            }

            Array.Sort(requests, (left, right) => left.PlacementIndex.CompareTo(right.PlacementIndex));
            foreach (AllySpawnRequest request in requests)
            {
                if (!request.IsLoaded)
                {
                    continue;
                }

                _allyPrefabHandles.Add(request.Handle);
                GameObject instance = UnityEngine.Object.Instantiate(
                    request.Prefab, request.SpawnPoint, false);
                Unit unit = CombatUnitFactory.ConfigureAlly(
                    instance,
                    $"{request.Prefab.name}_{request.PlacementIndex:00}",
                    request.Data);
                if (unit == null)
                {
                    continue;
                }

                CombatUnitViewBinder.BindCombatPresentation(unit, _allyHudPrefab);
                unit.gameObject.SetActive(true);

                CombatManager.SlotKey slot =
                    FormationPlacementResolver.PlacementIndexToSlotKey(request.PlacementIndex);
                slotUnits[slot.column, (int)slot.row] = unit;
                spawnedFormation[slot] = request.Data.id;
            }

            return spawnedFormation;
        }

        private static async Task<AllySpawnRequest> LoadAllyAsync(
            int placementIndex,
            UnitData data,
            Transform spawnPoint)
        {
            if (string.IsNullOrWhiteSpace(data.prefabAddress))
            {
                Debug.LogError($"[AllySpawner] 아군 프리팹 주소가 없습니다. 유닛 ID: {data.id}");
                return new AllySpawnRequest(placementIndex, data, spawnPoint);
            }

            AsyncOperationHandle<GameObject> handle =
                Addressables.LoadAssetAsync<GameObject>(data.prefabAddress);
            try
            {
                GameObject prefab = await handle.Task;
                if (handle.Status != AsyncOperationStatus.Succeeded || prefab == null)
                {
                    Debug.LogError(
                        $"[AllySpawner] 아군 프리팹 로드 실패: {data.prefabAddress} (유닛 ID: {data.id})");
                    if (handle.IsValid()) Addressables.Release(handle);
                    return new AllySpawnRequest(placementIndex, data, spawnPoint);
                }

                return new AllySpawnRequest(placementIndex, data, spawnPoint, prefab, handle);
            }
            catch (Exception error)
            {
                Debug.LogError(
                    $"[AllySpawner] 아군 프리팹 로드 예외: {data.prefabAddress} ({error.Message})");
                if (handle.IsValid()) Addressables.Release(handle);
                return new AllySpawnRequest(placementIndex, data, spawnPoint);
            }
        }

        private static void ReleaseLoadedHandles(IEnumerable<AllySpawnRequest> requests)
        {
            foreach (AllySpawnRequest request in requests)
            {
                if (request.IsLoaded && request.Handle.IsValid())
                {
                    Addressables.Release(request.Handle);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (AsyncOperationHandle<GameObject> handle in _allyPrefabHandles)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            _allyPrefabHandles.Clear();
        }

        public Unit SpawnEnemy()
        {
            if (_battleMapView == null || !_battleMapView.TryGetEnemySpawnPoint(out Transform spawnPoint))
            {
                Debug.LogError("[AllySpawner] BattleMap EnemySlot is missing.");
                return null;
            }

            GameObject prefab = UnitPrefabProvider.GetEnemyPrefab(_enemyPrefabResourceName);
            if (prefab == null)
            {
                Debug.LogError("[AllySpawner] Enemy prefab is missing.");
                return null;
            }

            Unit enemyUnit = CombatUnitFactory.CreateEnemy(prefab, spawnPoint, _enemyMonsterData);
            if (enemyUnit != null)
            {
                enemyUnit.transform.localScale = prefab.transform.localScale * _enemyScale;
                CombatUnitViewBinder.BindCombatPresentation(enemyUnit);
                enemyUnit.gameObject.SetActive(true);
            }

            return enemyUnit;
        }

        private readonly struct AllySpawnRequest
        {
            public AllySpawnRequest(
                int placementIndex,
                UnitData data,
                Transform spawnPoint,
                GameObject prefab = null,
                AsyncOperationHandle<GameObject> handle = default)
            {
                PlacementIndex = placementIndex;
                Data = data;
                SpawnPoint = spawnPoint;
                Prefab = prefab;
                Handle = handle;
            }

            public int PlacementIndex { get; }
            public UnitData Data { get; }
            public Transform SpawnPoint { get; }
            public GameObject Prefab { get; }
            public AsyncOperationHandle<GameObject> Handle { get; }
            public bool IsLoaded => Prefab != null;
        }
    }
}
