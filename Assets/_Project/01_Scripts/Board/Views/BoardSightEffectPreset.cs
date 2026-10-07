using System;
using UnityEngine;

namespace OzGameLab01.UI
{
    [CreateAssetMenu(fileName = "BoardSightEffectPreset",menuName = "OzGameLab01/UI/Board Sight Effect Preset")]
    public sealed class BoardSightEffectPreset : ScriptableObject
    {
        [Serializable]
        public sealed class LayerPreset
        {
            [Header("Color")]
            [SerializeField] private Color layerColor = Color.white;
            [SerializeField] private Color shadowColor = new (0f, 0f, 0f, 0.25f);

            [Header("Inner Hole")]
            [SerializeField, Range(0f, 1f)] private float holeRadius = 0.35f;
            [SerializeField, Range(0.001f, 0.15f)] private float innerSoftness = 0.015f;

            [Header("Outer Edge")]
            [SerializeField, Range(0.1f, 1.5f)] private float outerRadius = 0.47f;
            [SerializeField, Range(0.001f, 0.2f)] private float outerSoftness = 0.02f;

            [Header("Inner Shape")]
            [SerializeField, Range(2f, 20f)] private float lobeCount = 8f;
            [SerializeField, Range(0f, 0.2f)] private float lobeStrength = 0.045f;
            [SerializeField, Range(1f, 20f)] private float noiseScale = 5f;
            [SerializeField, Range(0f, 0.3f)] private float noiseStrength = 0.06f;
            [SerializeField, Range(5f, 50f)] private float fineNoiseScale = 18f;
            [SerializeField, Range(0f, 0.1f)] private float fineNoiseStrength = 0.015f;
            [SerializeField] private Vector4 noiseOffset;

            [Header("Outer Shape")]
            [SerializeField, Range(2f, 20f)] private float outerLobeCount = 7f;
            [SerializeField, Range(0f, 0.2f)] private float outerLobeStrength = 0.035f;
            [SerializeField, Range(1f, 20f)] private float outerNoiseScale = 4f;
            [SerializeField, Range(0f, 0.3f)] private float outerNoiseStrength = 0.025f;
            [SerializeField, Range(5f, 50f)] private float outerFineNoiseScale = 15f;
            [SerializeField, Range(0f, 0.1f)] private float outerFineNoiseStrength = 0.006f;
            [SerializeField] private Vector4 outerNoiseOffset = new (2.37f, 4.13f, 1.72f, 3.51f);

            [Header("Wobble")]
            [SerializeField, Range(0f, 3f)] private float wobbleSpeed = 0.6f;
            [SerializeField, Range(0f, 0.1f)] private float wobbleStrength = 0.012f;
            [SerializeField, Range(0f, 0.1f)] private float outerWobbleStrength = 0.012f;
            [SerializeField, Range(1f, 20f)] private float wobbleScale = 6f;
            [SerializeField, Range(0f, 0.2f)] private float noiseDrift = 0.025f;

            [Header("Shadow")]
            [SerializeField, Range(0f, 0.2f)] private float shadowExpand = 0.025f;
            [SerializeField, Range(0f, 0.2f)] private float outerShadowExpand = 0.025f;
            [SerializeField, Range(-0.2f, 0.2f)] private float shadowOffsetX;
            [SerializeField, Range(-0.2f, 0.2f)] private float shadowOffsetY = -0.015f;

            public Color LayerColor => layerColor;
            public Color ShadowColor => shadowColor;

            public float HoleRadius => holeRadius;
            public float InnerSoftness => innerSoftness;

            public float OuterRadius => outerRadius;
            public float OuterSoftness => outerSoftness;

            public float LobeCount => lobeCount;
            public float LobeStrength => lobeStrength;

            public float NoiseScale => noiseScale;
            public float NoiseStrength => noiseStrength;

            public float FineNoiseScale => fineNoiseScale;
            public float FineNoiseStrength => fineNoiseStrength;

            public Vector4 NoiseOffset => noiseOffset;

            public float OuterLobeCount => outerLobeCount;
            public float OuterLobeStrength => outerLobeStrength;

            public float OuterNoiseScale => outerNoiseScale;
            public float OuterNoiseStrength => outerNoiseStrength;

            public float OuterFineNoiseScale => outerFineNoiseScale;
            public float OuterFineNoiseStrength => outerFineNoiseStrength;

            public Vector4 OuterNoiseOffset => outerNoiseOffset;

            public float WobbleSpeed => wobbleSpeed;
            public float WobbleStrength => wobbleStrength;
            public float OuterWobbleStrength => outerWobbleStrength;
            public float WobbleScale => wobbleScale;
            public float NoiseDrift => noiseDrift;

            public float ShadowExpand => shadowExpand;
            public float OuterShadowExpand => outerShadowExpand;

            public float ShadowOffsetX => shadowOffsetX;
            public float ShadowOffsetY => shadowOffsetY;
        }

        [Header("Transition")]
        [SerializeField, Min(0f)] private float transitionDuration = 1f;
        [SerializeField, Min(0f)] private float layerDelay = 0.08f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f,0f,1f,1f);

        [Header("Layers")]
        [SerializeField] private LayerPreset[] layers = new LayerPreset[5];

        #region Public API

        /// <summary>
        /// 해당 상태로 전환할 때 사용하는 전체 전환 시간을 반환합니다.
        /// </summary>
        public float TransitionDuration => transitionDuration;

        /// <summary>
        /// 각 Fog Layer가 순차적으로 전환될 때 적용할 시간 간격을 반환합니다.
        /// </summary>
        public float LayerDelay => layerDelay;

        /// <summary>
        /// 상태 전환에 사용할 보간 곡선을 반환합니다.
        /// </summary>
        public AnimationCurve TransitionCurve => transitionCurve;

        /// <summary>
        /// 프리셋에 등록된 Fog Layer 수를 반환합니다.
        /// </summary>
        public int LayerCount => layers?.Length ?? 0;

        /// <summary>
        /// 지정한 인덱스의 Fog Layer 프리셋을 반환합니다.
        /// </summary>
        /// <param name="index">가져올 레이어 인덱스입니다.</param>
        /// <returns>
        /// 유효한 인덱스면 LayerPreset을 반환하고,
        /// 그렇지 않으면 null을 반환합니다.
        /// </returns>
        public LayerPreset GetLayer(int index)
        {
            if (layers == null || index < 0 || index >= layers.Length)
            {
                return null;
            }

            return layers[index];
        }

        #endregion
    }
}