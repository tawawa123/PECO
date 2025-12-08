using UnityEngine;

[CreateAssetMenu(fileName = "GameProgress", menuName = "Data/GameProgress")]
public class GameProgress : ScriptableObject
{
    public int storyProgress; // 例: 0=序盤, 1=中盤, 2=終盤 など
    [TextArea(2, 5)] public string[] texts; // 複数文を保持
}
