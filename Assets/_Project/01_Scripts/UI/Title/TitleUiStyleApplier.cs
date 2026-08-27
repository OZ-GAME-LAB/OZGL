using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    internal static class TitleUiStyleApplier
    {
        #region Internal API

        internal static void Apply(Image target, ImageStyle style)
        {
            if (target == null || style == null) return;

            if (style.sprite != null)
                target.sprite = style.sprite;

            if (style.material != null)
                target.material = style.material;

            target.color = style.color;
            target.preserveAspect = style.preserveAspect;
        }

        internal static void Apply(Button target, ButtonStyle style)
        {
            if (target == null || style == null) return;

            Image image = target.targetGraphic as Image;

            if (image == null) image = target.GetComponent<Image>();

            Apply(image, style.normal);

            if (style.transition != Selectable.Transition.SpriteSwap || HasSpriteState(style))
            {
                target.transition = style.transition;
            }

            if (style.transition != Selectable.Transition.SpriteSwap)
                return;

            if (!HasSpriteState(style))
                return;

            target.spriteState = new SpriteState
            {
                highlightedSprite = style.highlightedSprite,
                pressedSprite = style.pressedSprite,
                selectedSprite = style.selectedSprite,
                disabledSprite = style.disabledSprite
            };
        }

        internal static void Apply(Toggle target, ToggleStyle style, Image background, Image checkmark)
        {
            if (target == null || style == null) return;

            Apply(background, style.background);
            Apply(checkmark, style.checkmark);

            target.targetGraphic = background;
            target.graphic = checkmark;
        }

        #endregion

        #region Private Methods

        private static bool HasSpriteState(ButtonStyle style)
        {
            return style.highlightedSprite != null ||
                   style.pressedSprite != null ||
                   style.selectedSprite != null ||
                   style.disabledSprite != null;
        }

        #endregion
    }
}
