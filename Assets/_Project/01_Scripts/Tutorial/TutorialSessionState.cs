using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// Tutorial 씬과 Combat 씬 사이에서만 유지되는 튜토리얼 런타임 상태입니다.
    /// 영구 완료 기록과 분리하여 새 튜토리얼을 시작하거나 플레이 세션이
    /// 초기화되면 함께 초기화됩니다.
    /// </summary>
    public static class TutorialSessionState
    {
        private static int boardNextStepIndex;
        private static bool boardResumePending;

        public static bool IsActive { get; private set; }
        public static bool IsCombatTutorialCompleted { get; private set; }
        public static int BoardNextStepIndex => boardNextStepIndex;

        public static void BeginNewSession()
        {
            IsActive = true;
            IsCombatTutorialCompleted = false;
            boardNextStepIndex = 0;
            boardResumePending = false;
        }

        public static void RecordBoardProgress(int nextStepIndex)
        {
            if (!IsActive)
                return;

            boardNextStepIndex = Mathf.Max(0, nextStepIndex);
        }

        public static void PrepareCombat(int nextStepIndex)
        {
            IsActive = true;
            boardNextStepIndex = Mathf.Max(0, nextStepIndex);
            boardResumePending = false;
        }

        public static void MarkReturningFromCombat()
        {
            if (IsActive)
                boardResumePending = true;
        }

        public static bool TryConsumeBoardResume(out int nextStepIndex)
        {
            nextStepIndex = boardNextStepIndex;
            if (!IsActive || !boardResumePending)
                return false;

            boardResumePending = false;
            return true;
        }

        public static void MarkCombatTutorialCompleted()
        {
            if (IsActive)
                IsCombatTutorialCompleted = true;
        }

        public static void ResetCombatTutorial()
        {
            IsCombatTutorialCompleted = false;
        }

        public static void EndSession()
        {
            IsActive = false;
            IsCombatTutorialCompleted = false;
            boardNextStepIndex = 0;
            boardResumePending = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart()
        {
            EndSession();
        }
    }
}
