using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateManager;

public class AttackArea : MonoBehaviour
{
    [SerializeField] private int AttackDamage;
    private Collider attackAreaCollider = null;

    // 攻撃した回数 (多段ヒット防止用)
    private int attackCount = 0;

    public void Start()
    {
        SetAttackArea();
    }
    public void SetAttackArea()
    {
        attackAreaCollider = GetComponent<Collider>();
        attackAreaCollider.enabled = false;
    }
    public void StartAttackHit()
    {
        attackCount = 0; 
        attackAreaCollider.enabled = true; 
    }
    public void EndAttackHit()
    {
        attackCount = 0;
        attackAreaCollider.enabled = false;
    }


    private void OnTriggerEnter (Collider other)
    {
        // 多段ヒット防止
        if(attackCount >= 1)
            return;

        switch(LayerMask.LayerToName(other.gameObject.layer))
        {
            case "PlayerHit":
                // ParryController
                PlayerParryController pParryController = other.gameObject.GetComponentInParent<PlayerParryController>();
                
                // パリィ成功時の処理
                if (pParryController != null)
                {   
                    if (pParryController.IsParryActive)
                    {
                        // パリィ成功判定
                        pParryController.NotifyParrySuccess();
                        ProcessParried(other.gameObject);
                        
                        return;
                    }
                    // ガード成功判定
                    else if (pParryController.IsGuarding)
                    {                        
                        ProcessGuardSuccess(other.gameObject);
                        //ProcessParried(other.gameObject); 
                        
                        return;
                    }
                }
                
                // ヒット判定 (パリィもガードもしていない場合)
                ProcessHit(other, "PlayerStatus", "Damagable");
                break;

            // プレイヤーが敵に攻撃した場合の処理
            case "EnemyHit":
                ProcessHit(other, "EnemyStatus", "Damagable");
                break;
        }
    }
    
    // パリィ・ガード成功時の処理
    private void ProcessParried(GameObject target)
    {
        // 敵のアニメーションを硬直させたり、武器の軌道をリセットしたりする処理をここに追加
        Debug.Log("攻撃がパリィ/ガードされました！");
        var controller = this.GetComponentInParent<YarikumaController>();
        controller.ChangeParryedState();
        EndAttackHit(); // コライダーを無効化して多段ヒット防止
    }

    // ガード成功時のプレイヤー側の処理
    private void ProcessGuardSuccess(GameObject target)
    {
        Debug.Log("ガード成功！");
        var controller = target.GetComponentInParent<PlayerController>();
        var status = GameManager.Instance.CurrentStatus;

        // スタミナを多めに消費
        status.m_stumina -= 15;
    }
    
    // ヒット時の処理
    private void ProcessHit(Collider other, string statusType, string damagableType)
    {
        attackCount++;

        if (statusType == "PlayerStatus")
        {
            // PlayerHit
            PlayerController controller = other.gameObject.GetComponentInParent<PlayerController>();
            PlayerStatus pStatus = GameManager.Instance.CurrentStatus;
            if (pStatus != null)
            {
                pStatus.m_hp -= AttackDamage;
            }

            // スタミナ少なめに消費
            if (!pStatus.GetStun)
                pStatus.m_stumina -= 10;
        }
        else if (statusType == "EnemyStatus")
        {
            // EnemyHit
            EnemyStatus eStatus = other.gameObject.GetComponent<EnemyStatus>();
            if (eStatus != null)
            {
                eStatus.m_hp -= AttackDamage;
            }
        }
        
        // インターフェイス呼び出し
        var damagetarget = (statusType == "PlayerStatus") 
            ? other.gameObject.GetComponent<Damagable>()
            : other.gameObject.GetComponent<Damagable>();
        
        if (damagetarget != null)
        {
            damagetarget.AddDamage(AttackDamage);
            // 攻撃終了
            EndAttackHit();
        }
    }
}