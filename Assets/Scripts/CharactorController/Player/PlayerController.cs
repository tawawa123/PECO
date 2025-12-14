using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using GameUI;

namespace StateManager 
{
    public class PlayerController : MonoBehaviour, IPlayerContext, Damagable
    {
        public event Action OnParrySuccess;

        // 現在稼働中のStrategy
        private IPlayerControlStrategy currentStrategy;

        private Rigidbody rigid;
        private AwaitableAnimatorState animator;
        private OverrideDamageLayer damLayer;
        private AttackArea attackArea;
        private PlayerLockon lockon;

        [SerializeField] private GameObject stealthAttack;
        [SerializeField] private MeshRenderer arm;

        private bool canStealthAttack;
        private GameObject stealthAttackTarget;

        public int num = 0;

        void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            animator = GetComponent<AwaitableAnimatorState>();
            damLayer = GetComponent<OverrideDamageLayer>();
            attackArea = GetComponentInChildren<AttackArea>();
            lockon = GetComponent<PlayerLockon>();
        }

        public Transform tf => this.transform;
        public Rigidbody rb => rigid;
        public AwaitableAnimatorState animationState => animator;
        public OverrideDamageLayer damageLayer => damLayer;
        public AttackArea AA => attackArea;
        public PlayerLockon playerLo => lockon;
        public GameObject stealthAttackEffect => stealthAttack;
        public MeshRenderer weapon => arm;

        // ステルスアタック関係ののアクセサ
        public bool GetStealthAttackFlag()
        {
            return this.canStealthAttack;
        }
        public GameObject GetStealthAttackTarget()
        {
            return this.stealthAttackTarget;
        }


        public void Start()
        {
            ChangeStrategy(new DefaultControllerStrategy(this));
        }

        private void Update()
        {
            if(num != 0)
            {
                Transform(num);
                num = 0;
            }
            currentStrategy?.Tick();
        }

        public void ChangeStrategy(IPlayerControlStrategy next)
        {
            Debug.Log(next);
            currentStrategy?.OnExit();
            currentStrategy = next;
            currentStrategy?.OnEnter();


            rigid = GetComponent<Rigidbody>();
            animator = GetComponent<AwaitableAnimatorState>();
            damLayer = GetComponent<OverrideDamageLayer>();
            attackArea = GetComponentInChildren<AttackArea>();
            lockon = GetComponent<PlayerLockon>();
        }


        public void Transform(int id)
        {
            Debug.Log(id);
            if(id == 0)
                ChangeStrategy(new DefaultControllerStrategy(this));
            //if(id == 1000)
            //   ChangeStrategy(new ChangeKumaControllerStrategy(this));
            if(id == 1001)
                ChangeStrategy(new ChangeHeisiControllerStrategy(this));
        }


        public void AddDamage(int damage){
            // playerStatus.m_hp -= damage;
            if(!GameManager.Instance.CurrentStatus.GetStun)
                currentStrategy?.AddDamage(damage);
        }

        // 外部から強制的にステートを変更させるための補助関数たち
        public void ChangeStunState()
        {
            currentStrategy?.ChangeStun();
        }

        public void ChangeParryState()
        {
            currentStrategy?.ChangeParry();
        }

        public void CanStealthAttack(bool stealthAttackFlag)
        {
            // ステルスアタック用のフラグ
            this.canStealthAttack = stealthAttackFlag;
        }

        public void SetTarget(GameObject currentTarget)
        {
            // ステルスアタックのターゲット設定
            this.stealthAttackTarget = currentTarget;
        }
    }
}
