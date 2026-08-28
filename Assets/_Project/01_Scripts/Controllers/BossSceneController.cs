using OzGameLab01.Data;
using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 임시 보스 전투 씬의 결과 처리를 담당합니다.
    ///
    /// 현재는 실제 보스 전투 대신 승리와 패배 버튼으로 결과를 선택하며,
    /// 두 결과 모두 진행 데이터를 초기화한 후 타이틀 씬으로 이동합니다.
    /// </summary>
    public sealed class BossSceneController : MonoBehaviour
    {
        /// <summary>
        /// 임시 보스전 승리 버튼에서 호출합니다.
        /// </summary>
        public void HandleVictory()
        {
            FinishBossBattle("승리");
        }

        /// <summary>
        /// 임시 보스전 패배 버튼에서 호출합니다.
        /// </summary>
        public void HandleDefeat()
        {
            FinishBossBattle("패배");
        }

        /// <summary>
        /// 보스전을 종료하고 게임 진행 데이터를 초기화한 뒤
        /// 타이틀 씬으로 이동합니다.
        /// </summary>
        private void FinishBossBattle(string result)
        {
            SceneTransitioner transitioner =
                SceneTransitioner.Instance;

            if (transitioner == null)
            {
                Debug.LogError(
                    "[BossSceneController] SceneTransitioner가 없어 타이틀 씬으로 이동할 수 없습니다. " +
                    "00_Boot 씬부터 실행했는지 확인해주세요.", this);

                return;
            }

            if (transitioner.IsTransitioning)
            {
                Debug.LogWarning("[BossSceneController] 이미 씬 전환이 진행 중입니다.", this);

                return;
            }

            Time.timeScale = 1f;

            Debug.Log($"[BossSceneController] 보스전 {result} | 게임 진행 데이터를 초기화하고 타이틀로 이동합니다.", this);

            // 보스전 종료 후 현재 게임 진행 데이터 전체 초기화
            BoardRunData.Clear();

            transitioner.LoadTitleScene();
        }
    }
}