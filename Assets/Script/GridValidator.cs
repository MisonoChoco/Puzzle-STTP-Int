using UnityEngine;

public class GridValidator : MonoBehaviour
{
    public static GridValidator Instance;

    public TileType[,] mapData; // From level
    public int width, height;

    private void Awake() => Instance = this;

    public void Initialize(int w, int h)
    {
        width = w;
        height = h;
        mapData = new TileType[width, height];
    }

    public void SetTile(Vector2Int pos, TileType type)
    {
        if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
            mapData[pos.x, pos.y] = type;
    }

    public bool IsTileBlocked(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height)
            return true;

        TileType tile = mapData[pos.x, pos.y];
        return tile == TileType.Void || tile == TileType.Tree;
    }

    public bool AreAllTilesFree(Vector2Int[] tiles)
    {
        foreach (var pos in tiles)
        {
            if (IsTileBlocked(pos)) return false;
        }
        return true;
    }

    public bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }
}