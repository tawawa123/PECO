using App.BaseSystem.DataStores.ScriptableObjects.Item;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;


public class InteractManager : MonoBehaviour, Interactable
{
    // オブジェクトが破壊された時に呼び出すフラグ
    public static event Action<InteractManager> OnAboutToBeDestroyed;

    private ItemDataStore itemDataStore;
    private Inventory inventory;

    // ステートがGetItemの時のみ
    [SerializeField] private ItemData itemData;

    // ステートがTalkの時のみ
    [SerializeField] private ConversationData conversationData;
    [SerializeField] private GameProgress progress;
    private bool isTalking = false;

    // ステートがHideの時のみ

    public enum InteractState
    {
        Talk,
        Hide,
        GetItem
    }

    [Header("インタラクトタイプ")]
    [SerializeField] private InteractState state;

    void Awake()
    {
        itemDataStore = FindObjectOfType<ItemDataStore>();
    }
    
    public void Interact()
    {
        Action action = state switch
        {
            InteractState.Talk => Talk,
            InteractState.Hide => Hide,
            InteractState.GetItem => GetItem,
            _ => () => Debug.LogWarning("No action defined")
        };

        action.Invoke();
    }

    // NPCとの会話イベント
    private async void Talk()
    {
        if (isTalking) return; // 多重入力防止
        isTalking = true;

        Debug.Log(progress.storyProgress);
        var sequence = conversationData.GetConversation(progress.storyProgress);
        if (sequence == null)
        {
            Debug.Log($"{conversationData.npcName}: ……");
            isTalking = false;
            return;
        }

        foreach (var line in sequence.texts)
        {
            Debug.Log($"{conversationData.npcName}: {line}");

            // ここで「次へ」キー待ちをする（例：Spaceキー）
            await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.P));
        }

        Debug.Log("会話終了。");
        isTalking = false;
        
        // ここでUIに送る処理を追加（例：UIManager.ShowText(text)）
    }

    // フィールドオブジェクトに擬態するハイドアクション
    private void Hide()
    {
        Debug.Log("hiding!");
    }

    // フィールドアイテムの取得
    private void GetItem()
    {
        Debug.Log("get item!");

        // インベントリにこのアイテムを追加
        Inventory.Instance.AddItem(itemData.Id, 1);

        // 
        var col = this.gameObject.GetComponent<CapsuleCollider>();
        OnAboutToBeDestroyed?.Invoke(this);
        Destroy(this.gameObject);
    }

    public string GetInteractionText() => $"item interact !";
}
