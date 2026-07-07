using System.Collections.Generic;
using UnityEngine;
using StateManager;

/// <summary>
/// 武器などに付ける当たり判定(Hitbox)。
/// 有効窓の間に重なった対象を検出し、被弾側(IDamageable)へ命中を通知するだけを担う。
/// ダメージ量やパリィ/ガードの判断は被弾側が行う(責務分離)。
/// </summary>
public class AttackArea : MonoBehaviour
{
    // AttackDataを渡さない経路(主に敵)のフォールバック基礎攻撃力
    [SerializeField] private int AttackDamage;
    // 命中エフェクト
    [SerializeField] private GameObject Hit;

    private Collider attackAreaCollider = null;

    // 1スイングで同じ対象に多段ヒットしないための記録(窓を開くたびクリア)
    private readonly HashSet<Collider> hitTargets = new HashSet<Collider>();

    // 現在の攻撃データ
    private AttackData currentAttack;

    public void Start()
    {
        SetAttackArea();
    }

    public void SetAttackArea()
    {
        attackAreaCollider = GetComponent<Collider>();
        attackAreaCollider.enabled = false;
    }

    /// <summary>
    /// 明示的な攻撃データで当たり判定窓を開く(プレイヤーの各攻撃など)。
    /// </summary>
    public void Begin(AttackData data)
    {
        currentAttack = data;
        if (currentAttack.hitEffect == null) currentAttack.hitEffect = Hit;
        if (currentAttack.attacker == null) currentAttack.attacker = ResolveAttacker();

        hitTargets.Clear();
        if (attackAreaCollider != null) attackAreaCollider.enabled = true;
    }

    /// <summary>
    /// フォールバック用。SerializeFieldのAttackDamageで窓を開く(AttackData未対応の経路)。
    /// </summary>
    public void StartAttackHit()
    {
        Begin(new AttackData
        {
            baseDamage = AttackDamage,
            motionMultiplier = 1f,
            hitEffect = Hit,
        });
    }

    public void EndAttackHit()
    {
        hitTargets.Clear();
        if (attackAreaCollider != null) attackAreaCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 同一対象への多段ヒット防止(1スイングにつき各対象1回)。複数対象へは当たる。
        if (hitTargets.Contains(other)) return;

        // 有効な被弾レイヤーのみ処理する
        string layer = LayerMask.LayerToName(other.gameObject.layer);
        if (layer != "PlayerHit" && layer != "EnemyHit") return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;

        hitTargets.Add(other);
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        target.TakeHit(currentAttack, hitPoint);
    }

    /// <summary>この当たり判定を所有するキャラクター(パリィ反撃などで使用)。</summary>
    private GameObject ResolveAttacker()
    {
        var enemy = GetComponentInParent<EnemyController>();
        if (enemy != null) return enemy.gameObject;
        var player = GetComponentInParent<PlayerController>();
        return player != null ? player.gameObject : null;
    }
}
