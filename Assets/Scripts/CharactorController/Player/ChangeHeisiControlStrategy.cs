using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GameUI;

namespace StateManager
{
    using StateBase = StateMachine<ChangeHeisiControllerStrategy>.StateBase;

    public class ChangeHeisiControllerStrategy : IPlayerControlStrategy
    {
        private PlayerStatus playerStatus;

        // ctx = PlayerControllerへの参照
        private PlayerController ctx;
        public ChangeHeisiControllerStrategy(PlayerController context)
        {
            this.ctx = context;
        }

        //プレイヤー移動、回転制御
        private float inputHorizontal;
        private float inputVertical;
        private Vector3 moveForward;
        private Quaternion targetRotation;

        // カメラ回転制御
        private const float RotateSpeed = 900f;
        private const float RotateSpeedLockon = 500f;

        // 設置判定の大きさ
        private const float isGroundedSize = 0.1f;

        // private GameObject stealthAttackTarget; // ステルスアタックの対象位置
        private bool isGrounded; // 設置判定
        // private bool canStealthAttack; // ステルス攻撃用フラグ

        // StateTypeの定義
        private enum StateType
        {
            Idle,
            UIControll,

            // Move   
            Walk,
            Run,
            Jump,
            Fall,
            Sliding,
            Crouch,
            CrouchWalk,
            Avoid,
            Hide,

            // Battle
            Backstab,
            Stun,
            Attack,
            StealthAttack,
            DashAttack,
            Damage,
            Parry,
            Guard,

            // 死亡
            GameOver,
        }

        private StateMachine<ChangeHeisiControllerStrategy> stateMachine; //ステート遷移制御


        // 呼び出されたときの初期化処理
        public void OnEnter()
        {
            playerStatus = GameManager.Instance.CurrentStatus;

            // StateTypeの数だけステートの登録
            stateMachine = new StateMachine<ChangeHeisiControllerStrategy>(this);
            stateMachine.Add<StateIdle>((int) StateType.Idle);                      // Idle
            stateMachine.Add<StateUIControll>((int) StateType.UIControll);          // UI操作時の処理
            stateMachine.Add<StateWalk>((int) StateType.Walk);                      // 通常歩行
            stateMachine.Add<StateRun>((int) StateType.Run);                        // ダッシュ
            stateMachine.Add<StateJump>((int) StateType.Jump);                      // ジャンプ
            stateMachine.Add<StateFall>((int) StateType.Fall);                      // 落下中の着地判定
            stateMachine.Add<StateSliding>((int) StateType.Sliding);                // スライディング
            stateMachine.Add<StateCrouch>((int) StateType.Crouch);                  // しゃがみ
            stateMachine.Add<StateCrouchWalk>((int) StateType.CrouchWalk);          // しゃがみ歩き
            stateMachine.Add<StateAvoid>((int) StateType.Avoid);                    // 回避
            stateMachine.Add<StateHide>((int) StateType.Hide);                      // 隠密状態
            stateMachine.Add<StateBackstab>((int) StateType.Backstab);              // バックスタブ
            stateMachine.Add<StateStun>((int) StateType.Stun);                      // スタン
            stateMachine.Add<StateAttack>((int) StateType.Attack);                  // 攻撃
            stateMachine.Add<StateStealthAttack>((int) StateType.StealthAttack);    // ステルスアタック
            stateMachine.Add<StateDashAttack>((int) StateType.DashAttack);          // ダッシュ派生攻撃
            stateMachine.Add<StateDamage>((int) StateType.Damage);                  // ダメージ処理
            stateMachine.Add<StateParry>((int) StateType.Parry);                    // パリィ処理
            stateMachine.Add<StateGuard>((int) StateType.Guard);                    // ガード処理
            stateMachine.Add<StateGameOver>((int) StateType.GameOver);              // 死亡時処理

            UIManager.Instance.Hide(UIType.CutIn);
            ctx.weapon.enabled = false;

            stateMachine.OnStart((int) StateType.Idle);
        }

        // Update
        public void Tick()
        {
            stateMachine.OnUpdate();

            // LockForEnemy();

            // 着地判定
            isGrounded = Physics.CheckSphere(ctx.tf.position, isGroundedSize, LayerMask.GetMask("Ground"));

            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");

            if(playerStatus.GetHp <= 0)
                stateMachine.ChangeState((int) StateType.GameOver);

            stateMachine.OnUpdate();
        }

