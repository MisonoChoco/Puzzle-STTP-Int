// ===== ENHANCED DOG CONTROLLER =====
using DG.Tweening;
using UnityEngine;
using System.Collections;

public class DogController : MonoBehaviour
{
    [Header("Grid Properties")]
    public Vector2Int gridPos;

    public Direction facing = Direction.Right;

    [Header("Stick Interaction")]
    public bool hasStick = false;

    public StickController heldStick;

    [Header("Movement Settings")]
    public float moveTime = 0.3f;

    public float rotationTime = 0.2f;
    public Ease moveEase = Ease.OutQuart;
    public Ease rotationEase = Ease.OutBack;

    [Header("Visual Settings")]
    public float visualYOffset = 1.2f;

    [Header("Animation Events")]
    public UnityEngine.Events.UnityEvent OnMoveStart;

    public UnityEngine.Events.UnityEvent OnMoveComplete;
    public UnityEngine.Events.UnityEvent OnStickPickup;
    public UnityEngine.Events.UnityEvent OnStickDrop;
    public UnityEngine.Events.UnityEvent OnRotate;

    // State management
    private bool isMoving = false;

    private bool isRotating = false;
    private Tween currentMoveTween;
    private Tween currentRotationTween;

    // movement
    public bool TryMove(Vector2Int direction)
    {
        if (isMoving || isRotating) return false;

        AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Resources/Move"), transform.position);

        Vector2Int targetPos = gridPos + direction;

        // Validate dog's target position
        if (!IsValidPosition(targetPos))
        {
            Debug.Log($"[DogController] Movement blocked: Target position {targetPos} is invalid");
            PlayBumpAnimation(direction);
            return false;
        }

        // If holding stick, validate stick's new position
        if (hasStick && heldStick != null)
        {
            if (!ValidateStickMovement(direction))
            {
                Debug.Log($"[DogController] Movement blocked: Stick would collide at new position");
                PlayBumpAnimation(direction);
                return false;
            }
        }

        // Execute the move WITHOUT changing facing direction
        ExecuteMove(targetPos, direction);
        return true;
    }

    // NEW: Manual rotation methods
    public bool TryRotateLeft()
    {
        if (isMoving || isRotating) return false;

        AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Resources/Move"), transform.position);

        Direction newFacing = DirectionUtil.RotateLeft(facing);
        return ExecuteRotation(newFacing);
    }

    public bool TryRotateRight()
    {
        if (isMoving || isRotating) return false;

        AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Resources/Move"), transform.position);

        Direction newFacing = DirectionUtil.RotateRight(facing);
        return ExecuteRotation(newFacing);
    }

    private bool ExecuteRotation(Direction newFacing)
    {
        // If holding stick, check if rotation would cause collision
        if (hasStick && heldStick != null)
        {
            Vector2Int potentialStickEnd = gridPos + DirectionUtil.ToVector(newFacing);
            if (!GridValidator.Instance.IsWithinBounds(potentialStickEnd) ||
                GridValidator.Instance.IsTileBlocked(potentialStickEnd))
            {
                Debug.Log($"[DogController] Rotation blocked: Stick would collide");
                return false;
            }
        }

        isRotating = true;
        // Fixed: Register the current facing direction before changing it
        UndoManager.Instance.RegisterRotation(facing);

        Direction oldFacing = facing;
        facing = newFacing;

        float targetYRotation = DirectionUtil.ToYRotation(facing);

        currentRotationTween = transform.DORotate(new Vector3(0, targetYRotation, 0), rotationTime)
            .SetEase(rotationEase)
            .OnComplete(() =>
            {
                isRotating = false;
                OnRotate?.Invoke();

                // Update stick direction if held
                if (hasStick && heldStick != null)
                {
                    heldStick.UpdateDirectionWithDog(facing);
                }
            });

        return true;
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        bool withinBounds = GridValidator.Instance.IsWithinBounds(pos);
        bool isBlocked = GridValidator.Instance.IsTileBlocked(pos);

        if (!withinBounds)
            Debug.Log($"[DogController] Position {pos} is out of bounds");
        if (isBlocked)
            Debug.Log($"[DogController] Position {pos} is blocked by obstacle");

        return withinBounds && !isBlocked;
    }

    private bool ValidateStickMovement(Vector2Int moveDirection)
    {
        Vector2Int newStickRoot = heldStick.rootPosition + moveDirection;
        Vector2Int newStickEnd = newStickRoot + DirectionUtil.ToVector(heldStick.direction);

        bool valid = GridValidator.Instance.AreAllTilesFree(new[] { newStickRoot, newStickEnd });

        if (!valid)
            Debug.Log($"[DogController] Stick movement invalid: Root={newStickRoot}, End={newStickEnd}");

        return valid;
    }

