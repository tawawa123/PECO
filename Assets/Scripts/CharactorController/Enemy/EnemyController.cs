using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using GameUI;

namespace StateManager 
{
    public class EnemyController : MonoBehaviour, IEnemyContext, Damagable, StealthAttackable, IDamageable
    {
        // 現在稼働中のStrategy
        private IEnemyControllStrategy currentStrategy;

        // インスペクターから取得するプログラム群
        private Rigidbody rigid;
        private AwaitableAnimatorState animationState;
        private OverrideDamageLayer damLayer;
        private AttackArea attackArea;
        private Destination destination;
        private UnityEngine.AI.NavMeshAgent navAgent;
        private EnemyStatus enemyStatus;

        void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            animationState = GetComponent<AwaitableAnimatorState>();
            damLayer = GetComponent<OverrideDamageLayer>();
            attackArea = GetComponentInChildren<AttackArea>();
            destination = GetComponent<Destination>();
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            enemyStatus = GetComponent<EnemyStatus>();
        }


        // インターフェースIEnemyControllStrategyに登録されている変数に、実体を代入する
        // 各ストラテジーはこのプログラムの実体を取得することでrb等のインスペクター情報にアクセスする
        public Transform tf => this.transform;
        public Rigidbody rb => rigid;
        public AwaitableAnimatorState animator => animationState;
        public OverrideDamageLayer damageLayer => damLayer;
        public AttackArea AA => attackArea;
        public Destination des => destination;
        public UnityEngine.AI.NavMeshAgent nav => navAgent;
        public EnemyStatus estatus => enemyStatus;

        // どのエネミーのストラテジーを割り当てるかを選択
        public enum EnemyName
        {
            Yarikuma,
            Heisityou,
        }

        // インスペクターに表示する変数を宣言
        [SerializeField] private EnemyName enemyName;


        public void Start()
        {
            switch (enemyName)
            {
                case EnemyName.Yarikuma:
                    // やりくま
                    ChangeStrategy(new YarikumaControllerStrategy(this));
                    break;

                case EnemyName.Heisityou:
                    // 兵士長(中ボス)
                    ChangeStrategy(new HeishityouControllerStrategy(this));
                    break;
            }
        }

        public void Update()
        {
            if (currentStrategy == null)
            {
                currentStrategy = new YarikumaControllerStrategy(this);
                currentStrategy.OnEnter();
            }
            currentStrategy?.Tick();
        }

        // エネミーを操作するストラテジーを遷移させる
        // 現在は実装していないが、第一形態、第二形態のあるボスに使えるかも
        public void ChangeStrategy(IEnemyControllStrategy next)
        {
            currentStrategy?.OnExit();
            currentStrategy = next;
            currentStrategy?.OnEnter();
        }


        public void AddDamage(int damage){
            // playerStatus.m_hp -= damage;
            if(!GameManager.Instance.CurrentStatus.GetStun)
                currentStrategy?.AddDamage(damage);
        }

        // 攻撃を受けた時の解決(被弾)。旧AttackAreaのEnemyHit分岐を移植。
        public void TakeHit(in AttackData attack, Vector3 hitPoint)
        {
            if (enemyStatus != null)
                enemyStatus.m_hp -= attack.Damage;

            if (attack.hitEffect != null)
                Instantiate(attack.hitEffect, transform.position + new Vector3(0, 1, 0), Quaternion.identity);

            AddDamage(attack.Damage); // ひるみ(strategy.AddDamage)へ
        }

        // 外部から強制的にステートを変更させるための補助関数たち
        // パリィされたときの体勢崩し
        public void ChangeParryedState()
        {
            currentStrategy?.ChangeParryed();
        }

        // 特殊攻撃を受けるための判定
        public void HaveStealthAttack()
        {
            currentStrategy?.HaveStealthAttack();
        }
    }
}