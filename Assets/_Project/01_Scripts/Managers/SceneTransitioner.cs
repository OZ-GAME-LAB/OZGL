using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OzGameLab01.Combat;
using OzGameLab01.Data;

namespace OzGameLab01.Managers
{
    public class SceneTransitioner : MonoBehaviour
    {
        public static SceneTransitioner Instance;
        public static int MapTileIndex = 0;

        /// <summary>
        /// UnitPlaceScene의 FormationManager가 채우는 배치 결과(인덱스 0-8, 3x3 row-major).
        /// CombatManager.SpawnAllies()가 이 데이터가 있으면 우선 사용하고, 비어 있으면 인스펙터 allyFormation으로 폴백한다.
        /// </summary>
        public static Unit[] AllyFormationSlots;

        /// <summary>
        /// ProtoBoardScene에서 CombatScene으로 진입하기 직전의 보드 위치(MapNode.Position).
        /// 전투 종료 후 ProtoBoardScene으로 복귀할 때 이 위치에 플레이어를 되돌려 놓는다.
        /// 기본값 (0,0)은 시작 노드 위치와 같아 별도 플래그 없이도 "복귀 위치 없음"과 자연히 일치한다.
        /// </summary>
        public static Vector2Int BoardReturnPosition;

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

        // ==================== 씬 전환 리팩터링 ====================

        /// <summary>
        /// 현재 씬 전환이 진행 중인지 나타냅니다.
        /// </summary>
        private bool _isTransitioning;

        /// <summary>
        /// 외부에서 현재 씬 전환 여부를 확인할 수 있습니다.
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

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

        // ==================== 씬 전환 기능 ====================
        /// <summary>
        /// 지정한 씬을 비동기로 불러옵니다.
        /// 씬 전환 중에는 추가 요청을 받지 않습니다.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            // 중복 씬 전환 요청 방지
            if (_isTransitioning)
            {
                Debug.LogWarning(
                    $"[SceneTransitioner] 씬 전환 중이므로 '{sceneName}' 요청을 건너뜁니다.", this);

                return;
            }

            // 빈 씬 이름 방지
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[SceneTransitioner] 씬 이름이 비어 있어 전환할 수 없습니다.", this);

                return;
            }

            // Scene List에 등록되지 않은 씬 요청 방지
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SceneTransitioner] '{sceneName}' 씬을 불러올 수 없습니다. " +
                    "Build Profiles의 Scene List 등록 여부를 확인해주세요.", this);

                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        // ==================== 공용 씬 이동 메서드 ====================

        /// <summary>
        /// 타이틀 씬으로 이동합니다.
        /// </summary>
        public void LoadTitleScene()
        {
            LoadScene(SceneNames.Title);
        }

        /// <summary>
        /// 보드 씬으로 이동합니다.
        /// </summary>
        public void LoadBoardScene()
        {
            LoadScene(SceneNames.Board);
        }

        /// <summary>
        /// 전투 씬으로 이동합니다.
        /// </summary>
        public void LoadCombatScene()
        {
            LoadScene(SceneNames.Combat);
        }

        /// <summary>
        /// 임시 보스 전투 씬으로 이동합니다.
        /// </summary>
        public void LoadBossScene()
        {
            LoadScene(SceneNames.Boss);
        }

        /// <summary>
        /// 결과 씬으로 이동합니다.
        /// </summary>
        public void LoadResultScene()
        {
            LoadScene(SceneNames.Result);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            // 씬 전환 시작
            _isTransitioning = true;

            string previousSceneName =
                SceneManager.GetActiveScene().name;

            Debug.Log(
                $"[SceneTransitioner] 씬 전환 시작 | " +
                $"{previousSceneName} → {sceneName}",
                this);

            // 화면을 어둡게 전환
            yield return Fade(0f, 1f);

            // 일시정지 상태가 다음 씬까지 이어지지 않도록 복구
            Time.timeScale = 1f;

            // 씬 비동기 로드 시작
            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(sceneName);

            if (loadOperation == null)
            {
                Debug.LogError(
                    $"[SceneTransitioner] '{sceneName}' 씬의 " +
                    "비동기 로드를 시작하지 못했습니다.",
                    this);

                yield return Fade(1f, 0f);

                _isTransitioning = false;
                yield break;
            }

            // 씬 로드 완료 대기
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            // 새 씬의 초기 콜백 실행을 위해 한 프레임 대기
            yield return null;

            // 화면을 다시 밝게 전환
            yield return Fade(1f, 0f);

            // 씬 전환 완료
            _isTransitioning = false;

            Debug.Log($"[SceneTransitioner] 씬 전환 완료 | {sceneName}", this);
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
                // elapsed += Time.deltaTime;
                // 일시정지 상태에서도 페이드가 진행되도록 실제 시간 사용
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration);
                fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(color.r, color.g, color.b, toAlpha);
        }

        private void OnDestroy()
        {
            // 현재 인스턴스가 제거될 때만 정적 참조 해제
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

}
