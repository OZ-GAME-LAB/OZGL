using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using OzGameLab01.UI;
using OzGameLab01.UI.Battle;
using OzGameLab01.UI.Title;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.Save;
using OzGameLab01.Common;
using OzGameLab01.Effects.Models;
using OzGameLab01.Player;

namespace OzGameLab01.Controllers
{
    public class CombatUIController : MonoBehaviour
    {
        [Header("Views (UI 연결)")]
        public CombatUIView battleUIView;
        public TitleSettingsView settingsView;
        public ConfirmPopupView surrenderPopup;

        [Header("Scene Controller (로직 연결)")]
        public CombatSceneController combatSceneController;

        // 인스펙터 연결 여부와 무관하게 직접 물고 있을 숨겨진 뷰들
        private CombatControlView _controlView;
        private CombatTimerView _timerView;
        private CombatInfoView _infoView;
        private GameObject _infoPanel;
        private CombatSession _combatSession;
        private RunResultAnimationView _runResultView;
        private RunResultContentView _runResultContentView;
        private bool _isBattleInfoBinding;
        
        private bool _isFastForward = false; // 배속 상태 저장용 변수

        private enum SurrenderTarget
        {
            Board,
            Title
        }

        private SurrenderTarget _surrenderTarget;

        private void Awake()
        {
            // 이벤트 시스템 체크 (버튼 클릭 불가 원인 1순위)
            if (EventSystem.current == null)
            {
                Debug.LogWarning("[CombatUIController] 씬에 EventSystem이 없습니다! UI 버튼이 작동하지 않습니다. EventSystem을 추가해주세요.");
            }

            // 1. 최상단 UI 및 컨트롤러들을 찾습니다.
            if (battleUIView == null) battleUIView = FindFirstObjectByType<CombatUIView>(FindObjectsInactive.Include);
            if (settingsView == null) settingsView = FindFirstObjectByType<TitleSettingsView>(FindObjectsInactive.Include);
            if (surrenderPopup == null) surrenderPopup = FindFirstObjectByType<ConfirmPopupView>(FindObjectsInactive.Include);
            if (combatSceneController == null) combatSceneController = FindFirstObjectByType<CombatSceneController>(FindObjectsInactive.Include);
            _combatSession = FindFirstObjectByType<CombatSession>(FindObjectsInactive.Include);
            _runResultView = FindFirstObjectByType<RunResultAnimationView>(FindObjectsInactive.Include);
            _runResultContentView = FindFirstObjectByType<RunResultContentView>(FindObjectsInactive.Include);
            _infoView = FindFirstObjectByType<CombatInfoView>(FindObjectsInactive.Include);
            _infoPanel = FindBattleInfoPanel(_infoView);

            // 2. CombatUIView 내부 깊숙이 있는 컨트롤 뷰와 타이머 뷰를 직접 찾아냅니다!
            if (battleUIView != null)
            {
                _controlView = battleUIView.GetComponentInChildren<CombatControlView>(true);
                _timerView = battleUIView.GetComponentInChildren<CombatTimerView>(true);
            }
        }

        // 전투 진입 시 마지막 저장 배속 및 버튼 표시 복원
        private void Start()
        {
            _isFastForward = SystemBus.Get<SaveFacade>()?.CurrentData?.combatFastForward ?? false;
            combatSceneController?.SetFastForward(_isFastForward);
            UpdateSpeedDisplay(_controlView);

            _infoView?.SetBattleAction(HandleBattleClicked);
            _infoView?.SetBattleInteractable(false);
            SetBattleInfoVisible(true);

            if (_combatSession != null && _combatSession.IsBattleReady)
            {
                HandleBattleReady();
            }
        }

