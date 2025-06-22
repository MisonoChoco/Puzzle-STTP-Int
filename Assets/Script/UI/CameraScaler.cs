using UnityEngine;
using System;

namespace UI.Camera
{
    /// <summary>
    /// Advanced camera scaling system that maintains consistent viewport across different screen resolutions
    /// with support for multiple scaling modes and orientation handling.
    /// </summary>
    public class CameraScaler : MonoBehaviour
    {
        [System.Serializable]
        public enum ScalingMode
        {
            /// <summary>Expand view to fit wider screens, may show more content</summary>
            Expand,

            /// <summary>Crop view to maintain exact aspect ratio</summary>
            Crop,

            /// <summary>Scale uniformly, may result in letterboxing</summary>
            Uniform,

            /// <summary>Stretch to fit any aspect ratio (may distort)</summary>
            Stretch
        }

        [System.Serializable]
        public enum OrientationMode
        {
            /// <summary>Automatically detect orientation</summary>
            Auto,

            /// <summary>Force landscape orientation handling</summary>
            Landscape,

            /// <summary>Force portrait orientation handling</summary>
            Portrait
        }

        [Header("Target Aspect Ratio")]
        [SerializeField] private float targetAspectWidth = 16f;

        [SerializeField] private float targetAspectHeight = 9f;

        [Header("Scaling Settings")]
        [SerializeField] private ScalingMode scalingMode = ScalingMode.Expand;

        [SerializeField] private OrientationMode orientationMode = OrientationMode.Auto;

        [Header("Camera Settings")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [SerializeField] private float baseOrthographicSize = 5f;
        [SerializeField] private float baseFOV = 60f;

        [Header("Runtime Options")]
        [SerializeField] private bool updateOnScreenChange = true;

        [SerializeField] private float updateCheckInterval = 0.5f;
        [SerializeField] private bool smoothTransitions = true;
        [SerializeField] private float transitionSpeed = 2f;

        // Private fields
        private float targetAspectRatio;

        private Vector2 lastScreenSize;
        private float lastCheckTime;
        private float targetOrthographicSize;
        private float targetFieldOfView;

        // Events
        public event Action<float> OnAspectRatioChanged;

        public event Action<ScalingMode> OnScalingModeChanged;

        // Properties
        public float CurrentAspectRatio => targetCamera != null ? targetCamera.aspect : 0f;

        public float TargetAspectRatio => targetAspectRatio;
        public ScalingMode CurrentScalingMode => scalingMode;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCamera();
            CalculateTargetAspectRatio();
            CacheBaseCameraSettings();
        }

        private void Start()
        {
            ApplyScaling();
        }

        private void Update()
        {
            if (updateOnScreenChange && Time.time - lastCheckTime >= updateCheckInterval)
            {
                CheckForScreenSizeChange();
                lastCheckTime = Time.time;
            }

            if (smoothTransitions)
            {
                ApplySmoothTransitions();
            }
        }

        #endregion Unity Lifecycle

        #region Initialization

        private void InitializeCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<UnityEngine.Camera>();

