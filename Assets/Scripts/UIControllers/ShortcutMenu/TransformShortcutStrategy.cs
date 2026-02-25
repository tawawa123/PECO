using App.BaseSystem.DataStores.ScriptableObjects.Item;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using StateManager;

public class TransformShortcutStrategy : ShortcutStrategy
{
    private ItemDataStore itemDataStore;
    private Image itemImage;

    private int currentIndex;
    private List<int> slots;

    private GameObject currentPlayer;

    // 変身ショートカットの初期化
    public TransformShortcutStrategy(
        ItemDataStore itemDataStore,
        Image itemImage)
    {
        this.itemDataStore = itemDataStore;
        this.itemImage = itemImage;

        currentIndex = 0;
        slots = ShortcutManager.Instance.transfomationSlots;

        MoveToNextValidSlot(true);
    }

    // キーインプットのハンドラ
    public void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            ChangeSelection(1);

        if (Input.GetKeyDown(KeyCode.Q))
            UseItem();
    }

    // スロットに表示されるアイテムを変更
    private void ChangeSelection(int direction)
    {
        currentIndex = (currentIndex + 1) % slots.Count;

        if (slots[currentIndex] == 0) 
        { 
            // 無効なスロットだったら、次の有効なスロットを探す 
            MoveToNextValidSlot(direction > 0); 
        }

        Highlight();
    }

    // 有効なスロットに移動する 
    public void MoveToNextValidSlot(bool forward)
    {
        int slotCount = slots.Count; 
        for (int i = 0; i < slotCount; i++) 
        { 
            currentIndex = (currentIndex + (forward ? 1 : -1) + slotCount) % slotCount; 
            if (slots[currentIndex] != 0) 
                return; // 見つかったら終了 
        } 
        currentIndex = 0; // 見つからなかったら先頭へ 
    }

    // アイテム使用
    private void UseItem()
    {
        int id = ShortcutManager.Instance.transfomationSlots[currentIndex];
        TransformManager.Instance.StartTransform(id);

        Highlight();
    }


    // UIの再読み込み
    public void Highlight()
    {
        if(slots[currentIndex] != 0)
        {
            int id = slots[currentIndex];
            var data = itemDataStore.FindWithId(id);

            itemImage.sprite = data.Image;
            itemImage.color = Color.white;
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
    }
}
