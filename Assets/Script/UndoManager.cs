using System.Collections.Generic;
using UnityEngine;

// Define the Direction enum if not already defined elsewhere
//public enum Direction
//{
//    Right = 0,
//    Up = 1,
//    Left = 2,
//    Down = 3
//}

// Define the UndoType enum
public enum UndoType
{
    Move,
    RotateStick,
    PickUpStick,
    DropStick,
    RotateDog
}

// Define the UndoAction struct
[System.Serializable]
public struct UndoAction
{
    public UndoType actionType;
    public Vector2Int dogPosition;
    public Direction dogDirection;
    public Vector2Int stickRootPosition;
    public Direction stickDirection;
    public bool dogHasStick;
}

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance { get; private set; }
    private Stack<UndoAction> history = new Stack<UndoAction>();
    private DogController dog;
    private StickController stick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            UnityEngine.Object.Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(DogController dogRef, StickController stickRef)
    {
        dog = dogRef;
        stick = stickRef;
    }

    public void RegisterMove(Vector2Int dogPos, Direction dogDir, bool hadStick)
    {
        history.Push(new UndoAction
        {
            actionType = UndoType.Move,
            dogPosition = dogPos,
            dogDirection = dogDir,
            stickRootPosition = stick != null ? stick.rootPosition : Vector2Int.zero,
            stickDirection = stick != null ? stick.direction : Direction.Right,
            dogHasStick = hadStick
        });
    }

    public void RegisterStickRotate(bool clockwise)
    {
        if (stick == null || dog == null) return;

        // Store the previous state before rotation
        Vector2Int prevStickRoot = stick.rootPosition;
        Direction prevStickDir = clockwise ? DirectionUtil.RotateLeft(stick.direction) : DirectionUtil.RotateRight(stick.direction);

        history.Push(new UndoAction
        {
            actionType = UndoType.RotateStick,
            dogPosition = dog.gridPos,
            dogDirection = dog.facing,
            stickRootPosition = prevStickRoot,
            stickDirection = prevStickDir,
            dogHasStick = dog.hasStick
        });
    }

    public void RegisterRotation(Direction previousFacing)
    {
        if (dog == null) return;

        history.Push(new UndoAction
        {
            actionType = UndoType.RotateDog,
            dogPosition = dog.gridPos,
            dogDirection = previousFacing, // Store the previous facing direction
            stickRootPosition = stick != null ? stick.rootPosition : Vector2Int.zero,
            stickDirection = stick != null ? stick.direction : Direction.Right,
            dogHasStick = dog.hasStick
        });
    }

    public void RegisterPickup()
    {
        if (dog == null || stick == null) return;

        // Store state before pickup (when dog didn't have stick)
        history.Push(new UndoAction
        {
            actionType = UndoType.PickUpStick,
            dogPosition = dog.gridPos,
            dogDirection = dog.facing,
            stickRootPosition = stick.rootPosition,
            stickDirection = stick.direction,
            dogHasStick = false // Dog didn't have stick before pickup
        });
    }

    public void RegisterDrop()
    {
        if (dog == null || stick == null) return;

        // Store state before drop (when dog had stick)
        history.Push(new UndoAction
        {
            actionType = UndoType.DropStick,
            dogPosition = dog.gridPos,
            dogDirection = dog.facing,
            stickRootPosition = stick.rootPosition,
            stickDirection = stick.direction,
            dogHasStick = true // Dog had stick before drop
        });
    }

    public void Undo()
    {
        if (history.Count == 0 || dog == null) return;

        UndoAction action = history.Pop();

        // Restore dog position and facing
        dog.gridPos = action.dogPosition;
        dog.facing = action.dogDirection;
        dog.ApplyTransform();

        // Restore stick position and direction
        if (stick != null)
        {
            stick.rootPosition = action.stickRootPosition;
            stick.direction = action.stickDirection;
            stick.ApplyTransform();
        }

        // Handle stick pickup/drop state restoration
        switch (action.actionType)
        {
            case UndoType.PickUpStick:
                // Undo pickup: dog should not have stick, stick should be dropped
                dog.hasStick = false;
                dog.heldStick = null;
                if (stick != null)
                {
                    stick.Drop();
                }
                break;

            case UndoType.DropStick:
                // Undo drop: dog should have stick, stick should be picked up
                dog.hasStick = true;
                dog.heldStick = stick;
                if (stick != null)
                {
                    stick.PickUp(dog, action.dogDirection);
                }
                break;

            case UndoType.RotateDog:
                // For dog rotation, just restore the stick state
                dog.hasStick = action.dogHasStick;
                if (action.dogHasStick && stick != null)
                {
                    dog.heldStick = stick;
                    stick.PickUp(dog, action.dogDirection);
                }
                else if (!action.dogHasStick && stick != null)
                {
                    dog.heldStick = null;
                    stick.Drop();
                }
                break;

            case UndoType.Move:
            case UndoType.RotateStick:
            default:
                // For move and stick rotate, restore the previous stick state
                dog.hasStick = action.dogHasStick;
                if (action.dogHasStick && stick != null)
                {
                    dog.heldStick = stick;
                    stick.PickUp(dog, action.dogDirection);
                }
                else if (!action.dogHasStick && stick != null)
                {
                    dog.heldStick = null;
                    stick.Drop();
                }
                break;
        }

        // Check win condition after undo
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckForWin();
        }
    }

    public void ClearHistory()
    {
        history.Clear();
    }

    public bool CanUndo()
    {
        return history.Count > 0;
    }

    public int GetHistoryCount()
    {
        return history.Count;
    }
}