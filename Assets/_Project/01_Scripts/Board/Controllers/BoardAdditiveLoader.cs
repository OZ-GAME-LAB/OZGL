using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using OzGameLab01.Data; // SceneNames 상수를 가져오기 위함

namespace OzGameLab01.Map // Map 파트 네임스페이스
{
    /// <summary>
    /// 보드 맵 상태를 유지한 채 전투 씬을 화면 위에 덧씌우는 Additive 로더입니다.
    /// </summary>
    public class BoardAdditiveLoader : MonoBehaviour
    {
        public static BoardAdditiveLoader Instance { get; private set; }

        [Header("보드 씬 환경 제어")]
        [Tooltip("배틀 씬 로드 시 카메라/AudioListener 충돌을 막기 위해 잠시 꺼둘 보드 맵의 최상위 오브젝트")]
        [SerializeField] private GameObject _boardEnvironmentRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// 배틀 타일에 닿았을 때 전투 씬을 Additive로 엽니다.
        /// </summary>
        public void LoadCombatAdditive()
        {
            StartCoroutine(LoadAdditiveRoutine(SceneNames.Combat)); // "ProtoCombatScene"
        }

        /// <summary>
        /// 전투가 끝나고 다시 보드로 복귀할 때 호출합니다.
        /// </summary>
        public void UnloadCombatAdditive()
        {
            StartCoroutine(UnloadAdditiveRoutine(SceneNames.Combat));
        }

        private IEnumerator LoadAdditiveRoutine(string sceneName)
        {
            // 1. 보드 맵의 카메라 및 환경 비활성화 (충돌 방지)
            if (_boardEnvironmentRoot != null)
            {
                _boardEnvironmentRoot.SetActive(false);
            }

            // 2. 비동기로 Additive 씬 로드
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // 3. 새로 로드된 전투 씬을 Active Scene으로 설정
            // (이후 전투 씬에서 새로 생성되는 오브젝트가 보드 씬에 섞이지 않도록 격리)
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

            Debug.Log($"[BoardAdditiveLoader] {sceneName} Additive 로드 완료!");
        }

        private IEnumerator UnloadAdditiveRoutine(string sceneName)
        {
            // 1. 전투 씬 비동기 언로드 (제거)
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
            while (!asyncUnload.isDone)
            {
                yield return null;
            }

            // 2. 본래 씬(보드 씬)을 다시 Active Scene으로 원복
            SceneManager.SetActiveScene(gameObject.scene);

            // 3. 꺼두었던 보드 맵 환경(카메라 등) 재활성화
            if (_boardEnvironmentRoot != null)
            {
                _boardEnvironmentRoot.SetActive(true);
            }

            Debug.Log($"[BoardAdditiveLoader] {sceneName} 언로드 완료, 보드 맵 복귀!");
        }
    }
}