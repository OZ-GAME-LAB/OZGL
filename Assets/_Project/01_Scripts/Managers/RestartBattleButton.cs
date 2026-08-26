using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Combat
{
    public class RestartBattleButton : MonoBehaviour
    {
        private void Awake()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(Restart);
            }
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
