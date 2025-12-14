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
        if (id == 0) return;

        var data = itemDataStore.FindWithId(id);

        currentPlayer = GameManager.Instance.GetPlayerObj();
        GameObject nextPlayer = data.Costume;

        var transformMgr = GameManager.Instance;

        // すでに変身中 ＆ 同じアイテム → 元に戻る
        if (transformMgr.IsTransforming && transformMgr.IsSameItem(id))
        {
            data = itemDataStore.FindWithId(0);
            Debug.Log(data);
            nextPlayer = data.Costume; // defaultオブジェクト
            currentPlayer.GetComponent<PlayerController>().Transform(0); // 操作方式の変更(デフォルトに戻す)

            PerformTransformation(currentPlayer, nextPlayer); // オブジェクト入れ替え
            currentPlayer.GetComponent<PlayerController>().num = id;

            transformMgr.ClearTransInfo(); // 保持していた変身先情報をクリア
            Highlight(); // UI再読み込み
            return;
        }

        // 新規変身
        currentPlayer.GetComponent<PlayerController>().Transform(data.Id); // 操作方式の変更
        currentPlayer.GetComponent<PlayerController>().num = id;
        transformMgr.StartTransform(id);
        PerformTransformation(currentPlayer, nextPlayer);

        Highlight();
    }

    // 変身処理
    public void PerformTransformation(GameObject culPlayer, GameObject nextPlayer)
    {
        // 新プレイヤーを生成　旧プレイヤーを破棄
        nextPlayer = Object.Instantiate(nextPlayer, culPlayer.transform.position, culPlayer.transform.rotation);
        GameObject.Destroy(culPlayer);
        //culPlayer.SetActive(false);

        // カメラの追跡対象を更新
        Transform center = nextPlayer.transform.Find("center");
        GameManager.Instance.ChangeCameraTarget(center, center);
        
        // 現在のプレイヤー情報を更新
        GameManager.Instance.SetPlayerObj(nextPlayer);
        this.currentPlayer = nextPlayer;
        
        Debug.Log("変身完了！");
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
