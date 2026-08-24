using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance;


        public enum SlotRow { Front, Back }

        [System.Serializable]
        private struct SlotEntry
        {
            public int column;
            public SlotRow row;
            public string prefabResourceName;
        }

        private const int SlotColumns = 8;

        [SerializeField] private Transform unitsRoot;
        [SerializeField] private Vector3 gridOrigin = new Vector3(-2.5f, -3f, 0f);
        [SerializeField] private float columnSpacing = 1.2f;
        [SerializeField] private float rowSpacing = 1.2f;
        [SerializeField] private float gridTiltDegrees = 45f;
        [SerializeField] private float slotMarkerSize = 0.9f;
        [SerializeField] private float slotMarkerLineWidth = 0.03f;
        [SerializeField] private Color slotMarkerColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Vector3 enemyPosition = new Vector3(4.5f, 3f, 0f);
        [SerializeField] private string enemyPrefabResourceName = "Characters/Enemy_Melee";
        [SerializeField] private float enemyScale = 3f;

        private static readonly SlotEntry[] TestRoster =
        {
            new SlotEntry { column = 0, row = SlotRow.Front, prefabResourceName = "Characters/Ally_Sword" },
            new SlotEntry { column = 0, row = SlotRow.Back, prefabResourceName = "Characters/Ally_Bow" },
            new SlotEntry { column = 1, row = SlotRow.Front, prefabResourceName = "Characters/Ally_Sword" },
            new SlotEntry { column = 1, row = SlotRow.Back, prefabResourceName = "Characters/Ally_Bow" },
            new SlotEntry { column = 2, row = SlotRow.Front, prefabResourceName = "Characters/Ally_Sword" },
            new SlotEntry { column = 2, row = SlotRow.Back, prefabResourceName = "Characters/Ally_Bow" },
            new SlotEntry { column = 3, row = SlotRow.Front, prefabResourceName = "Characters/Ally_Sword" },
            new SlotEntry { column = 3, row = SlotRow.Back, prefabResourceName = "Characters/Ally_Bow" },
        };

        private readonly Unit[,] _slotUnits = new Unit[SlotColumns, 2];
        private Unit _enemyUnit;

        public Unit EnemyUnit => _enemyUnit;

        private void Awake()
        {
            Instance = this;
            SpawnSlotMarkers();
            SpawnAllies();
            SpawnEnemy();
        }

        public Unit ResolveAllyTarget()
        {
            List<Unit> exposed = new List<Unit>();

            for (int column = 0; column < SlotColumns; column++)
            {
                Unit front = _slotUnits[column, (int)SlotRow.Front];
                Unit back = _slotUnits[column, (int)SlotRow.Back];

                if (front != null && !front.IsDead)
                {
                    exposed.Add(front);
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
            foreach (SlotEntry entry in TestRoster)
            {
                GameObject prefab = Resources.Load<GameObject>(entry.prefabResourceName);
                if (prefab == null)
                {
                    continue;
                }

                GameObject instance = Instantiate(prefab, GetSlotPosition(entry.column, entry.row), Quaternion.identity, unitsRoot);
                _slotUnits[entry.column, (int)entry.row] = instance.GetComponent<Unit>();
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
            float localX = row == SlotRow.Front ? rowSpacing : 0f;
            float localY = column * columnSpacing;
            Vector2 rotated = RotateDegrees(new Vector2(localX, localY), gridTiltDegrees);
            return gridOrigin + new Vector3(rotated.x, rotated.y, 0f);
        }

        private static Vector2 RotateDegrees(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
