using OzGameLab01.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.UI.Battle;
using OzGameLab01.Controllers;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// CombatManager에서 분리된 아군/적 스폰 책임을 담당합니다.
    /// Inspector 참조는 CombatManager가 그대로 들고 있고, 이 클래스는 그 값을
    /// 생성자로 전달받아 사용하는 순수 C# 클래스입니다(씬/프리팹 재배선 불필요).
    /// </summary>
    public class AllySpawner
    {
        private const int SlotColumns = 3;
        private const int SlotRows = 3;

        public readonly struct SpawnResult
        {
            public readonly bool UsedPlacementData;
            public readonly Dictionary<CombatManager.SlotKey, int> SpawnedFormation;

            public SpawnResult(bool usedPlacementData, Dictionary<CombatManager.SlotKey, int> spawnedFormation)
            {
                UsedPlacementData = usedPlacementData;
                SpawnedFormation = spawnedFormation;
            }
        }

        private readonly BattleMainView battleMainView;
        private readonly GameObject allyTemplatePrefab;
        private readonly Transform unitsRoot;
        private readonly Vector3 gridOrigin;
        private readonly float columnSpacing;
        private readonly float rowSpacing;
        private readonly Vector3 enemyPosition;
        private readonly string enemyPrefabResourceName;
        private readonly float enemyScale;
        private readonly UIProjectilePool uiProjectilePool;
        private readonly MonsterData enemyMonsterData;

        public AllySpawner(
            BattleMainView battleMainView,
            GameObject allyTemplatePrefab,
            Transform unitsRoot,
            Vector3 gridOrigin,
            float columnSpacing,
            float rowSpacing,
            Vector3 enemyPosition,
            string enemyPrefabResourceName,
            float enemyScale,
            UIProjectilePool uiProjectilePool,
            MonsterData enemyMonsterData = null)
        {
            this.battleMainView = battleMainView;
            this.allyTemplatePrefab = allyTemplatePrefab;
            this.unitsRoot = unitsRoot;
            this.gridOrigin = gridOrigin;
            this.columnSpacing = columnSpacing;
            this.rowSpacing = rowSpacing;
            this.enemyPosition = enemyPosition;
            this.enemyPrefabResourceName = enemyPrefabResourceName;
            this.enemyScale = enemyScale;
            this.uiProjectilePool = uiProjectilePool;
            this.enemyMonsterData = enemyMonsterData;
        }

        /// <summary>
        /// 아군을 스폰합니다. 유닛 편성 화면에서 넘어온 배치 데이터(placedAllyFormation)가
        /// 있으면 그것을 사용하고, 없으면 인스펙터에 지정된 allyFormation으로 대체합니다.
        /// slotUnits는 호출자가 미리 할당한 배열을 그대로 채웁니다.
        /// placedAllyFormation/transferredUnits는 각각 SceneTransitioner.AllyFormationData,
        /// UnitFormationCombatLink.BattleUnits를 호출부(CombatManager)가 대신 읽어 전달한 값입니다
        /// (이 클래스가 다른 매니저의 static 상태를 직접 참조하지 않도록 하기 위함).
        /// </summary>
        public SpawnResult SpawnAllies(
            Unit[,] slotUnits,
            Dictionary<CombatManager.SlotKey, int> allyFormationFallback,
            Dictionary<int, UnitData> unitDataById,
            UnitData[] placedAllyFormation,
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> transferredUnits)
        {
            bool hasPlacementData = false;
            if (placedAllyFormation != null)
            {
                foreach (UnitData placedUnit in placedAllyFormation)
                {
                    if (placedUnit != null)
                    {
                        hasPlacementData = true;
                        break;
                    }
                }
            }

            if (hasPlacementData)
            {
                Dictionary<CombatManager.SlotKey, int> spawnedFromPlacement =
                    SpawnAlliesFromPlacement(slotUnits, placedAllyFormation, transferredUnits);
                return new SpawnResult(true, spawnedFromPlacement);
            }

            Dictionary<CombatManager.SlotKey, int> spawnedFormation = new Dictionary<CombatManager.SlotKey, int>();
            foreach (KeyValuePair<CombatManager.SlotKey, int> kvp in allyFormationFallback)
            {
                if (!unitDataById.TryGetValue(kvp.Value, out UnitData data) || data == null)
                {
                    continue;
                }

                CombatManager.SlotKey slot = kvp.Key;
                int placementIndex = SlotKeyToPlacementIndex(slot);
                slotUnits[slot.column, (int)slot.row] = SpawnAllyUnitInPlayerSlot(data, placementIndex, transferredUnits);
                spawnedFormation[slot] = kvp.Value;
            }

            return new SpawnResult(false, spawnedFormation);
        }

        private Dictionary<CombatManager.SlotKey, int> SpawnAlliesFromPlacement(
            Unit[,] slotUnits,
            UnitData[] placedUnits,
            IReadOnlyList<UnitFormationCombatLink.TransferredUnit> transferredUnits)
        {
            Dictionary<CombatManager.SlotKey, int> spawnedFormation = new Dictionary<CombatManager.SlotKey, int>();

            for (int placementIndex = 0; placementIndex < placedUnits.Length; placementIndex++)
            {
                UnitData data = placedUnits[placementIndex];
                if (data == null)
                {
                    continue;
                }

                CombatManager.SlotKey slot = PlacementIndexToSlotKey(placementIndex);
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

            GameObject prefabToSpawn = LoadAllyPrefab(data);
            if (prefabToSpawn == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefabToSpawn, slotView.UnitAnchor, false);
            instance.name = $"{prefabToSpawn.name}_{placementIndex:00}";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Unit unit = instance.GetComponent<Unit>();
            if (unit == null)
            {
                Debug.LogError($"[AllySpawner] '{prefabToSpawn.name}' 루트에 Unit 컴포넌트가 없습니다.", instance);
                Object.Destroy(instance);
                return null;
            }

            unit.Configure(data);
            Sprite unitSprite = instance.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
            if (unitSprite == null)
            {
                unitSprite = GetTransferredUnitSprite(placementIndex, transferredUnits);
            }
            Image combatImage = CreateCombatImage(slotView.UnitAnchor, $"BattleCombatUnit_{placementIndex:00}", unitSprite, Color.white);
            unit.BindCombatUI(slotView.UnitAnchor as RectTransform, combatImage, uiProjectilePool);
            unit.SetVisualsVisible(false);
            instance.SetActive(true);
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

        /// <summary>
        /// 전투 UnitAnchor 전체를 채우는 유닛 이미지를 생성합니다.
        /// </summary>
        private static Image CreateCombatImage(Transform anchor, string objectName, Sprite sprite, Color color)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(anchor, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        /// <summary>
        /// UnitData의 주소에 맞는 아군 전투 프리팹을 불러옵니다.
        /// </summary>
        private GameObject LoadAllyPrefab(UnitData data)
        {
            GameObject prefabToSpawn = null;
            if (data != null && !string.IsNullOrEmpty(data.spriteAddress))
            {
                prefabToSpawn = Resources.Load<GameObject>($"Characters/{data.spriteAddress}");
            }

            if (prefabToSpawn == null)
            {
                prefabToSpawn = allyTemplatePrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[AllySpawner] 아군 프리팹을 찾을 수 없습니다. " +
                    $"(spriteAddress: {data?.spriteAddress})");
            }

            return prefabToSpawn;
        }

        /// <summary>
        /// 공용 아군 프리팹을 비활성 상태로 Instantiate하여 UnitData로 Configure()한 뒤 활성화합니다.
        /// (Awake()가 Configure()에서 설정한 값을 읽어 초기화하므로 순서가 중요합니다.)
        /// 현재 스폰 경로(SpawnAllies)는 SpawnAllyUnitInPlayerSlot을 사용하므로 이 메서드는 호출되지 않습니다.
        /// CombatManager에서 그대로 옮겨온 기존 미사용 코드입니다.
        /// </summary>
        private Unit SpawnAllyUnit(UnitData data, Vector3 position)
        {
            GameObject prefabToSpawn = null;

            // 1. UnitData의 spriteAddress를 기반으로 Resources/Characters/ 폴더에서 프리팹을 찾습니다.
            if (!string.IsNullOrEmpty(data.spriteAddress))
            {
                prefabToSpawn = Resources.Load<GameObject>($"Characters/{data.spriteAddress}");
            }

            // 2. 만약 해당 이름의 프리팹이 없다면 기존의 공용 템플릿 프리팹을 사용합니다.
            if (prefabToSpawn == null)
            {
                prefabToSpawn = allyTemplatePrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[AllySpawner] 아군 프리팹을 찾을 수 없습니다! (spriteAddress: {data.spriteAddress})");
                return null;
            }

            GameObject instance = Object.Instantiate(prefabToSpawn, position, Quaternion.identity, unitsRoot);
            Unit unit = instance.GetComponent<Unit>();
            unit.Configure(data);
            instance.SetActive(true);

            return unit;
        }

        // UnitPlaceScene 배치 그리드(인덱스 0-8, row-major: row=idx/3 위→아래, col=idx%3 왼쪽→오른쪽)를
        // CombatManager 슬롯으로 옮긴다. 오른쪽 열=Front, 가운데=Mid, 왼쪽=Back로 취급하고,
        // 배치 UI의 위쪽 행이 CombatManager의 높은 column 값이 되도록 상하 시각 순서를 그대로 보존한다.
        private static CombatManager.SlotKey PlacementIndexToSlotKey(int placementIndex)
        {
            int placeRow = placementIndex / SlotColumns;
            int placeCol = placementIndex % SlotColumns;

            return new CombatManager.SlotKey
            {
                column = (SlotColumns - 1) - placeRow,
                row = (CombatManager.SlotRow)((SlotColumns - 1) - placeCol)
            };
        }

        /// <summary>
        /// Inspector 폴백 SlotKey를 BattleMainView.PlayerSlotViews의 0~8 인덱스로 역변환합니다.
        /// </summary>
        private static int SlotKeyToPlacementIndex(CombatManager.SlotKey slot)
        {
            int placeRow = (SlotColumns - 1) - slot.column;
            int placeCol = (SlotRows - 1) - (int)slot.row;
            return placeRow * SlotColumns + placeCol;
        }

        public Unit SpawnEnemy()
        {
            GameObject prefab = Resources.Load<GameObject>(enemyPrefabResourceName);
            if (prefab == null)
            {
                return null;
            }

            GameObject enemySlot = new GameObject("EnemySlot");
            enemySlot.transform.SetParent(unitsRoot);
            enemySlot.transform.position = enemyPosition;
            enemySlot.transform.localScale = Vector3.one * enemyScale;

            GameObject enemyInstance = Object.Instantiate(prefab, enemySlot.transform);
            enemyInstance.transform.localPosition = Vector3.zero;
            enemyInstance.transform.localRotation = Quaternion.identity;
            Unit enemyUnit = enemyInstance.GetComponent<Unit>();

            if (enemyUnit != null && enemyMonsterData != null)
            {
                enemyUnit.ConfigureEnemy(enemyMonsterData);
            }

            // 적도 BattleUI의 EnemyUnitAnchor에 표시하고 아군 투사체의 UI 도착점으로 사용
            if (enemyUnit != null && battleMainView != null && battleMainView.EnemyCombatArea != null)
            {
                Transform enemyAnchor = battleMainView.EnemyCombatArea.Find("EnemyUnitAnchor");
                if (enemyAnchor == null)
                {
                    enemyAnchor = battleMainView.EnemyCombatArea;
                }

                SpriteRenderer enemyRenderer = enemyInstance.GetComponentInChildren<SpriteRenderer>(true);
                Sprite enemySprite = enemyRenderer != null ? enemyRenderer.sprite : null;
                Color enemyColor = enemyRenderer != null ? enemyRenderer.color : Color.white;
                Image enemyImage = CreateCombatImage(enemyAnchor, "BattleCombatEnemy", enemySprite, enemyColor);
                enemyUnit.BindCombatUI(enemyAnchor as RectTransform, enemyImage, uiProjectilePool);
                enemyUnit.SetVisualsVisible(false);
            }

            return enemyUnit;
        }

        /// <summary>
        /// 현재 스폰 경로에서는 사용되지 않는, CombatManager에서 그대로 옮겨온 기존 미사용 코드입니다(SpawnAllyUnit 전용).
        /// </summary>
        private Vector3 GetSlotPosition(int column, CombatManager.SlotRow row)
        {
            // Front (closest to the enemy on the right) sits at the highest local X; Back sits at gridOrigin.
            float localX = (SlotRows - 1 - (int)row) * rowSpacing;
            float localY = column * columnSpacing;
            return gridOrigin + new Vector3(localX, localY, 0f);
        }
    }
}
