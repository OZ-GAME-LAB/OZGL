using System;
using System.Collections.Generic;

namespace OzGameLab01.Controllers
{
    /// <summary>
    /// 튜토리얼 Step의 순서와 런타임 상태만 관리하는 순수 C# 상태 머신입니다.
    /// Unity 오브젝트나 화면 표현을 알지 못하므로 보드/전투 튜토리얼이 공유할 수 있습니다.
    /// </summary>
    public sealed class TutorialSequenceState<TStep> where TStep : class
    {
        private readonly IReadOnlyList<TStep> steps;

        public TutorialSequenceState(IReadOnlyList<TStep> steps)
        {
            this.steps = steps ?? Array.Empty<TStep>();
        }

        public bool IsPlaying { get; private set; }
        public int NextStepIndex { get; private set; }
        public TStep ActiveStep { get; private set; }
        public TStep PendingStep { get; private set; }

        public bool HasConfiguredSteps
        {
            get
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i] != null)
                        return true;
                }

                return false;
            }
        }

        public bool HasRemainingSteps => FindNextConfiguredIndex() >= 0;

        public void Start()
        {
            NextStepIndex = 0;
            ActiveStep = null;
            PendingStep = null;
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            ActiveStep = null;
            PendingStep = null;
        }

        public void Complete()
        {
            IsPlaying = false;
            ActiveStep = null;
            PendingStep = null;
        }

        public TStep PeekNext()
        {
            if (!IsPlaying || ActiveStep != null || PendingStep != null)
                return null;

            int index = FindNextConfiguredIndex();
            if (index < 0)
                return null;

            NextStepIndex = index;
            return steps[index];
        }

        public bool TryQueue(TStep step)
        {
            TStep next = PeekNext();
            if (next == null || !ReferenceEquals(next, step))
                return false;

            PendingStep = next;
            return true;
        }

        public TStep ActivatePending()
        {
            if (!IsPlaying || PendingStep == null || ActiveStep != null)
                return null;

            ActiveStep = PendingStep;
            PendingStep = null;
            NextStepIndex++;
            return ActiveStep;
        }

        public TStep ClearActive()
        {
            TStep cleared = ActiveStep;
            ActiveStep = null;
            return cleared;
        }

        public void ClearPending()
        {
            PendingStep = null;
        }

        public TStep GetFirstConfiguredStep()
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    return steps[i];
            }

            return null;
        }

        private int FindNextConfiguredIndex()
        {
            for (int i = NextStepIndex; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    return i;
            }

            return -1;
        }
    }
}
