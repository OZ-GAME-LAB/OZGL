using System;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 전투 씬(03_Combat)의 핵심 로직을 담당합니다.
    /// UI 관련 처리는 BattleUIController로 위임하고, 승패 판정만 처리합니다.
    /// </summary>
    public sealed class CombatSceneController : MonoBehaviour
    {
        // 전투가 끝났을 때 UI 컨트롤러 쪽에 알려줄 이벤트
        public event Action<bool> OnBattleResolved;

        private bool _resolved;
        public bool IsResolved => _resolved;

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
                ResolveBattle(true);
            }
            else if (!allyAlive)
            {
                ResolveBattle(false);
            }
        }

        /// <summary>
        /// 승패를 판정하고 전투를 종료시킵니다.
        /// </summary>
        private void ResolveBattle(bool victory)
        {
            if (_resolved) return;

            _resolved = true;
            Time.timeScale = 0f;

            if (victory)
            {
                BoardRunData.CompleteCurrentBattle();
            }

            // UI 컨트롤러에게 결과창을 띄우라고 신호를 보냅니다.
            OnBattleResolved?.Invoke(victory);
        }

        /// <summary>
        /// 항복했거나 결과창에서 확인을 누르면 보드 씬으로 이동합니다.
        /// </summary>
        public void ReturnToBoard()
        {
            SceneTransitioner transitioner = SceneTransitioner.Instance;

            if (transitioner == null)
            {
                Debug.LogError("[CombatSceneController] SceneTransitioner를 찾을 수 없습니다.", this);
                return;
            }

            if (transitioner.IsTransitioning)
            {
                Debug.LogWarning("[CombatSceneController] 이미 씬 전환 중입니다.", this);
                return;
            }

            Time.timeScale = 1f;
            transitioner.LoadBoardScene();
        }
    }
}