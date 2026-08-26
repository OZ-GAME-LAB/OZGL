using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance;


        public enum SlotRow { Front, Mid, Back }

        [System.Serializable]
        public struct SlotKey : System.IEquatable<SlotKey>
        {
            public int column;
            public SlotRow row;

            public bool Equals(SlotKey other) => column == other.column && row == other.row;
            public override bool Equals(object obj) => obj is SlotKey other && Equals(other);
            public override int GetHashCode() => (column, row).GetHashCode();
        }

        [System.Serializable]
        private struct SlotPlacement
        {
            public SlotKey slot;
            public UnitData unitData;
        }

        private const int SlotColumns = 3;
        private const int SlotRows = 3;

        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Vector3 gridOrigin = new Vector3(-4.5f, -1.2f, 0f);
        [SerializeField] private float columnSpacing = 1.2f;
        [SerializeField] private float rowSpacing = 1.2f;
        [SerializeField] private float slotMarkerSize = 0.9f;
        [SerializeField] private float slotMarkerLineWidth = 0.03f;
        [SerializeField] private Color slotMarkerColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Vector3 enemyPosition = new Vector3(4.5f, 0f, 0f);
        [SerializeField] private string enemyPrefabResourceName = "Characters/Enemy_Melee";
        [SerializeField] private float enemyScale = 3f;

        [Tooltip("맵 씬에서 미리 정한 아군 배치. 슬롯(열+행)을 키로, 그 슬롯에 들어갈 유닛 클래스를 값으로 가짐.")]
        [SerializeField] private List<SlotPlacement> allyFormation = new List<SlotPlacement>();

        private Dictionary<SlotKey, UnitData> _allyFormation;
        private readonly Unit[,] _slotUnits = new Unit[SlotColumns, SlotRows];
        private Unit _enemyUnit;

        public Unit EnemyUnit => _enemyUnit;

        private void Awake()
        {
            Instance = this;
            BuildAllyFormation();
            SpawnSlotMarkers();
            SpawnAllies();
            SpawnEnemy();
        }

        private void BuildAllyFormation()
        {
            _allyFormation = new Dictionary<SlotKey, UnitData>();
            foreach (SlotPlacement placement in allyFormation)
            {
                _allyFormation[placement.slot] = placement.unitData;
            }
        }

        public Unit ResolveAllyTarget()
        {
            List<Unit> exposed = new List<Unit>();

            for (int column = 0; column < SlotColumns; column++)
            {
                Unit front = _slotUnits[column, (int)SlotRow.Front];
                Unit mid = _slotUnits[column, (int)SlotRow.Mid];
                Unit back = _slotUnits[column, (int)SlotRow.Back];

                if (front != null && !front.IsDead)
                {
                    exposed.Add(front);
                }
                else if (mid != null && !mid.IsDead)
                {
                    exposed.Add(mid);
                }
                else if (back != null && !back.IsDead)
                {
                    exposed.Add(back);
                }
            }

            if (exposed.Count == 0)
            {
                return null;
            }

            return exposed[Random.Range(0, exposed.Count)];
        }

        private void SpawnSlotMarkers()
        {
            for (int column = 0; column < SlotColumns; column++)
            {
                CreateSlotMarker(GetSlotPosition(column, SlotRow.Front));
                CreateSlotMarker(GetSlotPosition(column, SlotRow.Mid));
                CreateSlotMarker(GetSlotPosition(column, SlotRow.Back));
            }
        }

        private void CreateSlotMarker(Vector3 position)
        {
            GameObject marker = new GameObject("SlotMarker");
            marker.transform.SetParent(unitsRoot);
            marker.transform.position = position;

            LineRenderer lineRenderer = marker.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 4;
            lineRenderer.widthMultiplier = slotMarkerLineWidth;
            lineRenderer.startColor = slotMarkerColor;
            lineRenderer.endColor = slotMarkerColor;
            lineRenderer.sortingOrder = -1;

            float half = slotMarkerSize * 0.5f;
            lineRenderer.SetPosition(0, new Vector3(-half, -half, 0f));
            lineRenderer.SetPosition(1, new Vector3(-half, half, 0f));
            lineRenderer.SetPosition(2, new Vector3(half, half, 0f));
            lineRenderer.SetPosition(3, new Vector3(half, -half, 0f));
        }

        private void SpawnAllies()
        {
            foreach (KeyValuePair<SlotKey, UnitData> kvp in _allyFormation)
            {
                if (kvp.Value == null || kvp.Value.UnitPrefab == null)
                {
                    continue;
                }

                SlotKey slot = kvp.Key;
                GameObject instance = Instantiate(kvp.Value.UnitPrefab, GetSlotPosition(slot.column, slot.row), Quaternion.identity, unitsRoot);
                _slotUnits[slot.column, (int)slot.row] = instance.GetComponent<Unit>();
            }
        }

        private void SpawnEnemy()
        {
            GameObject prefab = Resources.Load<GameObject>(enemyPrefabResourceName);
            if (prefab == null)
            {
                return;
            }

            GameObject enemySlot = new GameObject("EnemySlot");
            enemySlot.transform.SetParent(unitsRoot);
            enemySlot.transform.position = enemyPosition;
            enemySlot.transform.localScale = Vector3.one * enemyScale;

            GameObject enemyInstance = Instantiate(prefab, enemySlot.transform);
            enemyInstance.transform.localPosition = Vector3.zero;
            enemyInstance.transform.localRotation = Quaternion.identity;
            _enemyUnit = enemyInstance.GetComponent<Unit>();
        }

        private Vector3 GetSlotPosition(int column, SlotRow row)
        {
            // Front (closest to the enemy on the right) sits at the highest local X; Back sits at gridOrigin.
            float localX = (SlotRows - 1 - (int)row) * rowSpacing;
            float localY = column * columnSpacing;
            return gridOrigin + new Vector3(localX, localY, 0f);
        }
    }
}
