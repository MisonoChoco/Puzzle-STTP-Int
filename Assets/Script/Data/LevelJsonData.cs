[System.Serializable]
public class LevelJsonData
{
    public int width;
    public int height;
    public string[][] ground;   //grass, void, goal
    public string[][] objects;  //tree, dog, stick, goal
    public string dogStartDir;
    public string stickDir;
}