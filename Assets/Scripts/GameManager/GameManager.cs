using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private GameObject player;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summury>
    /// ゲームマネージャー
    /// ゲーム全体の進捗管理と、一括で管理するオブジェクトの参照元になる
    /// 
    /// ゲーム全体の進捗管理
    /// 　- NPCの会話イベントなどでそれらを感知しシーン分け
    /// イベントシーン・ゲームオーバーなどの特殊シーンへの発火
    /// <summury>

    // player用のゲッター・セッター
    public GameObject GetPlayerObj()
    {
        return this.player;
    }

    public void SetPlayerObj(GameObject player)
    {
        this.player = player;
    }
}
