using UnityEngine;

/// <summary>
/// 1回の攻撃の性質を表すデータ。
/// 攻撃側が当たり判定を有効化する前に設定し、命中時に被弾側(IDamageable)へ渡す。
///
/// モーション係数の供給元は当面コード(案A)だが、将来 ScriptableObject や
/// Animation Event へ差し替えられるよう、この構造体を共通の受け皿とする。
/// </summary>
public struct AttackData
{
    public int baseDamage;          // 攻撃側の基礎攻撃力(PlayerStatus/EnemyStatus由来)
    public float motionMultiplier;  // モーションごとの係数
    public GameObject hitEffect;    // 命中エフェクト(任意)
    public GameObject attacker;     // 攻撃元(パリィ反撃などで使用)

    /// <summary>実際に与えるダメージ量。</summary>
    public int Damage => Mathf.Max(0, Mathf.RoundToInt(baseDamage * motionMultiplier));
}