        // 終了時処理
        public void OnExit()
        {
            // 武器非表示、UIクリアなど
        }

        // ステートの割り込み
        public void ChangeParry()
        {
            stateMachine.ChangeState((int) StateType.Parry);
        }
        public void ChangeStun()
        {
            stateMachine.ChangeState((int) StateType.Stun);
        }
        public void AddDamage(int damage)
        {
            stateMachine.ChangeState((int) StateType.Damage);
        }


        // 攻撃したときにバクスタ可能な位置にいるかどうかの判定
        public bool Backstab()
        {
            bool backstab = false;
            GameObject[] gos;
            gos = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject closest = null;
            float distance = 10;

            foreach (GameObject go in gos)
            {
                Vector3 diff = go.transform.position - ctx.transform.position;
                float curDistance = diff.sqrMagnitude;
                if (curDistance < distance)
                {
                    closest = go;
                    distance = curDistance;
                }
            }

            if(closest == null){
                return backstab;
            }
            
            float Angle = Vector3.Angle(closest.transform.forward, ctx.transform.forward);
            if(Mathf.Abs(Angle) < 20.0f){
                backstab = true;
                Debug.Log(closest.GetComponent<EnemyStatus>());
                closest.GetComponent<EnemyStatus>().m_backstabed = true;
            }

            return backstab;
        }

        // ロックオン中のターゲット注視処理
        public void LockForEnemy()
        {
            // ロックオン中はターゲットを向き続ける
            Quaternion from = ctx.transform.rotation;
            var dir = ctx.playerLo.GetLockonCameraLookAtTransform().position - ctx.transform.position;
            dir.y = 0;
            Quaternion to = Quaternion.LookRotation(dir);
            ctx.transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeedLockon * Time.deltaTime);
        }

