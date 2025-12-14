using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using StateManager;

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

    private PlayerStatus currentStatus;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        player = GameObject.FindGameObjectWithTag("Player");

        currentStatus = new PlayerStatus()
        {
            m_hp = 100,
            m_atkDamage = 5,
            m_atkPower = 1,
            m_stumina = 100f,
            m_walkSpeed = 3.5f,
            m_runSpeed = 6f,
            m_rotationRate = 8f,
            m_avoidPower = 5f,
            m_stun = false
        };
    }

    public PlayerStatus CurrentStatus => currentStatus;

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
    /// シーン遷移管理
    /// スタートシーンとゲームシーン
    /// 
    /// Start -> 開始シーン
    /// SampleScene -> ゲームシーン
    /// <summury>
    public void ChangeGameScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
    public void ChangeStartScene()
    {
        SceneManager.LoadScene("Start");
    }
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
        #else
            Application.Quit();//ゲームプレイ終了
        #endif
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
