using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(ThirdPersonMove))] // 若无需强制依赖ThirdPersonMove，可删除此特性
public class ThirdPersonCamera : MonoBehaviour
{
    // 仅保留相机核心组件引用（删除所有回正相关变量）
    private CinemachineFreeLook _cinemachineCam;
    private Transform _followTarget;

    private void Start()
    {
        // 移除：右键回正输入事件绑定（OnResetViewBool相关）
    }

    // 简化后：仅保留「设置相机跟随/观察目标」的核心逻辑
    public void SetCameraTarget(GameObject player, GameObject cameraObj)
    {
        if (player == null || cameraObj == null)
        {
            Debug.LogWarning("设置相机目标失败：玩家或相机对象为空");
            return;
        }

        _followTarget = player.transform;
        _cinemachineCam = cameraObj.GetComponent<CinemachineFreeLook>();

        if (_cinemachineCam == null)
        {
            Debug.LogError("相机对象上未找到CinemachineFreeLook组件！");
            return;
        }

        // 仅保留：绑定Cinemachine的跟随和观察目标（核心功能）
        _cinemachineCam.Follow = _followTarget;
        _cinemachineCam.LookAt = _followTarget;
    }

    // 移除：回正相关方法（DoSmoothReset、CheckResetDone、OnRightMouseAction等）
    // 移除：输入禁用/恢复方法（DisableMouseInput、RestoreMouseInput）

    private void OnDestroy()
    {
        // 移除：右键回正事件解绑（OnResetViewBool相关）
    }
}