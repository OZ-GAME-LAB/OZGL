using OzGameLab01.Board.Models;
using UnityEngine;

namespace OzGameLab01.Board.Views
{
    /// <summary>
    /// 시간대별 보드 곱셈 셰이더 색상을 아트 작업자가 조절하는 팔레트입니다.
    /// 흰색은 원본 색상을 유지하며, RGB 값이 낮을수록 해당 채널이 어두워집니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardTimeOfDayPalette",
        menuName = "OzGameLab01/Board/Time Of Day Palette")]
    public sealed class BoardTimeOfDayPalette : ScriptableObject
    {
        [Header("시간대 셰이더 색상")]
        [Tooltip("낮 시간대에 보드 전체에 곱할 색상입니다. 흰색이면 원본 색상을 유지합니다.")]
        [SerializeField, ColorUsage(false, false)] private Color dayTint = Color.white;

        [Tooltip("두 번째 시간대(점심/저녁)에 보드 전체에 곱할 색상입니다.")]
        [SerializeField, ColorUsage(false, false)] private Color noonTint = new Color(1f, 0.9f, 0.72f, 1f);

        [Tooltip("밤 시간대에 보드 전체에 곱할 색상입니다.")]
        [SerializeField, ColorUsage(false, false)] private Color nightTint = new Color(0.42f, 0.5f, 0.72f, 1f);

        public Color GetTint(BoardTimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                BoardTimeOfDay.Noon => noonTint,
                BoardTimeOfDay.Night => nightTint,
                _ => dayTint
            };
        }
    }
}
