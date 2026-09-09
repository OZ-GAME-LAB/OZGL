using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    public enum BoardSightState
    {
        State1,
        State2,
        State3
    }

    [DisallowMultipleComponent]
    public sealed class BoardSightEffectView : MonoBehaviour
    {
        [Serializable]
        private sealed class SightLayer
        {
            [SerializeField] private Image layerImage;
            [SerializeField] private Image shadowImage;

            public Image LayerImage => layerImage;
            public Image ShadowImage => shadowImage;

            [NonSerialized] public Material LayerMaterial;
            [NonSerialized] public Material ShadowMaterial;
        }

        private struct LayerRuntimeState
        {
            public Color LayerColor;
            public Color ShadowColor;

            public float HoleRadius;
            public float InnerSoftness;

            public float OuterRadius;
            public float OuterSoftness;

            public float LobeCount;
            public float LobeStrength;

            public float NoiseScale;
            public float NoiseStrength;

            public float FineNoiseScale;
            public float FineNoiseStrength;

            public Vector4 NoiseOffset;

            public float OuterLobeCount;
            public float OuterLobeStrength;

            public float OuterNoiseScale;
            public float OuterNoiseStrength;

            public float OuterFineNoiseScale;
            public float OuterFineNoiseStrength;

            public Vector4 OuterNoiseOffset;

            public float WobbleSpeed;
            public float WobbleStrength;
            public float OuterWobbleStrength;
            public float WobbleScale;
            public float NoiseDrift;

            public float ShadowExpand;
            public float OuterShadowExpand;

            public float ShadowOffsetX;
            public float ShadowOffsetY;

            public static LayerRuntimeState FromPreset(BoardSightEffectPreset.LayerPreset preset)
            {
                return new LayerRuntimeState
                {
                    LayerColor = preset.LayerColor,
                    ShadowColor = preset.ShadowColor,
                    HoleRadius = preset.HoleRadius,
                    InnerSoftness = preset.InnerSoftness,
                    OuterRadius = preset.OuterRadius,
                    OuterSoftness = preset.OuterSoftness,
                    LobeCount = preset.LobeCount,
                    LobeStrength = preset.LobeStrength,
                    NoiseScale = preset.NoiseScale,
                    NoiseStrength = preset.NoiseStrength,
                    FineNoiseScale = preset.FineNoiseScale,
                    FineNoiseStrength = preset.FineNoiseStrength,
                    NoiseOffset = preset.NoiseOffset,
                    OuterLobeCount = preset.OuterLobeCount,
                    OuterLobeStrength = preset.OuterLobeStrength,
                    OuterNoiseScale = preset.OuterNoiseScale,
                    OuterNoiseStrength = preset.OuterNoiseStrength,
                    OuterFineNoiseScale = preset.OuterFineNoiseScale,
                    OuterFineNoiseStrength = preset.OuterFineNoiseStrength,
                    OuterNoiseOffset = preset.OuterNoiseOffset,
                    WobbleSpeed = preset.WobbleSpeed,
                    WobbleStrength = preset.WobbleStrength,
                    OuterWobbleStrength = preset.OuterWobbleStrength,
                    WobbleScale = preset.WobbleScale,
                    NoiseDrift = preset.NoiseDrift,
                    ShadowExpand = preset.ShadowExpand,
                    OuterShadowExpand = preset.OuterShadowExpand,
                    ShadowOffsetX = preset.ShadowOffsetX,
                    ShadowOffsetY = preset.ShadowOffsetY
                };
            }

            public static LayerRuntimeState Lerp(LayerRuntimeState from,LayerRuntimeState to,float t)
            {
                return new LayerRuntimeState
                {
                    LayerColor = Color.Lerp(from.LayerColor,to.LayerColor,t),
                    ShadowColor = Color.Lerp(from.ShadowColor,to.ShadowColor,t),
                    HoleRadius = Mathf.Lerp(from.HoleRadius,to.HoleRadius,t),
                    InnerSoftness = Mathf.Lerp(from.InnerSoftness,to.InnerSoftness,t),
                    OuterRadius = Mathf.Lerp(from.OuterRadius,to.OuterRadius,t),
                    OuterSoftness = Mathf.Lerp(from.OuterSoftness,to.OuterSoftness,t),
                    LobeCount = Mathf.Lerp(from.LobeCount,to.LobeCount,t),
                    LobeStrength = Mathf.Lerp(from.LobeStrength,to.LobeStrength,t),
                    NoiseScale = Mathf.Lerp(from.NoiseScale,to.NoiseScale,t),
                    NoiseStrength = Mathf.Lerp(from.NoiseStrength,to.NoiseStrength,t),
                    FineNoiseScale = Mathf.Lerp(from.FineNoiseScale,to.FineNoiseScale,t),
                    FineNoiseStrength = Mathf.Lerp(from.FineNoiseStrength,to.FineNoiseStrength,t),
                    NoiseOffset = Vector4.Lerp(from.NoiseOffset,to.NoiseOffset,t),
                    OuterLobeCount = Mathf.Lerp(from.OuterLobeCount,to.OuterLobeCount,t),
                    OuterLobeStrength = Mathf.Lerp(from.OuterLobeStrength,to.OuterLobeStrength,t),
                    OuterNoiseScale = Mathf.Lerp(from.OuterNoiseScale,to.OuterNoiseScale,t),
                    OuterNoiseStrength = Mathf.Lerp(from.OuterNoiseStrength,to.OuterNoiseStrength,t),
                    OuterFineNoiseScale = Mathf.Lerp(from.OuterFineNoiseScale,to.OuterFineNoiseScale,t),
                    OuterFineNoiseStrength = Mathf.Lerp(from.OuterFineNoiseStrength,to.OuterFineNoiseStrength,t),
                    OuterNoiseOffset = Vector4.Lerp(from.OuterNoiseOffset,to.OuterNoiseOffset,t),
                    WobbleSpeed = Mathf.Lerp(from.WobbleSpeed,to.WobbleSpeed,t),
                    WobbleStrength = Mathf.Lerp(from.WobbleStrength,to.WobbleStrength,t),
                    OuterWobbleStrength = Mathf.Lerp(from.OuterWobbleStrength,to.OuterWobbleStrength,t),
                    WobbleScale = Mathf.Lerp(from.WobbleScale,to.WobbleScale,t),
                    NoiseDrift = Mathf.Lerp(from.NoiseDrift,to.NoiseDrift,t),
                    ShadowExpand = Mathf.Lerp(from.ShadowExpand,to.ShadowExpand,t),
                    OuterShadowExpand = Mathf.Lerp(from.OuterShadowExpand,to.OuterShadowExpand,t),
                    ShadowOffsetX = Mathf.Lerp(from.ShadowOffsetX,to.ShadowOffsetX,t),
                    ShadowOffsetY = Mathf.Lerp(from.ShadowOffsetY,to.ShadowOffsetY,t)
                };
            }
        }

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform sightRect;

        [Header("Materials")]
        [SerializeField] private Material fogBaseMaterial;
        [SerializeField] private Material shadowBaseMaterial;

        [Header("Layers")]
        [SerializeField] private SightLayer[] layers;

        [Header("State")]
        [SerializeField] private BoardSightState initialState = BoardSightState.State1;

        [SerializeField] private BoardSightEffectPreset state1Preset;
        [SerializeField] private BoardSightEffectPreset state2Preset;
        [SerializeField] private BoardSightEffectPreset state3Preset;

        private static readonly int HoleRadiusId = Shader.PropertyToID("_HoleRadius");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int OuterRadiusId = Shader.PropertyToID("_OuterRadius");
        private static readonly int OuterSoftnessId = Shader.PropertyToID("_OuterSoftness");
        private static readonly int LobeCountId = Shader.PropertyToID("_LobeCount");
        private static readonly int LobeStrengthId = Shader.PropertyToID("_LobeStrength");
        private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");
        private static readonly int FineNoiseScaleId = Shader.PropertyToID("_FineNoiseScale");
        private static readonly int FineNoiseStrengthId = Shader.PropertyToID("_FineNoiseStrength");
        private static readonly int NoiseOffsetId = Shader.PropertyToID("_NoiseOffset");
        private static readonly int OuterLobeCountId = Shader.PropertyToID("_OuterLobeCount");
        private static readonly int OuterLobeStrengthId = Shader.PropertyToID("_OuterLobeStrength");
        private static readonly int OuterNoiseScaleId = Shader.PropertyToID("_OuterNoiseScale");
        private static readonly int OuterNoiseStrengthId = Shader.PropertyToID("_OuterNoiseStrength");
        private static readonly int OuterFineNoiseScaleId = Shader.PropertyToID("_OuterFineNoiseScale");
        private static readonly int OuterFineNoiseStrengthId = Shader.PropertyToID("_OuterFineNoiseStrength");
        private static readonly int OuterNoiseOffsetId = Shader.PropertyToID("_OuterNoiseOffset");
        private static readonly int WobbleSpeedId = Shader.PropertyToID("_WobbleSpeed");
        private static readonly int WobbleStrengthId = Shader.PropertyToID("_WobbleStrength");
        private static readonly int OuterWobbleStrengthId = Shader.PropertyToID("_OuterWobbleStrength");
        private static readonly int WobbleScaleId = Shader.PropertyToID("_WobbleScale");
        private static readonly int NoiseDriftId = Shader.PropertyToID("_NoiseDrift");
        private static readonly int ShadowExpandId = Shader.PropertyToID("_ShadowExpand");
        private static readonly int OuterShadowExpandId = Shader.PropertyToID("_OuterShadowExpand");
        private static readonly int ShadowOffsetXId = Shader.PropertyToID("_ShadowOffsetX");
        private static readonly int ShadowOffsetYId = Shader.PropertyToID("_ShadowOffsetY");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private Coroutine _transitionRoutine;
        private BoardSightState _currentState;
        private bool _isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeMaterials();
            UpdateAspect();

            BoardSightEffectPreset preset = GetPreset(initialState);

            if (preset != null)
            {
                ApplyPresetImmediate(preset);
            }

            _currentState = initialState;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateAspect();
        }

        private void OnDestroy()
        {
            StopTransition();
            ReleaseMaterials();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 현재 적용된 시야 상태를 반환합니다.
        /// </summary>
        public BoardSightState CurrentState => _currentState;

        /// <summary>
        /// 지정한 시야 상태로 전환합니다.
        /// 해당 상태의 Preset에 설정된 Transition Duration,
        /// Layer Delay, Animation Curve를 사용합니다.
        /// </summary>
        /// <param name="state">
        /// 전환할 시야 상태입니다.
        /// </param>
        /// <param name="immediate">
        /// true면 애니메이션 없이 즉시 적용합니다.
        /// </param>
        public void SetState(BoardSightState state,bool immediate = false)
        {
            if (!_isInitialized)
            {
                return;
            }

            BoardSightEffectPreset preset = GetPreset(state);

            if (preset == null)
            {
                return;
            }

            StopTransition();

            if (immediate)
            {
                ApplyPresetImmediate(preset);

                _currentState = state;

                return;
            }

            _transitionRoutine = StartCoroutine(TransitionRoutine(state,preset));
        }

        /// <summary>
        /// 현재 설정된 초기 상태와 Preset으로
        /// 시야 효과를 즉시 복구합니다.
        /// </summary>
        public void ResetView()
        {
            StopTransition();

            BoardSightEffectPreset preset = GetPreset(initialState);

            if (preset != null)
            {
                ApplyPresetImmediate(preset);
            }

            _currentState = initialState;

            SetVisible(true);
        }

        /// <summary>
        /// 시야 효과 전체의 표시 여부를 변경합니다.
        /// 현재 상태와 Material 값은 유지합니다.
        /// </summary>
        /// <param name="isVisible">
        /// true면 표시하고 false면 숨깁니다.
        /// </param>
        public void SetVisible(bool isVisible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = isVisible ? 1f : 0f;
        }

        #endregion

        #region State

        private BoardSightEffectPreset GetPreset(BoardSightState state)
        {
            switch (state)
            {
                case BoardSightState.State1:
                    return state1Preset;

                case BoardSightState.State2:
                    return state2Preset;

                case BoardSightState.State3:
                    return state3Preset;

                default:
                    return null;
            }
        }

        private IEnumerator TransitionRoutine(BoardSightState targetState,BoardSightEffectPreset preset)
        {
            int count = Mathf.Min(layers.Length,preset.LayerCount);

            LayerRuntimeState[] startStates = new LayerRuntimeState[count];
            LayerRuntimeState[] targetStates = new LayerRuntimeState[count];

            for (int i = 0; i < count; i++)
            {
                BoardSightEffectPreset.LayerPreset targetPreset = preset.GetLayer(i);

                if (targetPreset == null)
                {
                    continue;
                }

                startStates[i] = CaptureCurrentState(layers[i]);
                targetStates[i] = LayerRuntimeState.FromPreset(targetPreset);
            }

            float duration = Mathf.Max(0f,preset.TransitionDuration);
            float layerDelay = Mathf.Max(0f,preset.LayerDelay);

            if (duration <= 0f)
            {
                ApplyPresetImmediate(preset);

                _currentState = targetState;
                _transitionRoutine = null;

                yield break;
            }

            float totalDuration = duration + layerDelay * Mathf.Max(0,count - 1);
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                for (int i = 0; i < count; i++)
                {
                    float layerElapsed = elapsed -layerDelay * i;

                    if (layerElapsed < 0f)
                    {
                        continue;
                    }

                    float normalized = Mathf.Clamp01(layerElapsed / duration);
                    float eased = preset.TransitionCurve != null ? preset.TransitionCurve.Evaluate(normalized) : normalized;

                    LayerRuntimeState state = LayerRuntimeState.Lerp(startStates[i],targetStates[i],eased);

                    ApplyRuntimeState(layers[i],state);
                }

                elapsed += Time.unscaledDeltaTime;

                yield return null;
            }

            ApplyPresetImmediate(preset);

            _currentState = targetState;
            _transitionRoutine = null;
        }

        private void StopTransition()
        {
            if (_transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        #endregion

        #region Initialization

        private void InitializeMaterials()
        {
            if (layers == null || fogBaseMaterial == null || shadowBaseMaterial == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                SightLayer layer = layers[i];

                if (layer == null)
                {
                    continue;
                }

                CreateLayerMaterials(layer,i);
            }

            _isInitialized = true;
        }

        private void CreateLayerMaterials(SightLayer layer,int index)
        {
            if (layer.LayerImage != null)
            {
                layer.LayerMaterial = new Material(fogBaseMaterial);
                layer.LayerMaterial.name = $"{fogBaseMaterial.name}_Layer_{index + 1}";
                layer.LayerImage.material =layer.LayerMaterial;
            }

            if (layer.ShadowImage != null)
            {
                layer.ShadowMaterial = new Material(shadowBaseMaterial);
                layer.ShadowMaterial.name = $"{shadowBaseMaterial.name}_Layer_{index + 1}";
                layer.ShadowImage.material = layer.ShadowMaterial;
            }
        }

        #endregion

        #region Preset

        private void ApplyPresetImmediate(BoardSightEffectPreset preset)
        {
            if (preset == null || layers == null)
            {
                return;
            }

            int count = Mathf.Min(layers.Length,preset.LayerCount);

            for (int i = 0; i < count; i++)
            {
                BoardSightEffectPreset.LayerPreset layerPreset = preset.GetLayer(i);

                if (layerPreset == null)
                {
                    continue;
                }

                LayerRuntimeState state = LayerRuntimeState.FromPreset(layerPreset);
                ApplyRuntimeState(layers[i], state);
            }
        }

        private LayerRuntimeState CaptureCurrentState( SightLayer layer)
        {
            LayerRuntimeState state = new LayerRuntimeState();

            if (layer.LayerImage != null)
            {
                state.LayerColor =layer.LayerImage.color;
            }

            if (layer.ShadowImage != null)
            {
                state.ShadowColor = layer.ShadowImage.color;
            }

            Material material = layer.LayerMaterial;

            if (material != null)
            {
                state.HoleRadius = material.GetFloat(HoleRadiusId);
                state.InnerSoftness = material.GetFloat(SoftnessId);
                state.OuterRadius = material.GetFloat(OuterRadiusId);
                state.OuterSoftness = material.GetFloat(OuterSoftnessId);
                state.LobeCount = material.GetFloat(LobeCountId);
                state.LobeStrength = material.GetFloat(LobeStrengthId);
                state.NoiseScale = material.GetFloat(NoiseScaleId);
                state.NoiseStrength = material.GetFloat(NoiseStrengthId);
                state.FineNoiseScale = material.GetFloat(FineNoiseScaleId);
                state.FineNoiseStrength = material.GetFloat(FineNoiseStrengthId);
                state.NoiseOffset = material.GetVector(NoiseOffsetId);
                state.OuterLobeCount = material.GetFloat(OuterLobeCountId);
                state.OuterLobeStrength = material.GetFloat(OuterLobeStrengthId);
                state.OuterNoiseScale = material.GetFloat(OuterNoiseScaleId);
                state.OuterNoiseStrength = material.GetFloat(OuterNoiseStrengthId);
                state.OuterFineNoiseScale = material.GetFloat(OuterFineNoiseScaleId);
                state.OuterFineNoiseStrength = material.GetFloat(OuterFineNoiseStrengthId);
                state.OuterNoiseOffset = material.GetVector(OuterNoiseOffsetId);
                state.WobbleSpeed = material.GetFloat(WobbleSpeedId);
                state.WobbleStrength = material.GetFloat(WobbleStrengthId);
                state.OuterWobbleStrength = material.GetFloat(OuterWobbleStrengthId);
                state.WobbleScale = material.GetFloat(WobbleScaleId);
                state.NoiseDrift = material.GetFloat(NoiseDriftId);
            }

            Material shadowMaterial = layer.ShadowMaterial;

            if (shadowMaterial != null)
            {
                state.ShadowExpand = shadowMaterial.GetFloat(ShadowExpandId);
                state.OuterShadowExpand = shadowMaterial.GetFloat(OuterShadowExpandId);
                state.ShadowOffsetX = shadowMaterial.GetFloat(ShadowOffsetXId);
                state.ShadowOffsetY = shadowMaterial.GetFloat(ShadowOffsetYId);
            }

            return state;
        }

        #endregion

        #region Material Control

        private void ApplyRuntimeState(SightLayer layer,LayerRuntimeState state)
        {
            if (layer.LayerImage != null)
            {
                layer.LayerImage.color = state.LayerColor;
            }

            if (layer.ShadowImage != null)
            {
                layer.ShadowImage.color = state.ShadowColor;
            }

            ApplyCommonMaterialState(layer.LayerMaterial,state);
            ApplyCommonMaterialState(layer.ShadowMaterial,state);

            if (layer.ShadowMaterial != null)
            {
                layer.ShadowMaterial.SetFloat(ShadowExpandId,state.ShadowExpand);
                layer.ShadowMaterial.SetFloat(OuterShadowExpandId,state.OuterShadowExpand);
                layer.ShadowMaterial.SetFloat(ShadowOffsetXId,state.ShadowOffsetX);
                layer.ShadowMaterial.SetFloat(ShadowOffsetYId,state.ShadowOffsetY);
            }
        }

        private void ApplyCommonMaterialState(Material material,LayerRuntimeState state)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat(HoleRadiusId,state.HoleRadius);
            material.SetFloat(SoftnessId,state.InnerSoftness);
            material.SetFloat(OuterRadiusId,state.OuterRadius);
            material.SetFloat(OuterSoftnessId,state.OuterSoftness);
            material.SetFloat(LobeCountId,state.LobeCount);
            material.SetFloat(LobeStrengthId,state.LobeStrength);
            material.SetFloat(NoiseScaleId,state.NoiseScale);
            material.SetFloat(NoiseStrengthId,state.NoiseStrength);
            material.SetFloat(FineNoiseScaleId,state.FineNoiseScale);
            material.SetFloat(FineNoiseStrengthId,state.FineNoiseStrength);
            material.SetVector(NoiseOffsetId,state.NoiseOffset);
            material.SetFloat(OuterLobeCountId,state.OuterLobeCount);
            material.SetFloat(OuterLobeStrengthId,state.OuterLobeStrength);
            material.SetFloat(OuterNoiseScaleId,state.OuterNoiseScale);
            material.SetFloat(OuterNoiseStrengthId,state.OuterNoiseStrength);
            material.SetFloat(OuterFineNoiseScaleId,state.OuterFineNoiseScale);
            material.SetFloat(OuterFineNoiseStrengthId,state.OuterFineNoiseStrength);
            material.SetVector(OuterNoiseOffsetId,state.OuterNoiseOffset);
            material.SetFloat(WobbleSpeedId,state.WobbleSpeed);
            material.SetFloat(WobbleStrengthId,state.WobbleStrength);
            material.SetFloat(OuterWobbleStrengthId,state.OuterWobbleStrength);
            material.SetFloat(WobbleScaleId,state.WobbleScale);
            material.SetFloat(NoiseDriftId,state.NoiseDrift);
        }

        private void UpdateAspect()
        {
            if (sightRect == null)
            {
                sightRect = transform as RectTransform;
            }

            if (sightRect == null || sightRect.rect.height <= 0f)
            {
                return;
            }

            float aspect = sightRect.rect.width / sightRect.rect.height;

            foreach (SightLayer layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                if (layer.LayerMaterial != null)
                {
                    layer.LayerMaterial.SetFloat(AspectId,aspect);
                }

                if (layer.ShadowMaterial != null)
                {
                    layer.ShadowMaterial.SetFloat(AspectId,aspect);
                }
            }
        }

        #endregion

        #region Cleanup

        private void ReleaseMaterials()
        {
            if (layers == null)
            {
                return;
            }

            foreach (SightLayer layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                if (layer.LayerMaterial != null)
                {
                    Destroy(layer.LayerMaterial);
                    layer.LayerMaterial = null;
                }

                if (layer.ShadowMaterial != null)
                {
                    Destroy(layer.ShadowMaterial);
                    layer.ShadowMaterial = null;
                }
            }

            _isInitialized = false;
        }

        #endregion
    }
}