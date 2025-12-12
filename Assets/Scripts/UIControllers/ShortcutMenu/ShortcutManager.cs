using App.BaseSystem.DataStores.ScriptableObjects.Item;
using System.Collections.Generic;
using UnityEngine;

public class ShortcutManager : MonoBehaviour
{
    public static ShortcutManager Instance;
    public List<int> shortcutSlots = new List<int>(new int[4]);         // 4スロットを0初期化
    public List<int> transfomationSlots = new List<int>(new int[3]);    // 3スロットを0初期化
    [SerializeField] private ShortcutSlotUI[] slotUIs;                  // UIスロット参照
    [SerializeField] private TransformationSlotUI[] transformSlotUIs;    // 変身スロットを参照

    private ItemDataStore itemDataStore;

    void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();

        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);
    }

    void Start()
    {
        RefreshUI(); // 起動時に初期表示
    }

    // 通常アイテムのショートカット追加
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

    // 変身用着ぐるみアイテムのショートカット追加
    public bool AddToTransformShortcut(int id)
    {
        // すでに登録されているかチェック
        if (transfomationSlots.Contains(id))
        {
            Debug.Log("すでに登録されたアイテムです");
            return false;
        }

        // 空きスロットを探して登録
        for (int i = 0; i < transfomationSlots.Count; i++)
        {
            if (transfomationSlots[i] == 0)
            {
                transfomationSlots[i] = id;
                Debug.Log($"ID:{id} をショートカットに登録しました");
                RefreshUI();
                return true;
            }
        }

        Debug.Log("ショートカットがいっぱいです");
        return false;
    }

    // 通常ショートカットの削除
    public void RemoveFromShortcut(int index)
    {
        if (index >= 0 && index < shortcutSlots.Count)
        {
            shortcutSlots[index] = 0;
            RefreshUI();
        }
    }

    // 変身アイテムショートカットの削除
    public void RemoveFromTransform(int index)
    {
        if (index >= 0 && index < transfomationSlots.Count)
        {
            transfomationSlots[index] = 0;
            RefreshUI();
        }
    }

    // UIスロット全体を更新
    public void RefreshUI()
    {
        // ------------- 一般アイテムに関しての更新 -------------------
        for (int i = 0; i < slotUIs.Length; i++)
        {
            // ショートカットスロットの更新
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

        // ------------- 変身アイテムに関しての更新 -------------------
        for (int j = 0; j < transformSlotUIs.Length; j++)
        {
            // 変身アイテムスロットの更新
            if (j < transfomationSlots.Count && transfomationSlots[j] != 0)
            {
                var data = itemDataStore.FindWithId(transfomationSlots[j]);
                if (data != null)
                {
                    transformSlotUIs[j].SetSlot(data.Id, data.Image);
                }
                else
                {
                    transformSlotUIs[j].Clear();
                }
            }
            else
            {
                transformSlotUIs[j].Clear();
            }
        }
    }
}
