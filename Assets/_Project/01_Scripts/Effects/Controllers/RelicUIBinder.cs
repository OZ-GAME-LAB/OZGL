using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OzGameLab01.Managers;
using OzGameLab01.Data;
using OzGameLab01.Effects.Models;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 유물 아이콘 출력을 지원하는 모든 UI View가 구현하는 표준 인터페이스
    /// </summary>
    public interface IRelicDisplayable
    {
        // 어드레서블 주소 기반 아이콘 비동기 갱신
        Task UpdateRelicIconAsync(string iconAddress);
    }

    /// <summary>
    /// RelicFacade 이벤트를 단일 창구에서 관리하고 
    /// 등록된 모든 IRelicDisplayable View에 데이터를 자동으로 주입하는 통합 컨트롤러
    /// </summary>
    public sealed class RelicUIBinder : MonoBehaviour
    {
        [Header("자동 등록 설정")]
        [Tooltip("체크 시 씬 내의 모든 IRelicDisplayable components를 자동 탐색합니다.")]
        [SerializeField] private bool autoScanOnEnable = true;

        [Header("수동 바인딩 Target Views")]
        [SerializeField] private List<MonoBehaviour> targetViews = new();

        private readonly List<IRelicDisplayable> _boundViews = new();

        private void Awake()
        {
            RegisterTargetViews();
        }

        private void OnEnable()
        {
            if (autoScanOnEnable)
            {
                ScanAndRegisterSceneViews();
            }

            // 이벤트 구독
            if (RelicManager.Instance != null && RelicManager.Instance.Facade != null)
            {
                RelicManager.Instance.Facade.Notification += OnRelicNotification;
            }
        }

        private void OnDisable()
        {
            // 이벤트 해제
            if (RelicManager.Instance != null && RelicManager.Instance.Facade != null)
            {
                RelicManager.Instance.Facade.Notification -= OnRelicNotification;
            }
        }

        /// <summary>
        /// 동적으로 생성된 View를 바인더에 추가 등록
        /// </summary>
        public void RegisterView(IRelicDisplayable view)
        {
            if (view != null && !_boundViews.Contains(view))
            {
                _boundViews.Add(view);
            }
        }

        public void UnregisterView(IRelicDisplayable view)
        {
            if (view != null)
            {
                _boundViews.Remove(view);
            }
        }

        private void RegisterTargetViews()
        {
            foreach (var mono in targetViews)
            {
                if (mono is IRelicDisplayable displayable)
                {
                    RegisterView(displayable);
                }
            }
        }

        private void ScanAndRegisterSceneViews()
        {
            // Current Active 씬 내의 IRelicDisplayable을 찾아 등록
            var foundViews = GetComponentsInChildren<IRelicDisplayable>(true);
            foreach (var view in foundViews)
            {
                RegisterView(view);
            }
        }

        private void OnRelicNotification(EffectsNotification notification)
        {
            if (notification.Kind == EffectsNotificationKind.RelicAcquired)
            {
                _ = DispatchRelicUpdateAsync(notification.SourceId);
            }
        }

        private async Task DispatchRelicUpdateAsync(int relicId)
        {
            // Catalog에서 데이터 조회
            RelicData relicData = RuntimeContent.Catalog.GetRelic(relicId);
            if (relicData == null || string.IsNullOrEmpty(relicData.iconAddress)) return;

            // 등록된 모든 View들에 일괄적으로 비동기 이미지 업데이트 전파
            List<Task> tasks = new List<Task>(_boundViews.Count);
            for (int i = 0; i < _boundViews.Count; i++)
            {
                var view = _boundViews[i];
                if (view != null)
                {
                    tasks.Add(view.UpdateRelicIconAsync(relicData.iconAddress));
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}