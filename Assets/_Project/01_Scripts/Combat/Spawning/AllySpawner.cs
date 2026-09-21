using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.UI.Battle;
using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 편성 인덱스에 대응하는 BattleMap 월드 슬롯에 전투 유닛을 생성합니다.
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

        /// <summary>
        /// 빈 슬롯을 유지하고 편성 인덱스와 동일한 Slot_00~Slot_08에 아군을 생성합니다.
        /// </summary>
        public Dictionary<CombatManager.SlotKey, int> SpawnAllies(
            Unit[,] slotUnits,
            UnitData[] placedAllyFormation)
        {
            Dictionary<CombatManager.SlotKey, int> spawnedFormation = new Dictionary<CombatManager.SlotKey, int>();
            if (_battleMapView == null || _allyTemplatePrefab == null || placedAllyFormation == null)
            {
                Debug.LogError("[AllySpawner] BattleMap, 아군 프리팹 또는 편성 데이터가 없습니다.");
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
                    Debug.LogError($"[AllySpawner] 편성 인덱스 {placementIndex}의 월드 슬롯이 없습니다.");
                    continue;
                }

                // 편성 데이터와 월드 슬롯 번호의 일대일 대응 관계
                Unit unit = CombatUnitFactory.CreateAlly(
                    _allyTemplatePrefab, spawnPoint, $"{_allyTemplatePrefab.name}_{placementIndex:00}", data);
                if (unit == null)
                {
                    continue;
                }

                CombatUnitViewBinder.BindCombatPresentation(unit, _allyHudPrefab);
                unit.gameObject.SetActive(true);
                CombatManager.SlotKey slot = FormationPlacementResolver.PlacementIndexToSlotKey(placementIndex);
                slotUnits[slot.column, (int)slot.row] = unit;
                spawnedFormation[slot] = data.id;
            }

            return spawnedFormation;
        }

        /// <summary>
        /// 일반전과 보스전의 전투 스펙을 EnemySlot 자식 유닛에 적용합니다.
        /// </summary>
        public Unit SpawnEnemy()
        {
            if (_battleMapView == null || !_battleMapView.TryGetEnemySpawnPoint(out Transform spawnPoint))
            {
                Debug.LogError("[AllySpawner] BattleMap의 EnemySlot이 없습니다.");
                return null;
            }

            GameObject prefab = UnitPrefabProvider.GetEnemyPrefab(_enemyPrefabResourceName);
            if (prefab == null)
            {
                Debug.LogError("[AllySpawner] 적 프리팹이 없습니다.");
                return null;
            }

            Unit enemyUnit = CombatUnitFactory.CreateEnemy(prefab, spawnPoint, _enemyMonsterData);
            if (enemyUnit != null)
            {
                // 슬롯 좌표 보존 및 유닛 전용 배율 적용
                enemyUnit.transform.localScale = prefab.transform.localScale * _enemyScale;
                CombatUnitViewBinder.BindCombatPresentation(enemyUnit);
                enemyUnit.gameObject.SetActive(true);
            }

            return enemyUnit;
        }
    }
}
