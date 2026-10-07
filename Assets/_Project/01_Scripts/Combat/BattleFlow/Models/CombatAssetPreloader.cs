using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 전투 정보 화면이 떠 있는 동안 이번 전투 참가 유닛의 투사체/스킬 VFX를 미리 로드합니다.
    /// 처음 쓰는 순간 디스크에서 읽으면 전투 중 수십 ms 프레임 멈춤이 생기기 때문입니다.
    /// 로드 핸들은 전투 씬이 끝날 때 Dispose로 한 번에 해제합니다.
    /// </summary>
    public sealed class CombatAssetPreloader : IDisposable
    {
        private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
        private readonly HashSet<object> _requestedKeys = new HashSet<object>();
        private bool _disposed;

        public void Preload(IEnumerable<Unit> units)
        {
            var keys = new List<object>();
            foreach (Unit unit in units)
            {
                unit?.CollectPreloadKeys(keys);
            }

            foreach (object key in keys)
            {
                Load<GameObject>(key);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (AsyncOperationHandle handle in _handles)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }
            _handles.Clear();
            _requestedKeys.Clear();
        }

        private void Load<T>(object key) where T : UnityEngine.Object
        {
            // 등록되지 않은 주소는 실제 사용 시점의 에러 로그에 맡기고, 미리 로드 단계에서는 조용히 건너뜁니다.
            if (_disposed || key == null || !_requestedKeys.Add(key) || !HasLocation<T>(key)) return;

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            _handles.Add(handle);
            handle.Completed += operation =>
            {
                if (_disposed || operation.Status != AsyncOperationStatus.Succeeded) return;
                // 투사체는 생성된 뒤 스프라이트와 이동/피격 VFX를 다시 로드하므로 함께 올려둡니다.
                if (operation.Result is GameObject prefab && prefab.TryGetComponent(out Projectile projectile))
                {
                    LoadReference<Sprite>(projectile.SpriteReference);
                    LoadReference<GameObject>(projectile.TravelEffectReference);
                    LoadReference<GameObject>(projectile.ImpactEffectReference);
                }
            };
        }

        private void LoadReference<T>(AssetReference reference) where T : UnityEngine.Object
        {
            if (reference != null && reference.RuntimeKeyIsValid())
            {
                Load<T>(reference.RuntimeKey);
            }
        }

        private static bool HasLocation<T>(object key)
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                if (locator.Locate(key, typeof(T), out _)) return true;
            }
            return false;
        }
    }
}
