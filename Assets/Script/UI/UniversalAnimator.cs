using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections.Generic;

namespace UI.Effects
{
    /// <summary>
    /// Universal animation controller for UI and world space objects with comprehensive support for various entrance effects.
    /// Supports SpriteRenderer, Image, Text, TextMeshPro, and CanvasGroup components with automatic component detection.
    /// </summary>
    [AddComponentMenu("UI/Effects/Universal Effect Animator")]
    public class UniversalEffectAnimator : MonoBehaviour
    {
        [System.Serializable]
        public enum EffectType
        {
            Random,
            FadeInFromTop,
            FadeInFromBottom,
            FadeInFromLeft,
            FadeInFromRight,
            PopUp,
            MergeFromRandom,
            FadeInPlace,
            SlideInFromTop,
            SlideInFromBottom,
            SlideInFromLeftSide,
            SlideInFromRightSide,
            Bounce,
            Elastic,
            Shake,
            Pulse
        }

        [Header("Animation Settings")]
        [SerializeField] private EffectType selectedEffect = EffectType.PopUp;

        [SerializeField, Range(0.1f, 5f)] private float duration = 0.5f;
        [SerializeField, Min(0f)] private float offsetDistance = 100f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool resetOnDisable = true;
        [SerializeField] private bool autoAddCanvasGroup = true;

        [Header("Advanced Settings")]
        [SerializeField] private Ease easeType = Ease.OutCubic;

        [SerializeField, Min(0f)] private float delay = 0f;
        [SerializeField] private bool useUnscaledTime = false;
        [SerializeField] private bool ignoreTimeScale = false;

        [Header("Effect Customization")]
        [SerializeField, Range(0.1f, 2f)] private float shakeStrength = 0.5f;

        [SerializeField, Range(0.5f, 2f)] private float pulseScale = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Events
        public event Action OnAnimationStart;

        public event Action OnAnimationComplete;

        public event Action<EffectType> OnEffectChanged;

        // Component references
        private readonly List<IAnimatableComponent> animatableComponents = new List<IAnimatableComponent>();

        // Original states
        private Vector3 originalPosition;

        private Vector3 originalScale;
        private Dictionary<IAnimatableComponent, float> originalAlphas = new Dictionary<IAnimatableComponent, float>();

        // Animation management
        private Sequence currentSequence;

        private bool isInitialized = false;
        private bool isAnimating = false;

        // Properties
        public bool IsAnimating => isAnimating;

        public EffectType CurrentEffect => selectedEffect;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            CacheOriginalStates();
        }

        private void Start()
        {
            if (playOnStart && gameObject.activeInHierarchy)
                PlayEffect();
        }

        private void OnEnable()
        {
            if (isInitialized && playOnStart && !isAnimating)
                PlayEffect();
        }

        private void OnDisable()
        {
            if (resetOnDisable)
                ResetToOriginalState();

            StopAnimation();
        }

        private void OnDestroy()
        {
            StopAnimation();
            ClearComponentCache();
        }

        #endregion Unity Lifecycle

        #region Initialization

        private void InitializeComponents()
        {
            ClearComponentCache();

            // Detect and wrap all animatable components
            DetectAnimatableComponents();

            // Auto-add CanvasGroup for UI elements if needed
            if (autoAddCanvasGroup && HasUIComponents() && !HasComponent<CanvasGroup>())
            {
                var canvasGroup = gameObject.AddComponent<CanvasGroup>();
                animatableComponents.Add(new CanvasGroupWrapper(canvasGroup));

                if (showDebugLogs)
                    Debug.Log($"[UniversalEffectAnimator] Auto-added CanvasGroup to {gameObject.name}", this);
            }

            isInitialized = animatableComponents.Count > 0;

            if (showDebugLogs)
            {
                Debug.Log($"[UniversalEffectAnimator] Initialized with {animatableComponents.Count} animatable components on {gameObject.name}", this);
            }
        }

        private void DetectAnimatableComponents()
        {
            // SpriteRenderer
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                animatableComponents.Add(new SpriteRendererWrapper(spriteRenderer));

            // UI Image
            var image = GetComponent<Image>();
            if (image != null)
                animatableComponents.Add(new ImageWrapper(image));

            // UI Text
            var text = GetComponent<Text>();
            if (text != null)
                animatableComponents.Add(new TextWrapper(text));

            // TextMeshPro UI
            var tmpUI = GetComponent<TextMeshProUGUI>();
            if (tmpUI != null)
                animatableComponents.Add(new TextMeshProWrapper(tmpUI));

            // TextMeshPro 3D
            var tmp3D = GetComponent<TextMeshPro>();
            if (tmp3D != null)
                animatableComponents.Add(new TextMeshPro3DWrapper(tmp3D));

            // CanvasGroup
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                animatableComponents.Add(new CanvasGroupWrapper(canvasGroup));
        }

