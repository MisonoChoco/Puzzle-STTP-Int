using UnityEngine;

// Enhanced Direction Utilities
public static class DirectionUtil
{
    public static Vector2Int ToVector(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Vector2Int.up,
            Direction.Right => Vector2Int.right,
            Direction.Down => Vector2Int.down,
            Direction.Left => Vector2Int.left,
            _ => Vector2Int.zero
        };
    }

    public static Direction FromVector(Vector2Int vec)
    {
        if (vec == Vector2Int.right) return Direction.Right;
        if (vec == Vector2Int.left) return Direction.Left;
        if (vec == Vector2Int.up) return Direction.Up;
        if (vec == Vector2Int.down) return Direction.Down;
        return Direction.Right; // Default fallback
    }

    // Convert direction to Y-axis rotation (for 3D objects)
    public static float ToYRotation(Direction dir)
    {
        return dir switch
        {
            Direction.Up => 0f,      // Fixed: Up should be 0 degrees
            Direction.Right => 90f,
            Direction.Down => 180f,   // Fixed: Down should be 180 degrees
            Direction.Left => 270f,
            _ => 0f
        };
    }

    // Convert direction to Z-axis rotation (for 2D sprites)
    public static float ToZRotation(Direction dir)
    {
        return dir switch
        {
            Direction.Right => 0f,
            Direction.Up => 90f,
            Direction.Left => 180f,
            Direction.Down => 270f,
            _ => 0f
        };
    }

    // Convert direction to angle in degrees (0-360)
    public static float ToAngle(Direction dir)
    {
        return dir switch
        {
            Direction.Right => 0f,
            Direction.Up => 90f,
            Direction.Left => 180f,
            Direction.Down => 270f,
            _ => 0f
        };
    }

    // Rotate direction clockwise
    public static Direction RotateRight(Direction d)
    {
        return (Direction)(((int)d + 1) % 4);
    }

    // Rotate direction counter-clockwise
    public static Direction RotateLeft(Direction d)
    {
        return (Direction)(((int)d + 3) % 4);
    }

    // Get opposite direction
    public static Direction GetOpposite(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => Direction.Right
        };
    }

    // Get angle between two directions
    public static float GetAngleBetween(Direction from, Direction to)
    {
        float fromAngle = ToAngle(from);
        float toAngle = ToAngle(to);
        float diff = Mathf.DeltaAngle(fromAngle, toAngle);
        return diff;
    }

    // Check if direction is horizontal (Left or Right)
    public static bool IsHorizontal(Direction dir)
    {
        return dir == Direction.Left || dir == Direction.Right;
    }

    // Check if direction is vertical (Up or Down)
    public static bool IsVertical(Direction dir)
    {
        return dir == Direction.Up || dir == Direction.Down;
    }

    // Get random direction
    public static Direction GetRandom()
    {
        return (Direction)Random.Range(0, 4);
    }

    // Convert from angle to direction (rounds to nearest cardinal direction)
    public static Direction FromAngle(float angle)
    {
        // Normalize angle to 0-360
        angle = ((angle % 360) + 360) % 360;

        if (angle >= 315 || angle < 45) return Direction.Right;
        if (angle >= 45 && angle < 135) return Direction.Up;
        if (angle >= 135 && angle < 225) return Direction.Left;
        return Direction.Down;
    }

    // Get all directions as array
    public static Direction[] GetAllDirections()
    {
        return new Direction[] { Direction.Right, Direction.Up, Direction.Left, Direction.Down };
    }
}