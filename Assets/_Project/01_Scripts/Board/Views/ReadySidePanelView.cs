using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public enum ReadySidePanelTab
    {
        None,
        Synergy,
        Artifact
    }

    [DisallowMultipleComponent]
    public sealed class ReadySidePanelView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button synergyButton;
        [SerializeField] private Button artifactButton;
        [SerializeField] private Button closeButton;

        [Header("Content")]
        [SerializeField] private GameObject contentPanel;
        [SerializeField] private GameObject synergyArea;
        [SerializeField] private GameObject artifactArea;

        [Header("Selection Visuals")]
        [Tooltip("선택 표시용 오브젝트입니다. 없어도 동작합니다.")]
        [SerializeField] private GameObject synergySelectedVisual;
        [SerializeField] private GameObject artifactSelectedVisual;

        [Header("Initial State")]
        [SerializeField] private ReadySidePanelTab initialTab = ReadySidePanelTab.Synergy;

        private bool initialized;

        #region Properties

        public ReadySidePanelTab CurrentTab { get; private set; } = ReadySidePanelTab.None;

        public bool IsContentOpen => CurrentTab != ReadySidePanelTab.None;

        /// <summary>
        /// 탭 전환 또는 닫기 후 발생합니다.
        /// 외부 툴팁 정리 등이 필요할 때 구독합니다.
        /// </summary>
        public event Action<ReadySidePanelTab> TabChanged;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            SubscribeButtons();
            ApplyVisuals();
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
        }

        #endregion

        #region API

        public void ShowSynergy()
        {
            SetTab(ReadySidePanelTab.Synergy);
        }

        public void ShowArtifacts()
        {
            SetTab(ReadySidePanelTab.Artifact);
        }

        public void HideContent()
        {
            SetTab(ReadySidePanelTab.None);
        }

        /// <summary>
        /// 표시할 탭을 변경합니다.
        /// 같은 탭을 다시 선택하면 현재 상태를 유지합니다.
        /// </summary>
        public void SetTab(ReadySidePanelTab tab)
        {
            Initialize();

            tab = NormalizeTab(tab);

            bool changed = CurrentTab != tab;

            CurrentTab = tab;
            ApplyVisuals();

            if (changed)
            {
                TabChanged?.Invoke(CurrentTab);
            }
        }

        public void ResetToInitialTab()
        {
            SetTab(initialTab);
        }

        #endregion

        #region Internal

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            CurrentTab = NormalizeTab(initialTab);

            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            bool showSynergy = CurrentTab == ReadySidePanelTab.Synergy;
            bool showArtifact = CurrentTab == ReadySidePanelTab.Artifact;

            // 목록 상태를 먼저 설정한 뒤 공통 패널을 표시합니다.
            SetActive(synergyArea, showSynergy);
            SetActive(artifactArea, showArtifact);
            SetActive(contentPanel, showSynergy || showArtifact);

            SetActive(synergySelectedVisual, showSynergy);
            SetActive(artifactSelectedVisual, showArtifact);
        }

        private void SubscribeButtons()
        {
            if (synergyButton != null)
            {
                synergyButton.onClick.AddListener(ShowSynergy);
            }

            if (artifactButton != null)
            {
                artifactButton.onClick.AddListener(ShowArtifacts);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideContent);
            }
        }

        private void UnsubscribeButtons()
        {
            if (synergyButton != null)
            {
                synergyButton.onClick.RemoveListener(ShowSynergy);
            }

            if (artifactButton != null)
            {
                artifactButton.onClick.RemoveListener(ShowArtifacts);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HideContent);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static ReadySidePanelTab NormalizeTab(ReadySidePanelTab tab)
        {
            return tab switch
            {
                ReadySidePanelTab.Synergy => ReadySidePanelTab.Synergy,
                ReadySidePanelTab.Artifact => ReadySidePanelTab.Artifact,_ => ReadySidePanelTab.None
            };
        }

        #endregion
    }
}