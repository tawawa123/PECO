using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace StateManager
{
    using StateBase = StateMachine<PlayerController>.StateBase;

    public class PlayerController : MonoBehaviour, Damagable
    {
        //プレイヤー移動、回転制御
        private float inputHorizontal;
        private float inputVertical;
        private Vector3 moveForward;
        private Quaternion targetRotation;
        // カメラ回転制御
        private const float RotateSpeed = 900f;
        private const float RotateSpeedLockon = 500f;

        // StateTypeの定義
        private enum StateType
        {
            Idle,
            Move,
            Run,
            Avoid,
            Hide,
            Backstab,
            Stun,
            Attack,
            Damage,
            GameOver,
        }


        private StateMachine<PlayerController> stateMachine; //ステート遷移制御
        private AttackArea AA; //攻撃判定
        private PlayerStatus playerStatus; //登録ステータス
        private AwaitableAnimatorState animationState; //アニメーション遷移制御
        private PlayerLockon playerLo; //ロックオンカメラ制御
        private Rigidbody rb;


        void Start() {
            rb = GetComponent<Rigidbody>();
            playerStatus = GetComponent<PlayerStatus>();
            playerLo = GetComponent<PlayerLockon>();
            animationState = GetComponent<AwaitableAnimatorState>();

            // StateTypeの数だけステートの登録
            stateMachine = new StateMachine<PlayerController>(this);
            stateMachine.Add<StateIdle>((int) StateType.Idle);
            stateMachine.Add<StateMove>((int) StateType.Move);
            stateMachine.Add<StateRun>((int) StateType.Run);
            stateMachine.Add<StateAvoid>((int) StateType.Avoid);
            stateMachine.Add<StateHide>((int) StateType.Hide);
            stateMachine.Add<StateBackstab>((int) StateType.Backstab);
            stateMachine.Add<StateStun>((int) StateType.Stun);
            stateMachine.Add<StateAttack>((int) StateType.Attack);
            stateMachine.Add<StateDamage>((int) StateType.Damage);
            stateMachine.Add<StateGameOver>((int) StateType.GameOver);

            stateMachine.OnStart((int) StateType.Idle);

            AA = this.GetComponentInChildren<AttackArea>();
            AA.SetAttackArea();
        }
        
        void Update() {
            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");

            if(playerStatus.GetHp <= 0)
                stateMachine.ChangeState((int) StateType.GameOver);

            if (moveForward != Vector3.zero)
            {
                if (playerLo.isLockon)
                {
                    // ロックオン中はターゲットを向き続ける
                    Quaternion from = transform.rotation;
                    var dir = playerLo.GetLockonCameraLookAtTransform().position - transform.position;
                    dir.y = 0;
                    Quaternion to = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeedLockon * Time.deltaTime);
                }
                else
                {
                    Quaternion from = transform.rotation;
                    Quaternion to = Quaternion.LookRotation(moveForward);
                    transform.rotation = Quaternion.RotateTowards(from, to, RotateSpeed * Time.deltaTime);
                }
            }

            stateMachine.OnUpdate();
        }

        public bool Backstab(){
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
                closest.GetComponent<EnemyStatus>().m_backstabed = true;
            }

            return backstab;
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
            }

            public override void OnUpdate()
            {
                // Move
                if(Mathf.Abs(Owner.inputHorizontal) >= 0.1f || Mathf.Abs(Owner.inputVertical) >= 0.1f){
                    Owner.animationState.SetState("Run", true);
                    StateMachine.ChangeState((int) StateType.Move);
                }

                // Avoid
                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    Owner.animationState.SetState("Rolling");
                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                // Attack or Backstab
                if (Input.GetMouseButtonDown(0)){
                    if(Owner.Backstab()){
                        Owner.animationState.SetState("Backstab");
                        StateMachine.ChangeState((int) StateType.Backstab);
                    } else{
                        Owner.animationState.SetState("Jab");
                        StateMachine.ChangeState((int) StateType.Attack);
                    }
                }
            }

            public override void OnEnd()
            {
                Debug.Log("end Idle");
            }
        }


        // move state 
        private class StateMove : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start move");
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
                    Owner.animationState.SetState("Rolling");
                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                // Attack or Backstab
                if (Input.GetMouseButtonDown(0)){
                    if(Owner.Backstab()){
                        Owner.animationState.SetState("Backstab");
                        StateMachine.ChangeState((int) StateType.Backstab);
                    } else{
                        Owner.animationState.SetState("Jab");
                        StateMachine.ChangeState((int) StateType.Attack);
                    }
                }
            }

            public override void OnEnd()
            {
                Owner.rb.velocity = Vector3.zero;
                Debug.Log("end move");
            }
        }


        // avoid state 
        private class StateAvoid : StateBase
        {
            private bool once;

            public override void OnStart()
            {
                Debug.Log("start avoid");
            }

            public override void OnUpdate()
            {
                if(once){
                    if(Owner.rb.velocity.magnitude >= 0.1f){
                        Owner.rb.AddForce(Owner.transform.forward * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
                    } else {
                        Owner.rb.AddForce(Owner.transform.forward * Owner.playerStatus.GetAvoidPower, ForceMode.Impulse);
                    }
                    once = false;
                }
                
                // アニメーションが終了した時にIdleに遷移
                if(Owner.animationState.AnimtionFinish("Rolling") >= 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end avoid");
            }
        }


        // run state 
        private class StateRun : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start run");
            }

            public override void OnUpdate()
            {

            }

            public override void OnEnd()
            {
                Debug.Log("end run");
            }
        }


        // state hide 
        private class StateHide : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start hide");
            }

            public override void OnUpdate()
            {
                
            }

            public override void OnEnd()
            {
                Debug.Log("end hide");
            }
        }


        // state backstab 
        private class StateBackstab : StateBase
        {
            public override void OnStart()
            {
                
                Debug.Log("start backstab");
            }

            public override void OnUpdate()
            {
                Debug.Log("バクスタ判定が出ました！");

                if(Owner.animationState.AnimtionFinish("Backstab") >= 1f)
                    StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end backstab");
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


        // state attack
        private class StateAttack : StateBase
        {
            public override void OnStart()
            {
                Owner.AA.StartAttackHit();
                Debug.Log("start attack");
            }

            public override void OnUpdate()
            {
                // 攻撃アニメーションが終了したらIdleに遷移
                if(Owner.animationState.AnimtionFinish("Jab") > 1.01f){
                    Owner.AA.EndAttackHit();
                    StateMachine.ChangeState((int) StateType.Idle);
                }
            }

            public override void OnEnd()
            {
                //Owner.AA.EndAttackHit();
                Debug.Log("end attack");
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


        // state gameover 
        private class StateGameOver : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start gameover");
            }

            public override void OnUpdate()
            {
                Debug.Log("死んだ！！！！！！！！！！！！");
            }

            public override void OnEnd()
            {
                Debug.Log("end gameover");
            }
        }
    }
}