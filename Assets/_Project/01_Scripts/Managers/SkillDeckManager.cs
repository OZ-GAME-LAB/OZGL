using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OzGameLab01.Controllers;
using OzGameLab01.UI;

namespace OzGameLab01.Managers
{
    public class SkillDeckManager : MonoBehaviour
    {
        [SerializeField] private Unit[] allyUnits;
        [SerializeField] private float drawInterval = 3f;
        [SerializeField] private float revealPause = 0.4f;
        [SerializeField] private SkillDeckUI ui;

        private readonly List<Unit> _deck = new List<Unit>();
        private int _deckIndex;
        private float _timer;
        private bool _isDrawing;

        private void Start()
        {
            _deck.AddRange(allyUnits);
            Shuffle(_deck);
            _deckIndex = 0;
        }

        private void Update()
        {
            if (_isDrawing)
            {
                return;
            }

            _timer += Time.deltaTime;

            if (ui != null)
            {
                ui.SetTimerProgress(Mathf.Clamp01(_timer / drawInterval));
            }

            if (_timer >= drawInterval)
            {
                _timer = 0f;
                StartCoroutine(DrawCardRoutine());
            }
        }

        private IEnumerator DrawCardRoutine()
        {
            if (_deck.Count == 0)
            {
                yield break;
            }

            _isDrawing = true;

            Unit drawn = _deck[_deckIndex];
            _deckIndex++;

            if (ui != null)
            {
                ui.ShowDrawnCard(drawn);
                yield return StartCoroutine(ui.MoveCardToCenter());
                StartCoroutine(ui.PlayGlow());
            }

            if (drawn != null && !drawn.IsDead)
            {
                drawn.UseSkill();
            }

            yield return new WaitForSeconds(revealPause);

            if (ui != null)
            {
                yield return StartCoroutine(ui.MoveCardToDiscard());
            }

            if (_deckIndex >= _deck.Count)
            {
                Shuffle(_deck);
                _deckIndex = 0;

                if (ui != null)
                {
                    ui.ResetDiscardPile();
                }
            }

            _isDrawing = false;
        }

        private void Shuffle(List<Unit> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
