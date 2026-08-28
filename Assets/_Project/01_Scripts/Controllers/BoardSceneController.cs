using OzGameLab01.Data;
using OzGameLab01.Managers;
using OZGL.Map;
using UnityEngine;

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

        private void Awake()
        {
            // Inspector 연결이 누락된 테스트 상황을 위한 보조 검색
            if (_boardPlayerController == null)
            {
                _boardPlayerController = FindFirstObjectByType<BoardPlayerController>();
            }

            if (_boardPlayerController != null)
            {
                return;
            }

            Debug.LogError("[BoardSceneController] BoardPlayerController를 찾을 수 없습니다.", this);

            enabled = false;
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
            BoardRunData.SavePlayerPosition( arrivedNode.Position);

            switch (arrivedNode.Type)
            {
                case NodeType.Battle:
                    HandleBattleNode(arrivedNode);
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
        private void HandleBattleNode(MapNode battleNode)
        {
            if (BoardRunData.IsBattleCompleted(
                    battleNode.Position))
            {
                Debug.Log(
                    $"[BoardSceneController] 이미 완료한 전투 타일입니다. " +
                    $"Position: {battleNode.Position}", this);

                return;
            }

            if (!TryGetSceneTransitioner(
                    out SceneTransitioner transitioner))
            {
                return;
            }

            // 일반 전투 정보와 복귀 위치 저장
            BoardRunData.BeginBattle(battleNode.Position, false);

            Debug.Log(
                $"[BoardSceneController] 일반 전투 씬으로 이동합니다. " +
                $"Position: {battleNode.Position}", this);

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