        private void CacheOriginalStates()
        {
            if (!isInitialized) return;

            originalPosition = transform.localPosition;
            originalScale = transform.localScale;

            originalAlphas.Clear();
            foreach (var component in animatableComponents)
            {
                originalAlphas[component] = component.GetAlpha();
            }
        }

        private bool HasUIComponents()
        {
            return GetComponent<RectTransform>() != null;
        }

        private bool HasComponent<T>() where T : Component
        {
            return GetComponent<T>() != null;
        }

        private void ClearComponentCache()
        {
            animatableComponents.Clear();
            originalAlphas.Clear();
        }

        #endregion Initialization

        #region Public Methods

        /// <summary>
        /// Play the selected effect animation
        /// </summary>
        public void PlayEffect()
        {
            PlayEffect(selectedEffect);
        }

        /// <summary>
        /// Play a specific effect animation
        /// </summary>
        public void PlayEffect(EffectType effectType)
        {
            if (!isInitialized || !gameObject.activeInHierarchy)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[UniversalEffectAnimator] Cannot play effect: not initialized or inactive", this);
                return;
            }

            StopAnimation();

            EffectType effectToPlay = effectType == EffectType.Random ? GetRandomEffect() : effectType;

            if (showDebugLogs)
                Debug.Log($"[UniversalEffectAnimator] Playing effect: {effectToPlay}", this);

            isAnimating = true;
            OnAnimationStart?.Invoke();

            currentSequence = CreateEffectSequence(effectToPlay);

