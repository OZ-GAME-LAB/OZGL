using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Effects.Models;

namespace OzGameLab01.Managers
{
    public class RelicManager : Singleton<RelicManager>, OzGameLab01.Effects.Contracts.IEffectsNotificationSource
    {
        private void OnDestroy()
        {
            _notifications.ClearSubscribers();
            _listeners.ClearAllListeners();
        }

        private readonly OzGameLab01.Effects.Controllers.EffectsNotificationPublisher _notifications = new OzGameLab01.Effects.Controllers.EffectsNotificationPublisher();
        public event System.Action<OzGameLab01.Effects.Models.EffectsNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        // 전체 보유 유물 목록
        private readonly RelicCollectionModel<RelicRuntimeInstance> _relics = new RelicCollectionModel<RelicRuntimeInstance>();

        private readonly EffectListenerRegistry _listeners = new EffectListenerRegistry();

        public IReadOnlyList<RelicRuntimeInstance> OwnedRelics => _relics.Items;

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
            _relics.Add(newInstance);


            newInstance.OnEquip();
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();

            SaveManager.Instance?.Facade.MarkAsDirty();
            _notifications.Publish(EffectsNotificationKind.RelicAcquired, relicId, _relics.Count);
        }

        /// <summary>
        /// dropWeight(RelicData.xlsx "확률" 열) 가중치로 무작위 유물 하나를 뽑아 즉시
        /// 획득시킵니다. 이미 보유한 유물은 후보에서 제외하고, 전부 보유한 극단적인
        /// 경우에만 중복을 허용합니다. 뽑을 유물이 전혀 없으면 null을 반환합니다.
        /// </summary>
        public RelicData AcquireRandomRelic()
        {
            List<int> ownedIds = new List<int>();
            foreach (RelicRuntimeInstance owned in OwnedRelics)
            {
                if (owned?.Data != null) ownedIds.Add(owned.Data.id);
            }
            RelicData picked = RelicSelectionModel.Select(RuntimeDataManager.Instance.Relics,
                ownedIds, count => Random.Range(0, count), () => Random.value);
            if (picked == null) return null;
            AcquireRelic(picked.id);
            return picked;
        }

        /// <summary>
        /// 유물 세이브 데이터 복원
        /// </summary>
        /// <param name="saveEntries"></param>
        public void RestoreFromSave(List<RelicSaveEntry> saveEntries)
        {
            // [수정] 보유 목록뿐 아니라 이전 런의 공격 및 주사위 발동 목록도 함께 초기화
            ClearRunStateCore();

            foreach (var entry in saveEntries)
            {
                RelicData data = RuntimeDataManager.Instance.GetRelic(entry.relicId);
                if (data == null) continue;

                var runtime = new RelicRuntimeInstance(data);
                _relics.Add(runtime);
                runtime.OnEquip();
            }

            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
            _notifications.Publish(EffectsNotificationKind.RelicsRestored, 0, _relics.Count);
        }

        /// <summary>
        /// New Game과 런 종료 시 이전 런에서 획득한 유물 상태를 모두 초기화
        /// </summary>
        public void ClearRunState()
        {
            ClearRunStateCore();
            _notifications.Publish(EffectsNotificationKind.RelicsCleared, 0, 0);
        }

        private void ClearRunStateCore()
        {
            _relics.Clear();
            _listeners.ClearAllListeners();
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
        }

        /// <summary>
        /// 유물 분류 메서드
        /// </summary>
        /// <param name="instance"></param>
        public void RegisterRuntimeRelic(RelicRuntimeInstance instance)
        {
            _listeners.RegisterListener(instance?.Logic);
        }

        public void DispatchAttack()
        {
            _listeners.DispatchAttack();
        }

        public void DispatchDiceRoll()
        {
            _listeners.DispatchDiceRoll();
        }
    }
}
