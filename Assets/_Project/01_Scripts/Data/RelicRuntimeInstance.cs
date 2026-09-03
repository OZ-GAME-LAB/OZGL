using UnityEngine;
using OzGameLab01.Interfaces;

namespace OzGameLab01.Data
{
    /// <summary>
    /// RelicData  : 유물의 정적 데이터
    /// RelicLogic : 유물의 동작 로직
    /// 
    /// 게임 실행 중 실제 적용을 위해 정보를 묶은 런타임 인스턴스 클래스
    /// </summary>
    public class RelicRuntimeInstance
    {
        public RelicData Data { get; }
        public RelicLogic Logic { get; }

        public RelicRuntimeInstance(RelicData data)
        {
            Data = data;

            Logic = RelicFactory.CreateLogic(data.relicLogic);
            Logic?.Initialize(Data, this);
        }

        public void OnEquip() => Logic?.OnEquip();
        public void OnAttack() => Logic?.OnAttack();
        public void OnDiceRolled() => Logic?.OnDiceRolled();
    }
}