        // 足元に設置判定を描画
        void OnDrawGizmos()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(ctx.transform.position, isGroundedSize);
        }


        /// <summary>
        /// 以下ステートマシン
        /// StateMachine.StateBaseクラスを継承した各ステート定義用クラスを作成し、動作を記述
        /// StateMachine.ChangeState => 指定したステートに状態遷移
        /// StateMachine.ChangePrevState => ひとつ前のステートに状態遷移
        /// </summary>
        
        // state idle 
        private class StateIdle : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Idle");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Idle", true);
            }

            public override void OnUpdate()
            {
                Debug.Log(Owner.playerStatus.m_stumina);
                Owner.playerStatus.m_stumina = Mathf.MoveTowards(Owner.playerStatus.GetStumina, 100, Time.deltaTime * 4);

                // Crouch
                if(Input.GetKeyDown(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Crouch);
                }

                // Walk
                if(Mathf.Abs(Owner.inputHorizontal) >= 0.1f || Mathf.Abs(Owner.inputVertical) >= 0.1f)
                {
                    StateMachine.ChangeState((int) StateType.Walk);
                }

                // Avoid
                if(Input.GetKeyDown(KeyCode.LeftShift))
                {
                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                // Attack or Backstab
                if (Input.GetMouseButtonDown(0))
                {
                    StateMachine.ChangeState((int) StateType.Attack);
                }

                // jump
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StateMachine.ChangeState((int) StateType.Jump);
                }

                // guard
                if (Input.GetMouseButtonDown(1))
                {
                    StateMachine.ChangeState((int) StateType.Guard);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Idle");
            }
        }


        // state UI controll
        public void ChangeUIControlleState()
        {
            stateMachine.ChangeState((int) StateType.UIControll);
        }
        // UI操作時の処理
        private class StateUIControll : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                ctx = Owner.ctx;
                Debug.Log("start Idle");
                ctx.animationState.SetState("Idle", true);
            }

            public override void OnUpdate()
            {
                Owner.playerStatus.m_stumina = Mathf.MoveTowards(Owner.playerStatus.GetStumina, 100, Time.deltaTime * 4);
                // 何もしない
            }

            public override void OnEnd()
            {
                Debug.Log("end Idle");
            }
        }


        // walk state 
        private class StateWalk : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                ctx = Owner.ctx;
                ctx.animationState.SetState("Walk", true);
                Debug.Log("start walk");
            }

            public override void OnUpdate()
            {
                Owner.playerStatus.m_stumina = Mathf.MoveTowards(Owner.playerStatus.GetStumina, 100, Time.deltaTime * 4);

                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                // 移動方向にスピードを掛ける
                ctx.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed + new Vector3(0, ctx.rb.velocity.y, 0);

                if (Owner.moveForward != Vector3.zero) {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    ctx.tf.rotation = Quaternion.Slerp(ctx.tf.rotation, Owner.targetRotation, Time.deltaTime * Owner.playerStatus.GetRotationRate);
                }

                // Idle
                if(ctx.rb.velocity.magnitude < 0.1f){
                    ctx.animationState.SetState("Idle");
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // Avoid
                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    Quaternion from = ctx.tf.rotation;
                    Quaternion to = Quaternion.LookRotation(Owner.moveForward);
                    ctx.tf.rotation = Quaternion.RotateTowards(from, to, RotateSpeed);

                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                // jump
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StateMachine.ChangeState((int) StateType.Jump);
                }

                // Attack or Backstab
                if (Input.GetMouseButtonDown(0))
                {
                    StateMachine.ChangeState((int) StateType.Attack);
                }

                // Crouch
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Crouch);
                }

                // fall
                if (!Owner.isGrounded)
                {
                    StateMachine.ChangeState((int) StateType.Fall);
                }

                // guard
                if (Input.GetMouseButtonDown(1))
                {
                    StateMachine.ChangeState((int) StateType.Guard);
                }
            }

            public override void OnEnd()
            {
                // Owner.rb.velocity = Vector3.zero;
                Debug.Log("end walk");
            }
        }


        // run state 
        private class StateRun : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                ctx = Owner.ctx;
                ctx.animationState.SetState("Run", true);
                Debug.Log("start run");
            }

            public override void OnUpdate()
            {
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                // 移動方向にスピードを掛ける
                ctx.rb.velocity = Owner.moveForward * Owner.playerStatus.GetRunSpeed + new Vector3(0, ctx.rb.velocity.y, 0);
                Owner.playerStatus.m_stumina -= 0.01f;
                
                // 向いている方向に回転
                if (Owner.moveForward != Vector3.zero) {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, Owner.targetRotation, Time.deltaTime * Owner.playerStatus.GetRotationRate);
                }

                // Idle
                if(ctx.rb.velocity.magnitude < 0.1f){
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // Avoid
                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    Quaternion from = ctx.transform.rotation;
                    Quaternion to = Quaternion.LookRotation(Owner.moveForward);
                    ctx.transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeed);

                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                // jump
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StateMachine.ChangeState((int) StateType.Jump);
                }

                // Attack or Backstab
                if (Input.GetMouseButtonDown(0))
                {
                    StateMachine.ChangeState((int) StateType.DashAttack);
                }

                // sliding
                if (Input.GetKeyDown(KeyCode.LeftControl)){
                    StateMachine.ChangeState((int) StateType.Sliding);
                }

                // fall
                if (!Owner.isGrounded)
                {
                    StateMachine.ChangeState((int) StateType.Fall);
                }

                // guard
                if (Input.GetMouseButtonDown(1))
                {
                    StateMachine.ChangeState((int) StateType.Guard);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end run");
            }
        }


        // state Jump
        private class StateJump : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Jump");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Jump", true);
                ctx.rb.AddForce(ctx.transform.up * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
            }

            public override void OnUpdate()
            {
                // 方向制御
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                ctx.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed + new Vector3(0, ctx.rb.velocity.y, 0);

                // 着地
                if(ctx.animationState.AnimtionFinish("Jump") >= 0.5f && Owner.isGrounded)
                {
                    StateMachine.ChangePrevState();
                } else if (ctx.animationState.AnimtionFinish("Jump") >= 1f && !Owner.isGrounded) {
                    StateMachine.ChangeState((int) StateType.Fall);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Jump");
            }
        }


        // state Fall
        private class StateFall : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Fall");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Fall", true);
            }

            public override void OnUpdate()
            {
                // 方向制御
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                ctx.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed / 2 + new Vector3(0, ctx.rb.velocity.y, 0);

                // 着地
                if(Owner.isGrounded)
                {
                    ctx.animationState.SetState("Land");
                }

                // idle
                if(ctx.animationState.AnimtionFinish("Land") >= 1f)
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Fall");
            }
        }


        // avoid state 
        private class StateAvoid : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start avoid");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Rolling");
                Owner.playerStatus.m_stumina -= 20;
                ctx.rb.AddForce(ctx.transform.forward * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
                ctx.gameObject.layer = LayerMask.NameToLayer("Avoid");
            }

            public override void OnUpdate()
            {   
                // アニメーション終了時の処理
                if(ctx.animationState.AnimtionFinish("Rolling") >= 0.6f && Input.GetKey(KeyCode.LeftShift))
                {
                    StateMachine.ChangeState((int) StateType.Run);
                } 
                else if(ctx.animationState.AnimtionFinish("Rolling") >= 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                ctx.gameObject.layer = LayerMask.NameToLayer("PlayerHit");
                Debug.Log("end avoid");
            }
        }


        // state crouch
        private class StateCrouch : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Crouch");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Crouch", true);
                ctx.gameObject.tag = "Hide";
            }

            public override void OnUpdate()
            {
                Owner.playerStatus.m_stumina = Mathf.MoveTowards(Owner.playerStatus.GetStumina, 100, Time.deltaTime * 4);

                // idle 
                if(Input.GetKeyUp(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // courchWalk
                if(Mathf.Abs(Owner.inputHorizontal) >= 0.1f || Mathf.Abs(Owner.inputVertical) >= 0.1f)
                {
                    StateMachine.ChangeState((int) StateType.CrouchWalk);
                }

                // stealthAttack
                if (Input.GetMouseButtonDown(0) && ctx.GetStealthAttackFlag())
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Crouch");
                ctx.gameObject.tag = "Player";
            }
        }


        // CrouchWalk state 
        private class StateCrouchWalk : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start CrouchWalk");

                ctx = Owner.ctx;
                ctx.animationState.SetState("CrouchWalk", true);
                ctx.gameObject.tag = "Hide";
            }

            public override void OnUpdate()
            {
                Owner.playerStatus.m_stumina = Mathf.MoveTowards(Owner.playerStatus.GetStumina, 100, Time.deltaTime * 4);

                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                // 移動方向にスピードを掛ける
                ctx.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed / 2 + new Vector3(0, ctx.rb.velocity.y, 0);

                if (Owner.moveForward != Vector3.zero) 
                {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    ctx.transform.rotation = Quaternion.Slerp(ctx.transform.rotation, Owner.targetRotation, Time.deltaTime * Owner.playerStatus.GetRotationRate);
                }

                // crouch
                if(ctx.rb.velocity.magnitude < 0.1f)
                {
                    StateMachine.ChangeState((int) StateType.Crouch);
                }

                // walk
                if(Input.GetKeyUp(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Walk);
                }

                // idle
                if(Input.GetKeyUp(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // stealthAttack
                if (Input.GetMouseButtonDown(0) && ctx.GetStealthAttackFlag())
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                }
            }

            public override void OnEnd()
            {
                ctx.rb.velocity = Vector3.zero;
                ctx.gameObject.tag = "Player";
                Debug.Log("end CrouchWalk");
            }
        }


        // state sliding 
        private class StateSliding : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start sliding");

                ctx = Owner.ctx;
                ctx.rb.AddForce(ctx.transform.forward * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
                ctx.animationState.SetState("Sliding", true);
            }

            public override void OnUpdate()
            {
                if(ctx.animationState.AnimtionFinish("Sliding") >= 0.75f)
                    StateMachine.ChangeState((int) StateType.Crouch);
            }

            public override void OnEnd()
            {
                Debug.Log("end Sliding");
            }
        }


        // state hide 
        private class StateHide : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start hide");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Hide", true);
                ctx.gameObject.tag = "Hide";
            }

            public override void OnUpdate()
            {
                Owner.playerStatus.m_stumina = Mathf.MoveTowards(Owner.playerStatus.GetStumina, 100, Time.deltaTime * 4);

                if(Input.GetKeyDown(KeyCode.F))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end hide");
                ctx.gameObject.tag = "Player";
            }
        }


        // state Attack
        private class StateAttack : StateBase
        {
            StateManager.PlayerController ctx;

            private int phase = 0;        // 攻撃が何段目かのカウント
            private bool comboInput = false;
            private string[] animNames = { "first", "second", "third", "forth" };

            // 入力受付の開始・終了 normalizedTime
            private float comboStart = 0.4f;
            private float comboEnd   = 0.8f;

            public override void OnStart()
            {
                Debug.Log("start attack");

                ctx = Owner.ctx;

                // ステルスアタック
                if(ctx.GetStealthAttackFlag())
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                    return;
                }

                phase = 0;
                comboInput = false;
                ctx.weapon.enabled = true;

                StartAttackPhase();
            }

            public override void OnUpdate()
            {
                string anim = animNames[phase];

                // アニメ終了判定
                if (ctx.animationState.AnimtionFinish(anim) > 1.0f)
                {
                    ctx.AA.EndAttackHit();

                    if (comboInput && phase < animNames.Length - 1)
                    {
                        comboInput = false;
                        phase++;
                        StartAttackPhase();
                        return;
                    }

                    StateMachine.ChangeState((int)StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                ctx.AA.EndAttackHit();
                ctx.weapon.enabled = false;
                Debug.Log("end attack");
            }

            /// <summary>
            /// 各段の開始処理
            /// </summary>
            private void StartAttackPhase()
            {
                string anim = animNames[phase];
                Debug.Log($"Attack Phase: {phase + 1}");

                ctx.AA.StartAttackHit();
                ctx.animationState.SetState(anim, true);
                Owner.playerStatus.m_stumina -= 10.0f;

                WaitForComboInput(anim).Forget();
            }

            /// <summary>
            /// normalizedTime で入力を受け付けるタスク
            /// </summary>
            private async UniTaskVoid WaitForComboInput(string animName)
            {
                comboInput = false;

                // アニメ再生が始まるまで待つ（normalizedTimeが取得できるようになるまで）
                await UniTask.Yield();

                while (true)
                {
                    float t = ctx.animationState.AnimtionFinish(animName);

                    // アニメ終了したら強制終了
                    if (t >= 1f)
                        break;

                    // 入力受付範囲に入ったら攻撃入力を監視
                    if (t >= comboStart && t <= comboEnd)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            comboInput = true;
                            return;
                        }
                    }

                    await UniTask.Yield();
                }
            }
        }


        // state dashAttack
        private class StateDashAttack : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start dashAttack");

                ctx = Owner.ctx;
                // 特殊攻撃判定
                // ステルスアタック
                if(ctx.GetStealthAttackFlag())
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                    return;
                }
                
                // バクスタ
                if(Owner.Backstab())
                {
                    StateMachine.ChangeState((int) StateType.Backstab);
                    return;
                }

                ctx.weapon.enabled = true;
                ctx.AA.StartAttackHit();
                Owner.playerStatus.m_stumina -= 15f;
                ctx.animationState.SetState("DashAttack", true);
            }

            public override void OnUpdate()
            {
                // Idle
                if(ctx.animationState.AnimtionFinish("DashAttack") > 1.01f){
                    ctx.AA.EndAttackHit();
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                ctx.AA.EndAttackHit();
                ctx.weapon.enabled = false;
                Debug.Log("end dashAttack");
            }
        }


        // state stealthAttack
        private class StateStealthAttack : StateBase
        {
            StateManager.PlayerController ctx;

            private CancellationTokenSource cts;
            private bool canAnimationPlay = false;

            public override void OnStart()
            {
                Debug.Log("start stealthAttack");

                ctx = Owner.ctx;
                cts = new CancellationTokenSource();
                PlayCutInAsync(cts.Token).Forget();

                ctx.animationState.SetState("Idle", true);
            }

            private async UniTask PlayCutInAsync(CancellationToken token, float displayDuration = 1f)
            {
                Time.timeScale = 0f;
                UIManager.Instance.Show(UIType.CutIn);

                // カットイン時間待機
                await UniTask.Delay(System.TimeSpan.FromSeconds(displayDuration), ignoreTimeScale: true);

                UIManager.Instance.Hide(UIType.CutIn);
                Time.timeScale = 1f;

                // プレイヤーを透明に
                SkinnedMeshRenderer[] s_meshRenderers = 
                    ctx.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer renderer in s_meshRenderers)
                    renderer.enabled = false;
                
                // プレイヤーをエネミーの座標へワープ
                GameObject target = ctx.GetStealthAttackTarget();
                ctx.transform.position = target.transform.position;
                ctx.rb.velocity = Vector3.zero;

                // エフェクトの生成
                Object.Instantiate(ctx.stealthAttackEffect, target.transform.position, Quaternion.identity);
                target.GetComponent<StealthAttackable>().HaveStealthAttack();
                // エフェクト再生時間待機
                await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f), ignoreTimeScale: true);

                foreach (SkinnedMeshRenderer renderer in s_meshRenderers)
                    renderer.enabled = true;

                canAnimationPlay = true;
            }

            public override void OnUpdate()
            {
                if(canAnimationPlay)
                {
                    ctx.animationState.SetState("Land", true);
                    canAnimationPlay = false;
                }

                if(ctx.animationState.AnimtionFinish("Land") > 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                ctx.AA.EndAttackHit();
                Debug.Log("end stealthAttack");
            }
        }


        // state backstab 
        private class StateBackstab : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start backstab");

                ctx = Owner.ctx;
                if(ctx.GetStealthAttackFlag())
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                
                ctx.animationState.SetState("Backstab");
            }

            public override void OnUpdate()
            {
                if(ctx.animationState.AnimtionFinish("Backstab") >= 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end backstab");
            }
        }

        // ダメージが発生した時の体力管理やアニメーション再生用のメソッド
        private class StateDamage : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Damage");

                ctx = Owner.ctx;
                Debug.Log(Owner.playerStatus.GetHp);

                Debug.Log(ctx.damageLayer);
                ctx.damageLayer.SetState("Light_Damage", true);
            }

            public override void OnUpdate()
            {
                // Idle
                if(ctx.damageLayer.AnimtionFinish("Light_Damage") > 0.7f){
                    ctx.AA.EndAttackHit();
                    ctx.damageLayer.SetState("None", true);
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Damage");
            }
        }


        // state parry
        public void NextParryState()
        {
            stateMachine.ChangeState((int) StateType.Parry);
        }
        // パリィ成功時の処理
        private class StateParry : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Parry");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Parry", true);
            }

            public override void OnUpdate()
            {
                if(ctx.animationState.AnimtionFinish("Parry") > 1.0f)
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Parry");
            }
        }


        // state guard
        public void NextGuardState()
        {
            stateMachine.ChangeState((int) StateType.Guard);
        }
        // ガード時の処理
        private class StateGuard : StateBase
        {
            StateManager.PlayerController ctx;

            public override void OnStart()
            {
                Debug.Log("start Guard");

                ctx = Owner.ctx;
                ctx.animationState.SetState("Guard", true);
            }

            public override void OnUpdate()
            {
                Owner.playerStatus.m_stumina -= 0.03f;
                if (Input.GetMouseButtonUp(1))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Guard");
            }
        }


        // state stun
        private class StateStun : StateBase
        {
            StateManager.PlayerController ctx;

            // CancellationTokenSourceはクラスレベルで管理
            private CancellationTokenSource cts;
            private PlayerController playerController;

            // パリィ硬直時間
            private const float STUN_DURATION = 2.5f; 

            public override void OnStart()
            {
                Debug.Log("start stun");
                
                ctx = Owner.ctx;
                // 既存のトークンを破棄し、新しく作成
                cts?.Dispose();
                cts = new CancellationTokenSource();
                
                // アニメーションステートを設定
                ctx.animationState.SetState("Stun", true);
                Owner.playerStatus.m_stun = true;

                // 非同期処理を開始
                WaitStun(cts.Token).Forget();
            }

            private async UniTask WaitStun(CancellationToken token)
            {
                // 待機時間が始まった時、プレイヤーコントローラー側に用意されているフラグを参照してtrueにする
                Debug.Log("スタン開始");

                // 2.5秒程度の待機時間を設ける
                bool isCanceled = await UniTask.Delay(
                    System.TimeSpan.FromSeconds(STUN_DURATION),
                    cancellationToken: token
                ).SuppressCancellationThrow();

                // プレイヤーのフラグを解除
                Debug.Log("追撃された");

                if (isCanceled)
                {
                    // 待機時間中に外部からのキャンセルがあった場合
                    // 硬直 -> ダメージ -> 即座に硬直解除になるらしいので、一旦何もしない
                    Debug.Log("攻撃を受けました");
                }
                else
                {
                    // 待機時間中に何もなかったのであれば（時間切れ）
                    Debug.Log("硬直時間終了。通常戦闘状態に戻ります。");
                    
                    // 通常の状態に戻る
                    Owner.playerStatus.m_stun = false;
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                // 待機時間をリセットする = キャンセル処理を行う
                cts?.Cancel();
                cts?.Dispose();
                cts = null;

                // 今スタンしているかどうか
                Owner.playerStatus.m_stun = false;
                Debug.Log("end stun");
            }
        }


        // state gameover 
        private class StateGameOver : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start gameover");
            }

            public override void OnUpdate()
            {
                Debug.Log("やられてしまった！");
            }

            public override void OnEnd()
            {
                Debug.Log("end gameover");
            }
        }
    }
}