// ===== ENHANCED STICK CONTROLLER =====
using DG.Tweening;
using UnityEngine;
using System;

public class StickController : MonoBehaviour
{
    [Header("Grid Properties")]
    public Vector2Int rootPosition;

    public Direction direction;
    public DogController holder;

    [Header("Animation Settings")]
    public float rotationTime = 0.25f;

    public Ease rotationEase = Ease.OutBack;

    [Header("Visual Settings")]
    public float groundYOffset = 0.1f;

    public float heldYOffset = 0.15f;

    // State management
    private bool isRotating = false;

    private Tween currentTween;

    private void Start()
    {
        //transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // Enhanced pickup with proper positioning
    public void PickUp(DogController dog, Direction dogFacing)
    {
        if (holder != null) return;

        holder = dog;
        rootPosition = dog.gridPos;
        direction = dogFacing; // Align stick with dog's facing direction

        Debug.Log($"[StickController] Picked up: Root={rootPosition}, Direction={direction}");
        ApplyTransform(true);
    }

    // NEW: Update stick direction when dog rotates
    public void UpdateDirectionWithDog(Direction newDirection)
    {
        if (holder == null) return;

        direction = newDirection;
        Debug.Log($"[StickController] Updated direction to {direction} following dog rotation");
        ApplyTransform(true);
    }

    public void Drop()
    {
        holder = null;
        Debug.Log($"[StickController] Dropped at: Root={rootPosition}, Direction={direction}");
        ApplyTransform(false);
        GameManager.Instance.CheckForWin();
    }

    public bool CanBePickedUp()
    {
        return holder == null && !isRotating;
    }

    public bool TryRotate(bool clockwise, Action onComplete = null)
    {
        if (isRotating || holder == null) return false;

        Direction originalDirection = direction;
        Direction targetDirection = clockwise
            ? DirectionUtil.RotateRight(direction)
            : DirectionUtil.RotateLeft(direction);

        // Validate rotation
        Vector2Int targetEndTile = rootPosition + DirectionUtil.ToVector(targetDirection);

        if (!GridValidator.Instance.IsWithinBounds(targetEndTile) ||
            GridValidator.Instance.IsTileBlocked(targetEndTile))
        {
            Debug.Log($"[StickController] Rotation blocked: Target end {targetEndTile} invalid");
            return false;
        }

        // Execute rotation
        ExecuteRotation(targetDirection, onComplete);
        return true;
    }

    private void ExecuteRotation(Direction targetDirection, Action onComplete)
    {
        isRotating = true;
        direction = targetDirection;

        Debug.Log($"[StickController] Rotating to direction {direction}");

        Vector3 targetPosition = GetStickWorldPosition(true);
        Quaternion targetRotation = GetStickRotation();

        Sequence rotationSequence = DOTween.Sequence();

        // Smooth rotation animation
        rotationSequence.Append(
            transform.DOMove(targetPosition, rotationTime).SetEase(rotationEase)
        );
        rotationSequence.Join(
            transform.DORotateQuaternion(targetRotation, rotationTime).SetEase(rotationEase)
        );

        rotationSequence.OnComplete(() =>
        {
            isRotating = false;
            onComplete?.Invoke();
        });

        currentTween = rotationSequence;
    }

    public void MoveWithDog(Vector2Int moveDirection, float moveTime, Ease moveEase)
    {
        rootPosition += moveDirection;

        Vector3 targetPosition = GetStickWorldPosition(holder != null);
        currentTween?.Kill();
        currentTween = transform.DOMove(targetPosition, moveTime).SetEase(moveEase);

        Debug.Log($"[StickController] Moving with dog: New root={rootPosition}");
    }

    public void ApplyTransform(bool isHeld = false)
    {
        currentTween?.Kill();

        transform.position = GetStickWorldPosition(isHeld);
        transform.rotation = GetStickRotation();

        isRotating = false;
    }

    // FIXED: Proper stick positioning - position at root, not center
    private Vector3 GetStickWorldPosition(bool isHeld = false)
    {
        // Position stick at its root position, not centered between root and end
        Vector3 rootWorldPos = GridToWorld(rootPosition, isHeld);

        // Offset the stick slightly towards its direction for better visual alignment
        Vector2Int dirVector = DirectionUtil.ToVector(direction);
        Vector3 directionOffset = new Vector3(dirVector.x * 0.5f, 0, -dirVector.y * 0.5f) * GridManager.Instance.cellSize;

        return rootWorldPos + directionOffset;
    }

    private Quaternion GetStickRotation()
    {
        return Quaternion.Euler(-90f, 0f, DirectionUtil.ToZRotation(direction));
    }

    public Vector2Int[] OccupiedTiles => new[]
    {
        rootPosition,
        rootPosition + DirectionUtil.ToVector(direction)
    };

    public bool OccupiesPosition(Vector2Int pos)
    {
        Vector2Int[] occupied = OccupiedTiles;
        foreach (Vector2Int tile in occupied)
        {
            if (tile == pos) return true;
        }
        return false;
    }

    private Vector3 GridToWorld(Vector2Int pos, bool isHeld = false)
    {
        float cellSize = GridManager.Instance.cellSize;
        float yOffset = isHeld ? heldYOffset : groundYOffset;
        return new Vector3(pos.x * cellSize, yOffset, -pos.y * cellSize);
    }

    public bool IsBusy()
    {
        return isRotating;
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}