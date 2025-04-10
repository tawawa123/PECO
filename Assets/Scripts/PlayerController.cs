using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace StateManager
{
    using StateBase = StateMachine<PlayerController>.StateBase;

    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float RotationRate = 6.0f;
        [SerializeField] private float avoidPower = 5.0f;

        //移動、回転制御の変数
        private float inputHorizontal;
        private float inputVertical;
        private Vector3 moveForward;
        private Quaternion targetRotation;

        private const float RotateSpeed = 900f;
        private const float RotateSpeedLockon = 500f;

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
            GameOver,
        }


        // 各メソッドの呼び出し
        private StateMachine<PlayerController> stateMachine;
        private AttackArea AA;
        private PlayerStatus pStatus;
        private Rigidbody rb;
        private Animator animator;
        private PlayerLockon playerLo;


        void Start() {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            pStatus = GetComponent<PlayerStatus>();
            playerLo = GetComponent<PlayerLockon>();

            // ステートの登録
            stateMachine = new StateMachine<PlayerController>(this);
            stateMachine.Add<StateIdle>((int) StateType.Idle);
            stateMachine.Add<StateMove>((int) StateType.Move);
            stateMachine.Add<StateRun>((int) StateType.Run);
            stateMachine.Add<StateAvoid>((int) StateType.Avoid);
            stateMachine.Add<StateHide>((int) StateType.Hide);
            stateMachine.Add<StateBackstab>((int) StateType.Backstab);
            stateMachine.Add<StateStun>((int) StateType.Stun);
            stateMachine.Add<StateAttack>((int) StateType.Attack);
            stateMachine.Add<StateGameOver>((int) StateType.GameOver);

            stateMachine.OnStart((int) StateType.Idle);

            AA = this.GetComponentInChildren<AttackArea>();
            AA.SetAttackArea();
        }
        
        void Update() {
            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");

            if(pStatus.Hp <= 0)
                stateMachine.ChangeState((int) StateType.GameOver);

            if (moveForward != Vector3.zero)
            {
                if (playerLo.isLockon)
                {
                    // ロックオンかつ非Run時はターゲットに向き続ける
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
            float distance = Mathf.Infinity;

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

            if(closest = null){
                return backstab;
            }
            
            float Angle = Vector3.Angle(closest.transform.forward, this.transform.forward);
            if(Mathf.Abs(Angle) > 10.0f){
                backstab = true;
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
                if(Mathf.Abs(Owner.inputHorizontal) >= 0.1f || Mathf.Abs(Owner.inputVertical) >= 0.1f){
                    Owner.animator.SetBool("Run", true);
                    StateMachine.ChangeState((int) StateType.Move);
                }

                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                if (Input.GetMouseButtonDown(0)){
                    StateMachine.ChangeState((int) StateType.Attack);
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
                Owner.rb.velocity = Owner.moveForward * Owner.moveSpeed + new Vector3(0, Owner.rb.velocity.y, 0);
                if (Owner.moveForward != Vector3.zero) {
                    Owner.targetRotation = Quaternion.LookRotation(Owner.moveForward);
                    Owner.transform.rotation = Quaternion.Slerp(Owner.transform.rotation, Owner.targetRotation, Time.deltaTime * Owner.RotationRate);
                }

                if(Owner.rb.velocity.magnitude < 0.1f){
                    StateMachine.ChangeState((int) StateType.Idle);
                }

                if(Input.GetKeyDown(KeyCode.LeftShift)){
                    StateMachine.ChangeState((int) StateType.Avoid);
                }

                if (Input.GetMouseButtonDown(0)){
                    StateMachine.ChangeState((int) StateType.Attack);
                }
            }

            public override void OnEnd()
            {
                Owner.animator.SetBool("Run", false);
                Debug.Log("end move");
            }
        }


        // avoid state 
        private class StateAvoid : StateBase
        {
            public override void OnStart()
            {
                Debug.Log("start avoid");
            }

            public override void OnUpdate()
            {
                //Owner.rb.AddForce(Owner.moveForward * Owner.avoidPower, ForceMode.Impulse);
                //DelayAsync();
                if(Owner.rb.velocity.magnitude >= 0.1f){
                    Owner.rb.AddForce(Owner.moveForward * Owner.avoidPower * 10, ForceMode.Impulse);
                } else {
                    Owner.rb.AddForce(Owner.transform.forward * Owner.avoidPower, ForceMode.Impulse);
                }
                StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end avoid");
            }

            private async void DelayAsync()
            {
                //0.2秒間回避時間
                await Task.Delay(200);
                StateMachine.ChangeState((int) StateType.Idle);
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
                Debug.Log("start attack");
            }

            public override void OnUpdate()
            {
                Owner.AA.StartAttackHit();
                DelayAsync();
                StateMachine.ChangeState((int) StateType.Idle);
            }

            public override void OnEnd()
            {
                Debug.Log("end attack");
            }

            private async void DelayAsync()
            {
                //0.2秒間回避時間
                await Task.Delay(200);
                
                StateMachine.ChangeState((int) StateType.Idle);
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