using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using OZGL.Map;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    public sealed class BoardSceneController : MonoBehaviour
    {
        [Header("보드 씬 연결")]
        [SerializeField] private BoardPlayerController _boardPlayerController;

        [Header("시간 시스템")]
        [SerializeField] private int _nightInterval = 3;

        // [수정] UI 컨트롤러가 방어막 역할을 하도록 직접 연결을 해제합니다.
        // [SerializeField] private Button _endTurnButton;

        [SerializeField] private NightEventPopupView _nightEventPopup;
        [SerializeField] private TimeStatusHUDView _timeStatusHud;

        [Header("전투 편성 확인")]
        [Tooltip("전투 시작 전 최소 편성 인원을 확인할 유닛 편성 컨트롤러입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private UnitFormationController _unitFormationController;

        [Header("유닛 획득 데이터")]
        [SerializeField] private UnitRosterData _unitRosterData;

        [Header("Event UI")]
        [SerializeField] private ChoiceEventManager _eventUIPanel;

        // ==================== 외부 시스템 통지용 이벤트 ====================

        public event Action<int> TurnEnded;
        public event Action<int> NightReached;

        private void Awake()
        {
            if (_boardPlayerController == null)
            {
                _boardPlayerController = FindFirstObjectByType<BoardPlayerController>();
            }

            if (_boardPlayerController == null)
            {
                Debug.LogError("[BoardSceneController] BoardPlayerController를 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }

            if(_eventUIPanel == null)
            {
                _eventUIPanel = FindFirstObjectByType<ChoiceEventManager>(FindObjectsInactive.Include);
                if (_eventUIPanel != null)
                {
                    _eventUIPanel.gameObject.SetActive(false);
                }
            }

            // [수정] 이제 버튼 클릭 처리는 BoardUIController가 전담합니다.
            /*
            if (_endTurnButton != null)
                _endTurnButton.onClick.AddListener(EndTurn);
            else
                Debug.LogError("[BoardSceneController] 턴 종료 버튼이 연결되지 않았습니다.", this);
            */

            if (_unitFormationController == null)
            {
                _unitFormationController = FindFirstObjectByType<UnitFormationController>(FindObjectsInactive.Include);
            }
        }

        private void Start() { UpdateTimeStatusHud(); }

        private void OnEnable()
        {
            if (_boardPlayerController != null)
                _boardPlayerController.PlayerArrived += HandlePlayerArrived;
        }

        private void OnDisable()
        {
            if (_boardPlayerController != null)
                _boardPlayerController.PlayerArrived -= HandlePlayerArrived;
        }

        public void EndTurn()
        {
            if (_boardPlayerController == null || !_boardPlayerController.EndTurn()) return;

            DiceManager.Instance.ResetTurnRoll();
            TurnEnded?.Invoke(BoardRunData.UnusedActionPoints);
            BoardRunData.AdvanceTurn();

            if (_nightInterval > 0 && BoardRunData.TurnCount % _nightInterval == 0)
                ShowNightEvent();

            UpdateTimeStatusHud();
        }

        private void UpdateTimeStatusHud()
        {
            if (_nightInterval <= 0) return;
            if (_timeStatusHud == null) _timeStatusHud = FindFirstObjectByType<TimeStatusHUDView>();
            if (_timeStatusHud == null) _timeStatusHud = new GameObject("TimeStatusHUD").AddComponent<TimeStatusHUDView>();

            int turnsUntilNight = _nightInterval - (BoardRunData.TurnCount % _nightInterval);
            _timeStatusHud.SetTurnsUntilNight(turnsUntilNight);
        }

        private void ShowNightEvent()
        {
            if (_nightEventPopup == null) _nightEventPopup = FindFirstObjectByType<NightEventPopupView>();
            if (_nightEventPopup == null) _nightEventPopup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>();

            _nightEventPopup.Show($"{BoardRunData.TurnCount}Turn, Night has arrived.\n(Undecided event)");
            NightReached?.Invoke(BoardRunData.TurnCount);
        }

        /// <summary>
        /// 전투 진입 전 최소 편성 인원(1명 이상)을 확인합니다.
        /// 편성 컨트롤러를 찾을 수 없으면 확인 없이 통과시킵니다.
        /// </summary>
        private bool EnsureCanStartBattle()
        {
            if (_unitFormationController == null || _unitFormationController.CanStartBattle)
            {
                return true;
            }

            if (_nightEventPopup == null)
            {
                _nightEventPopup = FindFirstObjectByType<NightEventPopupView>();
            }

            if (_nightEventPopup == null)
            {
                _nightEventPopup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>();
            }

            _nightEventPopup.Show("To start a battle, you must place at least one combat unit.");

            return false;
        }

        public void ReturnToTitle()
        {
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner)) return;
            Time.timeScale = 1f;
            BoardRunData.Clear();
            transitioner.LoadTitleScene();
        }

        private void HandlePlayerArrived(MapNode arrivedNode)
        {
            if (arrivedNode == null) return;
            BoardRunData.SavePlayerPosition(arrivedNode.Position);
            switch (arrivedNode.Type)
            {
                case NodeType.Battle: HandleBattleNode(arrivedNode, false); break;
                case NodeType.Elite: HandleBattleNode(arrivedNode, true); break;
                case NodeType.Boss: HandleBossNode(arrivedNode); break;
                case NodeType.Event:
                    if (_eventUIPanel != null)
                    {
                        _eventUIPanel.gameObject.SetActive(true);
                        _eventUIPanel.RandomEventOpenTest();
                    }
                    break;
                
                case NodeType.UnitAcquisition:
                    HandleUnitAcquisitionNode(arrivedNode);
                    break;
            }
        }

        private void HandleBattleNode(MapNode battleNode, bool isElite)
        {
            if (BoardRunData.IsBattleCompleted(battleNode.Position)) return;
            if (!EnsureCanStartBattle()) return;
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner)) return;

            BoardRunData.BeginBattle(battleNode.Position, false, isElite);
            transitioner.LoadCombatScene();
        }

        private void HandleBossNode(MapNode bossNode)
        {
            if (!EnsureCanStartBattle()) return;
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner)) return;

            BoardRunData.BeginBattle(bossNode.Position, true);
            transitioner.LoadCombatScene();
        }

        private bool TryGetSceneTransitioner(out SceneTransitioner transitioner)
        {
            transitioner = SceneTransitioner.Instance;
            return transitioner != null && !transitioner.IsTransitioning;
        }

        private void HandleUnitAcquisitionNode(MapNode unitNode)
        {
            // 맵 타일 1회 방문 체크(소모) 등 다른 규칙은 추후 적용합니다.
            // (현재 BoardRunData에 위치를 저장해서 중복 획득 여부를 확인할 수 있습니다.)
            if (_unitRosterData != null && _unitRosterData.UnitStats.Count > 0)
            {
                // 프리팹(SpriteAddress)이 세팅된 유닛만 필터링해서 뽑습니다. (테스트 용이성을 위해)
                System.Collections.Generic.List<UnitData> validUnits = new System.Collections.Generic.List<UnitData>();
                foreach (var u in _unitRosterData.UnitStats)
                {
                    if (!string.IsNullOrEmpty(u.spriteAddress))
                    {
                        validUnits.Add(u);
                    }
                }

                if (validUnits.Count == 0)
                {
                    Debug.LogWarning("[BoardSceneController] 프리팹(SpriteAddress)이 설정된 유닛이 로스터에 하나도 없습니다!");
                    validUnits = new System.Collections.Generic.List<UnitData>(_unitRosterData.UnitStats); // 전부 없으면 그냥 전체에서 뽑기
                }

                // 1. 유효한 유닛 중 무작위 하나를 뽑습니다.
                int randomIndex = UnityEngine.Random.Range(0, validUnits.Count);
                UnitData randomUnit = validUnits[randomIndex];
                
                // 2. 누락되는 스탯(color, skillType 등)이 없도록 완전한 복사본을 만듭니다.
                UnitData acquiredUnit = PlayerInventoryManager.CloneUnitData(randomUnit);
                
                // 3. 글로벌 인벤토리에 추가! (추후 로스터 패널과 연동됨)
                if (PlayerInventoryManager.Instance != null)
                {
                    PlayerInventoryManager.Instance.AddUnit(acquiredUnit);
                }

                // 4. 밤 이벤트용 범용 알림UI를 활용해서 획득 안내창을 띄웁니다.
                if (_nightEventPopup == null) _nightEventPopup = FindFirstObjectByType<NightEventPopupView>();
                if (_nightEventPopup == null) _nightEventPopup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>();
                _nightEventPopup.Show($"New Unit Acquired!\n[{acquiredUnit.name}]\n(Currently {PlayerInventoryManager.Instance.OwnedUnits.Count} units owned)");
            }
            else
            {
                Debug.LogWarning("[BoardSceneController] 인스펙터에 UnitRosterData가 할당되지 않아 유닛 획득이 불가능합니다!");
            }
        }
    }
}
