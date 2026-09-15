using System;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Combat;
using OzGameLab01.Data;
using OzGameLab01.Managers;

namespace OzGameLab01.Player
{
    /// <summary>
    /// Player 시스템 외부(SaveManager/BattleRewardService/UnitFormationController 등)가
    /// 호출하는 유일한 진입점입니다. 실제 상태는 <see cref="PlayerState"/>가 들고 있고,
    /// 이 클래스는 조회/명령을 위임만 합니다.
    /// </summary>
    public class PlayerFacade
    {
        private readonly PlayerState _state = new PlayerState();

        /// <summary>
        /// 현재 플레이어가 보유한 전체 유닛 목록
        /// </summary>
        public IReadOnlyList<UnitData> OwnedUnits => _state.OwnedUnits;

        public event Action<UnitData> OnUnitAdded;

        /// <summary>
        /// 새 유닛을 인벤토리에 추가합니다. (나중에 맵 타일 이벤트에서 호출할 함수)
        /// </summary>
        public void AddUnit(UnitData unit)
        {
            if (unit == null) return;

            _state.AddUnit(unit);
            OnUnitAdded?.Invoke(unit);
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
            Debug.Log($"[PlayerFacade] 유닛 획득 성공! : {unit.name} (현재 총 {_state.OwnedUnits.Count}명 보유 중)");
        }

        /// <summary>
        /// 게임 오버 또는 타이틀로 돌아갈 때 인벤토리를 비우는 용도입니다.
        /// </summary>
        public void ClearInventory()
        {
            _state.Clear();
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
            Debug.Log("[PlayerFacade] 인벤토리가 초기화되었습니다.");
        }

        /// <summary>
        /// 저장된 유닛 ID와 수량을 로스터 원본 데이터로 복원합니다.
        /// </summary>
        public void RestoreFromSave(IReadOnlyList<UnitStuff> savedUnits)
        {
            _state.Clear();

            if (savedUnits == null || savedUnits.Count == 0)
            {
                RuntimeEffectManager.Instance?.RefreshFromPlayerState();
                return;
            }

            foreach (UnitStuff savedUnit in savedUnits)
            {
                if (savedUnit == null || savedUnit.amount <= 0)
                {
                    continue;
                }

                UnitData rosterUnit = FindRosterUnit(savedUnit.unitId);
                if (rosterUnit == null)
                {
                    Debug.LogWarning($"[PlayerFacade] 로스터에 없는 저장 유닛을 건너뜁니다. ID: {savedUnit.unitId}");
                    continue;
                }

                for (int amountIndex = 0; amountIndex < savedUnit.amount; amountIndex++)
                {
                    _state.AddUnit(CloneUnitData(rosterUnit));
                }
            }

            Debug.Log($"[PlayerFacade] 저장된 인벤토리를 복원했습니다. 총 {_state.OwnedUnits.Count}명");
            RuntimeEffectManager.Instance?.RefreshFromPlayerState();
        }

        /// <summary>
        /// 저장된 유닛 ID와 일치하는 로스터 원본을 찾습니다.
        /// </summary>
        private static UnitData FindRosterUnit(int unitId)
        {
            return RuntimeDataManager.Instance.GetUnit(unitId);
        }

        /// <summary>
        /// UnitRosterData가 들고 있는 원본 UnitData를 그대로 참조하면 인벤토리 쪽에서
        /// 값을 바꿀 때 로스터 에셋까지 같이 바뀌므로, 별도 인스턴스로 복제해서 지급합니다.
        /// </summary>
        public static UnitData CloneUnitData(UnitData source)
        {
            return new UnitData
            {
                id = source.id,
                name = source.name,
                spriteAddress = source.spriteAddress,
                healthPoint = source.healthPoint,
                attackPoint = source.attackPoint,
                defensePoint = source.defensePoint,
                attackSpeed = source.attackSpeed,
                criticalMult = source.criticalMult,
                criticalRate = source.criticalRate,
                dodgeRate = source.dodgeRate,
                basicAttackCooldown = source.basicAttackCooldown,
                skillIds = new List<int>(source.skillIds ?? new List<int>()),
                passiveSkillKey = source.passiveSkillKey,
                activeSkillKey = source.activeSkillKey,
                skillCooldown = source.skillCooldown,
                attackKey = source.attackKey,
                passiveEffects = new List<EffectInstance>(source.passiveEffects ?? new List<EffectInstance>()),
                color = source.color,
                jobType = source.jobType,
                tribeType = source.tribeType
            };
        }
    }
}
