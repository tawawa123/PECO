using UnityEngine;

public class StealthAttackOwner : MonoBehaviour
{
    // オブジェクトを映すカメラ
    [SerializeField] private Camera targetCamera;
    // 表示するUI
    [SerializeField] private Transform targetUI;
    // オブジェクト位置のオフセット
    [SerializeField] private Vector3 worldOffset;
    // 表示させる距離
    [SerializeField] private float stealthAttackDistance;
    
    // UIを表示させる対象オブジェクト
    private GameObject target;

    private RectTransform parentUI;
    private PlayerLockon playerLockon;
    private StateManager.PlayerController playerController;

    private void Awake()
    {
        playerLockon = this.GetComponent<PlayerLockon>();
        playerController = this.GetComponent<StateManager.PlayerController>();

        // カメラが指定されていなければメインカメラにする
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 親UIのRectTransformを保持
        parentUI = targetUI.parent.GetComponent<RectTransform>();
    }

    private void Update()
    {
        target = playerLockon.GetLockonTarget(stealthAttackDistance);
        // 近くに敵がいなかったら終了
        if(target == null){
            targetUI.gameObject.SetActive(false);
            playerController.CanStealthAttack(false);
            return;
        }

        // カメラに敵が映っていなければ終了
        if (!CheckTargetOnFront()){
            targetUI.gameObject.SetActive(false);
            playerController.CanStealthAttack(false);
            return;
        }
        
        // 敵がプレイヤーに気づいていたら終了
        var enemyStatus = target.GetComponent<EnemyStatus>();
        if (enemyStatus.GetVigilancePoint == 100f){
            targetUI.gameObject.SetActive(false);
            playerController.CanStealthAttack(false);
            return;
        }
        
        // プレイヤーはステルス攻撃可能
        playerController.CanStealthAttack(true);
        playerController.SetTarget(target);
        OnUpdatePosition();
    }

    private bool CheckTargetOnFront()
    {

        var cameraTransform = targetCamera.transform;

        var cameraDir = cameraTransform.forward; // カメラの向き
        var targetDir = (target.transform.position + worldOffset) - cameraTransform.position; // カメラからターゲットへのベクトル

        // ターゲットがカメラの正面にいるか
        var isFront = Vector3.Dot(cameraDir, targetDir) > 0;

        return isFront;
    }

    // UIの位置を更新する
    private void OnUpdatePosition()
    {
        targetUI.gameObject.SetActive(true);

        // UIがオブジェクトに重なるように座標変換
        var targetScreenPos = targetCamera.WorldToScreenPoint(target.transform.position + worldOffset);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentUI,
            targetScreenPos,
            null,
            out var uiLocalPos
        );

        targetUI.localPosition = uiLocalPos;
    }
}