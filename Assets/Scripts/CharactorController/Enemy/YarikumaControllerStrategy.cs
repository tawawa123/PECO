using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Threading;
using SoundEffects;

namespace StateManager
{
    using StateBase = StateMachine<YarikumaControllerStrategy>.StateBase;

    public class YarikumaControllerStrategy : IEnemyControllStrategy
    {
        private GameObject player;
        private bool findPlayer = false;


        // ctx = PlayerControllerへの参照
        private EnemyController ctx;
        public YarikumaControllerStrategy(EnemyController context)
        {
            this.ctx = context;
        }


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
        private StateMachine<YarikumaControllerStrategy> stateMachine; //ステート遷移管理

        public void OnEnter()
        {
            stateMachine = new StateMachine<YarikumaControllerStrategy>(this);
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

            ctx.AA.SetAttackArea();
        }

        // Update is called once per frame
        public void Tick()
        {
            player = GameManager.Instance.GetPlayerObj();
            stateMachine.OnUpdate();

            if(ctx.estatus.GetBackstabed){
                ctx.animator.SetState("Backstabed", true);
                stateMachine.ChangeState((int) StateType.Backstabed);
            }
        }

        public void OnExit()
        {
            // 終了時処理
        }

        public void ChangeParryed()
        {
            stateMachine.ChangeState((int) StateType.Parryed);
        }
        public void ChangeDeath()
        {
            stateMachine.ChangeState((int) StateType.Death);
        }
        public void AddDamage(int damage)
        {
            stateMachine.ChangeState((int) StateType.Damage);
        }

        // エネミーの死亡判定メソッド
        private void CheckDeath()
        {
            if(ctx.estatus.GetHp <= 0)
            {
                int layer = LayerMask.NameToLayer("Dead");
                ctx.gameObject.layer = layer;
                stateMachine.ChangeState((int) StateType.Death);
            }
        }

        // Idle状態を定義するメソッド
        // 基本使わないけど、巡回中に立ち止まったりするときにIdleステートに入るかもなので一応定義
        private class StateIdle : StateBase
        {
            StateManager.EnemyController ctx;

            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.animator.SetState("Idle", true);
                ctx.AA.SetAttackArea();
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
            StateManager.EnemyController ctx;
            Vector3 posDelta;

            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.animator.SetState("Walk", true);
                posDelta = Vector3.zero;
                ctx.nav.SetDestination(ctx.des.GetDestination());

                Debug.Log("start Round");
            }

            public override void OnUpdate()
            {
                ctx.estatus.m_vigilancePoint = Mathf.Clamp((ctx.estatus.m_vigilancePoint - 0.05f), 0f, 100f);

                //navmeshによる巡回処理
                if(Vector3.Distance(ctx.tf.position, ctx.des.GetDestination()) < 1.5f)
                {
                    ctx.des.CreateDestination();
                    ctx.nav.SetDestination(ctx.des.GetDestination());
                }

                posDelta = Owner.player.transform.position - ctx.tf.position;
                float distance = posDelta.magnitude;


                // 周辺で戦闘状態に入ったエネミーがいたなら、即警戒状態に移行



                // 1. 視界範囲外なら終了
                if (distance > ctx.estatus.GetViewRange)
                    return;

                // 2. 視界角度外なら終了
                float targetAngle = Vector3.Angle(ctx.tf.forward, posDelta);
                if (targetAngle >= ctx.estatus.GetViewAngle)
                    return;

                // 3. Raycastでプレイヤーに遮蔽物があるなら終了
                Vector3 eyePosition = ctx.tf.position + Vector3.up * 1.5f;
                Vector3 direction = posDelta.normalized;

                if (!Physics.Raycast(eyePosition, direction, out RaycastHit hit, distance))
                    return;

                if (!hit.collider.CompareTag("Player"))
                    return;


                // --- 視界にプレイヤーが見えている際の処理 ---
                Debug.DrawRay(eyePosition, direction * distance, Color.red, 0.1f);

                // 4. 危険距離の判定
                if (distance <= ctx.estatus.GetWarningRange)
                {
                    ctx.estatus.m_vigilancePoint = 100f;
                    StateMachine.ChangeState((int)StateType.Chase);
                }
                else
                {
                    StateMachine.ChangeState((int)StateType.Vigilance);
                }

                Debug.Log(ctx.estatus.m_vigilancePoint);
                // ダメージ処理が起きたらここでストップ
                if(ctx.estatus.m_vigilancePoint >= 100f)
                    StateMachine.ChangeState((int) StateType.Battle);
            }

