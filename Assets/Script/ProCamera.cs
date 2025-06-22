using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 10, -5);
    public float smoothTime = 0.2f;
    public float zoomPadding = 1.5f;

    private Transform target;
    private Vector3 velocity;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        TryAutoAssignTarget();
        AdjustZoomToLevel();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        transform.rotation = Quaternion.Euler(45f, 0f, 0f); // top-down angled view
    }

    public void SetFollowTarget(Transform t)
    {
        target = t;
    }

    public void TryAutoAssignTarget()
    {
        var dog = Object.FindFirstObjectByType<DogController>();
        if (dog != null)
        {
            target = dog.transform;
        }
    }

    public void AdjustZoomToLevel()
    {
        if (cam == null || GridManager.Instance == null) return;

        Vector2Int size = GridManager.Instance.gridSize;
        float maxSize = Mathf.Max(size.x, size.y);
        cam.orthographicSize = maxSize / zoomPadding;
    }
}