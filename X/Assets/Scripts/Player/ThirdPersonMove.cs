using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonMove : NetworkBehaviour
{
    [Header("控制设置")]
    public double rotateSpeed = 90.0;         // 旋转速度（度/秒，绕 Y 轴）
    public double verticalSpeed = 4.0;        // Y轴移动速度（W/S 控制）
    public double moveSpeed = 5.0;            // 前进速度（沿自身 Z 轴）
    public double sprintMultiplier = 3.0;     // 冲刺速度倍数
    public double sprintDuration = 0.5;       // 冲刺持续时间（秒）
    public double sprintCooldown = 5.0;       // 冲刺冷却时间（秒）

    [Header("物理效果")]
    public double buoyancy = 0.5;             // 水中浮力系数（减缓下落）
    public bool useGravity = true;            // 是否启用重力

    [Header("调试设置")]
    public bool showDebugLogs = true;         // 是否输出调试日志
    public bool showDebugRay = true;          // 是否显示前进方向射线
    public float debugRayLength = 2.0f;       // 调试射线长度
    public Color debugRayColor = Color.red;   // 调试射线颜色

    // 输入状态变量
    private Vector2 _rotateInput;             // A/D（X：旋转）、W/S（Y：垂直移动）输入
    private bool _isRightMouseHeld;           // 鼠标右键按住状态（控制前进）
    private bool _isLeftMousePressed;         // 鼠标左键按下状态（触发冲刺）

    // 移动与冲刺状态（使用double提高精度）
    private Vector3 _velocity;                // 物理速度（处理重力）
    private bool _isSprinting;                // 是否处于冲刺中
    private double _sprintTimer;              // 冲刺计时器
    private double _cooldownTimer;            // 冷却计时器
    private bool _isOnCooldown;               // 是否处于冷却中
    [Tooltip("是否学会冲刺")]
    public bool haveDush;                    // 是否有冲刺技能

    // 组件引用（缓存）
    private CharacterController _controller;
    private Transform _transform;             // 缓存Transform组件
    private Vector3 _gravityCache;            // 缓存重力值（减少重复访问）

    void Start()
    {
        // 缓存常用组件和值，减少GetComponent和重复计算
        _transform = transform;
        _controller = GetComponent<CharacterController>();
        _gravityCache = Physics.gravity;

        if (_controller == null)
        {
            Debug.LogError("缺少 CharacterController 组件！请添加后运行。");
        }

    }

    void Update()
    {
        if (_controller == null) return; // 组件缺失时直接退出Update，避免无效计算
        if (!IsOwner)
        {
            return;
        }
        HandleCooldown();       // 处理冲刺冷却
        HandleRotation();       // 处理角色旋转（A/D）
        Vector3 moveDir = CalculateMoveDirection(); // 计算移动方向
        ApplyGravity(ref moveDir); // 应用重力和浮力
        ExecuteMovement(moveDir); // 执行移动
        HandleSprintInput();    // 处理冲刺输入（鼠标左键）

        if (showDebugRay) DrawDebugRay(); // 仅在启用时绘制调试射线

        // 调试鼠标右键状态
        if (showDebugLogs)
        {
            Debug.Log($"右键状态: {_isRightMouseHeld}");
        }
    }

    /// <summary>
    /// 处理冲刺冷却逻辑
    /// </summary>
    private void HandleCooldown()
    {
        if (_isOnCooldown)
        {
            _cooldownTimer += Time.deltaTime;
            if (_cooldownTimer >= sprintCooldown)
            {
                _isOnCooldown = false;
                _cooldownTimer = 0;
            }
        }
    }

    /// <summary>
    /// 处理角色旋转（A/D 键，绕 Y 轴旋转）
    /// </summary>
    private void HandleRotation()
    {
        // 使用阈值判断，减少微小输入导致的无效计算
        if (Mathf.Abs(_rotateInput.x) > 0.1f)
        {
            // 注意：Rotate参数需要float，这里进行显式转换
            float rotateAmount = (float)(_rotateInput.x * rotateSpeed * Time.deltaTime);
            _transform.Rotate(0, rotateAmount, 0);
        }
    }

    /// <summary>
    /// 计算移动方向（沿自身 Z 轴前进）
    /// </summary>
    private Vector3 CalculateMoveDirection()
    {
        Vector3 moveDir = Vector3.zero;

        // 1. 垂直移动（W/S 控制 Y 轴）
        moveDir.y = (float)(_rotateInput.y * verticalSpeed);

        // 2. 沿自身 Z 轴的水平移动（仅在右键按住或冲刺时）
        bool isMovingForward = _isSprinting || _isRightMouseHeld;
        if (isMovingForward)
        {
            Vector3 selfForward = _transform.forward;
            selfForward.y = 0; // 仅保留水平方向
            selfForward.Normalize();

            double currentSpeed = _isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
            moveDir += selfForward * (float)currentSpeed;
        }

        // 3. 冲刺状态管理
        if (_isSprinting)
        {
            _sprintTimer += Time.deltaTime;
            if (_sprintTimer >= sprintDuration)
            {
                _isSprinting = false;
                _sprintTimer = 0;
                _isOnCooldown = true;
            }
        }

        return moveDir;
    }

    /// <summary>
    /// 应用重力和浮力效果
    /// </summary>
    private void ApplyGravity(ref Vector3 moveDir)
    {
        if (!useGravity) return;

        if (!_controller.isGrounded)
        {
            // 重力计算使用double中间值提高精度
            double velocityY = _velocity.y;
            velocityY += _gravityCache.y * Time.deltaTime;
            if (velocityY < 0)
            {
                velocityY *= buoyancy;
            }
            _velocity.y = (float)velocityY;
            moveDir.y += _velocity.y;
        }
        else
        {
            _velocity.y = -0.5f;
        }
    }

    /// <summary>
    /// 执行移动
    /// </summary>
    private void ExecuteMovement(Vector3 moveDir)
    {
        _controller.Move(moveDir * Time.deltaTime);
    }

    /// <summary>
    /// 处理鼠标左键冲刺输入
    /// </summary>
    public void HandleSprintInput()
    {
        if (_isLeftMousePressed && !_isOnCooldown && !_isSprinting && haveDush)
        {
            _isSprinting = true;
            _sprintTimer = 0;
        }
        _isLeftMousePressed = false;
    }

    /// <summary>
    /// 绘制调试射线
    /// </summary>
    private void DrawDebugRay()
    {
        Vector3 selfForward = _transform.forward;
        selfForward.y = 0;
        selfForward.Normalize();

        Vector3 rayOrigin = _transform.position + Vector3.up * 0.5f;
        Debug.DrawRay(rayOrigin, selfForward * debugRayLength, debugRayColor);
    }

    // 输入回调函数
    void OnRotate(InputValue value) => _rotateInput = value.Get<Vector2>();

    // 处理鼠标右键状态
    void OnMove(InputValue value)
    {
        _isRightMouseHeld = value.isPressed;

        if (showDebugLogs)
        {
            Debug.Log($"OnMove触发 - 输入状态: {value.isPressed}");
        }
    }

    void OnSpeedUp(InputValue value) => _isLeftMousePressed = value.isPressed;

    /// <summary>
    /// 冲刺冷却提示
    /// </summary>
    private void OnGUI()
    {
        if (_isOnCooldown)
        {
            // 冷却时间显示需要转换为float
            float remainingCooldown = (float)Mathf.Ceil((float)(sprintCooldown - _cooldownTimer));
            GUI.Label(new Rect(10, Screen.height - 30, 200, 20), $"冲刺冷却中: {remainingCooldown}s");
        }

        // 显示当前右键状态（UI调试）
        GUI.Label(new Rect(10, Screen.height - 60, 200, 20), $"右键按住: {_isRightMouseHeld}");
    }
}