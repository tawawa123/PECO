using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace StateManager
{
    using StateBase = StateMachine<YarikumaController>.StateBase;

    public class YarikumaController : MonoBehaviour, Damagable, StealthAttackable
    {
        private GameObject player;
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
            Backstabed,
            StealthAttacked,
            Parryed,
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
            stateMachine.Add<StateStealthAttacked>((int) StateType.StealthAttacked);
            stateMachine.Add<StateParryed>((int) StateType.Parryed);
            stateMachine.Add<StateDeath>((int) StateType.Death);

            stateMachine.OnStart((int) StateType.Idle);

            AA = this.GetComponentInChildren<AttackArea>();
            AA.SetAttackArea();
        }

        // Update is called once per frame
        void Update()
        {
            player = GameManager.Instance.GetPlayerObj();
            stateMachine.OnUpdate();

            if(enemyStatus.GetBackstabed){
                animationState.SetState("Backstabed", true);
                stateMachine.ChangeState((int) StateType.Backstabed);
            }
        }

        private void CheckDeath()
        {
            if(enemyStatus.GetHp <= 0)
            {
                int layer = LayerMask.NameToLayer("Dead");
                this.gameObject.layer = layer;
                stateMachine.ChangeState((int) StateType.Death);
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
                    Owner.enemyStatus.m_vigilancePoint = Mathf.Clamp(Owner.enemyStatus.m_vigilancePoint, MIN, MAX);
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
            float targetRadius = 3.0f;

            private Vector3 lastPosition;       // 前回のフレームでの位置
            private float stuckTimer = 0f;      // 立ち往生を検出するためのタイマー
            private const float STUCK_THRESHOLD = 0.1f; // 停止とみなす移動量の閾値
            private const float STUCK_TIME_LIMIT = 2.0f; // 立ち往生と判断する時間 (秒)

            public override void OnStart()
            {
                Owner.animationState.SetState("Combat", true);

                Owner.navAgent.speed = 1;
                posDelta = Vector3.zero;

                // プレイヤーの周囲を動くための目的地設定
                Transform playerPos = Owner.player.transform;
                float targetAngle = Mathf.Atan2(playerPos.forward.z, playerPos.forward.x) * Mathf.Rad2Deg;

                // 移動先を決定
                SetNewDestination();
                Owner.navAgent.angularSpeed = 0;

                Debug.Log("start Battle");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - Owner.transform.position;

                if(Mathf.Abs(posDelta.magnitude) >= 15f)
                {
                    Owner.navAgent.angularSpeed = 120;
                    StateMachine.ChangeState((int) StateType.Chase);
                }

                // 立ち往生検出
                float movementSinceLastFrame = (Owner.transform.position - lastPosition).sqrMagnitude;
                
                if (movementSinceLastFrame < STUCK_THRESHOLD * STUCK_THRESHOLD)
                {
                    // ほとんど動いていない場合、タイマーを加算
                    stuckTimer += Time.deltaTime;
                    
                    if (stuckTimer >= STUCK_TIME_LIMIT)
                    {
                        Debug.Log("立ち往生を検出！移動パスを再計算します。");
                        
                        // 立ち往生と判断された場合、新しい目的地を設定
                        SetNewDestination(); 
                        stuckTimer = 0f;
                    }
                }
                else
                {
                    // 正常に動いている場合、タイマーをリセット
                    stuckTimer = 0f;
                }
                lastPosition = Owner.transform.position;


                Owner.navAgent.SetDestination(destination);

                // 確率で行動選択
                /// <summury>
                /// 80% - 攻撃に遷移
                /// 20% - 移動地点を再度指定
                /// <summury>
                if(Mathf.Abs((Owner.transform.position - destination).magnitude) <= 0.5f)
                {
                    int choice = Random.Range(0, 100);
                    Debug.Log(choice);
                    
                    if (choice < 75) // 60%
                    {
                        // 攻撃に遷移
                        StateMachine.ChangeState((int) StateType.Attack);
                    }
                    //else if (choice < 80) // 60% ~ 80%
                    //{
                        // 目の前に花火みたいなんを出しながら後退
                        // StateMachine.ChangeState((int) StateType.Dodge);
                    //}
                    else // 80% ~ 100%
                    {   
                        // 目的地を再設定
                        SetNewDestination();
                        Owner.navAgent.SetDestination(destination);
                    }
                }

                // プレイヤーの位置と敵の位置から角度を求める
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
                // 移動先を±60°の範囲でランダムに
                float randomOffset = Random.Range(-60f, 60f);

                // 角度をラジアンに変換
                float rad = (angleDeg + randomOffset) * Mathf.Deg2Rad;
                // 水平方向のみ
                float x = playerPos.x + radius * Mathf.Cos(rad);
                float z = playerPos.z + radius * Mathf.Sin(rad);

                return new Vector3(x, playerPos.y, z);
            }

            private void SetNewDestination()
            {
                Transform playerPos = Owner.player.transform;
                float centerAngle = Mathf.Atan2(playerPos.forward.z, playerPos.forward.x) * Mathf.Rad2Deg;
                
                // 最大試行回数を設定
                const int MAX_ATTEMPTS = 5; 
                
                for (int i = 0; i < MAX_ATTEMPTS; i++)
                {
                    // ランダムな円弧上の座標を計算
                    float randomOffset = Random.Range(-60f, 60f); 
                    float targetAngle = centerAngle + randomOffset;

                    // プレイヤーとの距離が近すぎる場合、少し離れる目標距離を設定
                    targetRadius = posDelta.magnitude < 2.5f ? 4.0f : 3.0f;
                    Vector3 randomPoint = GetPointOnArc(
                        Owner.player.transform.position, 
                        targetRadius, 
                        targetAngle
                    );

                    // NavMesh上で到達可能かチェック
                    UnityEngine.AI.NavMeshHit hit;

                    if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        // 有効なNavMesh上の地点が見つかった場合
                        destination = hit.position;
                        Owner.navAgent.SetDestination(destination);
                        Debug.Log($"新しい目的地を設定: {destination}");
                        return;
                    }
                }

                Debug.LogWarning("有効な移動先が見つかりませんでした。前回目的地を維持します。");
            }
        }


        // 攻撃判定用メソッド
        private class StateAttack : StateBase
        {
            string[] attackPattarn = new string[] {"1", "2", "3"};
            int currentAnimationNum;
            public override void OnStart()
            {
                Owner.navAgent.isStopped = true;
                Owner.rb.isKinematic = false;

                // 確率で3パターンから攻撃を選出
                int choice = Random.Range(0, 100);
                if (choice < 40)
                {
                    Owner.rb.AddForce(Owner.transform.forward * 1.0f, ForceMode.Impulse);
                    currentAnimationNum = 0;
                    Owner.animationState.SetState("1", true);
                }
                else if(choice < 70)
                {
                    Owner.rb.AddForce(Owner.transform.forward * 2.0f, ForceMode.Impulse);
                    currentAnimationNum = 1;
                    Owner.animationState.SetState("2", true);
                }
                else
                {
                    Owner.rb.AddForce(Owner.transform.forward * 2.0f, ForceMode.Impulse);
                    currentAnimationNum = 2;
                    Owner.animationState.SetState("3", true);
                }
                
                Owner.AA.StartAttackHit();
                Debug.Log("start Attack");
            }

            public override void OnUpdate()
            {
                // 攻撃アニメーションが終了したらBattleに遷移
                if(Owner.animationState.AnimtionFinish(
                    attackPattarn[currentAnimationNum]) >= 1f)
                {
                    Owner.AA.EndAttackHit();
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                Owner.rb.isKinematic = true;
                Owner.navAgent.isStopped = false;
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
                Owner.CheckDeath();

                if(Owner.animationState.AnimtionFinish("Damage") >= 1f){
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Damage");
            }
        }


        // 特殊攻撃用インターフェイス
        public void HaveStealthAttack(){
            navAgent.isStopped = true;
            stateMachine.ChangeState((int) StateType.StealthAttacked);
        }
        private class StateStealthAttacked : StateBase
        {
            private CancellationTokenSource cts;

            public override void OnStart()
            {
                Debug.Log("start StealthAttacked");
                Owner.enemyStatus.m_vigilancePoint = 100f;
                cts = new CancellationTokenSource();
                Owner.animationState.SetState("StealthAttacked", true);

                DelayDeath(cts.Token).Forget();
            }

            private async UniTask DelayDeath(CancellationToken token)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f));
                Owner.enemyStatus.m_hp = 0;
            }

            public override void OnUpdate()
            {
                // 死亡判定
                Owner.CheckDeath();
            }

            public override void OnEnd()
            {
                Debug.Log("end StealthAttacked");
            }
        }

        // プレイヤーにパリィされた際のスタン処理
        public void ChangeParryedState(){
            stateMachine.ChangeState((int) StateType.Parryed);
        }
        private class StateParryed : StateBase
        {
            // CancellationTokenSourceはクラスレベルで管理
            private CancellationTokenSource cts;
            private PlayerController playerController;

            // パリィ硬直時間
            private const float PARRY_STUN_DURATION = 2.5f; 

            public override void OnStart()
            {
                Debug.Log("start Parryed");
                
                playerController = Owner.player.GetComponent<PlayerController>();
                // 既存のトークンを破棄し、新しく作成
                cts?.Dispose();
                cts = new CancellationTokenSource();
                
                // アニメーションステートを設定
                Owner.animationState.SetState("Parryed", true); 
                // 今スタンしているかどうか
                Owner.enemyStatus.m_stun = false;

                // 非同期処理を開始
                WaitParryed(cts.Token).Forget();
            }

            private async UniTask WaitParryed(CancellationToken token)
            {
                // 待機時間が始まった時、プレイヤーコントローラー側に用意されているフラグを参照してtrueにする
                playerController.CanStealthAttack(true);
                Debug.Log("プレイヤーフラグをON: 追撃可能状態");
                
                // パリィされた際に、2.5秒程度の待機時間を設ける
                bool isCanceled = await UniTask.Delay(
                    System.TimeSpan.FromSeconds(PARRY_STUN_DURATION),
                    cancellationToken: token
                ).SuppressCancellationThrow();

                // プレイヤーのフラグを解除
                playerController.CanStealthAttack(false);
                Debug.Log("プレイヤーフラグをOFF: 追撃終了");

                if (isCanceled)
                {
                    // 待機時間中に外部からのキャンセルがあった場合
                    Debug.Log("外部からのキャンセル（例: 追撃ヒット）により、硬直を即時終了");
                    Owner.enemyStatus.m_stun = true;
                }
                else
                {
                    // 待機時間中に何もなかったのであれば（時間切れ）
                    Debug.Log("硬直時間終了。通常戦闘状態に戻ります。");
                    
                    // 通常の状態に戻る
                    Owner.enemyStatus.m_stun = true;
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                // 待機時間をリセットする = キャンセル処理を行う
                cts?.Cancel();
                cts?.Dispose();
                cts = null;

                // 今スタンしているかどうか
                Owner.enemyStatus.m_stun = true;
                Debug.Log("end Parryed state.");
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
                Owner.CheckDeath();

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
