using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// 输入管理器：集中处理所有输入事件，通过事件分发到各模块
/// </summary>
public class InputManager : MonoBehaviour
{
    // 单例实例
    public static InputManager Instance { get; private set; }

    #region 输入事件定义
    // ESC键按下事件
    public event Action<bool> OnESCPressed;
    // 鼠标移动事件（传递鼠标位移向量）
    public event Action<Vector2> OnLookInput;
    // 玩家移动事件
    public event Action<Vector2> OnMoveInput;
    // 玩家视角复位事件（传递复位触发向量）
    public event Action<bool> OnResetViewBool;
    // 加速事件（传递是否加速的布尔值）
    public event Action<bool> OnSpeedUpBool;
    // 上浮事件
    public event Action<float> Up;
    // 下潜事件
    public event Action<float>Down;
    #endregion

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }


    #region Input System回调方法（与Input Action名称一一对应）
    /// <summary>
    /// ESC键输入回调
    /// </summary>
    public void OnESC(InputValue value)
    {
        if (value.isPressed)
        {
            OnESCPressed?.Invoke(value.isPressed);
        }
    }

    /// <summary>
    /// 鼠标移动输入回调
    /// </summary>
    public void OnLook(InputValue value)
    {
        Vector2 lookDelta = value.Get<Vector2>();
        OnLookInput?.Invoke(lookDelta);
    }

    /// <summary>
    /// 移动回调
    /// </summary>
    public void OnMove(InputValue value)
    {
         Vector2 MoveDelta = value.Get<Vector2>();
        OnMoveInput?.Invoke(MoveDelta);
    }

    /// <summary>
    /// 玩家视角复位输入回调
    /// </summary>
    public void OnResetView(InputValue value)
    {
        OnResetViewBool?.Invoke(value.isPressed);
    }

    /// <summary>
    /// 加速输入回调（如Shift键）
    /// </summary>
    public void OnSpeedUp(InputValue value)
    {
        OnSpeedUpBool?.Invoke(value.isPressed);
    }
public void OnUp(InputValue value)
{
    float upValue = value.Get<float>();
    Up?.Invoke(upValue);
    // 调试：每帧输出输入值，按住时应为1.0，释放时为0.0
    // Debug.Log($"Up输入值: {upValue}"); 
}

public void OnDown(InputValue value)
{
    float downValue = value.Get<float>();
    Down?.Invoke(downValue);
    // Debug.Log($"Down输入值: {downValue}");
}
    #endregion
}