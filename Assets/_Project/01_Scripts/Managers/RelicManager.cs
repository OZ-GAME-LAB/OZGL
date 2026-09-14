using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.Data;

namespace OzGameLab01.Managers
{
    public class RelicManager : Singleton<RelicManager>
    {
        // 전체 보유 유물 목록
        private readonly List<RelicRuntimeInstance> _allRelics = new();

        public IReadOnlyList<RelicRuntimeInstance> OwnedRelics => _allRelics;

        /// <summary>
        /// GameDB의 ID 기반 유물 획득
        /// </summary>
        /// <param name="relicId"> 유물 ID </param>
        public void AcquireRelic(int relicId)
        {
            // 1. 정적 데이터 조회 (RuntimeDataManager 단일 진입점)
            var relicData = RuntimeDataManager.Instance.GetRelic(relicId);
            if (relicData == null)
            {
                Debug.LogError($"[RelicManager] ID: {relicId}에 해당하는 유물을 발견하지 못 했습니다.");
                return;
            }

            // 2. 런타임 인스턴스 생성, 장착
            var newInstance = new RelicRuntimeInstance(relicData);
            _allRelics.Add(newInstance);
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();

            SaveManager.Instance?.MarkAsDirty();
        }

        /// <summary>
        /// dropWeight(RelicData.xlsx "확률" 열) 가중치로 무작위 유물 하나를 뽑아 즉시
        /// 획득시킵니다. 이미 보유한 유물은 후보에서 제외하고, 전부 보유한 극단적인
        /// 경우에만 중복을 허용합니다. 뽑을 유물이 전혀 없으면 null을 반환합니다.
        /// </summary>
        public RelicData AcquireRandomRelic()
        {
            IReadOnlyDictionary<int, RelicData> allRelics = RuntimeDataManager.Instance.Relics;
            if (allRelics.Count == 0)
            {
                return null;
            }

            HashSet<int> ownedIds = new HashSet<int>();
            foreach (RelicRuntimeInstance owned in _allRelics)
            {
                if (owned?.Data != null)
                {
                    ownedIds.Add(owned.Data.id);
                }
            }

            List<RelicData> pool = new List<RelicData>();
            foreach (RelicData relic in allRelics.Values)
            {
                if (!ownedIds.Contains(relic.id))
                {
                    pool.Add(relic);
                }
            }

            if (pool.Count == 0)
            {
                pool.AddRange(allRelics.Values);
            }

            float totalWeight = 0f;
            foreach (RelicData relic in pool)
            {
                totalWeight += Mathf.Max(0f, relic.dropWeight);
            }

            RelicData picked;
            if (totalWeight <= 0f)
            {
                picked = pool[Random.Range(0, pool.Count)];
            }
            else
            {
                float roll = Random.value * totalWeight;
                float cumulative = 0f;
                picked = pool[pool.Count - 1];
                foreach (RelicData relic in pool)
                {
                    cumulative += Mathf.Max(0f, relic.dropWeight);
                    if (roll <= cumulative)
                    {
                        picked = relic;
                        break;
                    }
                }
            }

            AcquireRelic(picked.id);
            return picked;
        }

        /// <summary>
        /// 유물 세이브 데이터 복원
        /// </summary>
        /// <param name="saveEntries"></param>
        public void RestoreFromSave(List<RelicSaveEntry> saveEntries)
        {
            // [수정] 보유 목록을 이전 런 상태로부터 초기화
            ClearRunState();

            foreach (var entry in saveEntries)
            {
                RelicData data = RuntimeDataManager.Instance.GetRelic(entry.relicId);
                if (data == null) continue;

                _allRelics.Add(new RelicRuntimeInstance(data));
            }

            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
        }

        /// <summary>
        /// New Game과 런 종료 시 이전 런에서 획득한 유물 상태를 모두 초기화
        /// </summary>
        public void ClearRunState()
        {
            _allRelics.Clear();
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
        }
    }
}

