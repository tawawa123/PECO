using UnityEngine;

public class WeaponCollisionHandler : MonoBehaviour
{
    [SerializeField]
    private LayerMask enemyWeaponLayer;
    
    private PlayerParryController playerParryController;

    void Start()
    {
        playerParryController = FindObjectOfType<PlayerParryController>(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        // 敵の武器との接触かを確認
        if (((1 << other.gameObject.layer) & enemyWeaponLayer) != 0)
        {
            // --- 判定ロジック ---

            if (playerParryController.IsParryActive)
            {
                // 1. パリィウィンドウ中 パリィ成功
                playerParryController.NotifyParrySuccess();
            }
            else if (playerParryController.IsGuarding)
            {
                // 2. パリィウィンドウは終わったが、ガード入力は継続している ガード成功
                Debug.Log("✅ ガード成功！");
                // ガード時のエフェクト、ノックバック、スタミナ消費処理などを実行
            }
            else
            {
                // 3. パリィウィンドウ外で、ガード入力もしていない ヒット
                Debug.Log("💥 ヒット！ダメージを受ける。");
            }
        }
    }
}