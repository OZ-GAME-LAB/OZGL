using DG.Tweening;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    public enum CombatTutorialStepTrigger
    {
        BattleReady,
        PreviousStepCompleted,
        EnemySkillUsed,
        Manual
    }

    public enum CombatTutorialTarget
    {
        None,
        EnemyHpUI,
        EnemyCombatArea,
        EnemySkillArea
    }

    public enum CombatTutorialCompletionMode
    {
        GuideDismissed,
        TargetClicked,
        Either
    }

    [CreateAssetMenu(
        fileName = "CombatTutorialStep_",
        menuName = "OZGL/Tutorial/Combat Sequence Step")]
    public sealed class CombatTutorialStepData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string stepName;

        [Header("Start Condition")]
        [SerializeField] private CombatTutorialStepTrigger trigger =
            CombatTutorialStepTrigger.PreviousStepCompleted;
        [SerializeField] private string manualTriggerKey;
        [SerializeField, Min(0f)] private float showDelay;

        [Header("Guide")]
        [SerializeField] private bool showGuide = true;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private string characterName;
        [SerializeField, TextArea(2, 6)] private string dialogue;
        [SerializeField] private bool showCharacter = true;

        [Header("Combat Control")]
        [SerializeField] private bool pauseCombat = true;
        [Tooltip("이 Step이 끝날 때 Tutorial 일시정지 사유를 해제합니다. 바로 이어지는 Pause Step이 있으면 정지 상태를 유지합니다.")]
        [SerializeField] private bool resumeCombatOnComplete = true;

        [Header("Target")]
        [SerializeField] private CombatTutorialTarget target;
        [SerializeField] private bool highlightTarget = true;
        [SerializeField] private CombatTutorialCompletionMode completionMode =
            CombatTutorialCompletionMode.GuideDismissed;
        [SerializeField] private Vector2 targetPadding = new(12f, 12f);

        [Header("Outline Style")]
        [SerializeField] private Color outlineColor =
            new(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Vector2 outlineMinDistance = new(2f, 2f);
        [SerializeField] private Vector2 outlineMaxDistance = new(8f, 8f);
        [SerializeField, Range(0f, 1f)] private float outlineMinAlpha = 0.4f;
        [SerializeField, Range(0f, 1f)] private float outlineMaxAlpha = 1f;
        [SerializeField, Min(0.01f)] private float outlineHalfDuration = 0.8f;
        [SerializeField] private Ease outlineEase = Ease.InOutSine;

        public string StepName => stepName;
        public CombatTutorialStepTrigger Trigger => trigger;
        public string ManualTriggerKey => manualTriggerKey;
        public float ShowDelay => showDelay;
        public bool ShowGuide => showGuide;
        public Sprite CharacterSprite => characterSprite;
        public string CharacterName => characterName;
        public string Dialogue => dialogue;
        public bool ShowCharacter => showCharacter;
        public bool PauseCombat => pauseCombat;
        public bool ResumeCombatOnComplete => resumeCombatOnComplete;
        public CombatTutorialTarget Target => target;
        public bool HighlightTarget => highlightTarget;
        public CombatTutorialCompletionMode CompletionMode => completionMode;
        public Vector2 TargetPadding => targetPadding;
        public Color OutlineColor => outlineColor;
        public Vector2 OutlineMinDistance => outlineMinDistance;
        public Vector2 OutlineMaxDistance => outlineMaxDistance;
        public float OutlineMinAlpha => outlineMinAlpha;
        public float OutlineMaxAlpha => outlineMaxAlpha;
        public float OutlineHalfDuration => outlineHalfDuration;
        public Ease OutlineEase => outlineEase;

        public bool AllowsGuideDismiss =>
            completionMode == CombatTutorialCompletionMode.GuideDismissed ||
            completionMode == CombatTutorialCompletionMode.Either;

        public bool AllowsTargetClick =>
            completionMode == CombatTutorialCompletionMode.TargetClicked ||
            completionMode == CombatTutorialCompletionMode.Either;
    }
}
