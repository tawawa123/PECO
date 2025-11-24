using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace StateManager
{
    using StateBase = StateMachine<PlayerController>.StateBase;

    public class PlayerController : MonoBehaviour, Damagable
    {
        [SerializeField] private GameObject cutInUI;
        [SerializeField] private GameObject stealthAttackEffect;
        [SerializeField] private MeshRenderer weapon;

        //プレイヤー移動、回転制御
        private float inputHorizontal;
        private float inputVertical;
        private Vector3 moveForward;
        private Quaternion targetRotation;

        // 着地判定フラグ
        private bool isGrounded;

        // ステルス攻撃用フラグ
        private bool canStealthAttack = false;

        // カメラ回転制御
        private const float RotateSpeed = 900f;
        private const float RotateSpeedLockon = 500f;

        // 設置判定の大きさ
        private const float isGroundedSize = 0.1f;

        // StateTypeの定義
        private enum StateType
        {
            Idle,

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

            // 死亡
            GameOver,
        }

        private StateMachine<PlayerController> stateMachine; //ステート遷移制御
        private AttackArea AA; //攻撃判定
        private PlayerStatus playerStatus; //登録ステータス
        private AwaitableAnimatorState animationState; //アニメーション遷移制御
        private OverrideDamageLayer damageLayer;
        private PlayerLockon playerLo; //ロックオンカメラ制御
        private Rigidbody rb; //リジッドボディ

        private GameObject stealthAttackTarget; // ステルスアタックの対象位置


        void Start() 
        {
            rb = GetComponent<Rigidbody>();
            playerStatus = GetComponent<PlayerStatus>();
            playerLo = GetComponent<PlayerLockon>();
            animationState = GetComponent<AwaitableAnimatorState>();
            damageLayer = GetComponent<OverrideDamageLayer>();

            // StateTypeの数だけステートの登録
            stateMachine = new StateMachine<PlayerController>(this);
            stateMachine.Add<StateIdle>((int) StateType.Idle);                      // Idle
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
            stateMachine.Add<StateGameOver>((int) StateType.GameOver);              // 死亡時処理

            AA = this.GetComponentInChildren<AttackArea>();
            AA.SetAttackArea();
            cutInUI.SetActive(false);
            weapon.enabled = false;

            stateMachine.OnStart((int) StateType.Idle);
        }

        void Update() 
        {
            // LockForEnemy();

            // 着地判定
            isGrounded = Physics.CheckSphere(transform.position, isGroundedSize, LayerMask.GetMask("Ground"));

            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");

            if(playerStatus.GetHp <= 0)
                stateMachine.ChangeState((int) StateType.GameOver);

            stateMachine.OnUpdate();
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
                Vector3 diff = go.transform.position - this.transform.position;
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
            
            float Angle = Vector3.Angle(closest.transform.forward, this.transform.forward);
            if(Mathf.Abs(Angle) < 20.0f){
                backstab = true;
                Debug.Log(closest.GetComponent<EnemyStatus>());
                closest.GetComponent<EnemyStatus>().m_backstabed = true;
            }

            return backstab;
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

        // ロックオン中のターゲット注視処理
        public void LockForEnemy()
        {
            // ロックオン中はターゲットを向き続ける
            Quaternion from = transform.rotation;
            var dir = playerLo.GetLockonCameraLookAtTransform().position - transform.position;
            dir.y = 0;
            Quaternion to = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeedLockon * Time.deltaTime);
        }

        // 足元に設置判定を描画
        void OnDrawGizmos()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, isGroundedSize);
        }


        /// <summary>
        /// 以下ステートマシン
        /// StateMachine.StateBaseクラスを継承した各ステート定義用クラスを作成し、動作を記述
        /// StateMachine.ChangeState => 指定したステートに状態遷移
        /// StateMachine.ChangePrevState => ひとつ前のステートに状態遷移
        /// </summary>

        // idle state
        private class StateIdle : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Idle");

                Owner.animationState.SetState("Idle", true);
            }

            public override void OnUpdate()
            {
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
            }

            public override void OnEnd()
            {
                Debug.Log("end Idle");
            }
        }


        // walk state 
        private class StateWalk : StateBase
        {
            public override void OnStart()
            {
                Owner.animationState.SetState("Walk", true);
                Debug.Log("start walk");
            }

            public override void OnUpdate()
            {
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                // 移動方向にスピードを掛ける
                Owner.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed + new Vector3(0, Owner.rb.velocity.y, 0);

                if (Owner.moveForward != Vector3.zero) {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, Owner.targetRotation, Time.deltaTime * Owner.playerStatus.GetRotationRate);
                }

                // Idle
                if(Owner.rb.velocity.magnitude < 0.1f){
                    Owner.animationState.SetState("Idle");
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // Avoid
                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    Quaternion from = Owner.transform.rotation;
                    Quaternion to = Quaternion.LookRotation(Owner.moveForward);
                    Owner.transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeed);

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
            public override void OnStart()
            {
                Owner.animationState.SetState("Run", true);
                Debug.Log("start run");
            }

            public override void OnUpdate()
            {
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                // 移動方向にスピードを掛ける
                Owner.rb.velocity = Owner.moveForward * Owner.playerStatus.GetRunSpeed + new Vector3(0, Owner.rb.velocity.y, 0);
                
                // 向いている方向に回転
                if (Owner.moveForward != Vector3.zero) {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, Owner.targetRotation, Time.deltaTime * Owner.playerStatus.GetRotationRate);
                }

                // Idle
                if(Owner.rb.velocity.magnitude < 0.1f){
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // Avoid
                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    Quaternion from = Owner.transform.rotation;
                    Quaternion to = Quaternion.LookRotation(Owner.moveForward);
                    Owner.transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeed);

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
            }

            public override void OnEnd()
            {
                Debug.Log("end run");
            }
        }


        // state Jump
        private class StateJump : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Jump");

                Owner.animationState.SetState("Jump", true);
                Owner.rb.AddForce(Owner.transform.up * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
            }

            public override void OnUpdate()
            {
                // 方向制御
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                Owner.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed + new Vector3(0, Owner.rb.velocity.y, 0);

                // 着地
                if(Owner.animationState.AnimtionFinish("Jump") >= 0.5f && Owner.isGrounded)
                {
                    StateMachine.ChangePrevState();
                } else if (Owner.animationState.AnimtionFinish("Jump") >= 1f && !Owner.isGrounded) {
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
            public override void OnStart()
            {
                Debug.Log("start Fall");

                Owner.animationState.SetState("Fall", true);
            }

            public override void OnUpdate()
            {
                // 方向制御
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                Owner.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed / 2 + new Vector3(0, Owner.rb.velocity.y, 0);

                // 着地
                if(Owner.isGrounded)
                {
                    Owner.animationState.SetState("Land");
                }

                // idle
                if(Owner.animationState.AnimtionFinish("Land") >= 1f)
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
            public override void OnStart()
            {
                Debug.Log("start avoid");

                Owner.animationState.SetState("Rolling");
                Owner.rb.AddForce(Owner.transform.forward * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
                Owner.gameObject.layer = LayerMask.NameToLayer("Avoid");
            }

            public override void OnUpdate()
            {   
                // アニメーション終了時の処理
                if(Owner.animationState.AnimtionFinish("Rolling") >= 0.6f && Input.GetKey(KeyCode.LeftShift))
                {
                    StateMachine.ChangeState((int) StateType.Run);
                } 
                else if(Owner.animationState.AnimtionFinish("Rolling") >= 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Owner.gameObject.layer = LayerMask.NameToLayer("PlayerHit");
                Debug.Log("end avoid");
            }
        }


        // state crouch
        private class StateCrouch : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Crouch");

                Owner.animationState.SetState("Crouch", true);
                Owner.gameObject.tag = "Hide";
            }

            public override void OnUpdate()
            {
                // idle 
                if(Input.GetKeyDown(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // courchWalk
                if(Mathf.Abs(Owner.inputHorizontal) >= 0.1f || Mathf.Abs(Owner.inputVertical) >= 0.1f)
                {
                    StateMachine.ChangeState((int) StateType.CrouchWalk);
                }

                // stealthAttack
                if (Input.GetMouseButtonDown(0) && Owner.canStealthAttack)
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Crouch");
                Owner.gameObject.tag = "Player";
            }
        }


        // CrouchWalk state 
        private class StateCrouchWalk : StateBase
        {
            public override void OnStart()
            {
                Owner.animationState.SetState("CrouchWalk", true);
                Debug.Log("start CrouchWalk");

                Owner.gameObject.tag = "Hide";
            }

            public override void OnUpdate()
            {
                Vector3 cameraForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                Owner.moveForward = cameraForward * Owner.inputVertical + Camera.main.transform.right * Owner.inputHorizontal;
                // 移動方向にスピードを掛ける
                Owner.rb.velocity = Owner.moveForward * Owner.playerStatus.GetWalkSpeed / 2 + new Vector3(0, Owner.rb.velocity.y, 0);

                if (Owner.moveForward != Vector3.zero) {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, Owner.targetRotation, Time.deltaTime * Owner.playerStatus.GetRotationRate);
                }

                // crouch
                if(Owner.rb.velocity.magnitude < 0.1f){
                    StateMachine.ChangeState((int) StateType.Crouch);
                }

                // walk
                if(Input.GetKeyDown(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Walk);
                }

                // idle
                if(Input.GetKeyDown(KeyCode.LeftControl))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                // stealthAttack
                if (Input.GetMouseButtonDown(0) && Owner.canStealthAttack)
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                }
            }

            public override void OnEnd()
            {
                Owner.rb.velocity = Vector3.zero;
                Owner.gameObject.tag = "Player";
                Debug.Log("end CrouchWalk");
            }
        }


        // state sliding 
        private class StateSliding : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start sliding");

                Owner.rb.AddForce(Owner.transform.forward * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
                Owner.animationState.SetState("Sliding", true);
            }

            public override void OnUpdate()
            {
                if(Owner.animationState.AnimtionFinish("Sliding") >= 0.75f)
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
            public override void OnStart()
            {
                Debug.Log("start hide");

                Owner.animationState.SetState("Hide", true);
                Owner.gameObject.tag = "Hide";
            }

            public override void OnUpdate()
            {
                if(Input.GetKeyDown(KeyCode.F))
                {
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end hide");
                Owner.gameObject.tag = "Player";
            }
        }


        // state Attack
        private class StateAttack : StateBase
        {
            private int phase = 0;        // 攻撃が何段目かのカウント
            private bool comboInput = false;
            private string[] animNames = { "first", "second", "third", "forth" };

            // 入力受付の開始・終了 normalizedTime
            private float comboStart = 0.4f;
            private float comboEnd   = 0.8f;

            public override void OnStart()
            {
                Debug.Log("start attack");

                // ステルスアタック
                if(Owner.canStealthAttack)
                {
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                    return;
                }

                phase = 0;
                comboInput = false;
                Owner.weapon.enabled = true;

                StartAttackPhase();
            }

            public override void OnUpdate()
            {
                string anim = animNames[phase];

                // アニメ終了判定
                if (Owner.animationState.AnimtionFinish(anim) > 1.0f)
                {
                    Owner.AA.EndAttackHit();

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
                Owner.AA.EndAttackHit();
                Owner.weapon.enabled = false;
                Debug.Log("end attack");
            }

            /// <summary>
            /// 各段の開始処理
            /// </summary>
            private void StartAttackPhase()
            {
                string anim = animNames[phase];
                Debug.Log($"Attack Phase: {phase + 1}");

                Owner.AA.StartAttackHit();
                Owner.animationState.SetState(anim, true);

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
                    float t = Owner.animationState.AnimtionFinish(animName);

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
            public override void OnStart()
            {
                Debug.Log("start dashAttack");

                // 特殊攻撃判定

                // ステルスアタック
                if(Owner.canStealthAttack)
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

                Owner.weapon.enabled = true;
                Owner.AA.StartAttackHit();
                Owner.animationState.SetState("DashAttack", true);
            }

            public override void OnUpdate()
            {
                // Idle
                if(Owner.animationState.AnimtionFinish("DashAttack") > 1.01f){
                    Owner.AA.EndAttackHit();
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Owner.AA.EndAttackHit();
                Owner.weapon.enabled = false;
                Debug.Log("end dashAttack");
            }
        }


        // state stealthAttack
        private class StateStealthAttack : StateBase
        {
            private CancellationTokenSource cts;
            private bool canAnimationPlay = false;

            public override void OnStart()
            {
                Debug.Log("start stealthAttack");

                cts = new CancellationTokenSource();
                PlayCutInAsync(cts.Token).Forget();

                Owner.animationState.SetState("Idle", true);
            }

            private async UniTask PlayCutInAsync(CancellationToken token, float displayDuration = 1f)
            {
                Time.timeScale = 0f;
                Owner.cutInUI.SetActive(true);

                // カットイン時間待機
                await UniTask.Delay(System.TimeSpan.FromSeconds(displayDuration), ignoreTimeScale: true);

                Owner.cutInUI.SetActive(false);
                Time.timeScale = 1f;

                // プレイヤーを透明に
                SkinnedMeshRenderer[] s_meshRenderers = 
                    Owner.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer renderer in s_meshRenderers)
                    renderer.enabled = false;
                // プレイヤーをエネミーの座標へワープ
                Owner.transform.position = Owner.stealthAttackTarget.transform.position;
                Owner.rb.velocity = Vector3.zero;

                // エフェクトの生成
                Instantiate(Owner.stealthAttackEffect, Owner.stealthAttackTarget.transform.position, Quaternion.identity);
                Owner.stealthAttackTarget.GetComponent<StealthAttackable>().HaveStealthAttack();
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
                    Owner.animationState.SetState("Land", true);
                    canAnimationPlay = false;
                }

                if(Owner.animationState.AnimtionFinish("Land") > 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Owner.AA.EndAttackHit();
                Debug.Log("end stealthAttack");
            }
        }


        // state backstab 
        private class StateBackstab : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start backstab");

                if(Owner.canStealthAttack)
                    StateMachine.ChangeState((int) StateType.StealthAttack);
                
                Owner.animationState.SetState("Backstab");
            }

            public override void OnUpdate()
            {
                if(Owner.animationState.AnimtionFinish("Backstab") >= 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end backstab");
            }
        }


        // ダメージ処理用インターフェイス
        public void AddDamage(int damage){
            // playerStatus.m_hp -= damage;
            stateMachine.ChangeState((int) StateType.Damage);
        }
        // ダメージが発生した時の体力管理やアニメーション再生用のメソッド
        private class StateDamage : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start Damage");
                Debug.Log(Owner.playerStatus.GetHp);
                Owner.damageLayer.SetState("Light_Damage", true);
            }

            public override void OnUpdate()
            {
                // Idle
                if(Owner.damageLayer.AnimtionFinish("Light_Damage") > 0.7f){
                    Owner.AA.EndAttackHit();
                    Owner.damageLayer.SetState("None", true);
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Damage");
            }
        }


        // state stun 
        private class StateStun : StateBase
        {
            public override void OnStart()
            {
                
                Debug.Log("start stun");
            }

            public override void OnUpdate()
            {

            }

            public override void OnEnd()
            {
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