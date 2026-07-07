using UnityEngine;

/// <summary>
/// 攻撃を受ける側。命中時の処理(ダメージ/パリィ/ガード)を自身で解決する。
/// 当たり判定(Hitbox/AttackArea)は検出のみを担い、結果の意思決定は被弾側に委ねる。
/// </summary>
public interface IDamageable
{
    void TakeHit(in AttackData attack, Vector3 hitPoint);
}
