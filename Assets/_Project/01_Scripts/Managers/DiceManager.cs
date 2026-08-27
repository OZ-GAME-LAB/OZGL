using UnityEngine;
using OzGameLab01.Controllers;

namespace OzGameLab01.Managers
{
    public class DiceManager : MonoBehaviour
    {
        public static DiceManager Instance { get; private set; }

        [Header("Dice Settings")]
        [Tooltip("주사위의 최소 눈금")]
        [SerializeField] private int _minDice = 1;
        [Tooltip("주사위의 최대 눈금")]
        [SerializeField] private int _maxDice = 6;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // UI 캔버스의 '주사위 굴리기 버튼'의 OnClick 이벤트에 이 함수를 연결하면 됩니다.
        public void RollDice()
        {
            if (BoardPlayerController.Instance == null)
            {
                Debug.LogError("[DiceManager] BoardPlayerController를 찾을 수 없습니다!");
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
            BoardPlayerController.Instance.SetDiceValue(result);

            // TODO: (선택) 3D 주사위가 굴러가는 연출이나 사운드(SoundManager 호출)를 여기에 추가합니다.
        }
    }
}