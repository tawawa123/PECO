using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;               // シングルトン
    public Dictionary<int, int> items = new();      // <id, 個数>

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // サンプル初期データ
        items.Add(1, 3);
        items.Add(2, 5);
    }

    public void AddItem(int id, int count = 1)
    {
        if (items.ContainsKey(id))
            items[id] += count;
        else
            items[id] = count;
    }

    public void RemoveItem(int id, int count = 1)
    {
        if (items.ContainsKey(id))
        {
            items[id] -= count;
            if (items[id] <= 0) items.Remove(id);
        }
    }
}