                if (targetCamera == null)
                {
                    targetCamera = UnityEngine.Camera.main;

                    if (targetCamera == null)
                    {
                        Debug.LogError("[CameraScaler] No camera found! Please assign a target camera.");
                        enabled = false;
                        return;
                    }
                }
            }
        }

        private void CalculateTargetAspectRatio()
        {
            if (orientationMode == OrientationMode.Auto)
            {
                bool isLandscape = Screen.width > Screen.height;
                targetAspectRatio = isLandscape
                    ? targetAspectWidth / targetAspectHeight
                    : targetAspectHeight / targetAspectWidth;
            }
            else if (orientationMode == OrientationMode.Landscape)
            {
                targetAspectRatio = targetAspectWidth / targetAspectHeight;
            }
            else // Portrait
            {
                targetAspectRatio = targetAspectHeight / targetAspectWidth;
            }
        }

        private void CacheBaseCameraSettings()
        {
            if (targetCamera != null)
            {
                baseOrthographicSize = targetCamera.orthographicSize;
                baseFOV = targetCamera.fieldOfView;
            }
        }

        #endregion Initialization

        #region Public Methods

        /// <summary>
        /// Manually apply camera scaling
        /// </summary>
        public void ApplyScaling()
        {
            if (targetCamera == null) return;

            CalculateTargetAspectRatio();
            float currentAspect = (float)Screen.width / Screen.height;

            switch (scalingMode)
            {
                case ScalingMode.Expand:
                    ApplyExpandScaling(currentAspect);
                    break;

                case ScalingMode.Crop:
                    ApplyCropScaling(currentAspect);
                    break;

                case ScalingMode.Uniform:
                    ApplyUniformScaling(currentAspect);
                    break;

                case ScalingMode.Stretch:
                    ApplyStretchScaling();
                    break;
            }

            OnAspectRatioChanged?.Invoke(currentAspect);
        }

        /// <summary>
        /// Change scaling mode at runtime
        /// </summary>
        /// <param name="newMode">New scaling mode to apply</param>
        public void SetScalingMode(ScalingMode newMode)
        {
            if (scalingMode != newMode)
            {
                scalingMode = newMode;
                ApplyScaling();
                OnScalingModeChanged?.Invoke(newMode);
            }
        }

        /// <summary>
        /// Set target aspect ratio at runtime
        /// </summary>
        /// <param name="width">Target width ratio</param>
        /// <param name="height">Target height ratio</param>
        public void SetTargetAspectRatio(float width, float height)
        {
            targetAspectWidth = width;
            targetAspectHeight = height;
            ApplyScaling();
        }

        /// <summary>
        /// Get safe area for UI positioning (useful for devices with notches)
        /// </summary>
        /// <returns>Safe area rectangle in screen coordinates</returns>
        public Rect GetSafeArea()
        {
            return Screen.safeArea;
        }

        /// <summary>
        /// Convert screen point to world point using current camera settings
        /// </summary>
        /// <param name="screenPoint">Point in screen coordinates</param>
        /// <returns>Point in world coordinates</returns>
        public Vector3 ScreenToWorldPoint(Vector2 screenPoint)
        {
            if (targetCamera == null) return Vector3.zero;

            Vector3 screenPoint3D = new Vector3(screenPoint.x, screenPoint.y, targetCamera.nearClipPlane);
            return targetCamera.ScreenToWorldPoint(screenPoint3D);
        }

        #endregion Public Methods

        #region Screen Management

        private void CheckForScreenSizeChange()
        {
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);

            if (currentScreenSize != lastScreenSize)
            {
                lastScreenSize = currentScreenSize;
                ApplyScaling();
            }
        }

        #endregion Screen Management

        #region Scaling Methods

        private void ApplyExpandScaling(float currentAspect)
        {
            float scaleHeight = currentAspect / targetAspectRatio;

            if (targetCamera.orthographic)
            {
                targetOrthographicSize = scaleHeight < 1.0f
                    ? baseOrthographicSize / scaleHeight
                    : baseOrthographicSize;
            }
            else
            {
                targetFieldOfView = scaleHeight < 1.0f
                    ? baseFOV / scaleHeight
                    : baseFOV;
            }
        }

        private void ApplyCropScaling(float currentAspect)
        {
            float scaleHeight = currentAspect / targetAspectRatio;

            if (targetCamera.orthographic)
            {
                targetOrthographicSize = scaleHeight > 1.0f
                    ? baseOrthographicSize * scaleHeight
                    : baseOrthographicSize;
            }
            else
            {
                targetFieldOfView = scaleHeight > 1.0f
                    ? baseFOV * scaleHeight
                    : baseFOV;
            }
        }

        private void ApplyUniformScaling(float currentAspect)
        {
            float scaleHeight = currentAspect / targetAspectRatio;
            float uniformScale = Mathf.Min(scaleHeight, 1.0f);

            if (targetCamera.orthographic)
            {
                targetOrthographicSize = baseOrthographicSize / uniformScale;
            }
            else
            {
                targetFieldOfView = baseFOV / uniformScale;
            }
        }

        private void ApplyStretchScaling()
        {
            // Stretch mode doesn't change camera settings, relies on UI scaling
            if (targetCamera.orthographic)
            {
                targetOrthographicSize = baseOrthographicSize;
            }
            else
            {
                targetFieldOfView = baseFOV;
            }
        }

        private void ApplySmoothTransitions()
        {
            if (targetCamera == null) return;

            if (targetCamera.orthographic)
            {
                float currentSize = targetCamera.orthographicSize;
                if (Mathf.Abs(currentSize - targetOrthographicSize) > 0.01f)
                {
                    targetCamera.orthographicSize = Mathf.Lerp(
                        currentSize,
                        targetOrthographicSize,
                        Time.deltaTime * transitionSpeed
                    );
                }
            }
            else
            {
                float currentFOV = targetCamera.fieldOfView;
                if (Mathf.Abs(currentFOV - targetFieldOfView) > 0.01f)
                {
                    targetCamera.fieldOfView = Mathf.Lerp(
                        currentFOV,
                        targetFieldOfView,
                        Time.deltaTime * transitionSpeed
                    );
                }
            }
        }

        #endregion Scaling Methods

        #region Utility Methods

        /// <summary>
        /// Calculate the viewport rectangle for letterboxing/pillarboxing
        /// </summary>
        /// <returns>Viewport rectangle (0-1 coordinates)</returns>
        public Rect CalculateViewportRect()
        {
            float currentAspect = (float)Screen.width / Screen.height;
            float scaleHeight = currentAspect / targetAspectRatio;

            if (scaleHeight < 1.0f)
            {
                // Letterboxing (black bars on top/bottom)
                float height = scaleHeight;
                float yOffset = (1.0f - height) * 0.5f;
                return new Rect(0, yOffset, 1, height);
            }
            else
            {
                // Pillarboxing (black bars on sides)
                float width = 1.0f / scaleHeight;
                float xOffset = (1.0f - width) * 0.5f;
                return new Rect(xOffset, 0, width, 1);
            }
        }

        /// <summary>
        /// Get the current scale factor compared to target aspect ratio
        /// </summary>
        /// <returns>Scale factor</returns>
        public float GetScaleFactor()
        {
            float currentAspect = (float)Screen.width / Screen.height;
            return currentAspect / targetAspectRatio;
        }

        /// <summary>
        /// Check if current screen is wider than target aspect ratio
        /// </summary>
        /// <returns>True if screen is wider than target</returns>
        public bool IsWiderThanTarget()
        {
            return GetScaleFactor() > 1.0f;
        }

        #endregion Utility Methods

        #region Editor Helpers

