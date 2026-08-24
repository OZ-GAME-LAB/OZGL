using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OzGameLab01.Controllers;
using OzGameLab01.UI;

namespace OzGameLab01.Managers
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private Transform tilesRoot;
        [SerializeField] private Transform token;
        [SerializeField] private Button rollButton;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private float hopDuration = 0.25f;
        [SerializeField] private float combatTilePause = 0.5f;
        [SerializeField] private DiceRoller diceRoller;
        [SerializeField] private LevelUpSelector levelUpSelector;
        [SerializeField] private PartyStatusUI partyStatusUI;

        private MapTile[] _tiles;
        private int _currentIndex;
        private bool _isMoving;

        private void Awake()
        {
            int childCount = tilesRoot.childCount;
            _tiles = new MapTile[childCount];
            for (int i = 0; i < childCount; i++)
            {
                _tiles[i] = tilesRoot.GetChild(i).GetComponent<MapTile>();
            }

            _currentIndex = SceneTransitioner.MapTileIndex;
            token.position = tilesRoot.GetChild(_currentIndex).position;

            rollButton.onClick.AddListener(OnRollClicked);
        }

        private void OnEventLevelUpComplete()
        {
            rollButton.interactable = true;

            if (partyStatusUI != null)
            {
                partyStatusUI.Refresh();
            }
        }

        private void OnRollClicked()
        {
            if (_isMoving)
            {
                return;
            }

            StartCoroutine(RollAndMove());
        }

        private IEnumerator RollAndMove()
        {
            _isMoving = true;
            rollButton.interactable = false;

            int roll = Random.Range(1, 7);

            if (diceRoller != null)
            {
                yield return StartCoroutine(diceRoller.PlayRoll(roll));
            }

            if (resultText != null)
            {
                resultText.text = roll.ToString();
            }

            yield return StartCoroutine(MoveToken(roll));
        }

        private IEnumerator MoveToken(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                _currentIndex = (_currentIndex + 1) % _tiles.Length;
                Vector3 startPos = token.position;
                Vector3 endPos = tilesRoot.GetChild(_currentIndex).position;

                float elapsed = 0f;
                while (elapsed < hopDuration)
                {
                    elapsed += Time.deltaTime;
                    token.position = Vector3.Lerp(startPos, endPos, elapsed / hopDuration);
                    yield return null;
                }

                token.position = endPos;
            }

            _isMoving = false;

            MapTile finalTile = _tiles[_currentIndex];
            if (finalTile != null && finalTile.tileType == MapTile.TileType.Combat)
            {
                SceneTransitioner.MapTileIndex = _currentIndex;
                yield return new WaitForSeconds(combatTilePause);
                SceneTransitioner.Instance.LoadScene("CombatScene");
            }
            else if (finalTile != null && finalTile.tileType == MapTile.TileType.Event)
            {
                if (levelUpSelector != null)
                {
                    levelUpSelector.Show(OnEventLevelUpComplete);
                }
                else
                {
                    rollButton.interactable = true;
                }
            }
            else
            {
                rollButton.interactable = true;
            }
        }
    }
}
