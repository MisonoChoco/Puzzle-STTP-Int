using UnityEngine;
using System.Collections.Generic;
using System;

namespace UI.Layout
{
    /// <summary>
    /// Flexible UI controller that handles resolution-independent UI positioning
    /// with support for various anchor points and automatic background scaling.
    /// </summary>
    public class FlexibleUIController : MonoBehaviour
    {
        [System.Serializable]
        public enum AnchorType
        {
            TopLeft,
            TopCenter,
            TopRight,
            CenterLeft,
            Center,
            CenterRight,
            BottomLeft,
            BottomCenter,
            BottomRight,
            Custom // For custom anchor positions
        }

        [System.Serializable]
        public class UIElement
        {
            [Header("Target")]
            public Transform target;

            [Header("Positioning")]
            public AnchorType anchor = AnchorType.Center;

            public Vector2 offset = Vector2.zero;
            public Vector2 customAnchorPosition = Vector2.zero; // Used when anchor is Custom

            [Header("Scaling")]
            public bool scaleWithScreen = false;

            public Vector2 scaleMultiplier = Vector2.one;

            [Header("Options")]
            public bool maintainAspectRatio = true;

            public bool clampToScreen = false;

            [HideInInspector] public Vector3 originalScale;
            [HideInInspector] public Vector3 originalPosition;
        }

        [Header("Reference Resolution")]
        [SerializeField] private float referenceWidth = 1920f;

        [SerializeField] private float referenceHeight = 1080f;

        [Header("Background")]
        [SerializeField] private Transform background;

        [SerializeField] private bool autoScaleBackground = true;
        [SerializeField] private bool maintainBackgroundAspect = true;

        [Header("UI Elements")]
        [SerializeField] private List<UIElement> uiElements = new List<UIElement>();

        [Header("Update Settings")]
        [SerializeField] private bool updateOnScreenChange = true;

        [SerializeField] private float updateCheckInterval = 0.5f;

        // Private fields
        private UnityEngine.Camera targetCamera; // Fixed the error by explicitly specifying UnityEngine.Camera

        private Vector2 lastScreenSize;
        private float lastCheckTime;
        private Dictionary<AnchorType, Vector2> anchorPositions;

        // Events
        public event Action<Vector2> OnScreenSizeChanged;

        public event Action OnUIAdjusted;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCamera();
            CacheOriginalStates();
            InitializeAnchorPositions();
        }

        private void Start()
        {
            AdjustUI();
        }

        private void Update()
        {
            if (updateOnScreenChange && Time.time - lastCheckTime >= updateCheckInterval)
            {
                CheckForScreenSizeChange();
                lastCheckTime = Time.time;
            }
        }

        #endregion Unity Lifecycle



        #region Initialization

