using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private CinemachineVirtualCamera lockonCam;
    [SerializeField] private CinemachineFreeLook freeLookCam;
    private GameObject player;

    public bool IsTransforming => currentTransformItemId != 0;

    private int currentTransformItemId = 0;
    private int defaultPlayer_id;
    private int transformedPlayer_id;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        player = GameObject.FindGameObjectWithTag("Player");
    }


    /// <summury>
    /// ゲームマネージャー
    /// ゲーム全体の進捗管理と、一括で管理するオブジェクトの参照元になる
    /// 
    /// ゲーム全体の進捗管理
    /// 　- NPCの会話イベントなどでそれらを感知しシーン分け
    /// イベントシーン・ゲームオーバーなどの特殊シーンへの発火
    /// <summury>

    public void ChangeCameraTarget(Transform newFollow, Transform newLookAt = null)
    {
        if (freeLookCam == null || lockonCam == null) return;

        if (newFollow != null)
        {
            freeLookCam.Follow = newFollow;
            lockonCam.Follow = newFollow;
        }

        if (newLookAt != null)
        {
            freeLookCam.LookAt = newLookAt;
        }
    }

    /// <summury>
    /// 現在のプレイヤーの参照
    /// プレイヤーが変身するので、今どのオブジェクトが登録されていても参照が生きる
    /// 
    /// PlayerControllerへの直参照も避けれる(今後ここを経由するよう実装を変更)
    /// <summury>
    public GameObject GetPlayerObj()
    {
        return this.player;
    }

    public void SetPlayerObj(GameObject player)
    {
        this.player = player;
    }


    /// <summury>
    /// 変身の管理
    /// 本当はここでやるべきじゃないけど、時間がないので
    /// 
    /// UseItemから呼ばれて、現在のオブジェクトの情報を保持しておく
    /// <summury>
    public void StartTransform(int itemId)
    {
        currentTransformItemId = itemId;
    }

    public bool IsSameItem(int itemId)
    {
        return currentTransformItemId == itemId;
    }

    public void ClearTransInfo()
    {
        currentTransformItemId = 0;
    }
}
