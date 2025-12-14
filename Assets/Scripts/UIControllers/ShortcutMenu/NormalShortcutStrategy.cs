using App.BaseSystem.DataStores.ScriptableObjects.Item;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class NormalShortcutStrategy : ShortcutStrategy
{
    private ItemDataStore itemDataStore;
    private Image itemImage;
    private TextMeshProUGUI itemCount;

    private int currentIndex;
    private List<int> slots;

    // 通常アイテムのショートカットの初期化
    public NormalShortcutStrategy(
        ItemDataStore itemDataStore,
        Image itemImage,
        TextMeshProUGUI itemCount)
    {
        this.itemDataStore = itemDataStore;
        this.itemImage = itemImage;
        this.itemCount = itemCount;

        currentIndex = 0;
        slots = ShortcutManager.Instance.shortcutSlots;

        MoveToNextValidSlot(true);
    }

    // キーインプットのハンドラ
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.C))
            ChangeSelection(-1);

        if (Input.GetKeyDown(KeyCode.X))
            ChangeSelection(1);

        if (Input.GetKeyDown(KeyCode.R))
            UseItem();
    }


    // ショートカットのアイテムを変更
    private void ChangeSelection(int direction)
    {
        currentIndex = (currentIndex + direction + slots.Count) % slots.Count;

        if (slots[currentIndex] == 0) 
        { 
            // 無効なスロットだったら、次の有効なスロットを探す 
            MoveToNextValidSlot(direction > 0); 
        }
        Highlight();
    }

    // 有効なスロットに移動する 
    private void MoveToNextValidSlot(bool forward)
    {
        int slotCount = slots.Count;

        for (int i = 0; i < slotCount; i++)
        {
            currentIndex = (currentIndex + (forward ? 1 : -1) + slotCount) % slotCount;
            if (slots[currentIndex] != 0)
                return; // 見つかったら終了
        }
    }

    // アイテムを使用
    private void UseItem()
    {
        int id = slots[currentIndex];
        if (id == 0) return;

        Inventory.Instance.UseItem(id);
        Highlight();
    }

    // UIを再読み込み
    public void Highlight()
    {
        if(slots[currentIndex] != 0)
        {
            int id = slots[currentIndex];
            var data = itemDataStore.FindWithId(id);

            itemImage.sprite = data.Image;
            itemImage.color = Color.white;
            itemCount.text = Inventory.Instance.items[id].ToString();
        }

        if(slots[currentIndex] == 0)
        {
            Clear();
            return;
        }

        if(Inventory.Instance.items[slots[currentIndex]] == 0)
        {
            Clear();
            Inventory.Instance.RemoveItem(slots[currentIndex]);
            ShortcutManager.Instance.RemoveFromShortcut(currentIndex);
        }
    }

    public void Clear() 
    { 
        itemImage.sprite = null; 
        itemImage.color = new Color(1, 1, 1, 0); 
        itemCount.text = ""; 
    }
}