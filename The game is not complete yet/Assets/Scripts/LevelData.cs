using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "LevelData")]
public class LevelData : ScriptableObject
{
    public int keysRequired = 0;
    public int enemiesRequired = 0;
}