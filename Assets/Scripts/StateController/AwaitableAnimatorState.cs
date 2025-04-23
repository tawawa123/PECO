using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AwaitableAnimatorState : MonoBehaviour
{
    private void Start()
    {
        _animator = GetComponent<Animator>();
        AnimationStateLoop().Forget();
    }

    private Animator _animator;
    private bool loop = false;
    public const string StateDefault = "Idle";

    [SerializeField] private string State = StateDefault;
    public float DurationTimeSecond;

    private async UniTaskVoid AnimationStateLoop()
    {
        var token = this.GetCancellationTokenOnDestroy();
        var hashDefault = Animator.StringToHash(StateDefault);

        while (true)
        {
            // State更新のためUpdate分だけ待つ
            await UniTask.Yield();
            if (token.IsCancellationRequested)
            {
                break;
            }

            var hashExpect = Animator.StringToHash(State);
            var currentState = _animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.shortNameHash != hashExpect)
            {
                // DurationTimeSecondの間隔を挟んでAnimatorのStateを切り替える
                _animator.CrossFadeInFixedTime(hashExpect, DurationTimeSecond);
                // 切り替えている間のcurrentStateは切り替える前のStateが出てくる.
                // そのためDurationTimeSecondが過ぎるまで待つ
                await UniTask.Delay(TimeSpan.FromSeconds(DurationTimeSecond), cancellationToken: token);
                continue;
            }

            // stateが終了していた場合はdefaultに戻す
            if (currentState.shortNameHash != hashDefault && currentState.normalizedTime >= 1f && !loop)
            {
                SetState(StateDefault);
            }
        }
    }

    public void SetState(string nextState, bool loop = false, float DurationTimeSecond = 0.1f)
    {
        this.loop = loop;
        this.DurationTimeSecond = DurationTimeSecond;
        if (_animator.HasState(0, Animator.StringToHash(nextState)))
        {
            // 存在するStateだけ受け入れる
            State = nextState;
        }
    }

    public float AnimtionFinish(string animationName)
    {
        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            return 0;
        return _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
}