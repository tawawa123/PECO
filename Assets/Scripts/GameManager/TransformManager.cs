using App.BaseSystem.DataStores.ScriptableObjects.Item;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateManager;

public class TransformManager : MonoBehaviour
{
    public static TransformManager Instance;
    private ItemDataStore itemDataStore;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        itemDataStore = FindObjectOfType<ItemDataStore>();
    }

    public void StartTransform(int id)
    {
        var data = itemDataStore.FindWithId(id);

        GameObject currentPlayer = GameManager.Instance.GetPlayerObj();
        GameObject nextPlayer = data.Costume;

        // すでに変身中 ＆ 同じアイテム → 元に戻る
        if (GameManager.Instance.IsTransforming && GameManager.Instance.IsSameItem(id))
        {
            data = itemDataStore.FindWithId(0);
            nextPlayer = data.Costume; // defaultオブジェクト
            currentPlayer.GetComponent<PlayerController>().Transform(0); // 操作方式の変更(デフォルトに戻す)

            PerformTransformation(currentPlayer, nextPlayer); // オブジェクト入れ替え
            currentPlayer.GetComponent<PlayerController>().num = id;

            GameManager.Instance.ClearTransInfo(); // 保持していた変身先情報をクリア
            return;
        }

        // 新規変身
        currentPlayer.GetComponent<PlayerController>().Transform(data.Id); // 操作方式の変更
        currentPlayer.GetComponent<PlayerController>().num = id;
        GameManager.Instance.SetTransformItemId(id);
        PerformTransformation(currentPlayer, nextPlayer);
    }

    // 変身処理
    public void PerformTransformation(GameObject culPlayer, GameObject nextPlayer)
    {
        // 新プレイヤーを生成　旧プレイヤーを破棄
        nextPlayer = Object.Instantiate(nextPlayer, culPlayer.transform.position, culPlayer.transform.rotation);
        GameObject.Destroy(culPlayer);

        // カメラの追跡対象を更新
        Transform center = nextPlayer.transform.Find("center");
        GameManager.Instance.ChangeCameraTarget(center, center);
        
        // 現在のプレイヤー情報を更新
        GameManager.Instance.SetPlayerObj(nextPlayer);
        
        GameLog.Trace("変身完了！");
    }
}
