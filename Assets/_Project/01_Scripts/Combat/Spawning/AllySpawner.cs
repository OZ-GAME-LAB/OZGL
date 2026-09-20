using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Controllers;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// Creates combat units at the world slots supplied by CombatMapView.
    /// </summary>
    public class AllySpawner
    {
        private readonly CombatMapView _battleMapView;
        private readonly GameObject _allyTemplatePrefab;
        private readonly string _enemyPrefabResourceName;
        private readonly float _enemyScale;
        private readonly MonsterData _enemyMonsterData;
        private readonly AllyUnitCombatHUDView _allyHudPrefab;

        public AllySpawner(
            CombatMapView battleMapView,
            GameObject allyTemplatePrefab,
            string enemyPrefabResourceName,
            float enemyScale,
            MonsterData enemyMonsterData,
            AllyUnitCombatHUDView allyHudPrefab)
        {
            _battleMapView = battleMapView;
            _allyTemplatePrefab = allyTemplatePrefab;
            _enemyPrefabResourceName = enemyPrefabResourceName;
            _enemyScale = enemyScale;
            _enemyMonsterData = enemyMonsterData;
            _allyHudPrefab = allyHudPrefab;
        }

        public Dictionary<CombatManager.SlotKey, int> SpawnAllies(
            Unit[,] slotUnits,
            UnitData[] placedAllyFormation)
        {
            Dictionary<CombatManager.SlotKey, int> spawnedFormation =
                new Dictionary<CombatManager.SlotKey, int>();

            if (_battleMapView == null || _allyTemplatePrefab == null || placedAllyFormation == null)
            {
                Debug.LogError("[AllySpawner] BattleMap, ally prefab, or formation data is missing.");
                return spawnedFormation;
            }

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

                Unit unit = CombatUnitFactory.CreateAlly(
                    _allyTemplatePrefab,
                    spawnPoint,
                    $"{_allyTemplatePrefab.name}_{placementIndex:00}",
                    data);
                if (unit == null)
                {
                    continue;
                }

                CombatUnitViewBinder.BindCombatPresentation(unit, _allyHudPrefab);
                unit.gameObject.SetActive(true);

                CombatManager.SlotKey slot =
                    FormationPlacementResolver.PlacementIndexToSlotKey(placementIndex);
                slotUnits[slot.column, (int)slot.row] = unit;
                spawnedFormation[slot] = data.id;
            }

            return spawnedFormation;
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
    }
}
