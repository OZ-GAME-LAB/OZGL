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

        // 추가된 부분: 외부(UI)에서 주사위를 굴렸는지 확인할 수 있도록 상태를 열어둡니다.
        public bool HasRolledThisTurn => _hasRolledThisTurn;

        public event System.Action<int> OnDiceRolled;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            base.Awake();
        }

        public void RollDice()
        {
            if (BoardPlayerController.Instance == null) return;

            if (_hasRolledThisTurn)
            {
                Debug.LogWarning("[DiceManager] 이번 턴에는 이미 주사위를 굴렸습니다. 턴을 종료해야 다시 굴릴 수 있습니다.");
                return;
            }

            if (BoardPlayerController.Instance.IsMoving || BoardPlayerController.Instance.CurrentDiceValue > 0)
            {
                Debug.LogWarning("[DiceManager] 아직 이전 주사위 값을 소모하지 않았거나 이동 중입니다.");
                return;
            }

            int result = Random.Range(_minDice, _maxDice + 1);
            Debug.Log($"[DiceManager] 주사위를 굴렸습니다! 눈금: {result}");

            BoardPlayerController.Instance.CurrentDiceValue = result;
            _hasRolledThisTurn = true;

            OnDiceRolled?.Invoke(result);
        }

        public void ResetTurnRoll()
        {
            _hasRolledThisTurn = false;
        }
    }
}