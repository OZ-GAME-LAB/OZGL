using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OzGameLab01.Data;
using OzGameLab01.Managers;
using UnityEngine;

namespace OzGameLab01.UI
{
    /// <summary>
    /// 전투 예정 적 데이터를 CombatInfoView에 표시하는 Presenter입니다.
    /// View는 표시만 담당하고, 데이터 해석과 비동기 스프라이트 로드는 이 클래스가 담당합니다.
    /// </summary>
    public sealed class CombatInfoPresenter
    {
        private readonly CombatInfoView _view;
        private int _requestVersion;

        public bool IsAvailable => _view != null;

        public CombatInfoPresenter(CombatInfoView view)
        {
            _view = view;
        }

        public async void Show(MonsterData enemy, Func<bool> battleRequested)
        {
            if (_view == null || enemy == null)
            {
                return;
            }

            int requestVersion = ++_requestVersion;

            _view.Clear();
            _view.SetEnemy(enemy.name, null);
            PopulateStats(enemy);
            _view.SetBattleInteractable(false);
            _view.SetBattleAction(() => HandleBattleRequested(requestVersion, battleRequested));
            _view.SetVisible(true);

            List<SkillData> skills = ResolveSkills(enemy.skillIds);

            try
            {
                Task<Sprite> enemySpriteTask = SpriteManager.GetSpriteAsync(enemy.spriteAddress);
                Task<Sprite>[] skillSpriteTasks = new Task<Sprite>[skills.Count];

                for (int i = 0; i < skills.Count; i++)
                {
                    skillSpriteTasks[i] = SpriteManager.GetSpriteAsync(skills[i].iconAddress);
                }

                Sprite enemySprite = await enemySpriteTask;
                Sprite[] skillSprites = await Task.WhenAll(skillSpriteTasks);

                if (!IsCurrentRequest(requestVersion))
                {
                    return;
                }

                _view.SetEnemy(enemy.name, enemySprite);

                for (int i = 0; i < skills.Count; i++)
                {
                    SkillData skill = skills[i];
                    _view.AddSkill(
                        skillSprites[i],
                        skill.name,
                        BuildSkillDescription(skill));
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (!IsCurrentRequest(requestVersion))
                {
                    return;
                }

                // 이미지 로드가 실패해도 전투 진행이 막히지 않도록 텍스트 정보는 표시합니다.
                for (int i = 0; i < skills.Count; i++)
                {
                    SkillData skill = skills[i];
                    _view.AddSkill(null, skill.name, BuildSkillDescription(skill));
                }
            }

            if (IsCurrentRequest(requestVersion))
            {
                _view.SetBattleInteractable(true);
            }
        }

        public void Hide()
        {
            _requestVersion++;

            if (_view == null)
            {
                return;
            }

            _view.SetBattleInteractable(false);
            _view.Clear();
            _view.SetVisible(false);
        }

        private void HandleBattleRequested(int requestVersion, Func<bool> battleRequested)
        {
            if (!IsCurrentRequest(requestVersion))
            {
                return;
            }

            _view.SetBattleInteractable(false);

            bool accepted = battleRequested?.Invoke() ?? false;
            if (accepted)
            {
                Hide();
                return;
            }

            if (IsCurrentRequest(requestVersion))
            {
                _view.SetBattleInteractable(true);
            }
        }

        private bool IsCurrentRequest(int requestVersion)
        {
            return _view != null && requestVersion == _requestVersion;
        }

        private void PopulateStats(MonsterData enemy)
        {
            AddStat(enemy.healthPoint.ToString(), "체력", "전투 시작 시 적의 최대 체력입니다.");
            AddStat(enemy.attackPoint.ToString(), "공격력", "적의 기본 공격 피해에 사용되는 공격력입니다.");
            AddStat(enemy.defensePoint.ToString("0.##"), "방어력", "적이 받는 피해를 줄이는 방어력입니다.");
            AddStat($"{enemy.criticalRate}%", "치명타 확률", "적의 공격이 치명타로 적용될 확률입니다.");
            AddStat($"{enemy.criticalMult:0.##}%", "치명타 배율", "치명타가 발생했을 때 적용되는 피해 배율입니다.");
            AddStat($"{enemy.dodgeRate}%", "회피율", "적이 공격을 회피할 확률입니다.");
            AddStat(enemy.attackSpeed.ToString("0.##"), "공격 속도", "적의 기본 공격 주기입니다.");
        }

        private void AddStat(string value, string title, string description)
        {
            // 전용 스탯 아이콘 데이터가 아직 없으므로 값과 Hover 설명을 우선 표시합니다.
            _view.AddStat(null, value, title, description);
        }

        private static List<SkillData> ResolveSkills(IReadOnlyList<int> skillIds)
        {
            var skills = new List<SkillData>();
            if (skillIds == null)
            {
                return skills;
            }

            for (int i = 0; i < skillIds.Count; i++)
            {
                SkillData skill = RuntimeContent.Catalog.GetSkill(skillIds[i]);
                if (skill != null)
                {
                    skills.Add(skill);
                }
            }

            return skills;
        }

        private static string BuildSkillDescription(SkillData skill)
        {
            if (skill == null)
            {
                return string.Empty;
            }

            if (skill.cooldown <= 0f)
            {
                return skill.description ?? string.Empty;
            }

            string cooldown = $"쿨다운: {skill.cooldown:0.##}";
            return string.IsNullOrWhiteSpace(skill.description)
                ? cooldown
                : $"{skill.description}\n{cooldown}";
        }
    }
}
