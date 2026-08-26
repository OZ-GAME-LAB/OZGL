using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Combat;

namespace OzGameLab01.Managers
{
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance;
        public static int MapTileIndex = 0;

        public static int SwordLevel = 1;
        public static int BowLevel = 1;
        public static int StaffLevel = 1;

        public const float ExpPerLevel = 100f;

        public static float SwordExp = 0f;
        public static float BowExp = 0f;
        public static float StaffExp = 0f;

        public static float GetAllyExpRatio(Unit.SkillType skillType)
        {
            switch (skillType)
            {
                case Unit.SkillType.Warrior:
                    return SwordExp / ExpPerLevel;
                case Unit.SkillType.Archer:
                    return BowExp / ExpPerLevel;
                case Unit.SkillType.Mage:
                    return StaffExp / ExpPerLevel;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 지정한 클래스에 경험치를 더합니다. 누적치가 ExpPerLevel을 넘으면 레벨업하고 남은 경험치는 이월됩니다.
        /// 레벨업이 발생했으면 true를 반환합니다.
        /// </summary>
        public static bool AddExp(Unit.SkillType skillType, float amount)
        {
            switch (skillType)
            {
                case Unit.SkillType.Warrior:
                    return AddExpInternal(ref SwordExp, ref SwordLevel, amount);
                case Unit.SkillType.Archer:
                    return AddExpInternal(ref BowExp, ref BowLevel, amount);
                case Unit.SkillType.Mage:
                    return AddExpInternal(ref StaffExp, ref StaffLevel, amount);
                default:
                    return false;
            }
        }

        private static bool AddExpInternal(ref float exp, ref int level, float amount)
        {
            exp += amount;
            bool leveledUp = false;

            while (exp >= ExpPerLevel)
            {
                exp -= ExpPerLevel;
                level++;
                leveledUp = true;
            }

            return leveledUp;
        }

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
