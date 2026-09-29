using System;
using System.Collections;
using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 유닛의 전투 애니메이션 상태 재생을 담당합니다.
    /// 액티브 스킬은 Animator 상태를 변경하지 않습니다.
    /// 현재 재생 중인 애니메이션을 그대로 유지하고 스킬 연출은 VFX에서 처리합니다.
    /// </summary>
    public sealed class UnitAnimationController : MonoBehaviour
    {
        [Header("애니메이션")]
        [Tooltip("유닛 전투 애니메이션의 재생 속도입니다. VFX 재생 속도와는 별도로 적용됩니다.")]
        [SerializeField, Min(0.1f)] private float animationSpeed = 2f;

        private Animator _animator;
        private Coroutine _returnToIdleRoutine;
        private Coroutine _deadRoutine;

        /// <summary>
        /// 자식 오브젝트에서 Animator를 찾아 저장하고
        /// Inspector에서 설정한 애니메이션 재생 속도를 적용합니다.
        /// </summary>
        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null)
            {
                return;
            }

            // VFX와 별개로 유닛 Animator에만 적용되는 재생 속도
            _animator.speed = animationSpeed;
        }

        public void PlayIdle() => PlayState("_Idle", false);
        public void PlayAttack() => PlayState("_Attack", true);
        public void PlayCrowdControl() => PlayState("_CC", true);

        /// <summary>
        /// Attack 클립 1회의 실제 재생 시간을 반환합니다.
        /// 기본 공격의 발사 타이밍 계산에 사용합니다.
        /// Attack 클립을 찾을 수 없다면 0을 반환합니다.
        /// </summary>
        public float GetAttackDuration()
        {
            if (_animator == null)
            {
                return 0f;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                return 0f;
            }

            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null)
                {
                    continue;
                }

                if (!clip.name.EndsWith("_Attack"))
                {
                    continue;
                }

                return clip.length / Mathf.Max(0.01f, _animator.speed);
            }

            return 0f;
        }

        /// <summary>
        /// 사망 애니메이션을 재생하고 애니메이션 종료 후 전달받은 작업을 실행합니다.
        /// Dead는 Idle, Attack, CC보다 우선합니다.
        /// </summary>
        public void PlayDead(Action onComplete)
        {
            if (_animator == null)
            {
                onComplete?.Invoke();
                return;
            }

            int stateHash = FindStateHash("_Dead");

            if (stateHash == 0 || !_animator.HasState(0, stateHash))
            {
                onComplete?.Invoke();
                return;
            }

            // Dead 상태가 가장 우선이므로 이전 상태에서 Idle로 돌아가면 안 됨
            if (_returnToIdleRoutine != null)
            {
                StopCoroutine(_returnToIdleRoutine);
                _returnToIdleRoutine = null;
            }

            // 이미 Dead 종료 대기 코루틴이 있다면 중복 실행을 방지하기 위해 기존 코루틴을 중단
            if (_deadRoutine != null)
            {
                StopCoroutine(_deadRoutine);
                _deadRoutine = null;
            }

            _animator.Play(stateHash, 0, 0f);

            _deadRoutine = StartCoroutine(WaitForDeadAnimation(stateHash, onComplete));
        }

        /// <summary>
        /// 전달받은 접미사와 일치하는 Animator State를 재생합니다.
        /// returnToIdle이 true라면 해당 애니메이션이 끝난 뒤 Idle로 돌아갑니다.
        /// Dead가 재생 중일 때는 일반 상태가 Dead를 덮어쓸 수 없습니다.
        /// </summary>
        private void PlayState(string suffix, bool returnToIdle)
        {
            if (_animator == null)
            {
                return;
            }

            if (_deadRoutine != null)
            {
                return;
            }

            int stateHash = FindStateHash(suffix);

            if (stateHash == 0 || !_animator.HasState(0, stateHash))
            {
                return;
            }

            _animator.Play(stateHash, 0, 0f);

            if (!returnToIdle)
            {
                return;
            }

            if (_returnToIdleRoutine != null)
            {
                StopCoroutine(_returnToIdleRoutine);
                _returnToIdleRoutine = null;
            }

            _returnToIdleRoutine = StartCoroutine(ReturnToIdle(stateHash));
        }

        /// <summary>
        /// 전달받은 접미사와 일치하는 AnimationClip을 찾아
        /// Animator State의 Hash 값을 반환합니다.
        /// 일치하는 AnimationClip이 없다면 0을 반환합니다.
        /// </summary>
        private int FindStateHash(string suffix)
        {
            RuntimeAnimatorController controller =
                _animator.runtimeAnimatorController;

            if (controller == null)
            {
                return 0;
            }

            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip == null)
                {
                    continue;
                }

                if (!clip.name.EndsWith(suffix))
                {
                    continue;
                }

                // AnimationClip 이름과 Animator State 이름을 동일하게 사용
                return Animator.StringToHash(clip.name);
            }

            return 0;
        }

        /// <summary>
        /// Attack 또는 CC 애니메이션이 끝날 때까지 기다린 뒤 Idle 상태로 돌아갑니다.
        /// 고정 시간을 계산하지 않고 AnimatorStateInfo.normalizedTime을 사용하여
        /// 실제 애니메이션 완료 여부를 확인합니다.
        /// </summary>
        private IEnumerator ReturnToIdle(int stateHash)
        {
            yield return null;

            while (true)
            {
                if (_animator == null)
                {
                    _returnToIdleRoutine = null;
                    yield break;
                }

                AnimatorStateInfo stateInfo =
                    _animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.shortNameHash != stateHash)
                {
                    _returnToIdleRoutine = null;
                    yield break;
                }

                if (stateInfo.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }

            _returnToIdleRoutine = null;

            PlayIdle();
        }

        /// <summary>
        /// Dead 애니메이션이 끝날 때까지 기다린 뒤 전달받은 사망 완료 작업을 실행합니다.
        /// 고정 시간을 계산하지 않고 AnimatorStateInfo.normalizedTime을 사용하여
        /// 실제 Dead 애니메이션 완료 여부를 확인합니다.
        /// </summary>
        private IEnumerator WaitForDeadAnimation(int stateHash, Action onComplete)
        {
            yield return null;

            while (true)
            {
                if (_animator == null)
                {
                    _deadRoutine = null;
                    onComplete?.Invoke();
                    yield break;
                }

                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.shortNameHash != stateHash)
                {
                    _deadRoutine = null;
                    onComplete?.Invoke();
                    yield break;
                }

                if (stateInfo.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }

            _deadRoutine = null;

            onComplete?.Invoke();
        }
    }
}