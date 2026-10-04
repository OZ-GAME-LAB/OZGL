using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace OzGameLab01.Combat
{
    /// <summary>
    /// 풀에서 대여·반납될 때 런타임 상태를 초기화해야 하는 컴포넌트가 구현합니다.
    /// </summary>
    public interface ICombatPoolable
    {
        void OnRentFromCombatPool();
        void OnReturnToCombatPool();
    }

    /// <summary>
    /// 한 Combat 씬 동안만 살아 있는 투사체·일회성 VFX 풀입니다.
    /// 반납 시 인스턴스를 비활성화하고, 전투 씬 종료 시 Addressables 핸들과 일반 인스턴스를
    /// 실제로 해제합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatPoolContext : MonoBehaviour, IDisposable
    {
        private sealed class PoolBucket
        {
            public bool IsAddressable;
            public readonly Stack<GameObject> Inactive = new();
            public readonly List<GameObject> All = new();
        }

        private sealed class InstanceRecord
        {
            public PoolBucket Bucket;
            public GameObject Instance;
            public Vector3 OriginalScale;
            public ParticleSystemRenderer[] ParticleRenderers;
            public int[] OriginalSortingOrders;
            public bool IsActive;
            public int LeaseVersion;
        }

        private readonly Dictionary<string, PoolBucket> _addressablePools = new();
        private readonly Dictionary<int, PoolBucket> _prefabPools = new();
        private readonly Dictionary<GameObject, InstanceRecord> _records = new();

        private Transform _inactiveRoot;
        private bool _disposing;
        private bool _disposed;

        public static CombatPoolContext Current { get; private set; }

        public static CombatPoolContext Ensure(Component owner)
        {
            if (owner == null)
            {
                return null;
            }

            CombatPoolContext context = owner.GetComponent<CombatPoolContext>();
            return context != null ? context : owner.gameObject.AddComponent<CombatPoolContext>();
        }

        public static bool TryReturn(GameObject instance)
        {
            return Current != null && Current.Return(instance);
        }

        private void Awake()
        {
            Current = this;
            var rootObject = new GameObject("CombatPool_Inactive");
            _inactiveRoot = rootObject.transform;
            _inactiveRoot.SetParent(transform, false);
            rootObject.SetActive(false);
        }

        public void RentAddressable(
            object runtimeKey,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            Action<GameObject> onReady,
            Action onFailed = null)
        {
            if (_disposed || runtimeKey == null)
            {
                onFailed?.Invoke();
                return;
            }

            string poolKey = runtimeKey.ToString();
            if (string.IsNullOrWhiteSpace(poolKey))
            {
                onFailed?.Invoke();
                return;
            }

            if (!_addressablePools.TryGetValue(poolKey, out PoolBucket bucket))
            {
                bucket = new PoolBucket { IsAddressable = true };
                _addressablePools.Add(poolKey, bucket);
            }

            GameObject reused = TakeInactive(bucket);
            if (reused != null)
            {
                Activate(reused, position, rotation, parent);
                onReady?.Invoke(reused);
                return;
            }

            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(
                runtimeKey,
                position,
                rotation,
                parent);
            handle.Completed += operation =>
            {
                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    if (operation.IsValid())
                    {
                        Addressables.Release(operation);
                    }

                    onFailed?.Invoke();
                    return;
                }

                GameObject instance = operation.Result;
                if (this == null || _disposed || _disposing)
                {
                    Addressables.ReleaseInstance(instance);
                    return;
                }

                Register(bucket, instance);
                Activate(instance, position, rotation, parent);
                onReady?.Invoke(instance);
            };
        }

        public GameObject RentPrefab(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null)
        {
            if (_disposed || prefab == null)
            {
                return null;
            }

            int poolKey = prefab.GetInstanceID();
            if (!_prefabPools.TryGetValue(poolKey, out PoolBucket bucket))
            {
                bucket = new PoolBucket { IsAddressable = false };
                _prefabPools.Add(poolKey, bucket);
            }

            GameObject instance = TakeInactive(bucket);
            if (instance == null)
            {
                instance = Instantiate(prefab, position, rotation, parent);
                Register(bucket, instance);
            }

            Activate(instance, position, rotation, parent);
            return instance;
        }

        public bool Return(GameObject instance)
        {
            if (instance == null || _disposed || !_records.TryGetValue(instance, out InstanceRecord record))
            {
                return false;
            }

            if (!record.IsActive)
            {
                return true;
            }

            NotifyPoolables(instance, rented: false);
            StopAndClearParticles(instance);
            record.IsActive = false;
            instance.transform.SetParent(_inactiveRoot, false);
            instance.SetActive(false);
            record.Bucket.Inactive.Push(instance);
            return true;
        }

        public void ReturnAfter(GameObject instance, float seconds)
        {
            if (instance == null || !_records.TryGetValue(instance, out InstanceRecord record))
            {
                return;
            }

            StartCoroutine(ReturnAfterDelay(
                instance,
                record.LeaseVersion,
                Mathf.Max(0.01f, seconds)));
        }

        public void Dispose()
        {
            if (_disposed || _disposing)
            {
                return;
            }

            _disposing = true;

            var records = new List<InstanceRecord>(_records.Values);
            foreach (InstanceRecord record in records)
            {
                if (record.Instance == null)
                {
                    continue;
                }

                if (record.IsActive)
                {
                    NotifyPoolables(record.Instance, rented: false);
                    StopAndClearParticles(record.Instance);
                    record.IsActive = false;
                }
            }

            // 투사체의 자식으로 사용 중인 VFX도 독립적으로 해제할 수 있도록 먼저 분리합니다.
            foreach (InstanceRecord record in records)
            {
                if (record.Instance != null)
                {
                    record.Instance.transform.SetParent(null, true);
                }
            }

            foreach (InstanceRecord record in records)
            {
                GameObject instance = record.Instance;
                if (instance == null)
                {
                    continue;
                }

                if (record.Bucket.IsAddressable)
                {
                    Addressables.ReleaseInstance(instance);
                }
                else
                {
                    Destroy(instance);
                }
            }

            StopAllCoroutines();
            _records.Clear();
            _addressablePools.Clear();
            _prefabPools.Clear();
            _disposed = true;
            _disposing = false;

            if (Current == this)
            {
                Current = null;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Register(PoolBucket bucket, GameObject instance)
        {
            ParticleSystemRenderer[] renderers =
                instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            int[] sortingOrders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                sortingOrders[i] = renderers[i].sortingOrder;
            }

            var record = new InstanceRecord
            {
                Bucket = bucket,
                Instance = instance,
                OriginalScale = instance.transform.localScale,
                ParticleRenderers = renderers,
                OriginalSortingOrders = sortingOrders
            };
            bucket.All.Add(instance);
            _records.Add(instance, record);
        }

        private GameObject TakeInactive(PoolBucket bucket)
        {
            while (bucket.Inactive.Count > 0)
            {
                GameObject instance = bucket.Inactive.Pop();
                if (instance != null)
                {
                    return instance;
                }
            }

            return null;
        }

        private void Activate(
            GameObject instance,
            Vector3 position,
            Quaternion rotation,
            Transform parent)
        {
            InstanceRecord record = _records[instance];
            RestoreRendererSorting(record);
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = record.OriginalScale;
            NotifyPoolables(instance, rented: true);
            instance.SetActive(true);
            RestartParticles(instance);
            record.IsActive = true;
            record.LeaseVersion++;
        }

        private IEnumerator ReturnAfterDelay(
            GameObject instance,
            int leaseVersion,
            float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (instance != null &&
                _records.TryGetValue(instance, out InstanceRecord record) &&
                record.IsActive &&
                record.LeaseVersion == leaseVersion)
            {
                Return(instance);
            }
        }

        private static void NotifyPoolables(GameObject instance, bool rented)
        {
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is not ICombatPoolable poolable)
                {
                    continue;
                }

                if (rented)
                    poolable.OnRentFromCombatPool();
                else
                    poolable.OnReturnToCombatPool();
            }
        }

        private static void StopAndClearParticles(GameObject instance)
        {
            foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static void RestartParticles(GameObject instance)
        {
            foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particle.Clear(true);
                particle.Play(true);
            }
        }

        private static void RestoreRendererSorting(InstanceRecord record)
        {
            for (int i = 0; i < record.ParticleRenderers.Length; i++)
            {
                ParticleSystemRenderer renderer = record.ParticleRenderers[i];
                if (renderer != null)
                {
                    renderer.sortingOrder = record.OriginalSortingOrders[i];
                }
            }
        }
    }
}
