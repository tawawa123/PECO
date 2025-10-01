using App.BaseSystem.DataStores.ScriptableObjects.Item;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UseShortcutItem : MonoBehaviour
{
    public static UseShortcutItem Instance;

    public int currentIndex = 0;     // 現在選択されているスロット番号
    [SerializeField] private ShortcutManager shortcutManager;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemCount;

    private ItemDataStore itemDataStore;

    void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        MoveToNextValidSlot(forward: true);
    }

    private void Update()
    {
        // 前のスロットに移動 (Cキー)
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeSelection(-1);
        }

        // 次のスロットに移動 (Xキー)
        if (Input.GetKeyDown(KeyCode.X))
        {
            ChangeSelection(1);
        }

        // 選択中アイテムを使用 (Rキー)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Inventory.Instance.UseItem(shortcutManager.shortcutSlots[currentIndex]);
            HighlightCurrentSlot();
        }
    }

    /// <summary>
    /// スロット切り替え
    /// </summary>
    private void ChangeSelection(int direction)
    {
        int slotCount = shortcutManager.shortcutSlots.Count;

        // directionが1なら次、-1なら前へ
        currentIndex = (currentIndex + direction + slotCount) % slotCount;

        if (shortcutManager.shortcutSlots[currentIndex] == 0)
        {
            // 無効なスロットだったら、次の有効なスロットを探す
            MoveToNextValidSlot(direction > 0);
        }

        HighlightCurrentSlot();
    }

    /// <summary>
    /// 有効なスロットに移動する
    /// </summary>
    private void MoveToNextValidSlot(bool forward)
    {
        int slotCount = shortcutManager.shortcutSlots.Count;
        for (int i = 0; i < slotCount; i++)
        {
            currentIndex = (currentIndex + (forward ? 1 : -1) + slotCount) % slotCount;
            if (shortcutManager.shortcutSlots[currentIndex] != 0)
                return; // 見つかったら終了
        }
        currentIndex = 0; // 見つからなかったら先頭へ
    }

    /// <summary>
    /// 選択中のスロットを視覚的に強調表示する（UI連携用）
    /// </summary>
    public void HighlightCurrentSlot()
    {
        // ここではデバッグログのみ。UIに枠をつける処理は別途実装可能。
        // Debug.Log($"現在選択中のスロット: {currentIndex + 1}");

        if(shortcutManager.shortcutSlots[currentIndex] != 0)
        {
            var data = itemDataStore.FindWithId(shortcutManager.shortcutSlots[currentIndex]);

            itemImage.sprite = data.Image;
            itemImage.color = new Color(1, 1, 1, 255);
            itemCount.text = Inventory.Instance.items[shortcutManager.shortcutSlots[currentIndex]].ToString();
        }


        if(Inventory.Instance.items[shortcutManager.shortcutSlots[currentIndex]] == 0)
        {
            Clear();
            Inventory.Instance.RemoveItem(shortcutManager.shortcutSlots[currentIndex]);
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
