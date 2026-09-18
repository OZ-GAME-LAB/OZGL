using DG.Tweening;
using OzGameLab01.UI;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    public enum TutorialStepTrigger
    {
        SequenceStarted,
        PreviousGuideDismissed,
        ReadyViewShown,
        ReadyViewHidden,
        ButtonClicked,
        Manual
    }

    [CreateAssetMenu(
        fileName = "TutorialStep_",
        menuName = "OZGL/Tutorial/Sequence Step")]
    public sealed class TutorialSequenceStepData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string stepName;

        [Header("Show Condition")]
        [SerializeField] private TutorialStepTrigger trigger;
        [Tooltip("조건이 충족된 후 이 Step의 동작을 실행하기까지 기다리는 시간입니다.")]
        [SerializeField, Min(0f)] private float showDelay;
        [SerializeField] private bool ignoreShowDelayTimeScale = true;
        [SerializeField] private ReadySceneViewType targetReadyView;
        [Tooltip("TutorialTargetRegistry에 등록한 버튼 키입니다.")]
        [SerializeField] private string triggerButtonKey;
        [SerializeField] private string manualTriggerKey;

        [Header("Step Actions")]
        [Tooltip("이 Step이 실행될 때 RollView 열기를 요청합니다.")]
        [SerializeField] private bool openRollView;
        [Tooltip("UI 동작 실행 후 TutorialGuideView도 함께 표시합니다. 끄면 Guide 해제를 기다리지 않는 동작 전용 Step이 됩니다.")]
        [SerializeField] private bool showGuide = true;

        [Header("Guide Content")]
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private string characterName;
        [SerializeField, TextArea(2,6)] private string dialogue;
        [SerializeField] private bool showCharacter = true;

        [Header("Button Highlight")]
        [SerializeField] private bool highlightButton;
        [Tooltip("TutorialTargetRegistry에 등록한 버튼 키입니다.")]
        [SerializeField] private string highlightButtonKey;
        [SerializeField, Min(1f)] private float highlightScale = 1.08f;
        [SerializeField, Min(0.01f)] private float highlightHalfDuration = 0.3f;
        [SerializeField] private Color highlightColor = new(1f,0.8f,0.2f,1f);
        [SerializeField] private Ease highlightEase = Ease.InOutSine;
        [SerializeField] private bool ignoreTimeScale = true;

        [Header("UI Outline Highlight")]
        [SerializeField] private bool outlineHighlight;
        [Tooltip("TutorialTargetRegistry의 Outline Targets에 등록한 키입니다.")]
        [SerializeField] private string outlineTargetKey;
        [SerializeField] private Color outlineColor = new(1f,0.8f,0.2f,1f);
        [SerializeField] private Vector2 outlineMinDistance = new(2f,2f);
        [SerializeField] private Vector2 outlineMaxDistance = new(8f,8f);
        [SerializeField, Range(0f,1f)] private float outlineMinAlpha = 0.4f;
        [SerializeField, Range(0f,1f)] private float outlineMaxAlpha = 1f;
        [SerializeField, Min(0.01f)] private float outlineHalfDuration = 0.8f;
        [SerializeField] private Ease outlineEase = Ease.InOutSine;
        [SerializeField] private bool outlineIgnoreTimeScale = true;
        [SerializeField] private bool keepOutlineUntilReadyViewHidden = true;
        [SerializeField] private ReadySceneViewType outlineReleaseView =
            ReadySceneViewType.Unit;

        public string StepName => stepName;
        public TutorialStepTrigger Trigger => trigger;
        public float ShowDelay => showDelay;
        public bool IgnoreShowDelayTimeScale => ignoreShowDelayTimeScale;
        public ReadySceneViewType TargetReadyView => targetReadyView;
        public string TriggerButtonKey => triggerButtonKey;
        public string ManualTriggerKey => manualTriggerKey;
        public bool OpenRollView => openRollView;
        public bool ShowGuide => showGuide;
        public Sprite CharacterSprite => characterSprite;
        public string CharacterName => characterName;
        public string Dialogue => dialogue;
        public bool ShowCharacter => showCharacter;
        public bool HighlightButton => highlightButton;
        public string HighlightButtonKey => highlightButtonKey;
        public float HighlightScale => highlightScale;
        public float HighlightHalfDuration => highlightHalfDuration;
        public Color HighlightColor => highlightColor;
        public Ease HighlightEase => highlightEase;
        public bool IgnoreTimeScale => ignoreTimeScale;
        public bool OutlineHighlight => outlineHighlight;
        public string OutlineTargetKey => outlineTargetKey;
        public Color OutlineColor => outlineColor;
        public Vector2 OutlineMinDistance => outlineMinDistance;
        public Vector2 OutlineMaxDistance => outlineMaxDistance;
        public float OutlineMinAlpha => outlineMinAlpha;
        public float OutlineMaxAlpha => outlineMaxAlpha;
        public float OutlineHalfDuration => outlineHalfDuration;
        public Ease OutlineEase => outlineEase;
        public bool OutlineIgnoreTimeScale => outlineIgnoreTimeScale;
        public bool KeepOutlineUntilReadyViewHidden =>
            keepOutlineUntilReadyViewHidden;
        public ReadySceneViewType OutlineReleaseView => outlineReleaseView;
    }
}
