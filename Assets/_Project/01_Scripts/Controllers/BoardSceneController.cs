using System;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using OzGameLab01.UI;
using OZGL.Map;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 보드 씬의 공통 흐름을 관리합니다.
    ///
    /// BoardPlayerController로부터 플레이어 도착 정보를 받아
    /// 위치 저장, 일반 전투, 보스 전투 씬 전환을 처리합니다.
    ///
    /// 플레이어 이동과 맵 생성은 직접 관리하지 않습니다.
    /// </summary>
    public sealed class BoardSceneController : MonoBehaviour
    {
        [Header("보드 씬 연결")]
        [Tooltip("플레이어 이동 완료 이벤트를 전달할 BoardPlayerController입니다.")]
        [SerializeField] private BoardPlayerController _boardPlayerController;

        [Header("시간 시스템")]
        [Tooltip("몇 턴마다 밤 시간이 되는지 설정합니다. (그레이박스 기본값: 3턴)")]
        [SerializeField] private int _nightInterval = 3;
        [Tooltip("행동력을 모두 소모한 뒤 눌러야 턴이 종료되는 버튼입니다.")]
        [SerializeField] private Button _endTurnButton;
        [Tooltip("밤 시간 도달을 알리는 팝업입니다. 비워두면 씬에서 자동으로 찾거나 새로 생성합니다.")]
        [SerializeField] private NightEventPopupView _nightEventPopup;
        [Tooltip("\"밤까지 N턴\"을 상시 표시하는 HUD입니다. 비워두면 씬에서 자동으로 찾거나 새로 생성합니다.")]
        [SerializeField] private TimeStatusHUDView _timeStatusHud;

        // ==================== 외부 시스템 통지용 이벤트 ====================

        /// <summary>
        /// 턴이 실제로 종료되었을 때 발생합니다. 종료 시점에 남아 있던 행동력을 전달합니다.
        /// 유닛 회복 등 실제 효과는 아직 기획되지 않아, 여기서는 이벤트 발생까지만 담당합니다.
        /// </summary>
        public event Action<int> TurnEnded;

        /// <summary>
        /// 밤 시간에 도달했을 때 발생합니다. 도달 시점의 누적 턴 수를 전달합니다.
        /// 실제 밤 이벤트 효과는 아직 기획되지 않아, 팝업 알림과 별개로 이벤트만 발생시킵니다.
        /// </summary>
        public event Action<int> NightReached;

        private void Awake()
        {
            // Inspector 연결이 누락된 테스트 상황을 위한 보조 검색
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

            if (_endTurnButton != null)
            {
                _endTurnButton.onClick.AddListener(EndTurn);
            }
            else
            {
                Debug.LogError("[BoardSceneController] 턴 종료 버튼이 연결되지 않았습니다.", this);
            }
        }

        private void Start()
        {
            UpdateTimeStatusHud();
        }

        private void OnEnable()
        {
            if (_boardPlayerController == null)
            {
                return;
            }

            _boardPlayerController.PlayerArrived += HandlePlayerArrived;
        }

        private void OnDisable()
        {
            if (_boardPlayerController == null)
            {
                return;
            }

            _boardPlayerController.PlayerArrived -= HandlePlayerArrived;
        }

        /// <summary>
        /// "턴 종료" 버튼에서 호출합니다.
        /// 행동력을 모두 소모한 것만으로는 턴이 끝나지 않고,
        /// 이 버튼을 눌러야 비로소 한 턴이 지나갑니다.
        ///
        /// 남은 행동력 저장은 BoardPlayerController.EndTurn()이 담당하며,
        /// 이동 중이라 턴을 끝낼 수 없는 경우 여기서도 진행을 멈춥니다.
        /// </summary>
        public void EndTurn()
        {
            if (_boardPlayerController == null || !_boardPlayerController.EndTurn())
            {
                return;
            }

            DiceManager.Instance.ResetTurnRoll();

            TurnEnded?.Invoke(BoardRunData.UnusedActionPoints);

            BoardRunData.AdvanceTurn();

            if (_nightInterval > 0 && BoardRunData.TurnCount % _nightInterval == 0)
            {
                ShowNightEvent();
            }

            UpdateTimeStatusHud();
        }

        /// <summary>
        /// "밤까지 N턴" HUD 표시를 최신 턴 수 기준으로 갱신합니다.
        /// </summary>
        private void UpdateTimeStatusHud()
        {
            if (_nightInterval <= 0)
            {
                return;
            }

            if (_timeStatusHud == null)
            {
                _timeStatusHud = FindFirstObjectByType<TimeStatusHUDView>();
            }

            if (_timeStatusHud == null)
            {
                _timeStatusHud = new GameObject("TimeStatusHUD").AddComponent<TimeStatusHUDView>();
            }

            int turnsUntilNight = _nightInterval - (BoardRunData.TurnCount % _nightInterval);
            _timeStatusHud.SetTurnsUntilNight(turnsUntilNight);
        }

        /// <summary>
        /// 밤 시간 도달을 팝업으로 알립니다.
        ///
        /// 밤에 발생하는 실제 효과는 아직 기획되지 않아
        /// 그레이박스 단계에서는 알림 팝업만 표시합니다.
        /// </summary>
        private void ShowNightEvent()
        {
            if (_nightEventPopup == null)
            {
                _nightEventPopup = FindFirstObjectByType<NightEventPopupView>();
            }

            if (_nightEventPopup == null)
            {
                _nightEventPopup = new GameObject("NightEventPopup").AddComponent<NightEventPopupView>();
            }

            Debug.Log(
                $"[BoardSceneController] 밤 시간 도달. TurnCount: {BoardRunData.TurnCount}", this);

            _nightEventPopup.Show(
                $"{BoardRunData.TurnCount}턴째, 밤이 되었습니다.\n(발생 이벤트 미정 - 추후 구현)");

            NightReached?.Invoke(BoardRunData.TurnCount);
        }

        /// <summary>
        /// 뒤로가기 버튼을 통해 타이틀 씬으로 이동합니다.
        /// 보드에서 타이틀로 돌아가는 것은 현재 게임 포기로 판단하여
        /// 저장된 보드 진행 데이터를 초기화합니다.
        /// </summary>
        public void ReturnToTitle()
        {
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner))
            {
                return;
            }

            // 일시정지 상태가 남아 있을 가능성을 방지
            Time.timeScale = 1f;

            // 현재 게임 진행 포기 처리
            BoardRunData.Clear();

            Debug.Log(
                "[BoardSceneController] 보드 진행 데이터를 초기화하고 " +
                "타이틀 씬으로 이동합니다.", this);

            transitioner.LoadTitleScene();
        }

        /// <summary>
        /// 플레이어가 이동을 완료했을 때 도착한 타일을 처리합니다.
        /// </summary>
        private void HandlePlayerArrived(MapNode arrivedNode)
        {
            if (arrivedNode == null)
            {
                Debug.LogError(
                    "[BoardSceneController] 도착한 MapNode가 null입니다.", this);

                return;
            }

            // 모든 타일에서 마지막 도착 위치 저장
            BoardRunData.SavePlayerPosition(arrivedNode.Position);

            switch (arrivedNode.Type)
            {
                case NodeType.Battle:
                    HandleBattleNode(arrivedNode, false); // 일반 전투
                    break;
                case NodeType.Elite:
                    HandleBattleNode(arrivedNode, true);  // 엘리트 전투로 전달!
                    break;
                case NodeType.Boss:
                    HandleBossNode(arrivedNode);
                    break;
                default:
                    Debug.Log(
                      $"[BoardSceneController] 타일 도착 | " +
                      $"Position: {arrivedNode.Position}, " +
                      $"Type: {arrivedNode.Type}",
                      this);
                    break;
            }
        }


        /// <summary>
        /// 일반 전투 타일 도착을 처리합니다.
        /// 이미 완료한 전투 타일이라면 다시 전투를 시작하지 않습니다.
        /// </summary>
        private void HandleBattleNode(MapNode battleNode, bool isElite)
        {
            if (BoardRunData.IsBattleCompleted(battleNode.Position))
            {
                Debug.Log($"[BoardSceneController] 이미 완료한 전투 타일입니다. " + $"Position: {battleNode.Position}", this);
                return;
            }

            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner)) return;
            // BeginBattle 호출 시 isElite 값을 넘겨줌
            BoardRunData.BeginBattle(battleNode.Position, false, isElite);

            Debug.Log($"[BoardSceneController] 일반 전투 씬으로 이동합니다. " + $"Position: {battleNode.Position}", this);

            transitioner.LoadCombatScene();
        }

        /// <summary>
        /// 보스 타일 도착을 처리합니다.
        /// 보스전 결과는 보드가 아닌 Result 씬으로 이어집니다.
        /// </summary>
        private void HandleBossNode(MapNode bossNode)
        {
            if (!TryGetSceneTransitioner(out SceneTransitioner transitioner))
            {
                return;
            }

            // 보스전 정보와 마지막 보드 위치 저장
            BoardRunData.BeginBattle(bossNode.Position, true);

            Debug.Log(
                $"[BoardSceneController] 보스 전투 씬으로 이동합니다. " +
                $"Position: {bossNode.Position}", this);

            transitioner.LoadBossScene();
        }

        /// <summary>
        /// 유지 중인 SceneTransitioner를 가져옵니다.
        /// </summary>
        private bool TryGetSceneTransitioner(
            out SceneTransitioner transitioner)
        {
            transitioner = SceneTransitioner.Instance;

            if (transitioner == null)
            {
                Debug.LogError(
                    "[BoardSceneController] SceneTransitioner가 없어 씬 전환 요청을 처리할 수 없습니다. " +
                    "00_Boot 씬부터 실행했는지 확인해주세요.", this);

                return false;
            }

            if (transitioner.IsTransitioning)
            {
                Debug.LogWarning("[BoardSceneController] 이미 씬 전환이 진행 중입니다.", this);

                return false;
            }

            return true;
        }
    }
}