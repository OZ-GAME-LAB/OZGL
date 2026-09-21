using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UIMotionView : MonoBehaviour
    {
        #region Nested Types

        public enum MotionState { Resting, Entering, Visible, Exiting, Hidden }
        public enum EnableAction { None, Ambient, Enter, Prepare }

        [Serializable]
        public sealed class Pose
        {
            public Vector3 positionOffset;
            public Vector3 rotationOffset;
            public Vector3 scaleMultiplier = Vector3.one;
            [Range(0f, 1f)] public float alphaMultiplier = 1f;
        }

        [Serializable]
        public sealed class Element
        {
            public string label;
            public Transform target;
            public CanvasGroup opacity;
            public bool ambientEnabled = true;
            public Vector3 ambientPosition;
            public Vector3 ambientRotation;
            public Vector3 ambientScale;
            [Min(0.01f)] public float ambientPeriod = 4f;
            [Min(0f)] public float ambientDelay;
            public Pose enter = new();
            public Pose exit = new();
            [Min(0f)] public float enterDelay;
            [Min(0f)] public float exitDelay;
        }

        private sealed class RuntimeElement
        {
            internal Element settings;
            internal Transform target;
            internal RectTransform rect;
            internal CanvasGroup opacity;
            internal Vector3 position;
            internal Quaternion rotation;
            internal Vector3 scale;
            internal float alpha;
            internal Vector3 offset;
            internal Vector3 rotationOffset;
            internal Vector3 scaleMultiplier = Vector3.one;
            internal float alphaMultiplier = 1f;
            internal float wave;
            internal Tween ambient;
        }

        #endregion

        [Header("Elements — assign visual objects, including their masks")]
        [SerializeField] private Element[] elements = Array.Empty<Element>();
        [Header("Playback")]
        [SerializeField] private EnableAction onEnable = EnableAction.None;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool ambientAfterEnter = true;
        [Header("Transitions")]
        [SerializeField, Min(0f)] private float enterDuration = 0.6f;
        [SerializeField, Min(0f)] private float exitDuration = 0.4f;
        [SerializeField] private Ease enterEase = Ease.OutCubic;
        [SerializeField] private Ease exitEase = Ease.InCubic;
        [Header("Completion — cancellation does not invoke these events")]
        [SerializeField] private UnityEvent entered = new UnityEvent();
        [SerializeField] private UnityEvent exited = new UnityEvent();

        private readonly List<RuntimeElement> runtime = new List<RuntimeElement>();
        private Sequence transition;
        private bool initialized;

        #region Properties

        public IReadOnlyList<Element> Elements => elements;
        public MotionState State { get; private set; } = MotionState.Resting;
        public bool IsTransitioning => transition != null && transition.IsActive();
        public bool IsAmbientPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public UnityEvent Entered => entered;
        public UnityEvent Exited => exited;
        public EnableAction OnEnableAction { get => onEnable; set => onEnable = value; }
        public Ease EnterEase { get => enterEase; set => enterEase = value; }
        public Ease ExitEase { get => exitEase; set => exitEase = value; }
        public float EnterDuration { get => enterDuration; set => enterDuration = Mathf.Max(0f, value); }
        public float ExitDuration { get => exitDuration; set => exitDuration = Mathf.Max(0f, value); }
        public bool AmbientAfterEnter { get => ambientAfterEnter; set => ambientAfterEnter = value; }
        public bool UseUnscaledTime
        {
            get => useUnscaledTime;
            set
            {
                useUnscaledTime = value;
                transition?.SetUpdate(value);
                foreach (RuntimeElement item in runtime)
                    item.ambient?.SetUpdate(value);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake() => Initialize();

        private void OnEnable()
        {
            if (onEnable == EnableAction.Enter) ReplayEnter();
            else if (onEnable == EnableAction.Ambient) PlayAmbient();
            else if (onEnable == EnableAction.Prepare) PrepareEnter();
        }

        private void OnDisable() => ResetToRest();
        private void OnDestroy() => Stop();

        #endregion

        #region Public API

        public void PlayEnter() => PlayTransition(true);

        /// <summary>
        /// 등장 시작 포즈로 배치하고 재생 없이 대기합니다.
        /// 숨긴 상태로 준비하려면 각 Enter 포즈의 alphaMultiplier를 0으로 설정합니다.
        /// </summary>
        public void PrepareEnter()
        {
            if (!CanPlay()) return;
            Stop();
            foreach (RuntimeElement item in runtime)
            {
                item.wave = 0f;
                SetPose(item, item.settings.enter);
            }
            State = MotionState.Hidden;
        }

        public void ReplayEnter()
        {
            if (!CanPlay()) return;
            PrepareEnter();
            PlayEnter();
        }

        public void PlayExit() => PlayTransition(false);

        public void PlayAmbient()
        {
            if (!CanPlay() || IsTransitioning) return;
            bool any = false;
            foreach (RuntimeElement item in runtime)
            {
                if (!item.settings.ambientEnabled || item.target == null) continue;
                if (item.ambient == null || !item.ambient.IsActive())
                {
                    item.wave = 0f;
                    item.ambient = DOVirtual.Float(0f, Mathf.PI * 2f,
                            Mathf.Max(0.01f, item.settings.ambientPeriod), phase =>
                            {
                                item.wave = Mathf.Sin(phase);
                                Apply(item);
                            })
                        .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart)
                        .SetDelay(Mathf.Max(0f, item.settings.ambientDelay))
                        .SetUpdate(useUnscaledTime);
                }
                if (IsPaused) item.ambient.Pause();
                else item.ambient.Play();
                any = true;
            }
            IsAmbientPlaying = any;
        }

        public void StopAmbient()
        {
            foreach (RuntimeElement item in runtime)
            {
                item.ambient?.Kill(false);
                item.ambient = null;
                item.wave = 0f;
                Apply(item);
            }
            IsAmbientPlaying = false;
        }

        public void Pause()
        {
            IsPaused = true;
            transition?.Pause();
            foreach (RuntimeElement item in runtime) item.ambient?.Pause();
        }

        public void Resume()
        {
            IsPaused = false;
            transition?.Play();
            if (!IsTransitioning && IsAmbientPlaying)
                foreach (RuntimeElement item in runtime) item.ambient?.Play();
        }

        public void Stop()
        {
            transition?.Kill(false);
            transition = null;
            foreach (RuntimeElement item in runtime)
            {
                item.ambient?.Kill(false);
                item.ambient = null;
            }
            IsAmbientPlaying = false;
            IsPaused = false;
            State = MotionState.Resting;
        }

        public void ResetToRest()
        {
            Stop();
            foreach (RuntimeElement item in runtime)
            {
                item.wave = 0f;
                SetPose(item, null);
            }
        }

        public void Configure(Element[] bindings)
        {
            ResetToRest();
            elements = bindings == null ? Array.Empty<Element>() : (Element[])bindings.Clone();
            initialized = false;
            Initialize();
        }

        #endregion

        #region Private Methods

        private bool CanPlay()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return false;
            Initialize();
            return true;
        }

        private void Initialize()
        {
            if (initialized) return;
            runtime.Clear();
            var targets = new HashSet<Transform>();
            var groups = new HashSet<CanvasGroup>();
            foreach (Element element in elements ?? Array.Empty<Element>())
            {
                if (element == null || element.target == null) continue;
                if (!targets.Add(element.target))
                {
                    Debug.LogWarning("UIMotionView: duplicate target skipped: " + element.target.name, this);
                    continue;
                }
                CanvasGroup group = element.opacity;
                if (group != null && !groups.Add(group))
                {
                    Debug.LogWarning("UIMotionView: duplicate opacity binding ignored.", this);
                    group = null;
                }
                var rect = element.target as RectTransform;
                runtime.Add(new RuntimeElement
                {
                    settings = element,
                    target = element.target,
                    rect = rect,
                    opacity = group,
                    position = rect != null ? rect.anchoredPosition3D : element.target.localPosition,
                    rotation = element.target.localRotation,
                    scale = element.target.localScale,
                    alpha = group != null ? group.alpha : 1f
                });
            }
            initialized = true;
        }

        private void PlayTransition(bool entering)
        {
            if (!CanPlay()) return;
            transition?.Kill(false);
            transition = null;
            IsPaused = false;

            foreach (RuntimeElement item in runtime) item.ambient?.Pause();
            IsAmbientPlaying = false;
            State = entering ? MotionState.Entering : MotionState.Exiting;
            float duration = Mathf.Max(0f, entering ? enterDuration : exitDuration);
            if (runtime.Count == 0 || duration == 0f)
            {
                foreach (RuntimeElement item in runtime)
                {
                    item.wave = 0f;
                    SetPose(item, entering ? null : item.settings.exit);
                }
                CompleteTransition(entering);
                return;
            }
            transition = DOTween.Sequence().SetUpdate(useUnscaledTime);
            foreach (RuntimeElement item in runtime)
            {
                Pose pose = entering ? null : item.settings.exit;
                Vector3 fromPosition = item.offset;
                Vector3 fromRotation = item.rotationOffset;
                Vector3 fromScale = item.scaleMultiplier;
                float fromAlpha = item.alphaMultiplier;
                float fromWave = item.wave;
                float delay = Mathf.Max(0f, entering ? item.settings.enterDelay : item.settings.exitDelay);
                transition.Insert(delay, DOVirtual.Float(0f, 1f, duration, t =>
                {
                    item.offset = Vector3.LerpUnclamped(fromPosition, pose?.positionOffset ?? Vector3.zero, t);
                    item.rotationOffset = Vector3.LerpUnclamped(fromRotation, pose?.rotationOffset ?? Vector3.zero, t);
                    item.scaleMultiplier = Vector3.LerpUnclamped(fromScale, pose?.scaleMultiplier ?? Vector3.one, t);
                    item.alphaMultiplier = Mathf.LerpUnclamped(fromAlpha, pose?.alphaMultiplier ?? 1f, t);
                    item.wave = fromWave * (1f - t);
                    Apply(item);
                }).SetEase(entering ? enterEase : exitEase));
            }
            transition.OnComplete(() => CompleteTransition(entering));
        }

        private void CompleteTransition(bool entering)
        {
            transition = null;
            StopAmbient();
            State = entering ? MotionState.Visible : MotionState.Hidden;
            if (entering && ambientAfterEnter) PlayAmbient();
            if (entering) entered.Invoke();
            else exited.Invoke();
        }

        private static void SetPose(RuntimeElement item, Pose pose)
        {
            item.offset = pose?.positionOffset ?? Vector3.zero;
            item.rotationOffset = pose?.rotationOffset ?? Vector3.zero;
            item.scaleMultiplier = pose?.scaleMultiplier ?? Vector3.one;
            item.alphaMultiplier = pose?.alphaMultiplier ?? 1f;
            Apply(item);
        }

        private static void Apply(RuntimeElement item)
        {
            if (item.target == null) return;
            Vector3 position = item.position + item.offset + item.settings.ambientPosition * item.wave;
            if (item.rect != null) item.rect.anchoredPosition3D = position;
            else item.target.localPosition = position;
            item.target.localRotation = item.rotation * Quaternion.Euler(
                item.rotationOffset + item.settings.ambientRotation * item.wave);
            item.target.localScale = Vector3.Scale(item.scale,
                item.scaleMultiplier + item.settings.ambientScale * item.wave);
            if (item.opacity != null)
                item.opacity.alpha = item.alpha * Mathf.Clamp01(item.alphaMultiplier);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Playback/Prepare Enter")]
        private void ContextPrepareEnter() => PrepareEnter();

        [ContextMenu("Playback/Enter")]
        private void ContextPlayEnter() => PlayEnter();

        [ContextMenu("Playback/Replay Enter")]
        private void ContextReplayEnter() => ReplayEnter();

        [ContextMenu("Playback/Exit")]
        private void ContextPlayExit() => PlayExit();

        [ContextMenu("Playback/Play Ambient")]
        private void ContextPlayAmbient() => PlayAmbient();

        [ContextMenu("Playback/Stop Ambient")]
        private void ContextStopAmbient() => StopAmbient();

        [ContextMenu("Playback/Pause")]
        private void ContextPause() => Pause();

        [ContextMenu("Playback/Resume")]
        private void ContextResume() => Resume();

        [ContextMenu("Playback/Reset To Rest")]
        private void ContextResetToRest() => ResetToRest();
#endif

        #endregion
    }
}
