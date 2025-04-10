using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackArea : MonoBehaviour
{
    [SerializeField] private int AttackDamage;
    private Collider attackAreaCollider = null;

    // 攻撃したした回数　多段ヒット防止用
    int attackCount = 0;

    public void Start()
    {
        SetAttackArea();
    }

    // 武器の付け替えなどのときに、コライダーを再取得する
    public void SetAttackArea()
    {
        attackAreaCollider = this.GetComponent<Collider>();
        attackAreaCollider.enabled = false;
    }

    // 攻撃モーション時に受け取ってコライダを有効にする
    public void StartAttackHit()
    {
        attackCount++;
        attackAreaCollider.enabled = true;        
    }

    // アニメーションイベントのEndAttackHitを受け取ってコライダを無効にする
    public void EndAttackHit()
    {
        attackCount--;
        attackAreaCollider.enabled = false;
    }

    private void OnTriggerEnter (Collider other)
    {
        switch(LayerMask.LayerToName(other.gameObject.layer))
        {
            case "PlayerHit":
                PlayerStatus pStatus = other.gameObject.GetComponent<PlayerStatus>();
                pStatus.m_hp -= AttackDamage;
                break;

            case "EnemyHit":
                EnemyStatus eStatus = other.gameObject.GetComponent<EnemyStatus>();
                eStatus.m_hp -= AttackDamage;
                break;
        }

        EndAttackHit();
    }
}
