using App.BaseSystem.DataStores.ScriptableObjects.Item;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UseShortcutItem : MonoBehaviour
{
    private enum Mode
    {
        normal,
        transform
    }
    [SerializeField] private Mode mode;

    [SerializeField] private ShortcutManager shortcutManager;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemCount;

    private ItemDataStore itemDataStore;
    private ShortcutStrategy strategy;

    private void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();
    }

    private void Start()
    {
        // 初期戦略
        if(mode==Mode.normal)
            SetNormalMode();
        else if(mode==Mode.transform)
            SetTransformMode();
        
        strategy?.Highlight();
    }

    private void Update()
    {
        strategy?.HandleInput();

        if (Input.GetKeyDown(KeyCode.Tab))
            strategy?.Highlight();
    }

    public void SetNormalMode()
    {
        strategy = new NormalShortcutStrategy(
            itemDataStore,
            itemImage,
            itemCount
        );
    }

    public void SetTransformMode()
    {
        strategy = new TransformShortcutStrategy(
            itemDataStore,
            itemImage
        );
    }
}
