#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/*
 * [개발의 편의성 툴 - 강제 부팅 매니저] 
 * 역할: 어떤 씬에서 작업 중이든, 플레이 버튼을 누르면 무조건 00_Boot 씬 부터 시작하게 만들어 줍니다. 
 * 주의 : 이 스크립트는 꼬옥 01_Scripts -> Editor 이라는 폴더 안에 있어야 한다.
 */
public class BootSceneLauncher : EditorWindow
{
    private const string BOOT_SCENE_NAME = "00_Boot";
    private Vector2 _scrollPos;

    [MenuItem("Tools/ 항상 Boot 씬에서 시작하기 (토글)")]
    public  static void TogglePlayModeStartScene()
    {
        // 1. 프로젝트 전체 에서 "00_Boot" 라는 이름의 씬(Scene) 파일을 검색합니다.
        string[] guids = AssetDatabase.FindAssets(BOOT_SCENE_NAME + " t:Scene");

        if(guids.Length == 0)
        {
            Debug.LogError($"{BOOT_SCENE_NAME} 씬을 찾을 수 없습니다! 씬 이름이 정확한지 확인해 주세요");
            return;
        }

        // 2. 찾은 씬의 실제 파일 경로를 가져옵니다.
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

        if (bootScene != null)
        {
            // 3. 이미 강제 부팅이 '켜져' 있다면 -> '끄기'
            if(EditorSceneManager.playModeStartScene != null)
            {
                EditorSceneManager.playModeStartScene = null;
                Debug.Log("[에디터] '항상 Boot 씬에서 시작' 기능을 [껐습니다]. 이제 현재 열려있는 씬에서 바로 시작합니다.");
            }
            // 4. 강제 부팅이 '꺼져' 있다면 -> '켜기' (Boot 씬 강제 지정)
            else
            {
                EditorSceneManager.playModeStartScene = bootScene;
                Debug.Log($"[에디터] '항상 Boot 씬에서 시작' 기능을 [켰습니다]. 이제부터 무조건 {BOOT_SCENE_NAME}부터 시작합니다.");
            }
        }
        
    }

    //===============================================
    // [기능] 씬 이동 컨트롤러 (Build Settings 연동)
    //===============================================

    [MenuItem("Tools/씬 이동 컨트롤러 열기")]
    public static void ShowWindow()
    {
        BootSceneLauncher window = GetWindow<BootSceneLauncher>("씬 컨트롤러");
        window.minSize = new Vector2(250, 400);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(20);
        // 1. 강제 부팅 토글 버튼
        GUILayout.Label(" 강제 부팅 설정", EditorStyles.boldLabel);

        bool isBootSceneForced = EditorSceneManager.playModeStartScene != null;
        string statusText = isBootSceneForced ? "상태: [ON] 무조건 Boot 씬부터 켜짐" : "상태: [OFF] 현재 씬부터 켜짐";

        // 기존 색상 저장
        Color oldBgColor = GUI.backgroundColor;
        Color oldContentColor = GUI.contentColor;

        // 배경은 ON일 땐 초록색, OFF일 땐 밝은 회색
        GUI.backgroundColor = isBootSceneForced ? new Color(0.4f, 1.0f, 0.4f) : new Color(0.9f, 0, 9f, 0.9f);
        // 글씨를 무조건 겁은색으로 고정!
        GUI.contentColor = Color.black;

        // 글씨를 굵게 만들기 위해 스타일 지정
        GUIStyle toggleStyle = new GUIStyle(GUI.skin.button);
        toggleStyle.fontStyle = FontStyle.Bold;

        if(GUILayout.Button(statusText, GUILayout.Height(30)))
        {
            TogglePlayModeStartScene();

            GUI.backgroundColor = oldBgColor;
            GUI.contentColor = oldContentColor;

            // 안전 장치 : 상태가 바뀌면 UI 갱신을 위해 현재 프레임 랜더링 즉시 종료
            GUIUtility.ExitGUI();
        }

        // 원상 복구
        GUI.backgroundColor = oldBgColor;
        GUI.contentColor = oldContentColor;

        GUILayout.Space(10);
        DrawLine();
        GUILayout.Space(10);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        // 2. 정식 씬 리스트 
        GUILayout.Label("⭐ 정식 씬 (Build Setting)", EditorStyles.boldLabel);

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        HashSet<string> buildScenePaths = new HashSet<string>();      // 나중에 테스트 씬 걸러내기 위한 명부
        
        if(buildScenes.Length == 0)
        {
            GUILayout.Label("Build Setting에 등록된 씬이 없습니다.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            foreach(var scene in buildScenes)
            {
                string scenePath = scene.path;
                if (string.IsNullOrEmpty(scenePath)) continue;

                buildScenePaths.Add(scenePath);     // 명부에 등록
                DrawSceneButton(scenePath, true, scene.enabled);
            }
        }

        GUILayout.Space(15);

        // 3. 테스트 씬 리스트 (특정 테스트 폴더만 탐색)

        GUILayout.Label("테스트 씬 (TestScene 폴더)", EditorStyles.boldLabel);
        GUILayout.Label("팀원들이 각자 만들고 있는 임시 씬들입니다.", EditorStyles.helpBox);

        // 프로젝트 내의 모든 씬 파일(.unity)을 찾습니다.
        string[] allSceneGuids = AssetDatabase.FindAssets("t:Scene");
        bool hasTestScene = false;

        // 팀원 분들의 테스트 씬들이 들어 있는 폴더 위치
        string testSceneFolderPath = "Assets/_Project/03_Scenes/TestScenes";

        foreach(string guid in allSceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);

            // 지정된 테스트 폴더 안에 있으면서, Build Settings에 등록이 되지 않은 씬들만 걸러냅니다.
            if(scenePath.StartsWith(testSceneFolderPath) && !buildScenePaths.Contains(scenePath))
            {
                hasTestScene = true;
                DrawSceneButton(scenePath, false, true);
            }
        }

        if (!hasTestScene)
        {
            GUILayout.Label("현재 생성된 테스트 씬입니다.", EditorStyles.centeredGreyMiniLabel);
        }

        GUILayout.EndScrollView();
    }
    /// <summary>
    ///  씬 이동 버튼 예쁘게  그려주는 헬퍼 합수
    /// </summary>
    private void DrawSceneButton(string scenePath, bool isBuildScene, bool isEnabled)
    {
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        bool isCurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scenePath;

        // 버튼 색상 : 현재 씬(하늘색) / 활성화된 정식 씬 (흰색) / 비활성 및 테스트 씬(연한 회색)
        GUI.color = isCurrentScene ? Color.cyan : (isEnabled ? Color.white : new Color(0.8f, 0.8f, 0.8f));

        // 아이콘 달아주기
        string prefix = isCurrentScene ? "▶ " : (isBuildScene ? "📁 " : "🔨 ");
        string buttonText = prefix + sceneName;

        // 버튼 생성 및 클릭 처리
        if (GUILayout.Button(buttonText, GUILayout.Height(25)))
        {
            if (isCurrentScene) return; // 이미 열려있으면 무시

            // 수정된 내용이 있으면 저장할지 팝업을 띄워줌 (안전장치)
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }

            // 버튼 클릭으로 다이얼로그가 뜨거나 씬이 전환 될 때
            // 유니티 GUI 렌더링 사이클이 꼬이는 것을 방지하기 위해 즉시 탈출합니다.
            GUIUtility.ExitGUI();
        }
        GUI.color = Color.white;
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1f));
    }
}
#endif