using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class InteractManager : MonoBehaviour, Interactable
{
    public enum InteractState
    {
        Talk,
        Hide,
        GetItem
    }

    [Header("インタラクトタイプ")]
    [SerializeField] private InteractState state;
    
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
    private void Talk()
    {
        Debug.Log("会話が始まりました");
    }

    // フィールドオブジェクトに擬態するハイドアクション
    private void Hide()
    {
        Debug.Log("隠れました");
    }

    // フィールドアイテムの取得
    private void GetItem()
    {
        Debug.Log("アイテムを取得しました");
    }

    public string GetInteractionText() => $"Hello World!";
}
