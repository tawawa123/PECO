using UnityEngine;

namespace Cinemachine
{
    [AddComponentMenu("")] // Hide in menu
    [ExecuteAlways]
    [SaveDuringPlay]
    public class CinemaChineResetAngle : CinemachineExtension
    {
        /// <summary>
        /// field を public にすることで SaveDuringPlay が可能になる
        /// </summary>
        [Header("適用段階")]
        public CinemachineCore.Stage m_ApplyAfter = CinemachineCore.Stage.Aim;

        [SerializeField] private Transform player;   // プレイヤー参照
        [SerializeField] private float resetDuration = 1.0f; // 補間時間

        private bool isResetting = false;
        private float resetTimer = 0f;

        private Quaternion startRotation;
        private Quaternion targetRotation;

        // 外部から呼び出してリセットを開始
        public void ResetCameraBehindPlayer()
        {
            if (player == null) return;

            // カメラをプレイヤー正面に揃える目標回転を計算
            Vector3 forward = player.forward;
            forward.y = 0; // 水平方向に限定
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            targetRotation = Quaternion.LookRotation(forward, Vector3.up);
            startRotation = VirtualCamera.State.FinalOrientation;

            isResetting = true;
            resetTimer = 0f;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (isResetting)
            {
                Debug.Log("aaaaaaaaaaaaaaa");
                resetTimer += deltaTime;
                float t = Mathf.Clamp01(resetTimer / resetDuration);

                // スムーズに回転補間
                Quaternion blended = Quaternion.Slerp(startRotation, targetRotation, t);
                state.OrientationCorrection = blended * Quaternion.Inverse(state.FinalOrientation);

                if (t >= 1f)
                    isResetting = false;
            }
        }
    }
}