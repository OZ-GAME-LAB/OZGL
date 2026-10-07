using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace OzGameLab01.Combat
{
    /// <summary>Releases a short-lived Addressables VFX instance after its particles have finished.</summary>
    public sealed class AddressableVfxLifetime : MonoBehaviour
    {
        private float _lifetime;
        private bool _initialized;

        public void Initialize(float lifetime)
        {
            _lifetime = Mathf.Max(0.01f, lifetime);
            if (_initialized) return;
            _initialized = true;
            StartCoroutine(ReleaseAfterLifetime());
        }

        private IEnumerator ReleaseAfterLifetime()
        {
            yield return new WaitForSeconds(_lifetime);
            Addressables.ReleaseInstance(gameObject);
        }
    }
}
