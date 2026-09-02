using UnityEngine;
using OzGameLab01.Controllers;

namespace OzGameLab01.Managers
{
    public class DiceManager : Singleton<DiceManager>
    {
        [Header("Dice Settings")]
        [Tooltip("주사위의 최소 눈금")]
        [SerializeField] private int _minDice = 1;
        [Tooltip("주사위의 최대 눈금")]
        [SerializeField] private int _maxDice = 6;

        private bool _hasRolledThisTurn;

        protected override void Awake()
        {
            // Singleton<T>는 DontDestroyOnLoad만 처리하고 중복 인스턴스를 파괴하지 않으므로,
            // 보드 씬이 재로드되며 새로 배치된 인스턴스를 여기서 직접 정리합니다.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();
        }

        // UI 캔버스의 '주사위 굴리기 버튼'의 OnClick 이벤트에 이 함수를 연결하면 됩니다.
        public void RollDice()
        {
            if (BoardPlayerController.Instance == null)
            {
                Debug.LogError("[DiceManager] BoardPlayerController를 찾을 수 없습니다!");
                return;
            }

            // 한 턴에 주사위는 한 번만 굴릴 수 있습니다.
            if (_hasRolledThisTurn)
            {
                Debug.LogWarning("[DiceManager] 이번 턴에는 이미 주사위를 굴렸습니다. 턴을 종료해야 다시 굴릴 수 있습니다.");
                return;
            }

            // 플레이어가 이미 이동 중이거나, 굴려놓은 주사위 눈금을 아직 안 썼다면 무시
            if (BoardPlayerController.Instance.IsMoving || BoardPlayerController.Instance.CurrentDiceValue > 0)
            {
                Debug.LogWarning("[DiceManager] 아직 이전 주사위 값을 소모하지 않았거나 이동 중입니다.");
                return;
            }

            // 랜덤 값 추출 (Random.Range에서 int를 쓸 때 최대값은 포함되지 않으므로 +1을 해줍니다)
            int result = Random.Range(_minDice, _maxDice + 1);
            Debug.Log($"[DiceManager] 주사위를 굴렸습니다! 눈금: {result}");

            // 추출된 값을 플레이어 컨트롤러에 전달하여 이동 가능 상태로 전환
            BoardPlayerController.Instance.CurrentDiceValue = result;

            _hasRolledThisTurn = true;

            // TODO: (선택) 3D 주사위가 굴러가는 연출이나 사운드(SoundManager 호출)를 여기에 추가합니다.
        }

        /// <summary>
        /// 턴 종료 시 호출합니다. 다음 턴에 다시 주사위를 굴릴 수 있도록 허용합니다.
        /// </summary>
        public void ResetTurnRoll()
        {
            _hasRolledThisTurn = false;
        }
    }
}
