using UnityEngine;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 튜토리얼 시퀀스 완료 여부를 PlayerPrefs에 기록합니다.
    /// </summary>
    public static class TutorialProgress
    {
        private const string CompletedKey = "Tutorial.Completed";

        public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

        public static bool IsCompletedFor(string progressKey)
        {
            return !string.IsNullOrWhiteSpace(progressKey) &&
                   PlayerPrefs.GetInt(progressKey, 0) == 1;
        }

        public static void MarkCompleted()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void MarkCompletedFor(string progressKey)
        {
            if (string.IsNullOrWhiteSpace(progressKey))
                return;

            PlayerPrefs.SetInt(progressKey, 1);
            PlayerPrefs.Save();
        }

        public static void ResetFor(string progressKey)
        {
            if (string.IsNullOrWhiteSpace(progressKey))
                return;

            PlayerPrefs.DeleteKey(progressKey);
            PlayerPrefs.Save();
        }
    }
}
