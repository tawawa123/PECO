using UnityEngine;
using Cinemachine;
using System.Linq;

/// <summary>
/// CinemachineVirtualCameraを制御するクラス
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    private CinemachineVirtualCamera lockonCamera;

    // カメラ優先度
    private readonly int LockonCameraActivePriority = 11;
    private readonly int LockonCameraInactivePriority = 0;

    public void Awake()
    {
        CinemachineVirtualCamera[] allCameras = FindObjectsOfType<CinemachineVirtualCamera>();
        lockonCamera = allCameras.FirstOrDefault(cam => cam.name == "LockonCamera");
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