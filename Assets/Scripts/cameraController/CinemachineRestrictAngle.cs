using UnityEngine;

namespace Cinemachine
{
    [AddComponentMenu("")] // Hide in menu
    [ExecuteAlways]
    [SaveDuringPlay]
    public class CinemachineRestrictAngle : CinemachineExtension
    {
        /// <summary>
        /// 上下方向のアングルを固定
        /// ロックオン時にカメラが必要以上に頭上へ移動するような挙動の抑制
        /// 視認性向上のために導入
        /// </summary>
        
        // field を public にすることで SaveDuringPlay が可能になる
        [Header("適用段階")]
        public CinemachineCore.Stage m_ApplyAfter = CinemachineCore.Stage.Aim;

        [Header("俯瞰角 閾値")]
        [Range(0f, 90f)]
        public float lowAngleThreshold = 80f;

        [Header("アオリ角 閾値")]
        [Range(0f, 90f)]
        public float highAngleThreshold = 80f;

        /// <summary>
        /// カメラパラメータ更新後に呼び出される Callback。ここで結果を微調整する。
        /// </summary>
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != m_ApplyAfter) return;

            // カメラの X 軸回転を制限する
            var eulerAngles = state.RawOrientation.eulerAngles;
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -highAngleThreshold, lowAngleThreshold);
            state.RawOrientation = Quaternion.Euler(eulerAngles);
        }
    }
}