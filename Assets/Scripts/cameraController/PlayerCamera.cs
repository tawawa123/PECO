using UnityEngine;
using Cinemachine;

/// <summary>
/// CinemachineVirtualCameraを制御するクラス
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("フリーカメラ")]
    [SerializeField] CinemachineVirtualCamera freeLookCamera;
    [Header("ロックオンカメラ")]
    [SerializeField] CinemachineVirtualCamera lockonCamera;
    [Header("プレイヤー")]
    [SerializeField] Transform player;

    [SerializeField] private float resetSpeed = 3f; // カメラリセットのスピード
    public bool resetting = false;
    private Quaternion targetRotation;
    private Transform cameraTarget;

    // カメラ優先度
    readonly int LockonCameraActivePriority = 11;
    readonly int LockonCameraInactivePriority = 0;

    private void LateUpdate()
    {
    }

    /// <summary>
    /// カメラの角度をプレイヤーを基準にリセット
    /// </summary>
    public void ResetFreeLookCamera()
    {
        var test = freeLookCamera.GetComponent<CinemaChineResetAngle>();
        test.ResetCameraBehindPlayer();
        
        cameraTarget = lockonCamera.Follow;
        Vector3 lookDir = player.forward;
        targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
        resetting = true;
    }


    /// <summary>
    /// ロックオン時のVirtualCamera切り替え
    /// </summary>
    /// <param name="target"></param>
    public void ActiveLockonCamera(GameObject target)
    {
        lockonCamera.Priority = LockonCameraActivePriority;
        lockonCamera.LookAt = target.transform;
    }


    /// <summary>
    /// ロックオン解除時のVirtualCamera切り替え
    /// </summary>
    public void InactiveLockonCamera()
    {
        lockonCamera.Priority = LockonCameraInactivePriority;
        lockonCamera.LookAt = null;
    }

    public Transform GetLookAtTransform()
    {
        return lockonCamera.LookAt.transform;
    }
}