using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using StateManager;

public class PlayerParryController : MonoBehaviour
{
    // パリィ入力の受付時間 (秒)
    [SerializeField]
    private float parryWindowDuration = 0.2f;

    public bool IsParrySuccessful { get; private set; } = false;
    public bool IsParryActive { get; private set; } = false;
    public bool IsGuarding { get; private set; } = false;
    
    private CancellationTokenSource parryCts;
    private PlayerController playerController;

    private void Start()
    {
        playerController = this.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) && !IsParryActive)
        {
            // 初めて押した時のみパリィ受付ウィンドウを開始
            if (!IsParryActive)
            {
                StartParryWindow();
            }
            
            if (!IsGuarding)
            {
                // ガード開始時の処理
                IsGuarding = true;
                playerController.ChangeGuardState();
            }
        }
        else if (Input.GetMouseButtonUp(1))
            IsGuarding = false;
    }

    private async void StartParryWindow()
    {
        // 既存のトークンをキャンセルして破棄
        parryCts?.Cancel();
        parryCts = new CancellationTokenSource();
        CancellationToken token = parryCts.Token;

        IsParryActive = true;
        IsParrySuccessful = false;
        
        Debug.Log("🛡️ パリィ受付開始！");

        // キャンセルされたかどうかがBoolで返ってくる
        bool isCanceled = await UniTask.Delay(
            System.TimeSpan.FromSeconds(parryWindowDuration),
            cancellationToken: token
        ).SuppressCancellationThrow();

        // --- パリィ受付終了時の判定ロジック ---
        
        // isCanceled が true の場合、NotifyParrySuccess() で明示的にキャンセルされたことを意味する
        if (isCanceled)
        {
            if (IsParrySuccessful)
            {
                Debug.Log("パリィ成功");
            }
            else
            {
                Debug.Log("なんでここ来たん？ 失敗ですけれども");
                // ここに到達することは稀だが、例外を使わない場合の安全策。
                // (通常、成功時のみキャンセルされるため)
            }
        }
        else // タイムアウト
        {
            // 時間切れで、かつパリィが成功しなかった場合
            if (!IsParrySuccessful)
            {
                Debug.Log("❌ パリィ失敗：タイムアウト。");
            }
        }
        
        // 受付終了時の共通処理
        IsParryActive = false;
        parryCts.Dispose();
        parryCts = null;
    }

    /// <summary>
    /// 武器の当たり判定ロジックから呼び出され、パリィ成功を確定させるメソッド。
    /// </summary>
    public void NotifyParrySuccess()
    {
        if (IsParryActive)
        {
            IsParrySuccessful = true;
            // パリィ成功時の処理
            playerController.ChangeParryState();
            
            // 待ち時間キャンセルして終了処理移行
            parryCts?.Cancel();
        }
    }
}