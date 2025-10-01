using App.BaseSystem.DataStores.ScriptableObjects.Item;
using System.Collections.Generic;
using UnityEngine;

public class ShortcutManager : MonoBehaviour
{
    public static ShortcutManager Instance;
    public List<int> shortcutSlots = new List<int>(new int[4]); // 4スロットを0初期化
    [SerializeField] private ShortcutSlotUI[] slotUIs;                              // UIスロット参照

    private ItemDataStore itemDataStore;

    void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        RefreshUI(); // 起動時に初期表示
    }

    public bool AddToShortcut(int id)
    {
        // すでに登録されているかチェック
        if (shortcutSlots.Contains(id))
        {
            Debug.Log("すでに登録されたアイテムです");
            return false;
        }

        // 空きスロットを探して登録
        for (int i = 0; i < shortcutSlots.Count; i++)
        {
            if (shortcutSlots[i] == 0)
            {
                shortcutSlots[i] = id;
                Debug.Log($"ID:{id} をショートカットに登録しました");
                RefreshUI();
                return true;
            }
        }

        Debug.Log("ショートカットがいっぱいです");
        return false;
    }

    public void RemoveFromShortcut(int index)
    {
        if (index >= 0 && index < shortcutSlots.Count)
        {
            shortcutSlots[index] = 0;
            RefreshUI();
        }
    }

    // UIスロット全体を更新
    public void RefreshUI()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (i < shortcutSlots.Count && shortcutSlots[i] != 0)
            {
                var data = itemDataStore.FindWithId(shortcutSlots[i]);
                if (data != null)
                {
                    slotUIs[i].SetSlot(data.Id, data.Image, Inventory.Instance.items[data.Id]);
                }
                else
                {
                    slotUIs[i].Clear();
                }
            }
            else
            {
                slotUIs[i].Clear();
            }
        }
    }
}
