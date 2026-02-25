using UnityEngine;
using UnityEngine.AI;

public interface IEnemyContext
{
    // この辺は絶対共有する変数
    Transform tf { get; } // transform
    Rigidbody rb { get; } // rigidBody
    AwaitableAnimatorState animator { get; } // animator
    OverrideDamageLayer damageLayer { get; }
    AttackArea AA { get; } // attack area
    Destination des { get; } // destination
    NavMeshAgent nav { get; } // navmesh
    EnemyStatus estatus { get; } // enemy status
}