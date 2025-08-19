using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("基础设置")]
    [Tooltip("跟随的目标")]
    public GameObject CameraTarget;
    [Tooltip("上移动的最大角度（垂直方向上限）")]
    public float TopClamp = 70.0f;
    [Tooltip("下移动的最大角度（垂直方向下限）")]
    public float BottomClamp = -30.0f;

    [Header("鼠标控制（手感优化）")]
    [Tooltip("鼠标灵敏度（数值越大旋转越灵敏）")]
    [Range(0.1f, 5f)] public float rotationSpeed = 0.5f;
    [Tooltip("水平旋转倍率（X轴）")]
    [Range(0.5f, 2f)] public float xRotationMultiplier = 1.0f;
    [Tooltip("垂直旋转倍率（Y轴）")]
    [Range(0.5f, 2f)] public float yRotationMultiplier = 1.0f;
    [Tooltip("旋转平滑系数（越小越灵敏，越大越丝滑）")]
    [Range(0.001f, 0.1f)] public float rotationSmooth = 0.01f; // 新增：旋转平滑参数

    [Header("复位设置")]
    [Tooltip("镜头复位时的平滑过渡时间")]
    public float resetSmoothTime = 0.2f;
    [Tooltip("是否使用最短路径旋转复位")]
    public bool useShortestRotationPath = true;

    // 摄像机核心变量
    private const float _inputThreshold = 0.01f; // 输入死区（过滤微小抖动）
    private GameObject _mainCamera;
    private float _targetYaw;       // 目标水平角度（允许超过360度）
    private float _targetPitch;     // 目标垂直角度
    private float _currentYaw;      // 当前水平角度（带平滑）
    private float _currentPitch;    // 当前垂直角度（带平滑）
    private float _yawVelocity;     // 水平旋转速度缓存
    private float _pitchVelocity;   // 垂直旋转速度缓存

    // 初始角度（复位基准）
    private float _initialYaw;
    private float _initialPitch;

    // 输入状态
    private Vector2 _lookInput;     // 鼠标输入
    private bool _isRightMouseDown; // 右键按下状态

    private void Start()
    {
        // 获取主相机
        Camera cam = Camera.main;
        _mainCamera = cam ? cam.gameObject : GameObject.FindGameObjectWithTag("MainCamera");

        // 记录初始角度（复位基准，保留原始角度值，不归一化）
        Vector3 initialRotation = transform.rotation.eulerAngles;
        _initialYaw = initialRotation.y;
        _initialPitch = initialRotation.x;

        // 初始化角度（允许Yaw累积超过360度）
        _targetYaw = _initialYaw;
        _targetPitch = _initialPitch;
        _currentYaw = _initialYaw;
        _currentPitch = _initialPitch;

        // 锁定鼠标（提升沉浸感，可按需关闭）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCameraRotation();
    }

    private void HandleCameraRotation()
    {
        // 标记：是否处于复位状态（右键按下 或 正在平滑复位中）
        bool isResetting = _isRightMouseDown ||
                           Mathf.Abs(_currentYaw - _targetYaw) > 0.1f ||
                           Mathf.Abs(_currentPitch - _targetPitch) > 0.1f;

        // 处理鼠标输入（核心手感优化）
        if (_lookInput.sqrMagnitude >= _inputThreshold && !_isRightMouseDown)
        {
            // 优化1：乘以Time.deltaTime，确保帧率无关，旋转速度稳定
            // 优化2：输入直接累加，允许Yaw超过360度（不做归一化）
            float deltaYaw = _lookInput.x * rotationSpeed * xRotationMultiplier * Time.deltaTime * 100f;
            float deltaPitch = -_lookInput.y * rotationSpeed * yRotationMultiplier * Time.deltaTime * 100f;

            // 水平角度：直接累加，允许超过360度（如360→370→400等）
            _targetYaw += deltaYaw;
            // 垂直角度：累加后限制在上下范围内
            _targetPitch = Mathf.Clamp(_targetPitch + deltaPitch, BottomClamp, TopClamp);
        }

        // 右键复位逻辑
        if (_isRightMouseDown)
        {
            // 水平复位目标：物体正前方（计算时保留角度累积特性）
            float targetResetYaw = GetTargetResetYaw();
            if (useShortestRotationPath)
            {
                // 计算最短路径（考虑角度累积，如从400度到30度，会走-70度而非290度）
                _targetYaw = CalculateShortestRotationYaw(_targetYaw, targetResetYaw);
            }
            else
            {
                _targetYaw = targetResetYaw;
            }

            // 垂直复位到初始角度
            _targetPitch = _initialPitch;
        }

        // 平滑过渡：只要在复位状态（点按/长按），就用 resetSmoothTime
        float smoothTime = isResetting ? resetSmoothTime : rotationSmooth;
        _currentYaw = Mathf.SmoothDamp(_currentYaw, _targetYaw, ref _yawVelocity, smoothTime);
        _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, smoothTime);

        // 应用最终旋转（Yaw允许超过360度，直接使用累积值）
        CameraTarget.transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0.0f);
    }
    // 获取物体正前方的水平角度（保留角度累积特性）
    private float GetTargetResetYaw()
    {
        // 计算物体前方水平方向（忽略Y轴倾斜）
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        // 直接返回原始角度值（不归一化，允许超过360度）
        return Quaternion.LookRotation(forward).eulerAngles.y;
    }

    // 计算最短路径旋转（支持角度累积，如400度到30度的最短路径）
    private float CalculateShortestRotationYaw(float currentYaw, float targetYaw)
    {
        // 计算差值（考虑角度可以无限累积，如currentYaw=400，targetYaw=30，差值为-70而非290）
        float diff = targetYaw - currentYaw;
        // 处理环绕：如果差值绝对值超过180度，取反方向（最短路径）
        if (diff > 180)
            diff -= 360;
        else if (diff < -180)
            diff += 360;
        return currentYaw + diff;
    }

    // 输入回调：鼠标移动
    public void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }

    // 处理鼠标右键输入（保持函数名为OnMove，不修改）
    public void OnMove(InputValue value)
    {
        _isRightMouseDown = value.isPressed;
    }
}