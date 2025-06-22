using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Tile Prefabs")]
    public GameObject tile_Grass;

    public GameObject tile_Goal;
    public GameObject tile_Void;

    [Header("Object Prefabs")]
    public GameObject obj_Tree;

    public GameObject obj_Dog;
    public GameObject obj_Stick;

    [Header("Level Container")]
    public Transform Container;

    [Header("Placement Settings")]
    public float objectYOffset = 1.5f;

    public float cellSize = 1f;

    public Vector2Int gridSize { get; private set; }

    public Vector3 GridCenter => new Vector3(gridSize.x / 2f - 0.5f, 0f, -gridSize.y / 2f + 0.5f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GenerateLevel(LevelJsonData data, out DogController dog, out StickController stick)
    {
        dog = null;
        stick = null;

        foreach (Transform child in Container)
            Destroy(child.gameObject);

        gridSize = new Vector2Int(data.width, data.height);

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                Vector3 basePos = new Vector3(x * cellSize, 0f, -y * cellSize);
                Vector3 elevatedPos = basePos + Vector3.up * objectYOffset;
                Vector2Int gridPos = new Vector2Int(x, y);

                // ---- GROUND ----
                string groundType = data.ground[y][x]?.Trim().ToLower();
                GameObject tilePrefab = groundType switch
                {
                    "grass" => tile_Grass,
                    "goal" => tile_Goal,
                    "void" => tile_Void,
                    _ => null
                };

                if (tilePrefab)
                    Instantiate(tilePrefab, basePos, Quaternion.identity, Container);

                // ---- OBJECTS ----
                string objType = data.objects[y][x]?.Trim().ToLower();
                switch (objType)
                {
                    case "tree":
                        Instantiate(obj_Tree, elevatedPos, Quaternion.identity, Container);
                        break;

                    //case "goal":
                    //    Instantiate(tile_Goal, elevatedPos, Quaternion.identity, Container);
                    //    break;

                    case "dog":
                        GameObject dogObj = Instantiate(obj_Dog, elevatedPos, Quaternion.identity, Container);
                        dog = dogObj.GetComponent<DogController>();
                        dog.gridPos = gridPos;
                        dog.facing = ParseDirection(data.dogStartDir);
                        dog.ApplyTransform();

                        InputManager input = Object.FindFirstObjectByType<InputManager>();
                        if (input != null)
                            input.dog = dog;
                        break;

                    case "stick":
                        GameObject stickObj = Instantiate(obj_Stick, elevatedPos, Quaternion.identity, Container);
                        stick = stickObj.GetComponent<StickController>();
                        stick.rootPosition = gridPos;
                        stick.direction = ParseDirection(data.stickDir);
                        stick.ApplyTransform();
                        break;
                }
                // After placing tile and object...
                GridValidator.Instance.SetTile(gridPos, DetermineTileType(groundType, objType));
            }
        }

        Debug.Log($"Generated Level {data.width}x{data.height}");
    }

    private TileType DetermineTileType(string ground, string obj)
    {
        if (ground == "void") return TileType.Void;
        if (obj == "tree") return TileType.Tree;
        if (ground == "goal") return TileType.Goal;
        return TileType.Grass;
    }

    private Direction ParseDirection(string dir)
    {
        return dir.Trim().ToLower() switch
        {
            "up" => Direction.Up,
            "down" => Direction.Down,
            "left" => Direction.Left,
            "right" => Direction.Right,
            _ => Direction.Right
        };
    }
}