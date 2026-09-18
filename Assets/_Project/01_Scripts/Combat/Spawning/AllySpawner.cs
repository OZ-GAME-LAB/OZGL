using OzGameLab01.Data;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.UI.Battle;
using OzGameLab01.Controllers;
using OzGameLab01.Managers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// CombatManager에서 분리된 아군/적 스폰 책임을 담당합니다.
    /// Inspector 참조는 CombatManager가 그대로 들고 있고, 이 클래스는 그 값을
    /// 생성자로 전달받아 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요).
    /// </summary>
    public class AllySpawner
    {
        private readonly CombatMainView battleMainView;
        private readonly GameObject allyTemplatePrefab;
        private readonly Transform unitsRoot;
        private readonly Vector3 enemyPosition;
        private readonly string enemyPrefabResourceName;
        private readonly float enemyScale;
        private readonly UIProjectilePool uiProjectilePool;
        private readonly MonsterData enemyMonsterData;
        private readonly AllyUnitCombatHUDView allyHudPrefab;

        public AllySpawner(
            CombatMainView battleMainView,
            GameObject allyTemplatePrefab,
            Transform unitsRoot,
            Vector3 enemyPosition,
            string enemyPrefabResourceName,
            float enemyScale,
            UIProjectilePool uiProjectilePool,
            MonsterData enemyMonsterData = null,
            AllyUnitCombatHUDView allyHudPrefab = null)
        {
            this.battleMainView = battleMainView;
            this.allyTemplatePrefab = allyTemplatePrefab;
            this.unitsRoot = unitsRoot;
            this.enemyPosition = enemyPosition;
            this.enemyPrefabResourceName = enemyPrefabResourceName;
            this.enemyScale = enemyScale;
            this.uiProjectilePool = uiProjectilePool;
            this.enemyMonsterData = enemyMonsterData;
            this.allyHudPrefab = allyHudPrefab;
        }

        /// <summary>
        /// 유닛 편성 화면에서 넘어온 배치 데이터(placedAllyFormation)로 아군을 스폰합니다.
        /// slotUnits는 호출자가 미리 할당한 배열을 그대로 채웁니다. placedAllyFormation/
        /// transferredUnits는 각각 SceneTransitioner.AllyFormationData,
        /// UnitFormationCombatLink.BattleUnits를 호출부(CombatManager)가 대신 읽어 전달한 값입니다
        /// (이 클래스가 다른 매니저의 static 상태를 직접 참조하지 않도록 하기 위함).
        /// </summary>
        public Dictionary<CombatManager.SlotKey, int> SpawnAllies(
            Unit[,] slotUnits,
            UnitData[] placedAllyFormation,
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> transferredUnits)
        {
            Dictionary<CombatManager.SlotKey, int> spawnedFormation = new Dictionary<CombatManager.SlotKey, int>();

            if (placedAllyFormation == null)
            {
                Debug.LogError("[AllySpawner] 배치 데이터(placedAllyFormation)가 없어 아군을 스폰할 수 없습니다.");
                return spawnedFormation;
            }

            for (int placementIndex = 0; placementIndex < placedAllyFormation.Length; placementIndex++)
            {
                UnitData data = placedAllyFormation[placementIndex];
                if (data == null)
                {
                    continue;
                }

                CombatManager.SlotKey slot = FormationPlacementResolver.PlacementIndexToSlotKey(placementIndex);
                slotUnits[slot.column, (int)slot.row] = SpawnAllyUnitInPlayerSlot(data, placementIndex, transferredUnits);
                spawnedFormation[slot] = data.id;
            }

            return spawnedFormation;
        }

        /// <summary>
        /// 편성 인덱스(0~8)에 대응하는 PlayerSlotItemView의 UnitAnchor 아래에 실제 Unit 프리팹을 생성합니다.
        /// </summary>
        private Unit SpawnAllyUnitInPlayerSlot(
            UnitData data,
            int placementIndex,
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> transferredUnits)
        {
            if (battleMainView == null)
            {
                Debug.LogError("[AllySpawner] BattleMainView가 연결되지 않아 플레이어 슬롯에 유닛을 생성할 수 없습니다.");
                return null;
            }

            IReadOnlyList<PlayerSlotItemView> slotViews = battleMainView.PlayerSlotViews;
            if (placementIndex < 0 || placementIndex >= slotViews.Count)
            {
                Debug.LogError($"[AllySpawner] 편성 인덱스 {placementIndex}에 대응하는 PlayerSlot이 없습니다.");
                return null;
            }

            PlayerSlotItemView slotView = slotViews[placementIndex];
            if (slotView == null || slotView.UnitAnchor == null)
            {
                Debug.LogError($"[AllySpawner] PlayerSlot {placementIndex}의 UnitAnchor가 연결되지 않았습니다.");
                return null;
            }

            GameObject prefabToSpawn = UnitPrefabProvider.GetAllyPrefab(data, allyTemplatePrefab);
            if (prefabToSpawn == null)
            {
                return null;
            }

            Unit unit = CombatUnitFactory.CreateAlly(
                prefabToSpawn, slotView.UnitAnchor, $"{prefabToSpawn.name}_{placementIndex:00}", data);
            if (unit == null)
            {
                return null;
            }

            Sprite unitSprite = unit.gameObject.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
            if (unitSprite == null)
            {
                unitSprite = GetTransferredUnitSprite(placementIndex, transferredUnits);
            }
            CombatUnitViewBinder.BindCombatPresentation(
                unit,
                slotView.UnitAnchor as RectTransform,
                $"BattleCombatUnit_{placementIndex:00}",
                unitSprite,
                Color.white,
                uiProjectilePool,
                allyHudPrefab);

            unit.gameObject.SetActive(true);
            return unit;
        }

        /// <summary>
        /// 메인보드에서 전달된 유닛 아이콘을 편성 인덱스로 찾습니다.
        /// </summary>
        private static Sprite GetTransferredUnitSprite(
            int placementIndex,
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> units)
        {
            if (units == null || placementIndex < 0 || placementIndex >= units.Count || units[placementIndex] == null)
            {
                return null;
            }

            return units[placementIndex].Sprite;
        }

        public Unit SpawnEnemy()
        {
            GameObject prefab = UnitPrefabProvider.GetEnemyPrefab(enemyPrefabResourceName);
            if (prefab == null)
            {
                return null;
            }

            GameObject enemySlot = new GameObject("EnemySlot");
            enemySlot.transform.SetParent(unitsRoot);
            enemySlot.transform.position = enemyPosition;
            enemySlot.transform.localScale = Vector3.one * enemyScale;

            Unit enemyUnit = CombatUnitFactory.CreateEnemy(prefab, enemySlot.transform, enemyMonsterData);

            // 적도 BattleUI의 EnemyUnitAnchor에 표시하고 아군 투사체의 UI 도착점으로 사용
            if (enemyUnit != null && battleMainView != null && battleMainView.EnemyCombatArea != null)
            {
                Transform enemyAnchor = battleMainView.EnemyCombatArea.Find("EnemyUnitAnchor");
                if (enemyAnchor == null)
                {
                    enemyAnchor = battleMainView.EnemyCombatArea;
                }

                SpriteRenderer enemyRenderer = enemyUnit.gameObject.GetComponentInChildren<SpriteRenderer>(true);
                Sprite enemySprite = enemyRenderer != null ? enemyRenderer.sprite : null;
                Color enemyColor = enemyRenderer != null ? enemyRenderer.color : Color.white;
                CombatUnitViewBinder.BindCombatPresentation(
                    enemyUnit, enemyAnchor as RectTransform, "BattleCombatEnemy", enemySprite, enemyColor, uiProjectilePool);
            }

            return enemyUnit;
        }
    }
}
