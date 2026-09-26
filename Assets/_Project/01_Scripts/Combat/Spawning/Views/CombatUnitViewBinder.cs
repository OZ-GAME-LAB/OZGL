using UnityEngine;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 월드 좌표에 생성된 전투 유닛의 렌더러, 투사체 경로 및 HUD 표시를 연결합니다.
    /// 아군과 적의 공통 표시 규칙을 한 곳에서 관리하는 순수 C# 클래스입니다.
    /// </summary>
    public static class CombatUnitViewBinder
    {
        /// <summary>
        /// 프리팹의 월드 렌더러와 투사체 경로를 활성화하고 선택적 HUD를 월드 캔버스에 연결합니다.
        /// </summary>
        public static void BindCombatPresentation(Unit unit, AllyUnitCombatHUDView hudPrefab = null)
        {
            // UI 앵커 대신 실제 유닛 Transform을 사용하는 월드 투사체 경로
            unit.BindCombatUI(null, null, null);
            unit.SetVisualsVisible(true);
            if (hudPrefab == null)
            {
                return;
            }

            GameObject hudRoot = new GameObject("AllyCombatHUD", typeof(RectTransform), typeof(Canvas));
            hudRoot.transform.SetParent(unit.transform, false);
            // HUD는 머리 앵커 바로 위에 둡니다(앵커가 없으면 기존 고정 오프셋).
            Transform head = unit.transform.Find(UnitPresenter.HeadAnchorPath);
            hudRoot.transform.localPosition = head != null
                ? unit.transform.InverseTransformPoint(head.position) + new Vector3(-0.5f, 0.7f, 0f)
                : new Vector3(-0.5f, 1.5f, 0f);
            hudRoot.transform.localScale = Vector3.one * 0.01f;
            RectTransform rect = hudRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 30f);
            Canvas canvas = hudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            AllyUnitCombatHUDView hud = Object.Instantiate(hudPrefab, rect, false);
            unit.BindHud(hud);
        }
    }
}
