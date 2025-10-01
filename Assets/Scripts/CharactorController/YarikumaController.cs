using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace StateManager
{
    using StateBase = StateMachine<YarikumaController>.StateBase;

    public class YarikumaController : MonoBehaviour, Damagable
    {
        [SerializeField] private GameObject player;
        private bool findPlayer = false;
        private float vigilancePoint = 0;

        private enum StateType
        {
            Idle,
            Round,
            Vigilance,
            Chase,
            Battle,
            Attack,
            Damage,
            Backstabed,
            Death,
        }

        // メソッド呼び出し
        private EnemyStatus enemyStatus; //エネミーの登録ステータス
        private AttackArea AA; //攻撃判定
        private AwaitableAnimatorState animationState; //アニメーション遷移管理
        private Destination destination; //巡回先座標登録
        private StateMachine<YarikumaController> stateMachine; //ステート遷移管理
        private Rigidbody rb;
        private NavMeshAgent navAgent;
        

        void Start()
        {
            enemyStatus = this.GetComponent<EnemyStatus>();
            AA = this.GetComponentInChildren<AttackArea>();
            animationState = GetComponent<AwaitableAnimatorState>();
            destination = GetComponent<Destination>();
            rb = GetComponent<Rigidbody>();
            navAgent = GetComponent<NavMeshAgent>();

            stateMachine = new StateMachine<YarikumaController>(this);
            stateMachine.Add<StateIdle>((int) StateType.Idle);
            stateMachine.Add<StateRound>((int) StateType.Round);
            stateMachine.Add<StateVigilance>((int) StateType.Vigilance);
            stateMachine.Add<StateChase>((int) StateType.Chase);
            stateMachine.Add<StateBattle>((int) StateType.Battle);
            stateMachine.Add<StateAttack>((int) StateType.Attack);
            stateMachine.Add<StateDamage>((int) StateType.Damage);
            stateMachine.Add<StateBackstabed>((int) StateType.Backstabed);
            stateMachine.Add<StateDeath>((int) StateType.Death);

            stateMachine.OnStart((int) StateType.Idle);

            AA = this.GetComponentInChildren<AttackArea>();
            AA.SetAttackArea();
        }

        // Update is called once per frame
        void Update()
        {
            stateMachine.OnUpdate();
            if(enemyStatus.GetHp <= 0){
                int layer = LayerMask.NameToLayer("Dead");
                this.gameObject.layer = layer;
                stateMachine.ChangeState((int) StateType.Death);
            }

            if(enemyStatus.GetBackstabed){
                animationState.SetState("Backstabed", true);
                stateMachine.ChangeState((int) StateType.Backstabed);
            }
        }

        // Idle状態を定義するメソッド
        // 基本使わないけど、巡回中に立ち止まったりするときにIdleステートに入るかもなので一応定義
        private class StateIdle : StateBase
        {
            public override void OnStart()
            {
                Owner.AA.SetAttackArea();
                Debug.Log("start Idle");
            }

            public override void OnUpdate()
            {
                Owner.animationState.SetState("walk", true);
                StateMachine.ChangeState((int) StateType.Round);

                if (Owner.findPlayer){
                    Owner.animationState.SetState("Conbat", true);
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
                Owner.navAgent.SetDestination(Owner.destination.GetDestination());

                Debug.Log("start Round");
            }

            public override void OnUpdate()
            {
                Owner.vigilancePoint = Mathf.Clamp((Owner.vigilancePoint - 0.05f), 0f, 100f);

                //navmeshによる巡回処理
                if(Vector3.Distance(Owner.transform.position, Owner.destination.GetDestination()) < 1.5f)
                {
                    Owner.destination.CreateDestination();
                    Owner.navAgent.SetDestination(Owner.destination.GetDestination());
                }

                posDelta = Owner.player.transform.position - Owner.transform.position;
                
                // プレイヤーがエネミーの視界に入っているかの判定
                if(Mathf.Abs(posDelta.magnitude) <= Owner.enemyStatus.GetViewRange)
                {
                    target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                    if (target_angle < Owner.enemyStatus.GetViewAngle) //target_angleがangleに収まっているかどうか
                    {
                        Debug.DrawRay(Owner.transform.position, posDelta, Color.red, 5);
                        if(Physics.Raycast(Owner.transform.position, posDelta, out RaycastHit hit)) //Rayを使用してtargetに当たっているか判別
                        {
                            if (hit.collider.gameObject.tag == "Player")
                            {
                                if(Mathf.Abs(posDelta.magnitude) <= Owner.enemyStatus.GetWarningRange)
                                {
                                    Owner.animationState.SetState("Run", true);
                                    //視界内の危険距離内に入っていればチェイス開始
                                    StateMachine.ChangeState((int) StateType.Chase);
                                }
                                else{
                                    Owner.animationState.SetState("Idle", true);
                                    StateMachine.ChangeState((int) StateType.Vigilance); //視界内なら警戒開始
                                }
                            }
                        }
                    }
                }

                // ダメージ処理が起きたらここでストップ
                if(Owner.vigilancePoint >= 100f)
                    StateMachine.ChangeState((int) StateType.Battle);
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

                Owner.navAgent.SetDestination(Owner.transform.position);
                Debug.Log("start Vigilance");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;
                target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                //エネミーの視界から抜けた時の処理
                if(Mathf.Abs(posDelta.magnitude) >= Owner.enemyStatus.GetViewRange)
                {
                    Owner.animationState.SetState("walk", true);
                    // 5秒間くらい処理を回して、なお視界外ならRoundステートに戻る
                    StateMachine.ChangeState((int) StateType.Round);
                }

                if (target_angle < Owner.enemyStatus.GetViewAngle) //target_angleがangleに収まっているかどうか
                {
                    Debug.DrawRay(Owner.transform.position, posDelta, Color.red, 5);
                    if(Physics.Raycast(Owner.transform.position, posDelta, out RaycastHit hit)) //Rayを使用してtargetに当たっているか判別
                    {
                        if (hit.collider.gameObject.tag == "Player")
                        {
                            PlusVigilancePoint();
                        }
                    }
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Vigilance");
            }

            private void PlusVigilancePoint()
            {
                float MAX = 100;
                float MIN = 0;

                if(Mathf.Abs(posDelta.magnitude) <= Owner.enemyStatus.GetWarningRange) //危険距離内
                    Owner.vigilancePoint = MAX;
                
                var inverseProportion = (1 - Mathf.InverseLerp(1, Owner.enemyStatus.GetViewRange, Mathf.Abs(posDelta.magnitude)));
                Owner.vigilancePoint += Mathf.Lerp(0.05f, 0.1f, inverseProportion);
                
                // 警戒度100以上でチェイス開始
                if(Mathf.Clamp(Owner.vigilancePoint, MIN, MAX) >= MAX){
                    Owner.animationState.SetState("Run", true);
                    StateMachine.ChangeState((int) StateType.Chase);
                }
            }
        }


        // プレイヤーを発見した時のチェイス処理を行うメソッド
        private class StateChase : StateBase
        {
            Vector3 posDelta;
            //float target_angle;

            public override void OnStart()
            {
                posDelta = Vector3.zero;
                //target_angle = 0;
                Owner.navAgent.speed = 4;
                Debug.Log("start Chase");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;
                //target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                Debug.Log("追跡中");
                // navmeshでプレイヤーの座標まで移動する
                Owner.navAgent.SetDestination(Owner.player.transform.position);

                // プレイヤーとの距離が一定以下になればBattleステートへ移行
                if (Mathf.Abs(posDelta.magnitude) <= 5.0f){
                    Owner.navAgent.ResetPath();
                    Owner.animationState.SetState("Combat", true);
                    StateMachine.ChangeState((int) StateType.Battle);
                }

                // エネミーの視界外にプレイヤーが抜けたらVigilanceステートへ移行
                if (Mathf.Abs(posDelta.magnitude) >= Owner.enemyStatus.GetViewRange){
                    Owner.vigilancePoint -= 5.0f;
                    Owner.animationState.SetState("Idle", true);
                    StateMachine.ChangeState((int) StateType.Vigilance);
                }
            }

            public override void OnEnd()
            {
                Owner.navAgent.speed = 2;
                Debug.Log("end Chase");
            }
        }


        // 戦闘状態の処理メソッド
        private class StateBattle : StateBase
        {
            Vector3 posDelta;
            Vector3 destination;
            float targetAngle;

            public override void OnStart()
            {
                posDelta = Vector3.zero;

                // プレイヤーの周囲を動くための目的地設定
                Transform p = Owner.player.transform;
                float centerAngle = Mathf.Atan2(p.forward.z, p.forward.x) * Mathf.Rad2Deg;
                float randomOffset = Random.Range(-60f, 60f); // ±60°の範囲
                targetAngle = centerAngle + randomOffset;

                Owner.navAgent.angularSpeed = 0;

                Debug.Log("start Battle");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;

                if(Mathf.Abs(posDelta.magnitude) >= 15f){
                    Owner.animationState.SetState("Chase", true);
                    Owner.navAgent.angularSpeed = 120;
                    StateMachine.ChangeState((int) StateType.Vigilance);
                }

                Vector3 destination = GetPointOnArc(Owner.player.transform.position, 3.0f, targetAngle);
                Owner.navAgent.SetDestination(destination);

                if(Mathf.Abs((Owner.transform.position - destination).magnitude) <= 0.5f){
                    Owner.animationState.SetState("Attack", true);
                    StateMachine.ChangeState((int) StateType.Attack);
                }

                // プレイヤーの位置とこの敵の位置から角度を求める。
                var qrot = Quaternion.LookRotation(Owner.player.transform.position - Owner.transform.position);
                Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, qrot, Time.time * 2);
            }

            public override void OnEnd()
            {
                Debug.Log("end Battle");
            }

            // プレイヤーを中心にした円弧上の座標を取得
            public Vector3 GetPointOnArc(Vector3 playerPos, float radius, float angleDeg)
            {
                // 角度をラジアンに変換
                float rad = angleDeg * Mathf.Deg2Rad;
                // 水平方向 (XZ平面) のみ
                float x = playerPos.x + radius * Mathf.Cos(rad);
                float z = playerPos.z + radius * Mathf.Sin(rad);

                return new Vector3(x, playerPos.y, z);
            }
        }


        // 攻撃判定用メソッド
        private class StateAttack : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Attack");
                Owner.AA.StartAttackHit();
            }

            public override void OnUpdate()
            {
                // 攻撃アニメーションが終了したらButtleに遷移
                if(Owner.animationState.AnimtionFinish("Attack") >= 1f){
                    Owner.AA.EndAttackHit();
                    Owner.animationState.SetState("Combat", true);
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Attack");
            }
        }


        // ダメージ処理用インターフェイス
        public void AddDamage(int damage){
            stateMachine.ChangeState((int) StateType.Damage);
        }
        // ダメージが発生した時の体力管理やアニメーション再生用のメソッド
        private class StateDamage : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Damage");
                Debug.Log(Owner.enemyStatus.GetHp);
                Owner.vigilancePoint = 100f;
                Owner.animationState.SetState("Damage", true);
            }

            public override void OnUpdate()
            {
                if(Owner.animationState.AnimtionFinish("Damage") >= 1f){
                    Owner.animationState.SetState("Combat", true);
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Damage");
            }
        }


        private class StateBackstabed : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Backstabed");
                Debug.Log(Owner.enemyStatus.GetHp);

                Owner.vigilancePoint = 100f;
                Owner.animationState.SetState("Backstabed", true);

                Owner.navAgent.speed = 0;
            }

            public override void OnUpdate()
            {
                if(Owner.animationState.AnimtionFinish("Backstabed") >= 1f){
                    Owner.animationState.SetState("Combat", true);
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                Owner.enemyStatus.m_backstabed = false;
                Owner.navAgent.speed = 2;
                Debug.Log("end Backstabed");
            }
        }


        // 死亡判定用メソッド
        private class StateDeath : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Death");
                Owner.animationState.SetState("Death", true);
            }

            public override void OnUpdate()
            {
                Debug.Log("体力が0になりました");
                if(Owner.animationState.AnimtionFinish("Death") >= 1f){
                    Destroy(Owner.gameObject);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Death");
            }
        }
    }
}
