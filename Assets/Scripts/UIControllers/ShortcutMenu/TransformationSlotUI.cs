using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TransformationSlotUI : MonoBehaviour
{
    public Image iconImage;

    private int itemId;

    // 空スロット表示
    public void Clear()
    {
        itemId = 0;
        iconImage.sprite = null;
        iconImage.color = new Color(1, 1, 1, 0);  // 非表示っぽくする
    }

    // アイテム表示
    public void SetSlot(int id, Sprite sprite)
    {
        itemId = id;
        iconImage.sprite = sprite;
        iconImage.color = Color.white;
    }
}