        private void OnEnable()
        {
            if (_controlView != null)
            {
                _controlView.SettingsClicked += HandleSettingsClicked;
                _controlView.SpeedClicked += HandleSpeedClicked; // 배속 버튼 구독
            }

            if (battleUIView != null)
            {
                battleUIView.EndBattleClicked += HandleEndBattleClicked;
            }

            if (settingsView != null)
            {
                settingsView.CloseRequested += HandleSettingsBackClicked;

                // 전투에서는 보드 복귀와 체크포인트를 유지하는 타이틀 복귀를 제공합니다.
                settingsView.ClearGameButtons();
                settingsView.AddGameButton("보드로 돌아가기", HandleReturnToBoardClicked);
                settingsView.AddGameButton("타이틀로 돌아가기", HandleReturnToTitleClicked);
            }

            if (surrenderPopup != null)
            {
                surrenderPopup.ConfirmClicked += HandleSurrenderConfirmClicked;
                surrenderPopup.CancelClicked += HandleSurrenderCancelClicked;
            }

            if (combatSceneController != null)
            {
                combatSceneController.OnBattleResolved += HandleBattleResolved;
            }

            if (_combatSession != null)
            {
                _combatSession.BattleReady += HandleBattleReady;
            }

            if (_runResultView != null)
            {
                _runResultView.MainButtonClicked += HandleRunResultMainButtonClicked;
            }
        }

        private void OnDisable()
        {
            if (_controlView != null)
            {
                _controlView.SettingsClicked -= HandleSettingsClicked;
                _controlView.SpeedClicked -= HandleSpeedClicked;
            }

            if (battleUIView != null)
            {
                battleUIView.EndBattleClicked -= HandleEndBattleClicked;
            }

            if (settingsView != null)
            {
                settingsView.CloseRequested -= HandleSettingsBackClicked;
                settingsView.ClearGameButtons();
            }

            if (surrenderPopup != null)
            {
                surrenderPopup.ConfirmClicked -= HandleSurrenderConfirmClicked;
                surrenderPopup.CancelClicked -= HandleSurrenderCancelClicked;
            }

            if (combatSceneController != null)
            {
                combatSceneController.OnBattleResolved -= HandleBattleResolved;
            }

            if (_combatSession != null)
            {
                _combatSession.BattleReady -= HandleBattleReady;
            }

            if (_runResultView != null)
            {
                _runResultView.MainButtonClicked -= HandleRunResultMainButtonClicked;
            }
        }