            public override void OnEnd()
            {
                Debug.Log("end Round");
            }
        }


        // 周囲から音が聞こえたときの警戒移行メソッド
        public void OnSoundHeard(SoundEvent soundEvent)
        {
            // 仮：音源を見る
            ctx.tf.LookAt(soundEvent.position);
            stateMachine.ChangeState((int)StateType.Vigilance);
        }
        // vigilance
        private class StateVigilance : StateBase
        {
            StateManager.EnemyController ctx;

            Vector3 posDelta;
            float timer = 0;
            CancellationTokenSource cts;

            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.animator.SetState("Search", true);
                ctx.nav.SetDestination(ctx.tf.position);

                Debug.Log("start Vigilance");

                // 警戒処理の開始
                cts = new CancellationTokenSource();
                VigilanceLoopAsync(cts.Token).Forget();
            }

            // 警戒処理ループ
            private async UniTaskVoid VigilanceLoopAsync(CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    // 視界にプレイヤーがいなければ、タイマーを進めて次フレームへ
                    if (!IsPlayerVisible(out float distance))
                    {
                        HandleInvisible(Time.deltaTime);
                        await UniTask.Yield(token);
                        continue;
                    }

                    // 見えているならば警戒ポイント加算
                    timer = 0f;
                    PlusVigilancePoint(distance);

                    await UniTask.Yield(token);
                }
            }

            // プレイヤーが視界内にいるかを判定
            private bool IsPlayerVisible(out float distance)
            {
                // 変身したとき、Playerの指定が間に合わずOwner.PlayerがNullになったとき用
                if (Owner.player == null)
                {
                    distance = 1;
                    return false;
                }

                posDelta = Owner.player.transform.position - ctx.tf.position;
                distance = posDelta.magnitude;

                // 視界範囲外
                if (distance >= ctx.estatus.GetViewRange)
                    return false;

                // 視野角外
                float angle = Vector3.Angle(ctx.tf.forward, posDelta);
                if (angle >= ctx.estatus.GetViewAngle)
                    return false;

                // Rayがヒットしない
                if (!Physics.Raycast(ctx.tf.position, posDelta, out RaycastHit hit, ctx.estatus.GetViewRange))
                    return false;

                // ヒットしたのがプレイヤーでなければ除外
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Transformation"))
                    return false;

                return true;
            }

            // 見失っている間のカウント処理
            private void HandleInvisible(float deltaTime)
            {
                timer += deltaTime;

                if (timer >= 5f)
                {
                    StateMachine.ChangeState((int)StateType.Round);
                }
            }

            // プレイヤーが視界内のときの警戒度加算処理
            private void PlusVigilancePoint(float distance)
            {
                const float MAX = 100;
                const float MIN = 0;

                // プレイヤーの距離が近いと警戒度が最大
                if (distance <= ctx.estatus.GetWarningRange)
                {
                    ctx.estatus.m_vigilancePoint = MAX;
                }
                // 距離に応じて警戒度の上昇量が上がる
                else
                {
                    float inverseProportion = 1 - Mathf.InverseLerp(1, ctx.estatus.GetViewRange, distance);
                    ctx.estatus.m_vigilancePoint += Mathf.Lerp(0.5f, 2f, inverseProportion);
                    ctx.estatus.m_vigilancePoint = Mathf.Clamp(ctx.estatus.m_vigilancePoint, MIN, MAX);
                }

                // Chase
                if (Mathf.Clamp(ctx.estatus.m_vigilancePoint, MIN, MAX) >= MAX)
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
            StateManager.EnemyController ctx;

            Vector3 posDelta;
            //float target_angle;

            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.animator.SetState("Run", true);

                posDelta = Vector3.zero;
                //target_angle = 0;
                ctx.nav.speed = 4;
                Debug.Log("start Chase");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - ctx.tf.position;
                //target_angle = Vector3.Angle(Owner.transform.forward, posDelta);

                // navmeshでプレイヤーの座標まで移動する
                ctx.nav.SetDestination(Owner.player.transform.position);

                // プレイヤーとの距離が一定以下になればBattleステートへ移行
                if (Mathf.Abs(posDelta.magnitude) <= 5.0f){
                    ctx.nav.ResetPath();
                    StateMachine.ChangeState((int) StateType.Battle);
                }

                // エネミーの視界外にプレイヤーが抜けたらVigilanceステートへ移行
                if (Mathf.Abs(posDelta.magnitude) >= ctx.estatus.GetViewRange){
                    ctx.estatus.m_vigilancePoint -= 5.0f;
                    StateMachine.ChangeState((int) StateType.Vigilance);
                }
            }

            public override void OnEnd()
            {
                ctx.nav.speed = 2;
                Debug.Log("end Chase");
            }
        }


        // 戦闘状態の処理メソッド
        private class StateBattle : StateBase
        {
            StateManager.EnemyController ctx;


            Vector3 posDelta;
            Vector3 destination;
            float targetAngle;
            float targetRadius = 3.0f;

            private Vector3 lastPosition;       // 前回のフレームでの位置
            private float stuckTimer = 0f;      // 立ち往生を検出するためのタイマー
            private const float STUCK_THRESHOLD = 0.1f; // 停止とみなす移動量の閾値
            private const float STUCK_TIME_LIMIT = 5.0f; // 立ち往生と判断する時間 (秒)

            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.animator.SetState("Combat", true);

                ctx.nav.speed = 1;
                posDelta = Vector3.zero;

                // プレイヤーの周囲を動くための目的地設定
                Transform playerPos = Owner.player.transform;
                float targetAngle = Mathf.Atan2(playerPos.forward.z, playerPos.forward.x) * Mathf.Rad2Deg;

                // 移動先を決定
                SetNewDestination();
                ctx.nav.angularSpeed = 0;

                Debug.Log("start Battle");
            }

            public override void OnUpdate()
            {
                posDelta = Owner.player.transform.position - ctx.tf.position;

                if(Mathf.Abs(posDelta.magnitude) >= 15f)
                {
                    ctx.nav.angularSpeed = 120;
                    StateMachine.ChangeState((int) StateType.Chase);
                }

                // 立ち往生検出
                float movementSinceLastFrame = (ctx.tf.position - lastPosition).sqrMagnitude;
                
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
                lastPosition = ctx.tf.position;


                ctx.nav.SetDestination(destination);

                // 確率で行動選択
                /// <summury>
                /// 80% - 攻撃に遷移
                /// 20% - 移動地点を再度指定
                /// <summury>
                if(Mathf.Abs((ctx.tf.position - destination).magnitude) <= 0.5f)
                {
                    int choice = Random.Range(0, 100);
                    
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
                        ctx.nav.SetDestination(destination);
                    }
                }

                // プレイヤーの位置と敵の位置から角度を求める
                var qrot = Quaternion.LookRotation(Owner.player.transform.position - ctx.tf.position);
                ctx.tf.rotation = Quaternion.Slerp(ctx.tf.rotation, qrot, Time.time * 2);
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
                        ctx.nav.SetDestination(destination);
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
            StateManager.EnemyController ctx;

            string[] attackPattarn = new string[] {"1", "2", "3"};
            int currentAnimationNum;
            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.nav.isStopped = true;
                ctx.rb.isKinematic = false;

                // 確率で3パターンから攻撃を選出
                int choice = Random.Range(0, 100);
                if (choice < 40)
                {
                    ctx.rb.AddForce(ctx.tf.forward * 1.0f, ForceMode.Impulse);
                    currentAnimationNum = 0;
                    ctx.animator.SetState("1", true);
                }
                else if(choice < 70)
                {
                    ctx.rb.AddForce(ctx.tf.forward * 5.0f, ForceMode.Impulse);
                    currentAnimationNum = 1;
                    ctx.animator.SetState("2", true);
                }
                else
                {
                    ctx.rb.AddForce(ctx.tf.forward * 2.0f, ForceMode.Impulse);
                    currentAnimationNum = 2;
                    ctx.animator.SetState("3", true);
                }
                
                ctx.AA.StartAttackHit();
                Debug.Log("start Attack");
            }

            public override void OnUpdate()
            {
                // 攻撃アニメーションが終了したらBattleに遷移
                if(ctx.animator.AnimtionFinish(
                    attackPattarn[currentAnimationNum]) >= 1f)
                {
                    ctx.AA.EndAttackHit();
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                ctx.rb.isKinematic = true;
                ctx.nav.isStopped = false;
                Debug.Log("end Attack");
            }
        }

        // ダメージが発生した時の体力管理やアニメーション再生用のメソッド
        private class StateDamage : StateBase
        {
            StateManager.EnemyController ctx;

            public override void OnStart()
            {
                ctx = Owner.ctx;

                ctx.animator.SetState("Damage", true);

                Debug.Log("start Damage");
                Debug.Log(ctx.estatus.GetHp);
                ctx.estatus.m_vigilancePoint = 100f;
                ctx.animator.SetState("Damage", true);
            }

            public override void OnUpdate()
            {
                Owner.CheckDeath();

                if(ctx.animator.AnimtionFinish("Damage") >= 1f){
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
            stateMachine.ChangeState((int) StateType.StealthAttacked);
        }
        private class StateStealthAttacked : StateBase
        {
            StateManager.EnemyController ctx;
            private CancellationTokenSource cts;

            public override void OnStart()
            {
                ctx = Owner.ctx;
                ctx.nav.isStopped = true;

                Debug.Log("start StealthAttacked");
                ctx.estatus.m_vigilancePoint = 100f;
                cts = new CancellationTokenSource();
                ctx.animator.SetState("StealthAttacked", true);

                DelayDeath(cts.Token).Forget();
            }

            private async UniTask DelayDeath(CancellationToken token)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f));
                ctx.estatus.m_hp = 0;
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
            StateManager.EnemyController ctx;

            // CancellationTokenSourceはクラスレベルで管理
            private CancellationTokenSource cts;
            private PlayerController playerController;

            // パリィ硬直時間
            private const float PARRY_STUN_DURATION = 2.5f; 

            public override void OnStart()
            {
                Debug.Log("start Parryed");
                
                ctx = Owner.ctx;
                playerController = Owner.player.GetComponent<PlayerController>();
                // 既存のトークンを破棄し、新しく作成
                cts?.Dispose();
                cts = new CancellationTokenSource();
                
                // アニメーションステートを設定
                ctx.animator.SetState("Parryed", true); 
                // 今スタンしているかどうか
                ctx.estatus.m_stun = false;

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
                    ctx.estatus.m_stun = true;
                }
                else
                {
                    // 待機時間中に何もなかったのであれば（時間切れ）
                    Debug.Log("硬直時間終了。通常戦闘状態に戻ります。");
                    
                    // 通常の状態に戻る
                    ctx.estatus.m_stun = true;
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
                ctx.estatus.m_stun = true;
                Debug.Log("end Parryed state.");
            }
        }


        private class StateBackstabed : StateBase
        {
            StateManager.EnemyController ctx;

            public override void OnStart()
            {
                ctx = Owner.ctx;

                Debug.Log(ctx.estatus.GetHp);

                ctx.estatus.m_vigilancePoint = 100f;
                ctx.animator.SetState("Backstabed", true);

                ctx.nav.speed = 0;

                Debug.Log("start Backstabed");
            }

            public override void OnUpdate()
            {
                Owner.CheckDeath();

                if(ctx.animator.AnimtionFinish("Backstabed") >= 1f){
                    StateMachine.ChangeState((int) StateType.Battle);
                }
            }

            public override void OnEnd()
            {
                ctx.estatus.m_backstabed = false;
                ctx.nav.speed = 2;
                Debug.Log("end Backstabed");
            }
        }


        // 死亡判定用メソッド
        private class StateDeath : StateBase
        {
            StateManager.EnemyController ctx;

            public override void OnStart()
            {
                Debug.Log("start Death");

                ctx = Owner.ctx;
                ctx.animator.SetState("Death", true);
            }

            public override void OnUpdate()
            {
                Debug.Log("体力が0になりました");
                if(ctx.animator.AnimtionFinish("Death") >= 1f){
                    GameManager.Instance.CheckGameCrear();
                    //ctx.Destroy(this);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Death");
            }
        }
    }
}
