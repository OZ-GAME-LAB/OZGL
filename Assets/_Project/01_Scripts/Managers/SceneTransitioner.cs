using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OzGameLab01.Controllers;

namespace OzGameLab01.Managers
{
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance;
        public static int MapTileIndex = 0;

        public static int SwordLevel = 1;
        public static int BowLevel = 1;
        public static int StaffLevel = 1;

        public static int GetAllyLevel(Unit.SkillType skillType)
        {
            switch (skillType)
            {
                case Unit.SkillType.Warrior:
                    return SwordLevel;
                case Unit.SkillType.Archer:
                    return BowLevel;
                case Unit.SkillType.Mage:
                    return StaffLevel;
                default:
                    return 1;
            }
        }

        public static void LevelUp(Unit.SkillType skillType)
        {
            switch (skillType)
            {
                case Unit.SkillType.Warrior:
                    SwordLevel++;
                    break;
                case Unit.SkillType.Archer:
                    BowLevel++;
                    break;
                case Unit.SkillType.Mage:
                    StaffLevel++;
                    break;
            }
        }

        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.3f;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(Fade(1f, 0f));
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(sceneName);
            yield return null;
            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha)
        {
            if (fadeImage == null)
            {
                yield break;
            }

            float elapsed = 0f;
            Color color = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration);
                fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(color.r, color.g, color.b, toAlpha);
        }
    }
}
