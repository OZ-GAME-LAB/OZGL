using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.Common
{
    /// <summary>
    /// Inspector에서 지정한 효과음을 UnityEvent 또는 활성화 시점에 요청합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoundTrigger : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool playOnEnable;

        private void OnEnable()
        {
            if (!playOnEnable)
            {
                return;
            }

            Play();
        }

        /// <summary>
        /// 지정한 효과음을 전역 사운드 흐름으로 요청합니다.
        /// </summary>
        public void Play()
        {
            if (clip == null)
            {
                return;
            }

            SoundConnector.RequestSfx(clip, volume);
        }
    }
}
