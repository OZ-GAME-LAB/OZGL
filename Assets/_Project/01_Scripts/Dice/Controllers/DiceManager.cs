using UnityEngine;
using OzGameLab01.Dice;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// Dice 시스템의 Unity 라이프사이클(싱글톤 초기화, Facade/State 배선)만 담당합니다.
    /// 외부 호출은 Facade가, 내부 상태/로직은 State가 각각 전담합니다.
    /// </summary>
    public class DiceManager : Singleton<DiceManager>
    {
        [Header("Dice Settings")]
        [Tooltip("주사위의 최소 눈금")]
        [SerializeField] private int _minDice = 1;
        [Tooltip("주사위의 최대 눈금")]
        [SerializeField] private int _maxDice = 6;

        public DiceFacade Facade { get; private set; }

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            base.Awake();
            Facade = new DiceFacade(new DiceState(), _minDice, _maxDice);
        }
    }
}
