using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ThirdPersonMove : MonoBehaviour
{
    [Header("控制设置")]
    public float rotateSpeed = 90.0f;         // 旋转速度（度/秒，绕 Y 轴）
    public float ascendSpeed = 4.0f;          // 上浮速度（Y轴正方向）
    public float descendSpeed = 4.0f;         // 下潜速度（Y轴负方向）
    public float moveSpeed = 5.0f;            // 前进速度（沿自身 Z 轴）
    public double sprintMultiplier = 3.0;     // 冲刺速度倍数
    public double sprintDuration = 0.5;       // 冲刺持续时间（秒）
    public double sprintCooldown = 5.0;       // 冷却时间（秒）

    [Header("物理效果")]
    public double buoyancy = 0.5;             // 水中浮力系数（减缓下落）
    public bool useGravity = true;            // 是否启用重力

    [Header("调试设置")]
    public bool showDebugLogs = true;         // 是否输出调试日志
    public bool showDebugRay = true;          // 是否显示前进方向射线
    public float debugRayLength = 2.0f;       // 调试射线长度
    public Color debugRayColor = Color.red;   // 调试射线颜色

    [Header("冲刺冷却UI")]
    [Tooltip("是否学会冲刺")]
    public bool haveDush;                    // 是否有冲刺技能
    public Slider DushSlider;                // 手动赋值的Slider（可选）
    public string sliderName = "DushCd";     // Slider物体名称（自动查找用）
    public string uiRootName = "InGameUI";   // UI根节点名称（自动查找用）
    public Color cooldownColor = Color.yellow; // 冷却中填充色
    public Color readyColor = Color.blue;    // 就绪状态填充色

    // 私有变量
    private Vector2 _moveInput;               // 移动输入（X：转向/左右，Y：前后）
    private bool _isSprintingInput;           // 冲刺输入状态
    private bool _isAscending;                // 上浮状态（按住=true）
    private bool _isDescending;               // 下潜状态（按住=true）
    private bool _isSprinting;                // 是否处于冲刺中
    private double _cooldownTimer;            // 冷却计时器
    private bool _isOnCooldown;               // 是否处于冷却中
    private float _currentMoveSpeed;          // 当前移动速度（受体型影响）
    private float _currentRotateSpeed;        // 当前转向速度（受体型影响）
    private Image _dushFillImage;             // 冲刺条填充Image缓存
    private bool _isSliderInitialized = false;// Slider是否初始化成功

    // 组件引用（缓存）
    private CharacterController _controller;
    private Transform _transform;             // 缓存Transform组件
    private Vector3 _gravityCache;            // 缓存重力值

    private void Start()
    {
        // 缓存组件
        _transform = transform;
        _controller = GetComponent<CharacterController>();
        _gravityCache = Physics.gravity;

        // 初始化速度
        _currentMoveSpeed = moveSpeed;
        _currentRotateSpeed = rotateSpeed;

        // 检查必要组件
        if (_controller == null)
        {
            Debug.LogError("缺少 CharacterController 组件！请添加后运行。");
        }

        // 绑定输入事件
        BindInputEvents();
    }

    private void Update()
    {
        if (FishGameFlowManager.Instance.CurrentState != FishGameFlowManager.GameState.GamePlaying)
            return;
        if (_controller == null) return;

        // 移除DushSlider != null判断，确保冷却逻辑始终执行（即使Slider未初始化也不影响移动）
        HandleCooldown();

        Vector3 moveDir = CalculateMoveDirection();
        ExecuteMovement(moveDir);
        HandleSprintInput();
        HandleVerticalMovement();  // 驱动垂直移动
        if (showDebugLogs) DrawDebugRay();
    }
    
    public void InitDushSlider()
    {
        StartCoroutine(InitDushSliderCoroutine());
    }
    /// <summary>
    /// 延迟初始化Slider（解决UI动态加载问题）
    /// </summary>
    private IEnumerator InitDushSliderCoroutine()
    {
        yield return null; // 延迟1帧，确保UI根节点已加载

        // 步骤1：优先使用手动赋值的Slider，没有则自动查找
        if (DushSlider == null)
        {
            DushSlider = AutoFindSlider();
            if (DushSlider == null)
            {
                Debug.LogError($"未找到冲刺冷却Slider！请检查：1.UI根节点名称是否为{uiRootName} 2.Slider名称是否为{sliderName} 3.Slider是否挂载Slider组件");
                _isSliderInitialized = false;
                yield break;
            }
        }

        // 步骤2：激活Slider并设置基础属性
        DushSlider.gameObject.SetActive(true);
        DushSlider.maxValue = (float)sprintCooldown;  // 最大值=冷却时间
        DushSlider.value = (float)sprintCooldown;     // 初始状态：满格（就绪）
        DushSlider.interactable = false;              // 禁止手动操作
        DushSlider.wholeNumbers = false;              // 允许小数（平滑更新）

        // 步骤3：查找Fill组件（按层级：Slider→Fill Area→Fill）
        Transform fillArea = DushSlider.transform.Find("Fill Area");
        if (fillArea == null)
        {
            Debug.LogError($"DushSlider（{DushSlider.name}）下未找到'Fill Area'子对象！请检查UI层级");
            _isSliderInitialized = false;
            yield break;
        }

        Transform fill = fillArea.Find("Fill");
        if (fill == null)
        {
            Debug.LogError($"Fill Area下未找到'Fill'子对象！请检查UI层级");
            _isSliderInitialized = false;
            yield break;
        }

        // 步骤4：获取Fill的Image组件
        if (fill.TryGetComponent<Image>(out Image fillImage))
        {
            _dushFillImage = fillImage;
            _dushFillImage.color = readyColor;  // 初始颜色：就绪色
            _isSliderInitialized = true;
            Debug.Log($"冲刺Slider初始化成功！名称：{DushSlider.name}，冷却时间：{sprintCooldown}s");
        }
        else
        {
            Debug.LogError("Fill子对象缺少Image组件！请添加后运行");
            _isSliderInitialized = false;
        }
    }

    /// <summary>
    /// 自动查找Slider（从UI根节点递归查找）
    /// </summary>
    private Slider AutoFindSlider()
    {
        // 查找UI根节点
        GameObject uiRoot = GameObject.Find(uiRootName);
        if (uiRoot == null)
        {
            Debug.LogError($"未找到UI根节点：{uiRootName}！请检查场景中是否存在该物体");
            return null;
        }

        // 递归查找所有子物体（含非激活），获取Slider组件
        Slider[] allSliders = uiRoot.GetComponentsInChildren<Slider>(includeInactive: true);
        foreach (Slider slider in allSliders)
        {
            if (slider.name == sliderName)
            {
                return slider; // 找到目标Slider
            }
        }

        return null;
    }

    private void HandleVerticalMovement()
    {
        if (_isAscending)
        {
            _controller.Move(Vector3.up * ascendSpeed * Time.deltaTime);
        }
        else if (_isDescending)
        {
            _controller.Move(Vector3.down * descendSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 处理冲刺冷却逻辑（同步更新Slider）
    /// </summary>
    private void HandleCooldown()
    {
        // 未初始化则直接返回，不报错
        if (!_isSliderInitialized || DushSlider == null || _dushFillImage == null)
            return;

        if (_isOnCooldown)
        {
            _cooldownTimer += Time.deltaTime;

            // 冷却结束：重置状态
            if (_cooldownTimer >= sprintCooldown)
            {
                _isOnCooldown = false;
                _cooldownTimer = 0;
                UpdateDushSlider((float)sprintCooldown, readyColor); // 满格+就绪色
            }
            else
            {
                // 冷却中：更新填充量（从0→maxValue）
                UpdateDushSlider((float)_cooldownTimer, cooldownColor);
            }
        }
        else
        {
            // 未冷却时，确保Slider是满格+就绪色（防止初始状态异常）
            UpdateDushSlider((float)sprintCooldown, readyColor);
        }
    }

    /// <summary>
    /// 更新冲刺冷却Slider的填充量和颜色
    /// </summary>
    private void UpdateDushSlider(float currentValue, Color targetColor)
    {
        if (!_isSliderInitialized || DushSlider == null || _dushFillImage == null)
            return;

        DushSlider.value = currentValue;
        _dushFillImage.color = targetColor;
    }

    public void ClearSliderUI()
    {
        // 先判断对象是否存在，再判断激活状态
        if (DushSlider != null && DushSlider.gameObject.activeSelf)
        {
            DushSlider.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 计算移动方向（移动输入包含转向逻辑）
    /// </summary>
    private Vector3 CalculateMoveDirection()
    {
        Vector3 moveDir = Vector3.zero;

        // 方向基准
        Vector3 selfForward = _transform.forward;
        Vector3 selfRight = _transform.right;
        Vector3 selfUp = _transform.up;

        // 前后移动（Y轴输入）
        moveDir += selfForward * _currentMoveSpeed * _moveInput.y;
        // 左右移动（X轴输入，降低横向移动权重）
        moveDir += selfRight * _currentMoveSpeed * 0.5f * _moveInput.x;

        // 转向逻辑（基于移动输入的X轴）
        if (_moveInput.x != 0)
        {
            float yawRotation = _moveInput.x * _currentRotateSpeed * Time.deltaTime;
            _transform.Rotate(0f, yawRotation, 0f, Space.World);
        }

        return moveDir;
    }

    /// <summary>
    /// 执行移动
    /// </summary>
    private void ExecuteMovement(Vector3 moveDir)
    {
        _controller.Move(moveDir * Time.deltaTime);
    }

    /// <summary>
    /// 更新移动速度和转向速度（供外部调用）
    /// </summary>
    public void UpdateSpeedStats(float newMoveSpeed, float newRotateSpeed)
    {
        _currentMoveSpeed = Mathf.Max(newMoveSpeed, 0.1f);
        _currentRotateSpeed = Mathf.Max(newRotateSpeed, 0.1f);

        if (showDebugLogs)
        {
            Debug.Log($"速度更新 - 移动速度: {_currentMoveSpeed}, 转向速度: {_currentRotateSpeed}");
        }
    }

    /// <summary>
    /// 处理冲刺输入逻辑
    /// </summary>
    private void HandleSprintInput()
    {
        if (_isSprintingInput && !_isOnCooldown && !_isSprinting && haveDush && _isSliderInitialized)
        {
            _isSprinting = true;
            MusicManager.Instance.Dush();
            StartCoroutine(ApplySprint());
        }
        // 重置输入状态（避免持续触发）
        _isSprintingInput = false;
    }

    /// <summary>
    /// 冲刺效果协程（冲刺时重置Slider为0）
    /// </summary>
    private IEnumerator ApplySprint()
    {
        float originalMoveSpeed = _currentMoveSpeed;
        _currentMoveSpeed *= (float)sprintMultiplier;

        // 冲刺开始：Slider重置为0（冷却开始）
        UpdateDushSlider(0f, cooldownColor);

        yield return new WaitForSeconds((float)sprintDuration);

        _currentMoveSpeed = originalMoveSpeed;
        _isSprinting = false;
        _isOnCooldown = true; // 开始冷却计时
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

    /// <summary>
    /// 绑定InputManager事件（仅保留移动和冲刺）
    /// </summary>
    private void BindInputEvents()
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager 实例为空，无法绑定输入事件！");
            return;
        }

        InputManager.Instance.OnSpeedUpBool += OnSprintInput;
        InputManager.Instance.OnMoveInput += OnMoveInput;  // 移动输入包含转向
        InputManager.Instance.Up += HandleUp;
        InputManager.Instance.Down += HandleDown;
    }

    /// <summary>
    /// 解除InputManager事件绑定（避免内存泄漏）
    /// </summary>
    private void OnDestroy()
    {
        if (InputManager.Instance == null) return;

        InputManager.Instance.OnSpeedUpBool -= OnSprintInput;
        InputManager.Instance.OnMoveInput -= OnMoveInput;
        InputManager.Instance.Up -= HandleUp;
        InputManager.Instance.Down -= HandleDown;
    }

    #region 输入事件处理
    /// <summary>
    /// 冲刺输入回调
    /// </summary>
    private void OnSprintInput(bool isPressed)
    {
        _isSprintingInput = isPressed;
    }

    /// <summary>
    /// 移动输入回调（包含转向逻辑，X轴控制转向和横向移动）
    /// </summary>
    private void OnMoveInput(Vector2 input)
    {
        _moveInput = input;  // input.x控制转向和左右移动，input.y控制前后移动
    }

    private void HandleUp(float inputValue)
    {
        bool isPressed = inputValue > 0.5f;
        if (isPressed != _isAscending)
        {
            _isAscending = isPressed;
            if (showDebugLogs) Debug.Log(_isAscending ? "开始上浮" : "停止上浮");
        }
        if (_isAscending) _isDescending = false; // 互斥，避免同时上下
    }

    private void HandleDown(float inputValue)
    {
        bool isPressed = inputValue > 0.5f;
        if (isPressed != _isDescending)
        {
            _isDescending = isPressed;
            if (showDebugLogs) Debug.Log(_isDescending ? "开始下潜" : "停止下潜");
        }
        if (_isDescending) _isAscending = false; // 互斥，避免同时上下
    }

    #endregion
}