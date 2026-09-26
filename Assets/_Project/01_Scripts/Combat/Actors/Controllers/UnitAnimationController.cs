using System;
using System.Collections;
using UnityEngine;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 캐릭터 Animator의 상태 재생을 담당합니다.
    /// </summary>
    public sealed class UnitAnimationController : MonoBehaviour
    {
        private Animator _animator;
        private Coroutine _returnToIdleRoutine;
        private Coroutine _deadRoutine;

        /// <summary>
        /// Animator를 참조하고 전투 애니메이션 재생 속도를 설정합니다.
        /// </summary>
        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>(true);

            // 전투 애니메이션 재생 속도 3배 적용 (임시)
            if (_animator != null)
            {
                _animator.speed = 3f;
            }
        }

        public void PlayIdle() => PlayState("_Idle", false);
        public void PlayAttack() => PlayState("_Attack", true);
        public void PlayUltimate() => PlayState("_Ult", true);

        /// <summary>
        /// Attack 클립 1회의 실제 재생 시간(클립 길이 ÷ Animator 속도)을 반환합니다. Attack 클립이 없으면 0입니다.
        /// </summary>
        public float GetAttackDuration()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return 0f;

            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name.EndsWith("_Attack"))
                {
                    return clip.length / Mathf.Max(0.01f, _animator.speed);
                }
            }

            return 0f;
        }

        /// <summary>
        /// 사망 애니메이션을 재생하고 종료 후 전달받은 작업을 실행합니다.
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

            if (_returnToIdleRoutine != null)
            {
                StopCoroutine(_returnToIdleRoutine);
                _returnToIdleRoutine = null;
            }

            if (_deadRoutine != null)
            {
                StopCoroutine(_deadRoutine);
            }

            _animator.Play(stateHash, 0, 0f);
            _deadRoutine = StartCoroutine(WaitForDeadAnimation(stateHash, onComplete));
        }

        /// <summary>
        /// 접미사에 해당하는 Animator 상태를 재생하고 필요 시 대기 상태로 복귀합니다.
        /// </summary>
        private void PlayState(string suffix, bool returnToIdle)
        {
            if (_animator == null)
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
            }

            _returnToIdleRoutine = StartCoroutine(ReturnToIdle(stateHash));
        }

        /// <summary>
        /// 접미사와 일치하는 애니메이션 상태의 해시 값을 반환합니다.
        /// </summary>
        private int FindStateHash(string suffix)
        {
            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
            if (controller == null)
            {
                return 0;
            }

            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip != null && clip.name.EndsWith(suffix))
                {
                    return Animator.StringToHash(clip.name);
                }
            }

            return 0;
        }

        /// <summary>
        /// 현재 애니메이션 재생이 끝난 후 대기 상태로 복귀합니다.
        /// </summary>
        private IEnumerator ReturnToIdle(int stateHash)
        {
            // Animator 상태 반영 대기
            yield return null;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float duration = stateInfo.length > 0f ? stateInfo.length : 0.1f;

            // AnimatorStateInfo.length는 Animator.speed가 이미 반영된 실제 재생 시간이라
            // 여기서 다시 animator.speed로 나누면 안 됩니다(3배속이면 1/3 지점에서 끊겨버림).
            yield return new WaitForSeconds(duration / Mathf.Max(0.01f, stateInfo.speed));

            if (_animator != null &&
                _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash)
            {
                PlayIdle();
            }

            _returnToIdleRoutine = null;
        }

        /// <summary>
        /// 사망 애니메이션 재생이 끝난 후 전달받은 작업을 실행합니다.
        /// </summary>
        private IEnumerator WaitForDeadAnimation(int stateHash, Action onComplete)
        {
            // Animator 상태 반영 대기
            yield return null;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float duration = stateInfo.length > 0f ? stateInfo.length : 0.1f;

            // AnimatorStateInfo.length는 Animator.speed가 이미 반영된 실제 재생 시간입니다.
            yield return new WaitForSeconds(duration / Mathf.Max(0.01f, stateInfo.speed));

            if (_animator != null &&
                _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == stateHash)
            {
                onComplete?.Invoke();
            }

            _deadRoutine = null;
        }
    }
}