    private void ExecuteMove(Vector2Int targetPos, Vector2Int direction)
    {
        isMoving = true;

        // Save state for undo before making changes
        UndoManager.Instance.RegisterMove(gridPos, facing, hasStick);

        // Update grid position (NO FACING DIRECTION CHANGE)
        Vector2Int oldPos = gridPos;
        gridPos = targetPos;

        OnMoveStart?.Invoke();

        // Simple movement without rotation
        Vector3 targetWorldPos = GridToWorld(gridPos);
        currentMoveTween = transform.DOMove(targetWorldPos, moveTime)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                isMoving = false;
                OnMoveComplete?.Invoke();
                GameManager.Instance.CheckForWin();
            });

        // Move stick simultaneously if held
        if (hasStick && heldStick != null)
        {
            heldStick.MoveWithDog(direction, moveTime, moveEase);
        }
    }

    private void PlayBumpAnimation(Vector2Int direction)
    {
        if (isMoving) return;

        Vector3 bumpOffset = new Vector3(direction.x * 0.1f, 0, -direction.y * 0.1f);
        Vector3 originalPos = transform.position;

        transform.DOMove(originalPos + bumpOffset, 0.1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOMove(originalPos, 0.1f).SetEase(Ease.InQuad);
            });
    }

    public bool TryRotateStick(bool clockwise)
    {
        if (!hasStick || heldStick == null || isMoving || isRotating) return false;

        return heldStick.TryRotate(clockwise, () =>
        {
            UndoManager.Instance.RegisterStickRotate(clockwise);
            GameManager.Instance.CheckForWin();
        });
    }

    public void ToggleStickPickup()
    {
        if (isMoving || isRotating) return;

        if (hasStick)
        {
            DropStick();
        }
        else
        {
            TryPickupStick();
        }
    }

    private void TryPickupStick()
    {
        // Look for stick directly in front of the dog (more precise pickup)
        Vector2Int frontPosition = gridPos + DirectionUtil.ToVector(facing);
        StickController targetStick = FindStickAtPosition(frontPosition);

        // If no stick in front, check current position as fallback
        if (targetStick == null)
        {
            targetStick = FindStickAtPosition(gridPos);
        }

        if (targetStick != null && targetStick.CanBePickedUp())
        {
            heldStick = targetStick;
            hasStick = true;
            // Fixed: Pass the facing direction as the second parameter
            heldStick.PickUp(this, facing);

            UndoManager.Instance.RegisterPickup();
            OnStickPickup?.Invoke();
            Debug.Log($"[DogController] Picked up stick at position {frontPosition}");
        }
        else
        {
            Debug.Log($"[DogController] No stick found to pickup near {gridPos} or {frontPosition}");
        }
    }

    private StickController FindStickAtPosition(Vector2Int position)
    {
        StickController[] allSticks = Object.FindObjectsByType<StickController>(FindObjectsSortMode.None);

        foreach (var stick in allSticks)
        {
            if (stick.holder != null) continue;

            if (stick.OccupiesPosition(position))
            {
                Debug.Log($"[DogController] Found stick occupying position {position}");
                return stick;
            }
        }

        return null;
    }

    private void DropStick()
    {
        if (heldStick != null)
        {
            heldStick.Drop();
            UndoManager.Instance.RegisterDrop();
            OnStickDrop?.Invoke();
            Debug.Log($"[DogController] Dropped stick at {gridPos}");
        }
        hasStick = false;
        heldStick = null;
    }

    public StickController FindStickNearby()
    {
        Vector2Int[] searchPositions = {
            gridPos,
            gridPos + DirectionUtil.ToVector(facing)
        };

        StickController[] allSticks = Object.FindObjectsByType<StickController>(FindObjectsSortMode.None);

        foreach (var stick in allSticks)
        {
            if (stick.holder != null) continue;

            foreach (Vector2Int searchPos in searchPositions)
            {
                if (stick.OccupiesPosition(searchPos))
                    return stick;
            }
        }

        return null;
    }

    public void ApplyTransform()
    {
        // Stop any current tweens to avoid conflicts
        currentMoveTween?.Kill();
        currentRotationTween?.Kill();

        transform.position = GridToWorld(gridPos);
        transform.rotation = Quaternion.Euler(0, DirectionUtil.ToYRotation(facing), 0);

        isMoving = false;
        isRotating = false;
    }

    private Vector3 GridToWorld(Vector2Int pos)
    {
        float size = GridManager.Instance.cellSize;
        return new Vector3(pos.x * size, visualYOffset, -pos.y * size);
    }

    public bool IsBusy()
    {
        return isMoving || isRotating;
    }

    private void OnDestroy()
    {
        currentMoveTween?.Kill();
        currentRotationTween?.Kill();
    }
}