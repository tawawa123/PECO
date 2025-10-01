using App.BaseSystem.DataStores.ScriptableObjects.Item;
using UnityEngine;

public class BagUI : MonoBehaviour
{
    [SerializeField] private GameObject bagPanel;          // バッグUI本体
    [SerializeField] private GameObject slotPrefab;        // スロットPrefab
    [SerializeField] private Transform slotParent;         // スロットを並べる親オブジェクト
    private ItemDataStore itemDataStore;

    private bool isOpen = false;

    void Start()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();
        bagPanel.SetActive(isOpen);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isOpen = !isOpen;
            bagPanel.SetActive(isOpen);
            if (isOpen)
                RefreshBag();
            else
                ClearBag();
        }
    }

    void RefreshBag()
    {
        ClearBag();
        foreach (var kvp in Inventory.Instance.items)
        {
            var id = kvp.Key;
            var count = kvp.Value;
            Debug.Log(id);
            var data = itemDataStore.FindWithId(id);
            if (data != null)
            {
                var slot = Instantiate(slotPrefab, slotParent);
                var slotUI = slot.GetComponent<BagSlotUI>();
                slotUI.Setup(id, data.Name, data.Image, count);
            }
        }
    }

    void ClearBag()
    {
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }
    }
}
