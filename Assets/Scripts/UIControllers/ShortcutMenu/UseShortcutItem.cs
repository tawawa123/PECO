using App.BaseSystem.DataStores.ScriptableObjects.Item;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UseShortcutItem : MonoBehaviour
{
    // モード切替
    [SerializeField] private enum Mode 
    { 
        normal, 
        transformer 
    }
    [SerializeField] private Mode mode;

    private ItemDataStore itemDataStore;
    [SerializeField] private ShortcutManager shortcutManager;
    [SerializeField] private Image itemImage;

    // 通常アイテムのショートカットメニュー用変数
    [ShowIf("mode", (int)Mode.normal)]
    public int scCurrentIndex = 0; // 今見ているリスト番号

    [ShowIf("mode", (int)Mode.normal)]
    [SerializeField] private TextMeshProUGUI itemCount; // アイテムを何個持っているか

    // 変身アイテムのショートカットメニュー用変数
    [ShowIf("mode", (int)Mode.transformer)]
    public int tfCurrentIndex = 0; // 今見ているリスト番号


    void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();
    }


    private void Start()
    {
        itemImage.color = new Color(1, 1, 1, 0);

        if (shortcutManager == null)
        {
            Debug.LogError("ShortcutManagerがシーンに見つかりません。");
            enabled = false;
            return;
        }

        // 初期スロットを有効なアイテムに合わせる
        MoveToNextValidSlot(forward: true, shortcutManager.shortcutSlots, scCurrentIndex);
        MoveToNextValidSlot(forward: true, shortcutManager.transfomationSlots, tfCurrentIndex);
    }


    private void Update()
    {
        // バッグメニューの表示タイミングでアイテム表示をリフレッシュ（tabキー）
        if (Input.GetKeyDown(KeyCode.Tab))
            HighlightCurrentSlot();

        
        /// <summury>
        /// 通常アイテムのショートカットメニュー操作
        /// 4要素のリストにアイテムが入っている　リストの移動はキー操作で行う
        /// C - リストの番号が小さい方へ移動
        /// X - リストの番号が大きい方へ移動
        /// R - ショートカットのアイテムを使用
        /// <summury>

        // 前のスロットに移動 (Cキー)
        if (Input.GetKeyDown(KeyCode.C) && mode==Mode.normal)
        {
            ChangeSelection(-1, shortcutManager.shortcutSlots, scCurrentIndex);
        }

        // 次のスロットに移動 (Xキー)
        if (Input.GetKeyDown(KeyCode.X) && mode==Mode.normal)
        {
            ChangeSelection(1, shortcutManager.shortcutSlots, scCurrentIndex);
        }

        // 選択中アイテムを使用 (Rキー)
        if (Input.GetKeyDown(KeyCode.R) && mode==Mode.normal)
        {
            Inventory.Instance.UseItem(shortcutManager.shortcutSlots[scCurrentIndex]);
            HighlightCurrentSlot();
        }


        /// <summury>
        /// 変身アイテムのショートカットメニュー操作
        /// 3要素のリストにアイテムが入っている　リストの移動はキー操作で行う
        /// Z - リストの順にアイテムを閲覧
        /// Q - ショートカットのアイテムを使用
        /// <summury>

        // 変身アイテムを次スロットへ移動（zキー）
        if(Input.GetKeyDown(KeyCode.Z) && mode==Mode.transformer)
        {
            ChangeSelection(1, shortcutManager.transfomationSlots, tfCurrentIndex);
        }

        // 選択中アイテムを使用 (Qキー)
        if (Input.GetKeyDown(KeyCode.Q) && mode==Mode.transformer)
        {
            Inventory.Instance.UseItem(shortcutManager.transfomationSlots[tfCurrentIndex]);
            HighlightCurrentSlot();
        }
    }


    /// <summary>
    /// スロット切り替え
    /// </summary>
    private void ChangeSelection(int direction, List<int> useSlot, int useCurrentIndex)
    {
        int slotCount = useSlot.Count;

        // directionが1なら次、-1なら前へ
        useCurrentIndex = (useCurrentIndex + direction + slotCount) % slotCount;

        if (useSlot[useCurrentIndex] == 0)
        {
            // 無効なスロットだったら、次の有効なスロットを探す
            MoveToNextValidSlot(direction > 0, useSlot, useCurrentIndex);
        }

        HighlightCurrentSlot();
    }


    /// <summary>
    /// 有効なスロットに移動する
    /// </summary>
    private void MoveToNextValidSlot(bool forward, List<int> useSlot, int useCurrentIndex)
    {
        int slotCount = shortcutManager.shortcutSlots.Count;
        for (int i = 0; i < slotCount; i++)
        {
            scCurrentIndex = (scCurrentIndex + (forward ? 1 : -1) + slotCount) % slotCount;
            if (shortcutManager.shortcutSlots[scCurrentIndex] != 0)
                return; // 見つかったら終了
        }
        scCurrentIndex = 0; // 見つからなかったら先頭へ
    }


    /// <summary>
    /// 選択中のスロットを視覚的に強調表示する（UI連携用）
    /// </summary>
    public void HighlightCurrentSlot()
    {
    // ------------------ 通常アイテムショートカット -----------------------
        if (mode == Mode.normal)
        {
            // ショートカットスロットに何かが入っていたなら表示
            if(shortcutManager.shortcutSlots[scCurrentIndex] != 0)
            {
                var data = itemDataStore.FindWithId(shortcutManager.shortcutSlots[scCurrentIndex]);

                itemImage.sprite = data.Image;
                itemImage.color = new Color(1, 1, 1, 255);
                itemCount.text = Inventory.Instance.items[shortcutManager.shortcutSlots[scCurrentIndex]].ToString();
            }

            // ショートカットスロットに何も入っていなかったら処理をスルー
            if(shortcutManager.shortcutSlots[scCurrentIndex] == 0)
                return;

            // ショートカットスロット内のアイテムが0個になったら削除
            if(Inventory.Instance.items[shortcutManager.shortcutSlots[scCurrentIndex]] == 0)
            {
                Clear();
                Inventory.Instance.RemoveItem(shortcutManager.shortcutSlots[scCurrentIndex]);
                ShortcutManager.Instance.RemoveFromShortcut(scCurrentIndex);
            }
        }
     // ------------------ 変身アイテムショートカット -----------------------
        else
        {
            // ショートカットスロットに何かが入っていたなら表示
            if(shortcutManager.transfomationSlots[tfCurrentIndex] != 0)
            {
                var data = itemDataStore.FindWithId(shortcutManager.transfomationSlots[tfCurrentIndex]);

                itemImage.sprite = data.Image;
                itemImage.color = new Color(1, 1, 1, 255);
            }

            // ショートカットスロットに何も入っていなかったら処理をスルー
            if(shortcutManager.transfomationSlots[tfCurrentIndex] == 0)
                return;
        }
    }

    public void Clear()
    {
        itemImage.sprite = null;
        itemImage.color = new Color(1, 1, 1, 0);
        itemCount.text = "";
    }
}
