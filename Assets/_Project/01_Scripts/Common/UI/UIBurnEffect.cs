using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OzGameLab01.UI
{
    [DisallowMultipleComponent]
    public sealed class UIBurnEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup targetCanvasGroup;
        [SerializeField] private Image burnMaskImage;
        [SerializeField] private Image burnOverlay;

        [Header("Burn")]
        [SerializeField, Min(0.05f)]
        private float burnDuration = 0.6f;

        private static readonly int BurnAmountId = Shader.PropertyToID("_BurnAmount");

        private Coroutine _burnRoutine;

        private Material _burnMaskMaterial;
        private Material _burnOverlayMaterial;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeMaterials();
            ResetEffect();
        }

        private void OnDisable()
        {
            StopEffect();
        }

        private void OnDestroy()
        {
            DestroyMaterials();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 중앙에서 외곽으로 진행되는 Burn 소멸 연출을 실행합니다.
        /// BurnMask와 BurnOverlay의 진행도를 동시에 제어하며,
        /// 연출이 끝나면 대상 UI는 마스크에 의해 완전히 사라집니다.
        /// </summary>
        /// <param name="onComplete">
        /// Burn 연출이 모두 종료된 후 호출되는 단발성 콜백입니다.
        /// </param>
        /// <returns>
        /// 현재 실행 중인 Burn 연출 Coroutine입니다.
        /// 외부 연출 시퀀스에서 완료 시점을 기다릴 때 사용할 수 있습니다.
        /// </returns>
        public Coroutine Play(Action onComplete = null)
        {
            StopEffect();

            _burnRoutine = StartCoroutine(PlayRoutine(onComplete));

            return _burnRoutine;
        }

        /// <summary>
        /// 현재 실행 중인 Burn 연출을 즉시 중단합니다.
        /// 현재 Burn 진행 상태는 그대로 유지됩니다.
        /// </summary>
        public void StopEffect()
        {
            if (_burnRoutine == null)
            {
                return;
            }

            StopCoroutine(_burnRoutine);
            _burnRoutine = null;
        }

        /// <summary>
        /// Burn 연출을 중단하고 Mask와 Overlay의 Burn 진행도를
        /// 초기 상태로 복구합니다.
        /// </summary>
        public void ResetEffect()
        {
            StopEffect();

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 1f;
                targetCanvasGroup.blocksRaycasts = true;
            }

            if (burnOverlay != null)
            {
                Color color = burnOverlay.color;
                color.a = 1f;

                burnOverlay.color = color;
            }

            SetBurnAmount(0f);
        }

        #endregion

        #region Initialization

        private void InitializeMaterials()
        {
            _burnMaskMaterial = CreateMaterialInstance(burnMaskImage);
            _burnOverlayMaterial = CreateMaterialInstance(burnOverlay);
            SetBurnAmount(0f);
        }

        private Material CreateMaterialInstance(Image targetImage)
        {
            if (targetImage == null ||
                targetImage.material == null)
            {
                return null;
            }

            Material material = new(targetImage.material);
            targetImage.material = material;

            return material;
        }

        private void DestroyMaterials()
        {
            if (_burnMaskMaterial != null)
            {
                Destroy(_burnMaskMaterial);
                _burnMaskMaterial = null;
            }

            if (_burnOverlayMaterial != null)
            {
                Destroy(_burnOverlayMaterial);
                _burnOverlayMaterial = null;
            }
        }

        #endregion

        #region Burn Effect

        private IEnumerator PlayRoutine(
            Action onComplete)
        {
            if (_burnMaskMaterial == null)
            {
                _burnRoutine = null;

                onComplete?.Invoke();
                yield break;
            }

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 1f;
                targetCanvasGroup.blocksRaycasts = false;
            }

            if (burnOverlay != null)
            {
                Color color = burnOverlay.color;
                color.a = 1f;

                burnOverlay.color = color;
            }

            SetBurnAmount(0f);
            float elapsed = 0f;

            while (elapsed < burnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / burnDuration);
                SetBurnAmount(t);

                yield return null;
            }

            SetBurnAmount(1f);

            if (burnOverlay != null)
            {
                Color color = burnOverlay.color;
                color.a = 0f;
                burnOverlay.color = color;
            }

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 0f;
                targetCanvasGroup.blocksRaycasts = false;
            }

            _burnRoutine = null;

            onComplete?.Invoke();
        }

        private void SetBurnAmount(float amount)
        {
            float value = Mathf.Clamp01(amount);
            SetMaterialBurnAmount(_burnMaskMaterial,value);

            if (burnMaskImage != null)
            {
                SetMaterialBurnAmount(burnMaskImage.materialForRendering,value);
            }

            SetMaterialBurnAmount(_burnOverlayMaterial,value);
        }

        private void SetMaterialBurnAmount(Material material,float amount)
        {
            if (material == null ||
                !material.HasProperty(BurnAmountId))
            {
                return;
            }

            material.SetFloat(BurnAmountId,amount);
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Test

        [ContextMenu("Test/Play")]
        private void TestPlay()
        {
            ResetEffect();
            Play();
        }

        [ContextMenu("Test/Reset")]
        private void TestReset()
        {
            ResetEffect();
        }

        [ContextMenu("Test/Set Burn 0.5")]
        private void TestSetBurnHalf()
        {
            StopEffect();

            if (targetCanvasGroup != null)
            {
                targetCanvasGroup.alpha = 1f;
            }

            if (burnOverlay != null)
            {
                Color color = burnOverlay.color;
                color.a = 1f;

                burnOverlay.color = color;
            }

            SetBurnAmount(0.5f);
        }

        #endregion
#endif
    }
}