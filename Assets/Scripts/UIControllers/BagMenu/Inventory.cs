using App.BaseSystem.DataStores.ScriptableObjects.Item;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    public Dictionary<int, int> items = new();      // <id, 個数>
    private ItemDataStore itemDataStore;

    void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // サンプル初期データ
        items.Add(1, 3);
        items.Add(2, 5);
        items.Add(1000, 1);
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
            Debug.Log(items.Remove(id));
        }
    }

    public void UseItem(int id, int count = 1)
    {
        if (items.ContainsKey(id))
        {
            var data = itemDataStore.FindWithId(id);
            var player = GameObject.FindGameObjectWithTag("Player");
            PlayerStatus p_status = player.GetComponent<PlayerStatus>();

            p_status.m_hp += data.Heal;
            p_status.m_hp = Mathf.Clamp(p_status.m_hp, 0, 100);

            items[id] -= count;
        }
    }
}
