using OzGameLab01.Controllers;
using UnityEngine;

namespace OzGameLab01.Dice
{
    /// <summary>
    /// Dice 시스템 외부(Board 등)가 호출하는 유일한 진입점입니다.
    /// Unity 라이프사이클은 DiceManager가, 내부 상태는 DiceState가 각각 전담합니다.
    /// </summary>
    public class DiceFacade
    {
        private readonly DiceState _state;
        private readonly int _minDice;
        private readonly int _maxDice;

        public bool HasRolledThisTurn => _state.HasRolledThisTurn;

        public event System.Action<int> OnDiceRolled;

        public DiceFacade(DiceState state, int minDice, int maxDice)
        {
            _state = state;
            _minDice = minDice;
            _maxDice = maxDice;
        }

        public void RollDice()
        {
            if (BoardPlayerController.Instance == null) return;

            if (_state.HasRolledThisTurn)
            {
                Debug.LogWarning("[DiceFacade] 이번 턴에는 이미 주사위를 굴렸습니다. 턴을 종료해야 다시 굴릴 수 있습니다.");
                return;
            }

            if (BoardPlayerController.Instance.IsMoving || BoardPlayerController.Instance.CurrentDiceValue > 0)
            {
                Debug.LogWarning("[DiceFacade] 아직 이전 주사위 값을 소모하지 않았거나 이동 중입니다.");
                return;
            }

            int result = Random.Range(_minDice, _maxDice + 1);
            Debug.Log($"[DiceFacade] 주사위를 굴렸습니다! 눈금: {result}");

            BoardPlayerController.Instance.CurrentDiceValue = result;
            _state.MarkRolled();

            OnDiceRolled?.Invoke(result);
        }

        public void ResetTurnRoll()
        {
            _state.ResetTurn();
        }

        public void ResetRunState()
        {
            ResetTurnRoll();
            OnDiceRolled = null;
        }
    }
}
