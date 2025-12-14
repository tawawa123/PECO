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
                // パリィウィンドウ中 パリィ成功
                playerParryController.NotifyParrySuccess();
            }
            else if (playerParryController.IsGuarding)
            {
                Debug.Log("ガード成功！");
            }
            else
            {
                Debug.Log("ヒット！ダメージを受ける。");
            }
        }
    }
}