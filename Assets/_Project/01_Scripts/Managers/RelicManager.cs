using UnityEngine;
using System.Collections.Generic;
using OzGameLab01.Data;
using OzGameLab01.Interfaces;

namespace OzGameLab01.Managers
{
    public class RelicManager : Singleton<RelicManager>
    {
        // 전체 보유 유물 목록
        private readonly List<RelicRuntimeInstance> _allRelics = new();

        /// <summary>
        /// GameDB의 ID 기반 유물 획득
        /// </summary>
        /// <param name="relicId"> 유물 ID </param>
        public void AcquireRelic(int relicId)
        {
            // 1. GameDB에서 정적 데이터 조회
            var relicData = DataManager.Relics.Get(relicId);
            if (relicData == null)
            {
                Debug.LogError($"[RelicManager] ID: {relicId}에 해당하는 유물을 발견하지 못 했습니다.");
                return;
            }

            // 2. 런타임 인스턴스 생성, 장착
            var newInstance = new RelicRuntimeInstance(relicData);
            _allRelics.Add(newInstance);


            newInstance.OnEquip();

            SaveManager.Instance?.MarkAsDirty();
        }

        /// <summary>
        /// 유물 세이브 데이터 복원
        /// </summary>
        /// <param name="saveEntries"></param>
        public void RestoreFromSave(List<RelicSaveEntry> saveEntries)
        {
            //_allRelics.Clear();
            // [수정] 보유 목록뿐 아니라 이전 런의 공격 및 주사위 발동 목록도 함께 초기화
            ClearRunState();

            foreach (var entry in saveEntries)
            {
                RelicData data = DataManager.Relics.Get(entry.relicId);
                if (data == null) continue;

                var runtime = new RelicRuntimeInstance(data);
                _allRelics.Add(runtime);
                runtime.OnEquip();
            }
        }

        /// <summary>
        /// New Game과 런 종료 시 이전 런에서 획득한 유물 상태를 모두 초기화
        /// </summary>
        public void ClearRunState()
        {
            _allRelics.Clear();
        }
    }
}

