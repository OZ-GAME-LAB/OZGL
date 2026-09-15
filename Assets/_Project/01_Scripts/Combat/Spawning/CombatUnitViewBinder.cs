using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.UI.Battle;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// AllySpawner에서 분리된 유닛 전투 UI 표시 책임을 담당합니다. 전투 이미지(Image) 생성,
    /// Unit의 UI 앵커/투사체 풀 바인딩, HUD 부착을 한 곳에서 처리하는 순수 C# 클래스입니다
    /// (씬/프리팹 재배선 불필요). 아군/적 스폰 경로가 거의 동일한 바인딩 코드를 각자
    /// 갖고 있던 것을 통합했습니다.
    /// </summary>
    public static class CombatUnitViewBinder
    {
        /// <summary>
        /// 전투 이미지를 생성해 Unit에 바인딩하고, hudPrefab이 있으면 HUD도 부착합니다.
        /// 마지막으로 SetVisualsVisible(false)까지 호출해 스폰 직후 시각 상태를 정리합니다.
        /// </summary>
        public static Image BindCombatPresentation(
            Unit unit,
            RectTransform anchor,
            string imageObjectName,
            Sprite sprite,
            Color color,
            UIProjectilePool projectilePool,
            AllyUnitCombatHUDView hudPrefab = null)
        {
            Image combatImage = CreateCombatImage(anchor, imageObjectName, sprite, color);
            unit.BindCombatUI(anchor, combatImage, projectilePool);

            if (hudPrefab != null)
            {
                AllyUnitCombatHUDView hud = Object.Instantiate(hudPrefab, anchor);
                hud.transform.SetAsLastSibling();
                unit.BindHud(hud);
            }

            unit.SetVisualsVisible(false);
            return combatImage;
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
    }
}
