using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;

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
                Owner.animationState.SetState("Idle", true);
                Owner.AA.SetAttackArea();
                Debug.Log("start Idle");
            }

            public override void OnUpdate()
            {
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

            public override void OnStart()
            {
                Owner.animationState.SetState("Walk", true);
                posDelta = Vector3.zero;
                Owner.navAgent.SetDestination(Owner.destination.GetDestination());

                Debug.Log("start Round");
            }

            public override void OnUpdate()
            {
                Owner.enemyStatus.m_vigilancePoint = Mathf.Clamp((Owner.enemyStatus.m_vigilancePoint - 0.05f), 0f, 100f);

                //navmeshによる巡回処理
                if(Vector3.Distance(Owner.transform.position, Owner.destination.GetDestination()) < 1.5f)
                {
                    Owner.destination.CreateDestination();
                    Owner.navAgent.SetDestination(Owner.destination.GetDestination());
                }

                posDelta = Owner.player.transform.position - Owner.transform.position;
                float distance = posDelta.magnitude;

                // 1. 視界範囲外なら終了
                if (distance > Owner.enemyStatus.GetViewRange)
                    return;

                // 2. 視界角度外なら終了
                float targetAngle = Vector3.Angle(Owner.transform.forward, posDelta);
                if (targetAngle >= Owner.enemyStatus.GetViewAngle)
                    return;

                // 3. Raycastでプレイヤーに遮蔽物があるなら終了
                Vector3 eyePosition = Owner.transform.position + Vector3.up * 1.5f;
                Vector3 direction = posDelta.normalized;

                if (!Physics.Raycast(eyePosition, direction, out RaycastHit hit, distance))
                    return;

                if (!hit.collider.CompareTag("Player"))
                    return;


                // --- 視界にプレイヤーが見えている際の処理 ---
                Debug.DrawRay(eyePosition, direction * distance, Color.red, 0.1f);

                // 4. 危険距離の判定
                if (distance <= Owner.enemyStatus.GetWarningRange)
                {
                    Owner.enemyStatus.m_vigilancePoint = 100f;
                    StateMachine.ChangeState((int)StateType.Chase);
                }
                else
                {
                    StateMachine.ChangeState((int)StateType.Vigilance);
                }

                Debug.Log(Owner.enemyStatus.m_vigilancePoint);
                // ダメージ処理が起きたらここでストップ
                if(Owner.enemyStatus.m_vigilancePoint >= 100f)
                    StateMachine.ChangeState((int) StateType.Battle);
            }

            public override void OnEnd()
            {
                Debug.Log("end Round");
            }
        }


        // vigilance
        private class StateVigilance : StateBase
        {
            Vector3 posDelta;
            float timer = 0;
            CancellationTokenSource cts;

            public override void OnStart()
            {
                Owner.animationState.SetState("Search", true);
                Owner.navAgent.SetDestination(Owner.transform.position);

                Debug.Log("start Vigilance");

                // 警戒処理の開始
                cts = new CancellationTokenSource();
                VigilanceLoopAsync(cts.Token).Forget();
            }

            /// <summary>
            /// 警戒状態の監視ループ（非同期）
            /// </summary>
            private async UniTaskVoid VigilanceLoopAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    // 視界にプレイヤーがいなければ、タイマーを進めて次のフレームへ
                    if (!IsPlayerVisible(out float distance))
                    {
                        HandleInvisible(Time.deltaTime);
                        await UniTask.Yield(token);
                        continue;
                    }

                    // 見えている → タイマーリセット＋警戒ポイント加算
                    timer = 0f;
                    PlusVigilancePoint(distance);

                    await UniTask.Yield(token);
                }
            }

            /// <summary>
            /// プレイヤーが視界内にいるかを判定
            /// </summary>
            private bool IsPlayerVisible(out float distance)
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;
                distance = posDelta.magnitude;

                // 視界範囲外
                if (distance >= Owner.enemyStatus.GetViewRange)
                    return false;

                // 視野角外
                float angle = Vector3.Angle(Owner.transform.forward, posDelta);
                if (angle >= Owner.enemyStatus.GetViewAngle)
                    return false;

                // Rayがヒットしない
                if (!Physics.Raycast(Owner.transform.position, posDelta, out RaycastHit hit, Owner.enemyStatus.GetViewRange))
                    return false;

                // ヒットしたのがプレイヤーでなければ除外
                if (!hit.collider.CompareTag("Player"))
                    return false;

                return true;
            }

            /// <summary>
            /// 見失っている間のカウント処理
            /// </summary>
            private void HandleInvisible(float deltaTime)
            {
                timer += deltaTime;

                if (timer >= 5f)
                {
                    StateMachine.ChangeState((int)StateType.Round);
                }
            }

            /// <summary>
            /// プレイヤーが視界内のときの警戒度加算処理
            /// </summary>
            private void PlusVigilancePoint(float distance)
            {
                const float MAX = 100;
                const float MIN = 0;

                // プレイヤーの距離が近いと警戒度が最大に
                if (distance <= Owner.enemyStatus.GetWarningRange)
                {
                    Owner.enemyStatus.m_vigilancePoint = MAX;
                }
                // 距離に応じて警戒度の上昇量が上がる
                else
                {
                    float inverseProportion = 1 - Mathf.InverseLerp(1, Owner.enemyStatus.GetViewRange, distance);
                    Owner.enemyStatus.m_vigilancePoint += Mathf.Lerp(0.05f, 0.1f, inverseProportion);
                }

                // Chase
                if (Mathf.Clamp(Owner.enemyStatus.m_vigilancePoint, MIN, MAX) >= MAX)
                {
                    StateMachine.ChangeState((int)StateType.Chase);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Vigilance");
                cts.Cancel();
            }
        }


        // プレイヤーを発見した時のチェイス処理を行うメソッド
        private class StateChase : StateBase
        {
            Vector3 posDelta;
            //float target_angle;

            public override void OnStart()
            {
                Owner.animationState.SetState("Run", true);

                posDelta = Vector3.zero;
                //target_angle = 0;
                Owner.navAgent.speed = 4;
                Debug.Log("start Chase");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;
                //target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                // navmeshでプレイヤーの座標まで移動する
                Owner.navAgent.SetDestination(Owner.player.transform.position);

                // プレイヤーとの距離が一定以下になればBattleステートへ移行
                if (Mathf.Abs(posDelta.magnitude) <= 5.0f){
                    Owner.navAgent.ResetPath();
                    StateMachine.ChangeState((int) StateType.Battle);
                }

                // エネミーの視界外にプレイヤーが抜けたらVigilanceステートへ移行
                if (Mathf.Abs(posDelta.magnitude) >= Owner.enemyStatus.GetViewRange){
                    Owner.enemyStatus.m_vigilancePoint -= 5.0f;
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
                Owner.animationState.SetState("Combat", true);

                posDelta = Vector3.zero;

                // プレイヤーの周囲を動くための目的地設定
                Transform playerPos = Owner.player.transform;
                float centerAngle = Mathf.Atan2(playerPos.forward.z, playerPos.forward.x) * Mathf.Rad2Deg;
                float randomOffset = Random.Range(-60f, 60f); // ±60°の範囲
                targetAngle = centerAngle + randomOffset;

                Owner.navAgent.angularSpeed = 0;

                Debug.Log("start Battle");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;

                if(Mathf.Abs(posDelta.magnitude) >= 15f){
                    Owner.navAgent.angularSpeed = 120;
                    StateMachine.ChangeState((int) StateType.Chase);
                }

                Vector3 destination = GetPointOnArc(Owner.player.transform.position, 3.0f, targetAngle);
                Owner.navAgent.SetDestination(destination);

                if(Mathf.Abs((Owner.transform.position - destination).magnitude) <= 0.5f){
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
                Owner.animationState.SetState("Attack", true);
                Owner.AA.StartAttackHit();

                Debug.Log("start Attack");
            }

            public override void OnUpdate()
            {
                // 攻撃アニメーションが終了したらButtleに遷移
                if(Owner.animationState.AnimtionFinish("Attack") >= 1f){
                    Owner.AA.EndAttackHit();
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
                Owner.animationState.SetState("Damage", true);

                Debug.Log("start Damage");
                Debug.Log(Owner.enemyStatus.GetHp);
                Owner.enemyStatus.m_vigilancePoint = 100f;
                Owner.animationState.SetState("Damage", true);
            }

            public override void OnUpdate()
            {
                if(Owner.animationState.AnimtionFinish("Damage") >= 1f){
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
                Debug.Log(Owner.enemyStatus.GetHp);

                Owner.enemyStatus.m_vigilancePoint = 100f;
                Owner.animationState.SetState("Backstabed", true);

                Owner.navAgent.speed = 0;

                Debug.Log("start Backstabed");
            }

            public override void OnUpdate()
            {
                if(Owner.animationState.AnimtionFinish("Backstabed") >= 1f){
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