#if UNITY_EDITOR

        [ContextMenu("Apply Scaling Now")]
        private void ForceApplyScaling()
        {
            ApplyScaling();
        }

        [ContextMenu("Reset Camera Settings")]
        private void ResetCameraSettings()
        {
            if (targetCamera != null)
            {
                targetCamera.orthographicSize = baseOrthographicSize;
                targetCamera.fieldOfView = baseFOV;
            }
        }

        private void OnValidate()
        {
            if (targetAspectWidth <= 0) targetAspectWidth = 16f;
            if (targetAspectHeight <= 0) targetAspectHeight = 9f;
            if (baseOrthographicSize <= 0) baseOrthographicSize = 5f;
            if (baseFOV <= 0) baseFOV = 60f;
            if (updateCheckInterval <= 0) updateCheckInterval = 0.1f;
            if (transitionSpeed <= 0) transitionSpeed = 1f;

            // Apply scaling in editor for immediate feedback
            if (Application.isPlaying)
            {
                ApplyScaling();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (targetCamera == null) return;

            // Draw target aspect ratio bounds
            Gizmos.color = Color.green;

            float height = targetCamera.orthographic ? targetCamera.orthographicSize * 2f : 10f;
            float width = height * targetAspectRatio;

            Vector3 center = transform.position;
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));

            // Draw current viewport
            Gizmos.color = Color.yellow;
            float currentHeight = targetCamera.orthographic ? targetCamera.orthographicSize * 2f : 10f;
            float currentWidth = currentHeight * targetCamera.aspect;

            Gizmos.DrawWireCube(center, new Vector3(currentWidth, currentHeight, 0.05f));
        }

#endif

        #endregion Editor Helpers
    }
}