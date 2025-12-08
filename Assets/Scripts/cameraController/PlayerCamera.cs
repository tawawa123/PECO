using UnityEngine;
using Cinemachine;

/// <summary>
/// CinemachineVirtualCameraを制御するクラス
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("ロックオンカメラ")]
    [SerializeField] CinemachineVirtualCamera lockonCamera;

    // カメラ優先度
    readonly int LockonCameraActivePriority = 11;
    readonly int LockonCameraInactivePriority = 0;


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