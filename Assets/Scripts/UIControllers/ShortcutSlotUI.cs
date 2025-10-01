using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShortcutSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;

    private int itemId;

    // 空スロット表示
    public void Clear()
    {
        itemId = 0;
        iconImage.sprite = null;
        iconImage.color = new Color(1, 1, 1, 0);  // 非表示っぽくする
        nameText.text = "";
    }

    // アイテム表示
    public void SetSlot(int id, Sprite sprite, int itemCount)
    {
        itemId = id;
        iconImage.sprite = sprite;
        iconImage.color = Color.white;
        nameText.text = itemCount.ToString();
    }
}
