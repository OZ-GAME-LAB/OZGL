using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OzGameLab01.Managers;
using OzGameLab01.UI;

namespace Combat
{
    public class BattleResultChecker : MonoBehaviour
    {
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button continueButton;
        [SerializeField] private BattleRewardPanel battleRewardPanel;

        private bool _resolved;

        private void Awake()
        {
            resultPanel.SetActive(false);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void Update()
        {
            if (_resolved)
            {
                return;
            }

            bool allyAlive = false;
            bool enemyAlive = false;

            foreach (Unit unit in Unit.All)
            {
                if (unit == null || unit.IsDead)
                {
                    continue;
                }

                if (unit.TeamValue == Unit.Team.Ally)
                {
                    allyAlive = true;
                }
                else if (unit.TeamValue == Unit.Team.Enemy)
                {
                    enemyAlive = true;
                }
            }

            if (!enemyAlive)
            {
                _resolved = true;
                Time.timeScale = 0f;

                if (battleRewardPanel != null && CombatManager.Instance != null)
                {
                    // BattleRewardPanel이 자체 배경/제목을 갖춘 독립 팝업이라 ResultPanel은 승리 시 띄우지 않음
                    battleRewardPanel.Show(CombatManager.Instance.GetParticipatingAllyUnits(), OnContinueClicked);
                }
                else
                {
                    resultText.text = "승리!";
                    resultPanel.SetActive(true);
                }
            }
            else if (!allyAlive)
            {
                _resolved = true;
                resultText.text = "패배...";
                resultPanel.SetActive(true);
                Time.timeScale = 0f;
            }
        }

        private void OnContinueClicked()
        {
            Time.timeScale = 1f;

            if (SceneTransitioner.Instance != null)
            {
                SceneTransitioner.Instance.LoadScene("ProtoBoardScene");
            }
            else
            {
                SceneManager.LoadScene("ProtoBoardScene");
            }
        }
    }
}
