using System;
using OzGameLab01.Interfaces;
using UnityEngine;

namespace OzGameLab01.Managers
{
    /// <summary>
    /// UI와 게임 로직의 사운드 요청을 이벤트로 받아 SoundManager에 전달합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoundConnector : MonoBehaviour, ISoundConnector
    {
        private static SoundConnector _global;
        private SoundManager _soundManager;

        /// <summary>
        /// UI와 게임 로직에서 발행한 사운드 요청 이벤트
        /// </summary>
        public event Action<SoundRequest> SoundRequested;

        /// <summary>
        /// 전역 SoundManager에 연결된 사운드 요청 지점
        /// </summary>
        public static SoundConnector Global
        {
            get
            {
                if (_global == null)
                {
                    SoundManager manager = FindFirstObjectByType<SoundManager>();
                    if (manager == null)
                    {
                        return null;
                    }

                    _global = manager.GetComponent<SoundConnector>();
                    if (_global == null)
                    {
                        _global = manager.gameObject.AddComponent<SoundConnector>();
                    }
                }

                return _global;
            }
        }

        /// <summary>
        /// 공격, 피격, UI 클릭 시점의 효과음 요청
        /// </summary>
        public static void RequestSfx(SoundId id)
        {
            SoundConnector connector = Global;
            if (connector == null)
            {
                return;
            }

            connector.Publish(new SoundRequest(id, SoundChannel.Sfx));
        }

        /// <summary>
        /// 씬 전환과 결과 화면 시점의 BGM 요청
        /// </summary>
        public static void RequestBgm(SoundId id, bool restart = false)
        {
            SoundConnector connector = Global;
            if (connector == null)
            {
                return;
            }

            connector.Publish(new SoundRequest(id, SoundChannel.Bgm, restart));
        }

        private void Awake()
        {
            _soundManager = GetComponent<SoundManager>();
        }

        private void OnEnable()
        {
            // SoundManager 재생 구독
            SoundRequested += OnSoundRequested;
        }

        private void OnDisable()
        {
            // SoundManager 재생 구독 해제
            SoundRequested -= OnSoundRequested;
            if (_global == this)
            {
                _global = null;
            }
        }

        /// <summary>
        /// 사운드 요청 이벤트의 구독자 전달
        /// </summary>
        public void Publish(SoundRequest request)
        {
            if (request.Id == SoundId.None || !isActiveAndEnabled)
            {
                return;
            }

            Action<SoundRequest> handlers = SoundRequested;
            if (handlers == null)
            {
                return;
            }

            foreach (Action<SoundRequest> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(request);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        /// <summary>
        /// SoundManager에 등록된 SoundId와 채널의 실제 재생
        /// </summary>
        private void OnSoundRequested(SoundRequest request)
        {
            if (_soundManager == null)
            {
                return;
            }

            if (request.Channel == SoundChannel.Bgm)
            {
                _soundManager.PlayBgm(request.Id, request.Restart);
                return;
            }

            _soundManager.PlaySfx(request.Id);
        }
    }
}
