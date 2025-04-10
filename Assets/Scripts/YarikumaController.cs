using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StateManager
{
    using StateBase = StateMachine<YarikumaController>.StateBase;

    public class YarikumaController : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private float warningDistance = 20.0f;
        [SerializeField] private float viewingDistance = 30.0f;
        [SerializeField] private float viewingAngle = 45.0f;

        private bool findPlayer = false;

        private enum StateType
        {
            Idle,
            Round,
            Vigilance,
            Chase,
            Battle,
            Attack,
            Damage,
            Death,
        }

        // 自作メソッドの呼び出し
        private EnemyStatus estatus;
        private AttackArea AA;
        private AwaitableAnimatorState animationState;
        private StateMachine<YarikumaController> stateMachine;
        private Rigidbody rb; //一緒に入れとく

        void Start()
        {
            estatus = this.GetComponent<EnemyStatus>();
            AA = this.GetComponentInChildren<AttackArea>();
            animationState = GetComponent<AwaitableAnimatorState>();
            rb = GetComponent<Rigidbody>();

            stateMachine = new StateMachine<YarikumaController>(this);
            stateMachine.Add<StateIdle>((int) StateType.Idle);
            stateMachine.Add<StateRound>((int) StateType.Round);
            stateMachine.Add<StateVigilance>((int) StateType.Vigilance);
            stateMachine.Add<StateChase>((int) StateType.Chase);
            stateMachine.Add<StateBattle>((int) StateType.Battle);
            stateMachine.Add<StateAttack>((int) StateType.Attack);
            stateMachine.Add<StateDamage>((int) StateType.Damage);
            stateMachine.Add<StateDeath>((int) StateType.Death);

            stateMachine.OnStart((int) StateType.Idle);
        }

        // Update is called once per frame
        void Update()
        {
            stateMachine.OnUpdate();
            if(estatus.Hp <= 0)
                stateMachine.ChangeState((int) StateType.Death);
        }

        // Idle状態を定義するメソッド
        // 基本使わないけど、巡回中に立ち止まったりするときにIdleステートに入るかもなので一応定義
        private class StateIdle : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Idle");
            }

            public override void OnUpdate()
            {
                Owner.AA.StartAttackHit();
                StateMachine.ChangeState((int) StateType.Round);

                if (Owner.findPlayer){
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Idle");
            }
        }


        // プレイヤーが周囲を巡回する動きを定義するメソッド
        private class StateRound : StateBase
        {
            Vector3 posDelta;
            float target_angle;

            public override void OnStart()
            {
                posDelta = Vector3.zero;
                target_angle = 0;
                Debug.Log("start Round");
            }

            public override void OnUpdate()
            {
                //navmeshによる巡回処理

                posDelta = Owner.player.transform.position - Owner.transform.position;
                
                // プレイヤーがエネミーの視界に入っているかの判定
                if(Mathf.Abs(posDelta.magnitude) <= Owner.viewingDistance)
                {
                    target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                    if (target_angle < Owner.viewingAngle) //target_angleがangleに収まっているかどうか
                    {
                        Debug.DrawRay(Owner.transform.position, posDelta, Color.red, 5);
                        if(Physics.Raycast(Owner.transform.position, posDelta, out RaycastHit hit)) //Rayを使用してtargetに当たっているか判別
                        {
                            if (hit.collider.gameObject.tag == "Player")
                            {
                                if(Mathf.Abs(posDelta.magnitude) <= Owner.warningDistance) //視界内の危険距離内に入っていればチェイス開始
                                    StateMachine.ChangeState((int) StateType.Chase);
                                else
                                    StateMachine.ChangeState((int) StateType.Vigilance); //視界内なら警戒開始
                            }
                        }
                    }
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Round");
            }
        }


        // エネミーの警戒処理　警戒状態に入ってからプレイヤーを発見する、もしくは見失う処理
        private class StateVigilance : StateBase
        {
            Vector3 posDelta;
            float target_angle;

            public override void OnStart()
            {
                posDelta = Vector3.zero;
                target_angle = 0;
                Debug.Log("start Vigilance");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;
                target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                //エネミーの視界から抜けた時の処理
                if(Mathf.Abs(posDelta.magnitude) >= Owner.viewingDistance)
                {
                    // 5秒間くらい処理を回して、なお視界外ならRoundステートに戻る
                    StateMachine.ChangeState((int) StateType.Round);
                }

                if (target_angle < Owner.viewingAngle) //target_angleがangleに収まっているかどうか
                {
                    Debug.DrawRay(Owner.transform.position, posDelta, Color.red, 5);
                    if(Physics.Raycast(Owner.transform.position, posDelta, out RaycastHit hit)) //Rayを使用してtargetに当たっているか判別
                    {
                        if (hit.collider.gameObject.tag == "Player")
                        {
                            if(Mathf.Abs(posDelta.magnitude) <= Owner.warningDistance) //危険距離内に入ったらチェイス開始
                                StateMachine.ChangeState((int) StateType.Chase);
                        }
                    }
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Vigilance");
            }
        }


        // プレイヤーを発見した時のチェイス処理を行うメソッド
        private class StateChase : StateBase
        {
            Vector3 posDelta;
            float target_angle;

            public override void OnStart()
            {
                posDelta = Vector3.zero;
                target_angle = 0;
                Debug.Log("start Chase");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;
                target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                Debug.Log("追跡中");
                // navmeshでプレイヤーの座標まで移動する

                // プレイヤーとの距離が一定以下になればBattleステートへ移行
                StateMachine.ChangeState((int) StateType.Battle);

                // エネミーの危険距離外にプレイヤーが抜けたらVigilanceステートへ移行
                if (Mathf.Abs(posDelta.magnitude) >= Owner.warningDistance)
                    StateMachine.ChangeState((int) StateType.Vigilance);
            }

            public override void OnEnd()
            {
                Debug.Log("end Chase");
            }
        }


        // 戦闘状態の処理メソッド
        private class StateBattle : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Battle");
            }

            public override void OnUpdate()
            {
                StateMachine.ChangeState((int) StateType.Vigilance);
                Owner.AA.StartAttackHit();
            }

            public override void OnEnd()
            {
                Debug.Log("end Battle");
            }
        }


        // 攻撃判定用メソッド
        private class StateAttack : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Attack");
            }

            public override void OnUpdate()
            {
                Owner.AA.StartAttackHit();

                // 攻撃アニメーションが終了したらIdleに遷移
                // if(Owner.animationState.AnimtionFinish("Jab") >= 1f)
                //     Owner.AA.EndAttackHit();
                //     StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end Attack");
            }
        }


        // ダメージが発生した時の体力管理やアニメーション再生用のメソッド
        private class StateDamage : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Damage");
                Debug.Log(Owner.estatus.Hp);
            }

            public override void OnUpdate()
            {
                StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end Damage");
            }
        }


        // 死亡判定用メソッド
        private class StateDeath : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Death");
            }

            public override void OnUpdate()
            {
                Debug.Log("体力が0になりました");
                Destroy(Owner.gameObject);
            }

            public override void OnEnd()
            {
                Debug.Log("end Death");
            }
        }
    }
}
