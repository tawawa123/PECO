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
            // ChangeStrategy(new DefaultControllerStrategy(this));
        }

        private void Update()
        {
            if (currentStrategy == null)
            {
                currentStrategy = new DefaultControllerStrategy(this);
                currentStrategy.OnEnter();
            }
            currentStrategy?.Tick();
        }

        public void ChangeStrategy(IPlayerControlStrategy next)
        {
            currentStrategy?.OnExit();
            currentStrategy = next;
            currentStrategy?.OnEnter();
        }


        public void Transform(int id)
        {
            // 変身先ごとに操作Strategyとm_transformフラグを整合させる。
            // 未対応のidでフラグだけtrueになる不整合を避けるため、
            // 実際にStrategyを適用する分岐でのみフラグを設定する。
            switch (id)
            {
                case 0: // 変身解除（デフォルトに戻る）
                    GameManager.Instance.CurrentStatus.m_transform = false;
                    ChangeStrategy(new DefaultControllerStrategy(this));
                    break;
                // case 1000: // クマへ変身（未実装）
                //     GameManager.Instance.CurrentStatus.m_transform = true;
                //     ChangeStrategy(new ChangeKumaControllerStrategy(this));
                //     break;
                case 1001: // 兵士へ変身
                    GameManager.Instance.CurrentStatus.m_transform = true;
                    ChangeStrategy(new ChangeHeisiControllerStrategy(this));
                    break;
                default:
                    Debug.LogWarning($"PlayerController.Transform: 未対応のitemId={id} のため操作方式を変更しません");
                    break;
            }
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
