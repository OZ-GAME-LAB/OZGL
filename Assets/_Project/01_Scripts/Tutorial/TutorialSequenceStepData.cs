using DG.Tweening;
using OzGameLab01.Map;
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
        Manual,
        LocateCompleted,
        OwnedUnitHovered
    }

    public enum TutorialTileTargetMode
    {
        Position,
        NodeType
    }

    public enum TutorialFormationSlotHighlightGroup
    {
        None,
        BattleFormationSlots,
        SupportFormationSlots,
        AllFormationSlots
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
        [Tooltip("Owned Unit Hovered 조건에서 감지할 UnitData ID입니다. 0이면 모든 보유 유닛 아이콘을 허용합니다.")]
        [SerializeField, Min(0)] private int targetOwnedUnitId;

        [Header("Step Actions")]
        [Tooltip("이 Step이 실행될 때 RollView 열기를 요청합니다.")]
        [SerializeField] private bool openRollView;
        [Tooltip("UI 동작 실행 후 TutorialGuideView도 함께 표시합니다. 끄면 Guide 해제를 기다리지 않는 동작 전용 Step이 됩니다.")]
        [SerializeField] private bool showGuide = true;

        [Header("Board Tile Focus")]
        [Tooltip("지정한 타일만 밝게 남기고 나머지 보드를 암전한 뒤 Locate 카메라 연출을 재생합니다.")]
        [SerializeField] private bool focusBoardTile;
        [SerializeField] private TutorialTileTargetMode tileTargetMode =
            TutorialTileTargetMode.NodeType;
        [Tooltip("Target Mode가 Position일 때 사용할 논리 타일 좌표입니다.")]
        [SerializeField] private Vector2Int tilePosition;
        [Tooltip("Target Mode가 NodeType일 때 찾을 타일 종류입니다.")]
        [SerializeField] private NodeType tileType = NodeType.UnitAcquisition;
        [Tooltip("같은 종류의 타일이 여러 개면 좌표 순으로 정렬한 뒤 사용할 인덱스입니다.")]
        [SerializeField, Min(0)] private int tileTypeOccurrence;
        [Tooltip("Guide를 표시하는 Step이면 Locate가 플레이어에게 복귀한 뒤 Guide를 표시합니다.")]
        [SerializeField] private bool showGuideAfterLocate = true;

        [Header("Guide Content")]
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private string characterName;
        [SerializeField, TextArea(2,6)] private string dialogue;
        [SerializeField] private bool showCharacter = true;

        [Header("Button Highlight")]
        [SerializeField] private bool highlightButton;
        [Tooltip("TutorialTargetRegistry에 등록한 버튼 키입니다.")]
        [SerializeField] private string highlightButtonKey;
        [Tooltip("Guide가 닫혀도 실제 버튼을 클릭할 때까지 강조 연출을 유지합니다.")]
        [SerializeField] private bool keepButtonHighlightUntilClicked;
        [SerializeField, Min(1f)] private float highlightScale = 1.08f;
        [SerializeField, Min(0.01f)] private float highlightHalfDuration = 0.3f;
        [SerializeField] private Color highlightColor = new(1f,0.8f,0.2f,1f);
        [SerializeField] private Ease highlightEase = Ease.InOutSine;
        [SerializeField] private bool ignoreTimeScale = true;

        [Header("UI Outline Highlight")]
        [SerializeField] private bool outlineHighlight;
        [Tooltip("TutorialTargetRegistry의 Outline Targets에 등록한 키입니다.")]
        [SerializeField] private string outlineTargetKey;

        [Header("Formation Slot Group Highlight")]
        [Tooltip("전투 9칸, 서포트 2칸 또는 전체 11칸을 개별 슬롯 단위로 동시에 강조합니다.")]
        [SerializeField] private TutorialFormationSlotHighlightGroup
            formationSlotHighlightGroup;
        [Tooltip("Guide가 닫힌 뒤에도 지정한 Ready View가 닫힐 때까지 슬롯 강조를 유지합니다.")]
        [SerializeField] private bool keepFormationSlotHighlightUntilReadyViewHidden = true;
        [SerializeField] private ReadySceneViewType formationSlotHighlightReleaseView =
            ReadySceneViewType.Unit;

        [Header("Outline Highlight Style")]
        [Tooltip("UI Outline과 Formation Slot Group 강조가 함께 사용하는 색상입니다.")]
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
        public int TargetOwnedUnitId => targetOwnedUnitId;
        public bool OpenRollView => openRollView;
        public bool ShowGuide => showGuide;
        public bool FocusBoardTile => focusBoardTile;
        public TutorialTileTargetMode TileTargetMode => tileTargetMode;
        public Vector2Int TilePosition => tilePosition;
        public NodeType TileType => tileType;
        public int TileTypeOccurrence => tileTypeOccurrence;
        public bool ShowGuideAfterLocate => showGuideAfterLocate;
        public Sprite CharacterSprite => characterSprite;
        public string CharacterName => characterName;
        public string Dialogue => dialogue;
        public bool ShowCharacter => showCharacter;
        public bool HighlightButton => highlightButton;
        public string HighlightButtonKey => highlightButtonKey;
        public bool KeepButtonHighlightUntilClicked =>
            keepButtonHighlightUntilClicked;
        public float HighlightScale => highlightScale;
        public float HighlightHalfDuration => highlightHalfDuration;
        public Color HighlightColor => highlightColor;
        public Ease HighlightEase => highlightEase;
        public bool IgnoreTimeScale => ignoreTimeScale;
        public bool OutlineHighlight => outlineHighlight;
        public string OutlineTargetKey => outlineTargetKey;
        public TutorialFormationSlotHighlightGroup FormationSlotHighlightGroup =>
            formationSlotHighlightGroup;
        public bool HighlightFormationSlots =>
            formationSlotHighlightGroup !=
            TutorialFormationSlotHighlightGroup.None;
        public bool KeepFormationSlotHighlightUntilReadyViewHidden =>
            keepFormationSlotHighlightUntilReadyViewHidden;
        public ReadySceneViewType FormationSlotHighlightReleaseView =>
            formationSlotHighlightReleaseView;
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
