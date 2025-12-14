using UnityEngine;

public interface IPlayerContext
{
    // この辺は絶対共有する変数
    Transform tf { get; } // transform
    Rigidbody rb { get; } // rigidBody
    PlayerStatus playerStatus { get; } // status
    AwaitableAnimatorState animationState { get; } // animator
    OverrideDamageLayer damageLayer { get; }
    AttackArea AA { get; } // attack area
    bool GetStealthAttackFlag(); // acceser in stealth attack 
    GameObject GetStealthAttackTarget(); // acceser in stealth attack targer

    // この辺はインスペクターから参照が欲しいので
    GameObject stealthAttackEffect { get; } // effect
    MeshRenderer weapon { get; } // waepon model

    // この辺はまとめるかどうか悩み
    PlayerLockon playerLo { get; } //ロックオンカメラ制御
}