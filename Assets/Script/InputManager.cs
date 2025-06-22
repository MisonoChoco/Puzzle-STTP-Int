// ===== ENHANCED INPUT MANAGER =====
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public DogController dog;

    public bool enableInput = true;
    public float inputCooldown = 0.05f; // Prevent input spam

    [Header("Key Bindings")]
    public KeyCode[] moveUpKeys = { KeyCode.W, KeyCode.UpArrow };

    public KeyCode[] moveDownKeys = { KeyCode.S, KeyCode.DownArrow };
    public KeyCode[] moveLeftKeys = { KeyCode.A, KeyCode.LeftArrow };
    public KeyCode[] moveRightKeys = { KeyCode.D, KeyCode.RightArrow };
    public KeyCode[] pickupKeys = { KeyCode.Space };
    public KeyCode[] stickRotateLeftKeys = { KeyCode.Q };
    public KeyCode[] stickRotateRightKeys = { KeyCode.E };

    //Dog rotation keys
    public KeyCode[] dogRotateLeftKeys = { KeyCode.C };

    public KeyCode[] dogRotateRightKeys = { KeyCode.V };
    public KeyCode[] undoKeys = { KeyCode.X };

    [Header("Input Feedback")]
    public UnityEngine.Events.UnityEvent OnValidInput;

    public UnityEngine.Events.UnityEvent OnInvalidInput;

    private float lastInputTime;

    private void Start()
    {
        if (dog == null)
            dog = Object.FindFirstObjectByType<DogController>();
    }

    private void Update()
    {
        if (!enableInput || dog == null || Time.time < lastInputTime + inputCooldown)
            return;

        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        bool inputProcessed = false;

        // Movement input - inverted because z = -y (Unity world space)
        if (AnyKeyPressed(moveUpKeys))
        {
            inputProcessed = dog.TryMove(Vector2Int.down); // Up input moves -Y (z+)
        }
        else if (AnyKeyPressed(moveDownKeys))
        {
            inputProcessed = dog.TryMove(Vector2Int.up); // Down input moves +Y (z-)
        }
        else if (AnyKeyPressed(moveLeftKeys))
        {
            inputProcessed = dog.TryMove(Vector2Int.left);
        }
        else if (AnyKeyPressed(moveRightKeys))
        {
            inputProcessed = dog.TryMove(Vector2Int.right);
        }
        // NEW: Dog rotation input
        else if (AnyKeyPressed(dogRotateLeftKeys))
        {
            inputProcessed = dog.TryRotateLeft();
        }
        else if (AnyKeyPressed(dogRotateRightKeys))
        {
            inputProcessed = dog.TryRotateRight();
        }
        // Stick interaction
        else if (AnyKeyPressed(pickupKeys))
        {
            dog.ToggleStickPickup();
            inputProcessed = true; // Always valid action
        }
        else if (AnyKeyPressed(stickRotateLeftKeys))
        {
            inputProcessed = dog.TryRotateStick(false); // counterclockwise
        }
        else if (AnyKeyPressed(stickRotateRightKeys))
        {
            inputProcessed = dog.TryRotateStick(true); // clockwise
        }
        // System actions
        else if (AnyKeyPressed(undoKeys))
        {
            UndoManager.Instance.Undo();
            inputProcessed = true; // Always valid action
        }

        // Handle input feedback
        if (inputProcessed)
        {
            lastInputTime = Time.time;
            OnValidInput?.Invoke();
        }
        else if (AnyKeyPressed(moveUpKeys) || AnyKeyPressed(moveDownKeys) ||
                 AnyKeyPressed(moveLeftKeys) || AnyKeyPressed(moveRightKeys) ||
                 AnyKeyPressed(dogRotateLeftKeys) || AnyKeyPressed(dogRotateRightKeys) ||
                 AnyKeyPressed(stickRotateLeftKeys) || AnyKeyPressed(stickRotateRightKeys))
        {
            // Input was attempted but failed (blocked movement/rotation)
            OnInvalidInput?.Invoke();
        }
    }

    private bool AnyKeyPressed(KeyCode[] keys)
    {
        foreach (KeyCode key in keys)
        {
            if (Input.GetKeyDown(key))
                return true;
        }
        return false;
    }

    // === Mobile/UI Button Hooks ===
    public void MoveUp()
    {
        if (CanProcessInput() && dog.TryMove(Vector2Int.down))
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void MoveDown()
    {
        if (CanProcessInput() && dog.TryMove(Vector2Int.up))
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void MoveLeft()
    {
        if (CanProcessInput() && dog.TryMove(Vector2Int.left))
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void MoveRight()
    {
        if (CanProcessInput() && dog.TryMove(Vector2Int.right))
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    // NEW: Dog rotation methods
    public void RotateDogLeft()
    {
        if (CanProcessInput() && dog.TryRotateLeft())
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void RotateDogRight()
    {
        if (CanProcessInput() && dog.TryRotateRight())
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void RotateStickLeft()
    {
        if (CanProcessInput() && dog.TryRotateStick(false))
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void RotateStickRight()
    {
        if (CanProcessInput() && dog.TryRotateStick(true))
            OnValidInput?.Invoke();
        else
            OnInvalidInput?.Invoke();
    }

    public void TogglePickup()
    {
        if (CanProcessInput())
        {
            dog.ToggleStickPickup();
            OnValidInput?.Invoke();
        }
    }

    public void Undo()
    {
        if (CanProcessInput())
        {
            UndoManager.Instance.Undo();
            OnValidInput?.Invoke();
        }
    }

    private bool CanProcessInput()
    {
        return enableInput && dog != null && !dog.IsBusy() &&
               Time.time >= lastInputTime + inputCooldown;
    }

    // === Public Control Methods ===
    public void SetInputEnabled(bool enabled)
    {
        enableInput = enabled;
    }

    public void ResetInputCooldown()
    {
        lastInputTime = 0f;
    }

    public bool IsInputEnabled()
    {
        return enableInput && CanProcessInput();
    }
}