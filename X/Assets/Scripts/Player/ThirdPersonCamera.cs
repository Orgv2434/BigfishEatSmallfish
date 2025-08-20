using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class ThirdPersonCamera : NetworkBehaviour
{
    [Header("相机预制体")]
    public GameObject cameraPrefab;

    [Header("基础设置")]
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;

    [Header("鼠标控制")]
    [Range(0.1f, 5f)] public float rotationSpeed = 0.5f;
    [Range(0.5f, 2f)] public float xRotationMultiplier = 1.0f;
    [Range(0.5f, 2f)] public float yRotationMultiplier = 1.0f;
    [Range(0.001f, 0.1f)] public float rotationSmooth = 0.01f;

    [Header("复位设置")]
    public float resetSmoothTime = 0.2f;
    public bool useShortestRotationPath = true;

    // 相机核心变量
    private CinemachineVirtualCamera _playerVcam;
    private const float _inputThreshold = 0.01f;

    // 旋转控制变量
    private float _targetYaw;
    private float _targetPitch;
    private float _currentYaw;
    private float _currentPitch;
    private float _yawVelocity;
    private float _pitchVelocity;
    private float _initialYaw;
    private float _initialPitch;

    // 输入状态
    private Vector2 _lookInput;
    private bool _isRightMouseDown;

    // 初始化标记
    private bool _isCameraInitialized = false;
    // 追踪目标（延迟赋值用）
    private Transform _followTarget;

    // 网络对象就绪标记
    private bool _isNetworkObjectReady = false;


    #region 网络生命周期
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isNetworkObjectReady = true;

        // 仅本地玩家执行相机初始化
        if (IsLocalPlayer())
        {
            StartCoroutine(InitializeCameraCoroutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // 释放鼠标锁定
        if (IsLocalPlayer())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    #endregion


    #region 初始化逻辑
    /// <summary>
    /// 协程：延迟初始化相机（确保 followTarget 正确赋值）
    /// </summary>
    private IEnumerator InitializeCameraCoroutine()
    {
        // 等待 2 帧确保 Transform 稳定（处理网络同步延迟）
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // 强制获取追踪目标（Host/Client 统一逻辑）
        _followTarget = GetReliableFollowTarget();

        // 检查相机预制体
        if (cameraPrefab == null)
        {
            Debug.LogError($"[{name}] 未设置相机预制体！");
            yield break;
        }

        // 实例化相机
        GameObject cameraInstance = Instantiate(cameraPrefab, transform.position, Quaternion.identity);
        cameraInstance.transform.SetParent(transform); // 设为玩家子物体

        // 获取 Cinemachine 组件
        _playerVcam = cameraInstance.GetComponent<CinemachineVirtualCamera>();
        if (_playerVcam == null)
        {
            _playerVcam = cameraInstance.GetComponentInChildren<CinemachineVirtualCamera>();
        }

        // 绑定追踪目标
        if (_playerVcam != null && _followTarget != null)
        {
            _playerVcam.Follow = _followTarget;
            _playerVcam.LookAt = _followTarget;

            // 初始化角度
            Vector3 initialRotation = _followTarget.rotation.eulerAngles;
            _initialYaw = initialRotation.y;
            _initialPitch = initialRotation.x;
            _targetYaw = _initialYaw;
            _targetPitch = _initialPitch;
            _currentYaw = _initialYaw;
            _currentPitch = _initialPitch;

            // 锁定鼠标
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _isCameraInitialized = true;
            Debug.Log($"[{name}] 相机初始化完成（{(IsServer ? "Host" : "Client")}）");
        }
        else
        {
            Debug.LogError($"[{name}] 相机初始化失败！");
        }
    }

    /// <summary>
    /// 强制获取追踪目标（兼容 Host/Client）
    /// </summary>
    private Transform GetReliableFollowTarget()
    {
        // 优先取第二个子物体
        if (transform.childCount >= 2)
        {
            return transform.GetChild(1);
        }
        // 降级取根对象
        Debug.LogWarning($"[{name}] 子物体不足 2 个，追踪目标降级为根对象");
        return transform;
    }
    #endregion


    #region 帧更新逻辑
    private void Update()
    {
        // 未就绪或未初始化则跳过
        if (!_isNetworkObjectReady || !_isCameraInitialized) return;

        // 仅本地玩家执行控制逻辑
        if (IsLocalPlayer())
        {
            HandleCameraRotation();
        }
    }

    /// <summary>
    /// 处理相机旋转（鼠标输入 + 平滑过渡）
    /// </summary>
    private void HandleCameraRotation()
    {
        bool isResetting = _isRightMouseDown ||
                           Mathf.Abs(_currentYaw - _targetYaw) > 0.1f ||
                           Mathf.Abs(_currentPitch - _targetPitch) > 0.1f;

        // 处理鼠标输入（有有效输入且未按右键）
        if (_lookInput.sqrMagnitude >= _inputThreshold && !_isRightMouseDown)
        {
            float deltaYaw = _lookInput.x * rotationSpeed * xRotationMultiplier * Time.deltaTime * 100f;
            float deltaPitch = -_lookInput.y * rotationSpeed * yRotationMultiplier * Time.deltaTime * 100f;

            _targetYaw += deltaYaw;
            _targetPitch = Mathf.Clamp(_targetPitch + deltaPitch, BottomClamp, TopClamp);
        }

        // 右键复位逻辑
        if (_isRightMouseDown)
        {
            float targetResetYaw = GetTargetResetYaw();
            if (useShortestRotationPath)
            {
                _targetYaw = CalculateShortestRotationYaw(_targetYaw, targetResetYaw);
            }
            else
            {
                _targetYaw = targetResetYaw;
            }
            _targetPitch = _initialPitch;
        }

        // 平滑过渡
        float smoothTime = isResetting ? resetSmoothTime : rotationSmooth;
        _currentYaw = Mathf.SmoothDamp(_currentYaw, _targetYaw, ref _yawVelocity, smoothTime);
        _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, smoothTime);

        // 应用旋转到追踪目标
        if (_followTarget != null)
        {
            _followTarget.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0.0f);
        }
    }
    #endregion


    #region 辅助方法
    /// <summary>
    /// 判断是否为本地玩家（Host/Client 统一逻辑）
    /// </summary>
    private new bool IsLocalPlayer()
    {
        return IsOwner;
    }

    /// <summary>
    /// 获取复位用的 Yaw 角度
    /// </summary>
    private float GetTargetResetYaw()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        return Quaternion.LookRotation(forward).eulerAngles.y;
    }

    /// <summary>
    /// 计算最短旋转路径
    /// </summary>
    private float CalculateShortestRotationYaw(float currentYaw, float targetYaw)
    {
        float diff = targetYaw - currentYaw;
        if (diff > 180) diff -= 360;
        else if (diff < -180) diff += 360;
        return currentYaw + diff;
    }
    #endregion


    #region 输入回调
    public void OnLook(InputValue value)
    {
        if (IsLocalPlayer())
        {
            _lookInput = value.Get<Vector2>();
        }
    }

    public void OnMove(InputValue value)
    {
        if (IsLocalPlayer())
        {
            _isRightMouseDown = value.isPressed;
        }
    }
    #endregion
}