using System;
using UnityEngine;

namespace OzGameLab01.UI
{
    // 클래스별 레벨업 기능 폐지로 더 이상 사용하지 않습니다. 프리팹 참조가 남아있어
    // 컴파일만 유지하는 빈 껍데기입니다 — 필요 없어지면 프리팹과 함께 정리하세요.
    public class LevelUpSelector : MonoBehaviour
    {
        [SerializeField] private GameObject panel;

        private void Awake()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Show(Action onComplete)
        {
        }
    }
}
