using System;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Data;
using OzGameLab01.Effects.Contracts;
using OzGameLab01.Effects.Controllers;
using OzGameLab01.Managers;
using OzGameLab01.Save;
using OzGameLab01.Common;

namespace OzGameLab01.Effects.Models
{
    /// <summary>
    /// RelicManager가 노출하는 유일한 진입점입니다. 보유 유물 목록 관리와 획득/복원/초기화를
    /// 담당합니다. RuntimeEffectManager는 같은 Effects 도메인이라 Facade를 직접 참조하고,
    /// SaveManager처럼 다른 도메인은 SystemBus를 거칩니다.
    /// </summary>
    public sealed class RelicFacade : IEffectsNotificationSource
    {
        private readonly EffectsNotificationPublisher _notifications = new EffectsNotificationPublisher();
        public event Action<EffectsNotification> Notification
        {
            add => _notifications.Notification += value;
            remove => _notifications.Notification -= value;
        }

        private readonly RelicCollectionModel<RelicData> _relics = new RelicCollectionModel<RelicData>();

        public IReadOnlyList<RelicData> OwnedRelics => _relics.Items;

        /// <summary>
        /// GameDB의 ID 기반 유물 획득
        /// </summary>
        /// <param name="relicId"> 유물 ID </param>
        public void AcquireRelic(int relicId)
        {
            // 1. 정적 데이터 조회 (RuntimeDataManager 단일 진입점)
            var relicData = OzGameLab01.Data.RuntimeContent.Catalog.GetRelic(relicId);
            if (relicData == null)
            {
                Debug.LogError($"[RelicFacade] ID: {relicId}에 해당하는 유물을 발견하지 못 했습니다.");
                return;
            }

            // 2. 보유 목록에 추가하고 효과 캐시를 갱신
            _relics.Add(relicData);
            RuntimeEffectManager.Instance.Facade.RefreshFromPlayerState();

            SystemBus.Get<SaveFacade>()?.MarkAsDirty();
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
            foreach (RelicData owned in OwnedRelics)
            {
                if (owned != null) ownedIds.Add(owned.id);
            }
            RelicData picked = RelicSelectionModel.Select(OzGameLab01.Data.RuntimeContent.Catalog.Relics,
                ownedIds, count => UnityEngine.Random.Range(0, count), () => UnityEngine.Random.value);
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
                RelicData data = OzGameLab01.Data.RuntimeContent.Catalog.GetRelic(entry.relicId);
                if (data == null) continue;

                _relics.Add(data);
            }

            RuntimeEffectManager.Instance.Facade.RefreshFromPlayerState();
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
            RuntimeEffectManager.Instance.Facade.RefreshFromPlayerState();
        }

        /// <summary>매니저 종료 시 대기 중인 알림과 리스너를 정리합니다.</summary>
        public void ClearSubscriptions()
        {
            _notifications.ClearSubscribers();
        }
    }
}
