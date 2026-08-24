using UnityEngine;

namespace Combat
{
    [RequireComponent(typeof(LineRenderer))]
    public class RangeIndicator : MonoBehaviour
    {
        private const int Segments = 32;

        [SerializeField] private Unit unit;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private float lineWidth = 0.03f;

        private LineRenderer _lineRenderer;

        private void Awake()
        {
            if (unit == null)
            {
                unit = GetComponent<Unit>();
            }
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.loop = true;
            _lineRenderer.positionCount = Segments;
            _lineRenderer.widthMultiplier = lineWidth;
            _lineRenderer.startColor = lineColor;
            _lineRenderer.endColor = lineColor;

            DrawCircle(unit.AttackRange);
        }

        private void DrawCircle(float radius)
        {
            for (int i = 0; i < Segments; i++)
            {
                float angle = (float)i / Segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }
}
