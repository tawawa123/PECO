using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    private int itemId;

    public void Setup(int id, string itemName, Sprite sprite, int count)
    {
        itemId = id;
        nameText.text = itemName;
        iconImage.sprite = sprite;
        countText.text = count.ToString();
    }

    public void Clear()
    {
        itemId = 0;
        nameText.text = "";
        iconImage.sprite = null;
        countText.text = "";
    }

    // UIボタンにこの関数をアサイン
    public void OnClickSlot()
    {
        if(itemId != 0)
            ShortcutManager.Instance.AddToShortcut(itemId);
    }
}
