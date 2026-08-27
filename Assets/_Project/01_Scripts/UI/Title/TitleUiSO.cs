using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{

    [CreateAssetMenu(fileName = "TitleUiSO",menuName = "OZGL/UI/Title UI SO")]
    public sealed class TitleUiSO : ScriptableObject
    {
        [Header("Main Canvas")]
        public TitleMainResources main = new();
        [Header("Settings")]
        public TitleSettingsResources settings = new();
        [Header("Exit Confirm")]
        public TitleExitConfirmResources exitConfirm = new();
    }

    [Serializable]
    public sealed class ImageStyle
    {
        public Sprite sprite;
        public Material material;
        public Color color = Color.white;
        public bool preserveAspect;
    }

    [Serializable]
    public sealed class ButtonStyle
    {
        public ImageStyle normal = new();

        [Header("Sprite Swap")]
        public Sprite highlightedSprite;
        public Sprite pressedSprite;
        public Sprite selectedSprite;
        public Sprite disabledSprite;

        public Selectable.Transition transition = Selectable.Transition.SpriteSwap;
    }

    [Serializable]
    public sealed class DropdownStyle
    {
        public ImageStyle background = new();
        public ImageStyle arrow = new();
        public ImageStyle itemBackground = new();
        public ImageStyle itemCheckmark = new();
    }

    [Serializable]
    public sealed class SliderStyle
    {
        public ImageStyle track = new();
        public ImageStyle fill = new();
        public ImageStyle handle = new();
    }

    [Serializable]
    public sealed class ToggleStyle
    {
        public ImageStyle background = new();
        public ImageStyle checkmark = new();
    }

    [Serializable]
    public sealed class TitleMainResources
    {
        [Header("Main")]
        public ImageStyle background = new();
        public ImageStyle title = new();

        [Header("Menu Buttons")]
        public ButtonStyle startButton = new();
        public ButtonStyle upgradeButton = new();
        public ButtonStyle settingsButton = new();
        public ButtonStyle exitButton = new();
    }

    [Serializable]
    public sealed class TitleSettingsResources
    {
        [Header("Base")]
        public ImageStyle dimmer = new();
        public ImageStyle panel = new();

        [Header("Buttons")]
        public ButtonStyle categoryButton = new();
        public ButtonStyle backButton = new();
        public ButtonStyle actionButton = new();

        [Header("Controls")]
        public DropdownStyle dropdown = new();
        public SliderStyle slider = new();
        public ToggleStyle toggle = new();
    }

    [Serializable]
    public sealed class TitleExitConfirmResources
    {
        public ImageStyle dimmer = new();
        public ImageStyle panel = new();

        public ButtonStyle confirmButton = new();
        public ButtonStyle cancelButton = new();
    }
}