            if (currentSequence != null)
            {
                currentSequence.SetUpdate(useUnscaledTime || ignoreTimeScale)
                              .SetDelay(delay)
                              .OnComplete(OnAnimationCompleted);
            }
            else
            {
                OnAnimationCompleted();
            }
        }

        /// <summary>
        /// Stop current animation and reset to original state
        /// </summary>
        public void StopAnimation()
        {
            if (currentSequence != null)
            {
                currentSequence.Kill();
                currentSequence = null;
            }
            isAnimating = false;
        }

        /// <summary>
        /// Reset object to its original state immediately
        /// </summary>
        public void ResetToOriginalState()
        {
            if (!isInitialized) return;

            StopAnimation();

            transform.localPosition = originalPosition;
            transform.localScale = originalScale;

            foreach (var component in animatableComponents)
            {
                if (originalAlphas.TryGetValue(component, out float alpha))
                    component.SetAlpha(alpha);
            }
        }

        /// <summary>
        /// Change the effect type and optionally play it immediately
        /// </summary>
        public void SetEffect(EffectType newEffect, bool playImmediately = false)
        {
            selectedEffect = newEffect;
            OnEffectChanged?.Invoke(newEffect);

            if (playImmediately)
                PlayEffect();
        }

        #endregion Public Methods

        #region Animation Creation

        private Sequence CreateEffectSequence(EffectType effectType)
        {
            switch (effectType)
            {
                case EffectType.FadeInFromTop:
                    return CreateDirectionalFade(Vector2.up);

                case EffectType.FadeInFromBottom:
                    return CreateDirectionalFade(Vector2.down);

                case EffectType.FadeInFromLeft:
                    return CreateDirectionalFade(Vector2.left);

                case EffectType.FadeInFromRight:
                    return CreateDirectionalFade(Vector2.right);

                case EffectType.PopUp:
                    return CreatePopUpAnimation();

                case EffectType.MergeFromRandom:
                    return CreateDirectionalFade(UnityEngine.Random.insideUnitCircle.normalized);

                case EffectType.FadeInPlace:
                    return CreateFadeAnimation();

                case EffectType.SlideInFromTop:
                    return CreateSlideAnimation(Vector2.up);

                case EffectType.SlideInFromBottom:
                    return CreateSlideAnimation(Vector2.down);

                case EffectType.SlideInFromLeftSide:
                    return CreateSlideAnimation(Vector2.left);

                case EffectType.SlideInFromRightSide:
                    return CreateSlideAnimation(Vector2.right);

                case EffectType.Bounce:
                    return CreateBounceAnimation();

                case EffectType.Elastic:
                    return CreateElasticAnimation();

                case EffectType.Shake:
                    return CreateShakeAnimation();

                case EffectType.Pulse:
                    return CreatePulseAnimation();

                default:
                    return CreateFadeAnimation();
            }
        }

        private Sequence CreateDirectionalFade(Vector2 direction)
        {
            SetAllAlpha(0f);
            Vector3 startPos = originalPosition + (Vector3)(direction * offsetDistance);
            transform.localPosition = startPos;

            var sequence = DOTween.Sequence();
            sequence.Join(transform.DOLocalMove(originalPosition, duration).SetEase(easeType));
            sequence.Join(CreateFadeToAlphaTween(1f, duration));

            return sequence;
        }

        private Sequence CreateSlideAnimation(Vector2 direction)
        {
            Vector3 startPos = originalPosition + (Vector3)(direction * offsetDistance);
            transform.localPosition = startPos;

            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOLocalMove(originalPosition, duration).SetEase(easeType));

            return sequence;
        }

        private Sequence CreatePopUpAnimation()
        {
            SetAllAlpha(0f);
            transform.localScale = Vector3.zero;

            var sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutBack));
            sequence.Join(CreateFadeToAlphaTween(1f, duration));

            return sequence;
        }

        private Sequence CreateBounceAnimation()
        {
            SetAllAlpha(0f);
            transform.localScale = Vector3.zero;

            var sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutBounce));
            sequence.Join(CreateFadeToAlphaTween(1f, duration));

            return sequence;
        }

        private Sequence CreateElasticAnimation()
        {
            SetAllAlpha(0f);
            transform.localScale = Vector3.zero;

            var sequence = DOTween.Sequence();
            sequence.Join(transform.DOScale(originalScale, duration).SetEase(Ease.OutElastic));
            sequence.Join(CreateFadeToAlphaTween(1f, duration));

            return sequence;
        }

        private Sequence CreateFadeAnimation()
        {
            SetAllAlpha(0f);

            var sequence = DOTween.Sequence();
            sequence.Append(CreateFadeToAlphaTween(1f, duration).SetEase(easeType));

            return sequence;
        }

        private Sequence CreateShakeAnimation()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOShakePosition(duration, shakeStrength).SetEase(easeType));

            return sequence;
        }

        private Sequence CreatePulseAnimation()
        {
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(originalScale * pulseScale, duration * 0.5f).SetEase(Ease.OutQuad));
            sequence.Append(transform.DOScale(originalScale, duration * 0.5f).SetEase(Ease.InQuad));

            return sequence;
        }

        #endregion Animation Creation

        #region Helper Methods

        private EffectType GetRandomEffect()
        {
            var values = Enum.GetValues(typeof(EffectType));
            EffectType randomEffect;

            do
            {
                randomEffect = (EffectType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            }
            while (randomEffect == EffectType.Random);

            return randomEffect;
        }

        private void OnAnimationCompleted()
        {
            isAnimating = false;
            OnAnimationComplete?.Invoke();

            if (showDebugLogs)
                Debug.Log($"[UniversalEffectAnimator] Animation completed on {gameObject.name}", this);
        }

        private void SetAllAlpha(float alpha)
        {
            foreach (var component in animatableComponents)
            {
                component.SetAlpha(alpha);
            }
        }

        private Sequence CreateFadeToAlphaTween(float targetAlpha, float animationDuration)
        {
            var sequence = DOTween.Sequence();

            foreach (var component in animatableComponents)
            {
                var tween = component.CreateFadeTween(targetAlpha, animationDuration);
                if (tween != null)
                    sequence.Join(tween);
            }

            return sequence;
        }

        #endregion Helper Methods

        #region Component Wrappers

        private interface IAnimatableComponent
        {
            float GetAlpha();

            void SetAlpha(float alpha);

            Tween CreateFadeTween(float targetAlpha, float duration);
        }

        private class SpriteRendererWrapper : IAnimatableComponent
        {
            private readonly SpriteRenderer spriteRenderer;

            public SpriteRendererWrapper(SpriteRenderer spriteRenderer)
            {
                this.spriteRenderer = spriteRenderer;
            }

            public float GetAlpha() => spriteRenderer.color.a;

            public void SetAlpha(float alpha)
            {
                var color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }

            public Tween CreateFadeTween(float targetAlpha, float duration)
            {
                return spriteRenderer.DOFade(targetAlpha, duration);
            }
        }

        private class ImageWrapper : IAnimatableComponent
        {
            private readonly Image image;

            public ImageWrapper(Image image)
            {
                this.image = image;
            }

            public float GetAlpha() => image.color.a;

            public void SetAlpha(float alpha)
            {
                var color = image.color;
                color.a = alpha;
                image.color = color;
            }

            public Tween CreateFadeTween(float targetAlpha, float duration)
            {
                return image.DOFade(targetAlpha, duration);
            }
        }

        private class TextWrapper : IAnimatableComponent
        {
            private readonly Text text;

            public TextWrapper(Text text)
            {
                this.text = text;
            }

            public float GetAlpha() => text.color.a;

            public void SetAlpha(float alpha)
            {
                var color = text.color;
                color.a = alpha;
                text.color = color;
            }

            public Tween CreateFadeTween(float targetAlpha, float duration)
            {
                return text.DOFade(targetAlpha, duration);
            }
        }

        private class TextMeshProWrapper : IAnimatableComponent
        {
            private readonly TextMeshProUGUI textMeshPro;

            public TextMeshProWrapper(TextMeshProUGUI textMeshPro)
            {
                this.textMeshPro = textMeshPro;
            }

            public float GetAlpha() => textMeshPro.color.a;

            public void SetAlpha(float alpha)
            {
                var color = textMeshPro.color;
                color.a = alpha;
                textMeshPro.color = color;
            }

            public Tween CreateFadeTween(float targetAlpha, float duration)
            {
                return textMeshPro.DOFade(targetAlpha, duration);
            }
        }

        private class TextMeshPro3DWrapper : IAnimatableComponent
        {
            private readonly TextMeshPro textMeshPro;

            public TextMeshPro3DWrapper(TextMeshPro textMeshPro)
            {
                this.textMeshPro = textMeshPro;
            }

            public float GetAlpha() => textMeshPro.color.a;

            public void SetAlpha(float alpha)
            {
                var color = textMeshPro.color;
                color.a = alpha;
                textMeshPro.color = color;
            }

            public Tween CreateFadeTween(float targetAlpha, float duration)
            {
                return textMeshPro.DOFade(targetAlpha, duration);
            }
        }

        private class CanvasGroupWrapper : IAnimatableComponent
        {
            private readonly CanvasGroup canvasGroup;

            public CanvasGroupWrapper(CanvasGroup canvasGroup)
            {
                this.canvasGroup = canvasGroup;
            }

            public float GetAlpha() => canvasGroup.alpha;

            public void SetAlpha(float alpha) => canvasGroup.alpha = alpha;

            public Tween CreateFadeTween(float targetAlpha, float duration)
            {
                return canvasGroup.DOFade(targetAlpha, duration);
            }
        }

        #endregion Component Wrappers

        #region Editor Helpers

#if UNITY_EDITOR

        [ContextMenu("Preview Effect")]
        private void PreviewEffect()
        {
            if (Application.isPlaying)
            {
                PlayEffect();
            }
            else
            {
                Debug.Log($"[UniversalEffectAnimator] Preview: {selectedEffect} effect would play with duration {duration}s on components: {string.Join(", ", GetComponentTypes())}", this);
            }
        }

        [ContextMenu("Reset to Original State")]
        private void EditorResetToOriginalState()
        {
            if (Application.isPlaying)
            {
                ResetToOriginalState();
            }
            else
            {
                Debug.Log("[UniversalEffectAnimator] Reset only works in Play mode", this);
            }
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.1f, duration);
            offsetDistance = Mathf.Max(0f, offsetDistance);
            delay = Mathf.Max(0f, delay);
            shakeStrength = Mathf.Max(0.1f, shakeStrength);
            pulseScale = Mathf.Max(0.5f, pulseScale);
        }

        private string[] GetComponentTypes()
        {
            var types = new System.Collections.Generic.List<string>();

            if (GetComponent<SpriteRenderer>()) types.Add("SpriteRenderer");
            if (GetComponent<Image>()) types.Add("Image");
            if (GetComponent<Text>()) types.Add("Text");
            if (GetComponent<TextMeshProUGUI>()) types.Add("TextMeshProUGUI");
            if (GetComponent<TextMeshPro>()) types.Add("TextMeshPro");
            if (GetComponent<CanvasGroup>()) types.Add("CanvasGroup");

            return types.ToArray();
        }

#endif

        #endregion Editor Helpers
    }
}