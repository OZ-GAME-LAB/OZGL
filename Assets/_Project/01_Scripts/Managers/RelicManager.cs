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

        // 이벤트별 유물 분류 목록
        private readonly List<IAttackTriggerRelic> _attackRelics = new();
        private readonly List<IDiceTriggerRelic> _diceRelics = new();

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
            RegisterRuntimeRelic(newInstance);
            newInstance.OnEquip();

            SaveManager.Instance?.MarkAsDirty();
        }

        /// <summary>
        /// 유물 세이브 데이터 복원
        /// </summary>
        /// <param name="saveEntries"></param>
        public void RestoreFromSave(List<RelicSaveEntry> saveEntries)
        {
            _allRelics.Clear();

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
        /// 유물 분류 메서드
        /// </summary>
        /// <param name="instance"></param>
        public void RegisterRuntimeRelic(RelicRuntimeInstance instance)
        {
            if (instance.Logic is IAttackTriggerRelic attackRelic)
                _attackRelics.Add(attackRelic);

            if (instance.Logic is IDiceTriggerRelic diceRelic)
                _diceRelics.Add(diceRelic);
        }

        #region 이벤트 별 디스패치 루프
        public void DispatchAttack()
        {
            int count = _attackRelics.Count;
            for (int i = 0; i < count; i++)
            {
                _attackRelics[i].OnAttack();
            }
        }

        public void DispatchDiceRoll()
        {
            int count = _diceRelics.Count;
            for (int i = 0; i < count; i++)
            {
                _diceRelics[i].OnDiceRolled();
            }
        }
        #endregion
    }
}