        private void Update()
        {
            // 3. 누락되어 있던 전투 타이머 동기화 로직 추가!
            if (combatSceneController != null &&
                !combatSceneController.IsResolved &&
                _combatSession != null &&
                _combatSession.IsBattleRunning)
            {
                // UnscaledDeltaTime을 쓰면 배속에 무관하게 흐르는 현실 시간을 잴 수 있고,
                // DeltaTime을 쓰면 배속 시 게임 시간도 빨리 흐릅니다. (보통 전투 타이머는 배속 시 빨리 흐릅니다)
                if (_timerView != null)
                {
                    TimeSpan time = TimeSpan.FromSeconds(_combatSession.BattleElapsedSeconds);
                    // mm:ss (예: 01:23) 형식으로 텍스트 업데이트
                    _timerView.SetTimeText(time.ToString(@"mm\:ss"));
                }
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (combatSceneController == null || combatSceneController.IsResolved)
            {
                return;
            }

            Rect buttonRect = new Rect(20f, 20f, 190f, 44f);
            if (GUI.Button(buttonRect, "DEBUG: Boss Victory"))
            {
                combatSceneController.DebugResolveBossVictory();
            }
        }
#endif

        #region 버튼 클릭 이벤트 처리

        private void HandleBattleClicked()
        {
            if (_combatSession == null || !_combatSession.IsBattleReady)
            {
                return;
            }

            _infoView?.SetBattleInteractable(false);
            SetBattleInfoVisible(false);
            _combatSession.StartBattle();
            combatSceneController?.BeginBattle();
        }

        // 배속 변경 즉시 저장을 위한 비동기 처리
        private async void HandleSpeedClicked(CombatControlView view)
        {
            _isFastForward = !_isFastForward; // 상태 토글 (1배속 <-> 2배속)

            // 설정창 등이 열려있어서 일시정지 상태인 경우가 아니라면 즉시 배속을 적용합니다.
            if (combatSceneController != null)
            {
                combatSceneController.SetFastForward(_isFastForward);
            }

            UpdateSpeedDisplay(view);

            // 씬 전환 및 게임 재실행에 사용할 마지막 전투 배속 저장
            SaveFacade saveFacade = SystemBus.Get<SaveFacade>();

            if (saveFacade != null)
            {
                await saveFacade.SetCombatFastForwardAsync(_isFastForward);
            }
        }

        // 전투 진입 및 배속 변경 시 버튼 표시 동기화
        private void UpdateSpeedDisplay(CombatControlView view)
        {
            view?.SetSpeedVisual(_isFastForward);            
        }

        private void HandleSettingsClicked(CombatControlView view)
        {
            combatSceneController?.SetPaused(true);
            settingsView?.Show();
        }

        private void HandleSettingsBackClicked()
        {
            settingsView?.Hide();
            if (combatSceneController != null && !combatSceneController.IsResolved)
            {
                // 일시정지를 풀 때, 플레이어가 설정해둔 배속 상태(1배속 또는 2배속)로 정확히 복원합니다.
                combatSceneController.SetPaused(false);
            }
        }

        /// <summary>
        /// 보드로 돌아가기 버튼 클릭 처리
        /// </summary>
        private void HandleReturnToBoardClicked()
        {
            _surrenderTarget = SurrenderTarget.Board;
            surrenderPopup?.Show("Would you like to surrender and return to the board?");
        }

        /// <summary>
        /// 타이틀로 돌아가기 버튼 클릭 처리
        /// </summary>
        private void HandleReturnToTitleClicked()
        {
            _surrenderTarget = SurrenderTarget.Title;
            surrenderPopup?.Show("Would you like to surrender and return to the title?");
        }

        private void HandleSurrenderConfirmClicked(ConfirmPopupView popup)
        {
            surrenderPopup?.Hide();
            settingsView?.Hide();

            if (combatSceneController == null)
            {
                return;
            }

            switch (_surrenderTarget)
            {
                case SurrenderTarget.Board:
                    combatSceneController.ReturnToBoard();
                    break;

                case SurrenderTarget.Title:
                    combatSceneController.ReturnToTitleFromCombat();
                    break;
            }
        }

        private void HandleSurrenderCancelClicked(ConfirmPopupView popup)
        {
            surrenderPopup?.Hide();
        }

        private void HandleEndBattleClicked(CombatResultView view)
        {
            if (combatSceneController != null)
            {
                if (combatSceneController.IsBossVictory)
                {
                    combatSceneController.ReturnToTitle();
                }
                else
                {
                    combatSceneController.ReturnToBoard();
                }
            }
        }

        #endregion

        #region 게임 로직 이벤트 처리

        private async void HandleBattleReady()
        {
            if (_isBattleInfoBinding || _infoView == null || _combatSession == null)
            {
                return;
            }

            _isBattleInfoBinding = true;

            MonsterData enemyData = _combatSession.EnemyData;
            Unit enemyUnit = _combatSession.State.EnemyUnit;
            if (enemyData == null || enemyUnit == null)
            {
                _isBattleInfoBinding = false;
                return;
            }

            SpriteRenderer enemyRenderer = enemyUnit.GetComponentInChildren<SpriteRenderer>(true);
            Sprite enemySprite = enemyRenderer != null ? enemyRenderer.sprite : null;
            string enemyIconAddress = enemyData.species != null && enemyData.species.Count > 0
                ? enemyData.species[0]?.iconAddress
                : null;
            if (!string.IsNullOrWhiteSpace(enemyIconAddress))
            {
                try
                {
                    enemySprite = await SpriteManager.GetSpriteAsync(enemyIconAddress) ?? enemySprite;
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
            if (_infoView == null)
            {
                _isBattleInfoBinding = false;
                return;
            }
            _infoView.Clear();
            _infoView.SetEnemy(enemyUnit.DisplayName, enemySprite);
            BindEnemyStats(enemyData);

            for (int index = 0; index < enemyData.skillIds.Count; index++)
            {
                SkillData skill = RuntimeContent.Catalog.GetSkill(enemyData.skillIds[index]);
                if (skill == null)
                {
                    continue;
                }


                Sprite icon = null;

                // 스킬 아이콘 주소가 있는 경우에만 SpriteManager에 로드를 요청합니다.
                // 주소가 비어 있으면 아이콘 없이 스킬 정보만 표시합니다.
                if (!string.IsNullOrWhiteSpace(skill.iconAddress))
                {
                    icon = await SpriteManager.GetSpriteAsync(skill.iconAddress);
                }

                if (_infoView == null)
                {
                    _isBattleInfoBinding = false;
                    return;
                }

                _infoView.AddSkill(icon, skill.name, skill.description);
            }

            _infoView.SetBattleAction(HandleBattleClicked);
            _infoView.SetBattleInteractable(true);
            SetBattleInfoVisible(true);
            _isBattleInfoBinding = false;
        }

        private void BindEnemyStats(MonsterData enemyData)
        {
            if (_infoView == null || enemyData == null)
            {
                return;
            }

            _infoView.BindStats(enemyData);
        }

        private void SetBattleInfoVisible(bool visible)
        {
            if (_infoPanel != null)
            {
                _infoPanel.SetActive(visible);
                return;
            }

            _infoView?.SetVisible(visible);
        }

        private static GameObject FindBattleInfoPanel(CombatInfoView infoView)
        {
            if (infoView == null)
            {
                return null;
            }

            Transform current = infoView.transform;
            while (current != null)
            {
                if (current.name == "Battle_Info_Ui")
                {
                    return current.gameObject;
                }

                current = current.parent;
            }

            return infoView.gameObject;
        }

        private void HandleBattleResolved(bool victory)
        {
            _ = PopulateDpsListAsync();

            if (battleUIView != null)
            {
                if (victory)
                {
                    if (combatSceneController != null && combatSceneController.IsBossVictory)
                    {
                        ShowRunResult();
                        return;
                    }

                    if (battleUIView.ResultView != null)
                    {
                        // 일반 전투는 승리 시 무작위 유물 1개를 자동으로 지급합니다
                        // (dropWeight 가중치, RelicFacade.AcquireRandomRelic).
                        RelicData grantedRelic = BattleRewardService.ApplyAutomaticVictoryReward(this);

                        battleUIView.ResultView.SetResultText("승리!");
                        battleUIView.ResultView.SetOptionalMessage(
                            grantedRelic != null ? $"유물 획득: {grantedRelic.name}" : string.Empty);
                        battleUIView.ResultView.SetEndBattleButtonText("보드로 돌아가기");
                        _ = battleUIView.ResultView.SetRewardIconAsync(grantedRelic?.iconAddress);
                    }

                    battleUIView.ShowResultView();
                }
                else
                {
                    // 패배 시 바로 결과 창(ResultView)을 띄웁니다.
                    if (battleUIView.ResultView != null)
                    {
                        battleUIView.ResultView.SetResultText("패배...");
                        battleUIView.ResultView.SetOptionalMessage("다음 기회에 다시 도전해 보세요.");
                        battleUIView.ResultView.SetEndBattleButtonText("보드로 돌아가기");
                        battleUIView.ResultView.SetRewardIcon(null);
                    }
                    battleUIView.ShowResultView();
                }
            }
        }

        private async Task PopulateDpsListAsync()
        {
            CombatResultView resultView = battleUIView?.ResultView;
            if (resultView == null)
            {
                Debug.LogError("[CombatUIController] CombatResultView 참조가 없습니다.", this);
                return;
            }

            resultView.ClearDpsInfoItems();
            if (_combatSession == null)
            {
                Debug.LogError("[CombatUIController] CombatSession 참조가 없습니다.", this);
                return;
            }

            float elapsedSeconds = _combatSession.BattleElapsedSeconds;
            if (elapsedSeconds <= 0f)
            {
                Debug.LogWarning("[CombatUIController] 전투 시간이 없어 DPS를 계산할 수 없습니다.", this);
                return;
            }

            List<DpsResultData> results = BuildDpsResults(elapsedSeconds);
            results.Sort(CompareDpsResults);
            for (int index = 0; index < results.Count; index++)
            {
                DpsResultData result = results[index];
                Sprite icon = null;
                if (!string.IsNullOrWhiteSpace(result.IconAddress))
                {
                    icon = await SpriteManager.GetSpriteAsync(result.IconAddress);
                }

                if (this == null || resultView == null)
                {
                    return;
                }

                DpsInfoItemView item = resultView.CreateDpsInfoItem();
                if (item == null)
                {
                    return;
                }

                item.SetData(icon, result.UnitName, result.Dps);
            }
        }

        private List<DpsResultData> BuildDpsResults(float elapsedSeconds)
        {
            List<DpsResultData> results = new List<DpsResultData>();
            foreach (KeyValuePair<CombatManager.SlotKey, int> entry in
                _combatSession.State.SpawnedFormation)
            {
                Unit unit = _combatSession.State.SlotUnits[
                    entry.Key.column,
                    (int)entry.Key.row];
                if (unit == null)
                {
                    Debug.LogWarning($"[CombatUIController] 출전 유닛 참조가 없습니다. ID: {entry.Value}", this);
                    continue;
                }

                if (!_combatSession.State.UnitDataById.TryGetValue(
                    entry.Value,
                    out UnitData unitData))
                {
                    Debug.LogWarning($"[CombatUIController] 유닛 데이터가 없습니다. ID: {entry.Value}", this);
                    continue;
                }

                results.Add(new DpsResultData(
                    unitData.id,
                    unitData.name,
                    unitData.iconAddress,
                    unit.TotalDamageDealt / elapsedSeconds));
            }

            return results;
        }

        private static int CompareDpsResults(DpsResultData left, DpsResultData right)
        {
            int dpsComparison = right.Dps.CompareTo(left.Dps);
            return dpsComparison != 0 ? dpsComparison : left.UnitId.CompareTo(right.UnitId);
        }

        private readonly struct DpsResultData
        {
            public DpsResultData(int unitId, string unitName, string iconAddress, float dps)
            {
                UnitId = unitId;
                UnitName = unitName;
                IconAddress = iconAddress;
                Dps = dps;
            }

            public int UnitId { get; }
            public string UnitName { get; }
            public string IconAddress { get; }
            public float Dps { get; }
        }

        private void ShowRunResult()
        {
            if (_runResultView == null || _runResultContentView == null)
            {
                Debug.LogError("[CombatUIController] ResultCanvas 연결을 찾을 수 없습니다.", this);
                return;
            }

            battleUIView?.HideAllOverlayViews();
            _runResultContentView.ClearAllItems();
            _runResultContentView.CreateStatItem("지나간 턴", BoardRunData.DiceRollCount.ToString());
            _runResultContentView.CreateStatItem("움직인 칸", BoardRunData.MovedNodeCount.ToString());
            _runResultContentView.CreateStatItem("승리한 배틀", BoardRunData.VictoryBattleCount.ToString());

            CreateSynergyIcons(_combatSession?.ActiveSynergies);
            CreateUnitIcons(SystemBus.Get<PlayerFacade>()?.OwnedUnits);
            CreateRelicIcons(SystemBus.Get<RelicFacade>()?.OwnedRelics);

            _runResultContentView.RefreshLayout();
            bool wasActive = _runResultView.gameObject.activeSelf;
            _runResultView.gameObject.SetActive(true);
            if (wasActive)
            {
                _runResultView.Replay();
            }
        }

        private void CreateSynergyIcons(IReadOnlyList<SynergyData> synergies)
        {
            if (synergies == null)
            {
                return;
            }

            foreach (SynergyData synergy in synergies)
            {
                CreateIcon(RunResultContentView.IconSection.Synergies, synergy?.iconAddress);
            }
        }

        private void CreateUnitIcons(IReadOnlyList<UnitData> units)
        {
            if (units == null)
            {
                return;
            }

            foreach (UnitData unit in units)
            {
                CreateIcon(RunResultContentView.IconSection.Units, unit?.iconAddress);
            }
        }

        private void CreateRelicIcons(IReadOnlyList<RelicData> relics)
        {
            if (relics == null)
            {
                return;
            }

            foreach (RelicData relic in relics)
            {
                CreateIcon(RunResultContentView.IconSection.Relics, relic?.iconAddress);
            }
        }

        private void CreateIcon(RunResultContentView.IconSection section, string iconAddress)
        {
            ResultIconItemView item = _runResultContentView.CreateIconItem(section, null);
            if (item != null)
            {
                _ = item.SetIconAsync(iconAddress);
            }
        }

        private void HandleRunResultMainButtonClicked(RunResultAnimationView view)
        {
            combatSceneController?.ReturnToTitle();
        }

        #endregion
    }
}