        private void InitializeCamera()
        {
            targetCamera = UnityEngine.Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<UnityEngine.Camera>();
                if (targetCamera == null)
                {
                    Debug.LogWarning("[FlexibleUIController] No camera found in scene!");
                }
            }
        }

        private void CacheOriginalStates()
        {
            foreach (var element in uiElements)
            {
                if (element.target != null)
                {
                    element.originalScale = element.target.localScale;
                    element.originalPosition = element.target.localPosition;
                }
            }
        }

        private void InitializeAnchorPositions()
        {
            anchorPositions = new Dictionary<AnchorType, Vector2>();
        }

        #endregion Initialization



        #region Public Methods

        /// <summary>
        /// Manually trigger UI adjustment
        /// </summary>
        public void AdjustUI()
        {
            if (targetCamera == null)
            {
                Debug.LogWarning("[FlexibleUIController] No camera available for UI adjustment");
                return;
            }

            CalculateScreenDimensions();
            UpdateAnchorPositions();
            AdjustBackground();
            AdjustUIElements();

            OnUIAdjusted?.Invoke();
        }

        /// <summary>
        /// Add a new UI element to be managed
        /// </summary>
        /// <param name="element">The UI element to add</param>
        public void AddUIElement(UIElement element)
        {
            if (element != null && element.target != null)
            {
                element.originalScale = element.target.localScale;
                element.originalPosition = element.target.localPosition;
                uiElements.Add(element);
                AdjustSingleElement(element);
            }
        }

        /// <summary>
        /// Remove a UI element from management
        /// </summary>
        /// <param name="target">The target transform to remove</param>
        public void RemoveUIElement(Transform target)
        {
            uiElements.RemoveAll(element => element.target == target);
        }

        /// <summary>
        /// Get current screen dimensions in world units
        /// </summary>
        /// <returns>Screen dimensions as Vector2 (width, height)</returns>
        public Vector2 GetScreenDimensions()
        {
            if (targetCamera == null) return Vector2.zero;

            float screenHeight = 2f * targetCamera.orthographicSize;
            float screenWidth = screenHeight * targetCamera.aspect;
            return new Vector2(screenWidth, screenHeight);
        }

        #endregion Public Methods



        #region Screen Management

        private void CheckForScreenSizeChange()
        {
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);

            if (currentScreenSize != lastScreenSize)
            {
                lastScreenSize = currentScreenSize;
                OnScreenSizeChanged?.Invoke(currentScreenSize);
                AdjustUI();
            }
        }

        private void CalculateScreenDimensions()
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        #endregion Screen Management



        #region Anchor Positioning

        private void UpdateAnchorPositions()
        {
            Vector2 screenDimensions = GetScreenDimensions();
            float halfWidth = screenDimensions.x * 0.5f;
            float halfHeight = screenDimensions.y * 0.5f;

            anchorPositions[AnchorType.TopLeft] = new Vector2(-halfWidth, halfHeight);
            anchorPositions[AnchorType.TopCenter] = new Vector2(0f, halfHeight);
            anchorPositions[AnchorType.TopRight] = new Vector2(halfWidth, halfHeight);

            anchorPositions[AnchorType.CenterLeft] = new Vector2(-halfWidth, 0f);
            anchorPositions[AnchorType.Center] = Vector2.zero;
            anchorPositions[AnchorType.CenterRight] = new Vector2(halfWidth, 0f);

            anchorPositions[AnchorType.BottomLeft] = new Vector2(-halfWidth, -halfHeight);
            anchorPositions[AnchorType.BottomCenter] = new Vector2(0f, -halfHeight);
            anchorPositions[AnchorType.BottomRight] = new Vector2(halfWidth, -halfHeight);
        }

        private Vector2 GetAnchorPosition(UIElement element)
        {
            if (element.anchor == AnchorType.Custom)
            {
                return element.customAnchorPosition;
            }

            return anchorPositions.ContainsKey(element.anchor)
                ? anchorPositions[element.anchor]
                : Vector2.zero;
        }

        #endregion Anchor Positioning



        #region Background Adjustment

        private void AdjustBackground()
        {
            if (background == null || !autoScaleBackground) return;

            SpriteRenderer bgRenderer = background.GetComponent<SpriteRenderer>();
            if (bgRenderer == null)
            {
                Debug.LogWarning("[FlexibleUIController] Background object missing SpriteRenderer component");
                return;
            }

            Vector2 screenDimensions = GetScreenDimensions();
            Vector2 spriteSize = bgRenderer.bounds.size;

            if (spriteSize.x <= 0 || spriteSize.y <= 0)
            {
                Debug.LogWarning("[FlexibleUIController] Background sprite has invalid size");
                return;
            }

            float scaleX = screenDimensions.x / spriteSize.x;
            float scaleY = screenDimensions.y / spriteSize.y;

            float finalScale = maintainBackgroundAspect ? Mathf.Max(scaleX, scaleY) : 1f;

            if (!maintainBackgroundAspect)
            {
                background.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                background.localScale = Vector3.one * finalScale;
            }
        }

        #endregion Background Adjustment



        #region UI Element Adjustment

        private void AdjustUIElements()
        {
            foreach (var element in uiElements)
            {
                AdjustSingleElement(element);
            }
        }

        private void AdjustSingleElement(UIElement element)
        {
            if (element.target == null) return;

            // Position adjustment
            Vector2 anchorPos = GetAnchorPosition(element);
            Vector3 finalPosition = anchorPos + element.offset;

            if (element.clampToScreen)
            {
                finalPosition = ClampToScreen(finalPosition);
            }

            element.target.position = finalPosition;

            // Scale adjustment
            if (element.scaleWithScreen)
            {
                Vector2 screenDimensions = GetScreenDimensions();
                float scaleFactorX = screenDimensions.x / referenceWidth;
                float scaleFactorY = screenDimensions.y / referenceHeight;

                Vector3 newScale = element.originalScale;

                if (element.maintainAspectRatio)
                {
                    float uniformScale = Mathf.Min(scaleFactorX, scaleFactorY);
                    newScale *= uniformScale;
                }
                else
                {
                    newScale.x *= scaleFactorX * element.scaleMultiplier.x;
                    newScale.y *= scaleFactorY * element.scaleMultiplier.y;
                }

                element.target.localScale = newScale;
            }
        }

        private Vector3 ClampToScreen(Vector3 position)
        {
            Vector2 screenDimensions = GetScreenDimensions();
            float halfWidth = screenDimensions.x * 0.5f;
            float halfHeight = screenDimensions.y * 0.5f;

            position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            position.y = Mathf.Clamp(position.y, -halfHeight, halfHeight);

            return position;
        }

        #endregion UI Element Adjustment



        #region Editor Helpers

#if UNITY_EDITOR

        [ContextMenu("Adjust UI Now")]
        private void ForceAdjustUI()
        {
            AdjustUI();
        }

        [ContextMenu("Reset All Elements")]
        private void ResetAllElements()
        {
            foreach (var element in uiElements)
            {
                if (element.target != null)
                {
                    element.target.localScale = element.originalScale;
                    element.target.localPosition = element.originalPosition;
                }
            }
        }

        private void OnValidate()
        {
            if (referenceWidth <= 0) referenceWidth = 1920f;
            if (referenceHeight <= 0) referenceHeight = 1080f;
            if (updateCheckInterval <= 0) updateCheckInterval = 0.1f;
        }

        private void OnDrawGizmosSelected()
        {
            if (targetCamera == null) return;

            Vector2 screenDimensions = GetScreenDimensions();
            Vector3 center = transform.position;

            // Draw screen bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(screenDimensions.x, screenDimensions.y, 0.1f));

            // Draw anchor points
            UpdateAnchorPositions();
            Gizmos.color = Color.red;

            foreach (var anchor in anchorPositions)
            {
                Vector3 anchorWorldPos = (Vector3)anchor.Value + center;
                Gizmos.DrawWireSphere(anchorWorldPos, 0.1f);
            }
        }

#endif

        #endregion Editor Helpers

    }
}