using UnityEngine;
using System;

public class PlayerFishGUIManager : MonoBehaviour
{
    private PlayerFishData _playerFishData;

    // GUI 布局参数
    public Vector2 guiOffset = new Vector2(20, 20);
    public float elementSpacing = 20;
    public float healthBarHeight = 20; // 血条高度
    public float smoothTime = 0.3f;    // 平滑动画时间

    // 血条插值用的中间值
    private float _targetHealthPercent;
    private float _currentHealthPercent;
    private float _healthVelocity;

    // 新增：平滑后的血量值（用于文本显示）
    private float _smoothedHealth;

    private void Awake()
    {
        _playerFishData = GetComponent<PlayerFishData>();
        if (_playerFishData == null)
        {
            Debug.LogError("缺少 PlayerFishData 组件！");
            enabled = false;
        }
    }

    private void Start()
    {
        // 初始化血条百分比
        _targetHealthPercent = _playerFishData.currentHealth / _playerFishData.maxHealth;
        _currentHealthPercent = _targetHealthPercent;
        _smoothedHealth = _playerFishData.currentHealth;

        // 订阅血条变化事件
        _playerFishData.OnHealthChanged += UpdateHealthTarget;
    }

    private void OnDestroy()
    {
        // 取消事件订阅
        _playerFishData.OnHealthChanged -= UpdateHealthTarget;
    }

    // 接收 PlayerFishData 的血量变化事件
    private void UpdateHealthTarget(float newHealthPercent)
    {
        _targetHealthPercent = newHealthPercent;
    }

    private void Update()
    {
        // 实时计算平滑后的血量（与血条动画同步）
        _currentHealthPercent = Mathf.SmoothDamp(
            _currentHealthPercent,
            _targetHealthPercent,
            ref _healthVelocity,
            smoothTime
        );
        _smoothedHealth = _currentHealthPercent * _playerFishData.maxHealth;
    }

    private void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 20;
        labelStyle.normal.textColor = Color.white;

        // 血条样式（绿色）
        GUIStyle healthBarStyle = new GUIStyle(GUI.skin.box);
        healthBarStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.8f, 0.2f));

        float yPos = guiOffset.y;

        // ---------- 生命值显示 ----------
        // 1. 文本显示（使用平滑后的血量值）
        GUI.Label(
            new Rect(guiOffset.x, yPos, 200, 30),
            $"生命值：{_smoothedHealth:F1}/{_playerFishData.maxHealth:F1}",
            labelStyle
        );

        // 2. 绘制背景条
        Rect bgBarRect = new Rect(
            guiOffset.x,
            yPos + 30,
            200,
            healthBarHeight
        );
        GUI.Box(bgBarRect, "", GUI.skin.box);

        // 3. 绘制当前血条（带平滑动画）
        Rect healthBarRect = new Rect(
            bgBarRect.x,
            bgBarRect.y,
            bgBarRect.width * _currentHealthPercent,
            bgBarRect.height
        );
        GUI.Box(healthBarRect, "", healthBarStyle);

        yPos += 30 + healthBarHeight + elementSpacing;


        // ---------- 经验值显示 ----------
        int requiredExp = _playerFishData.GetRequiredExpForNextTier();
        GUI.Label(
            new Rect(guiOffset.x, yPos, 200, 30),
            $"经验值：{_playerFishData.currentExp}/{requiredExp}",
            labelStyle
        );
        yPos += elementSpacing;


        // ---------- 挡位显示 ----------
        GUI.Label(
            new Rect(guiOffset.x, yPos, 200, 30),
            $"当前挡位：{_playerFishData.currentTier}",
            labelStyle
        );
        yPos += elementSpacing;


        // ---------- 移速显示 ----------
        GUI.Label(
            new Rect(guiOffset.x, yPos, 200, 30),
            $"移速：{_playerFishData.moveSpeed:F1}",
            labelStyle
        );
        yPos += elementSpacing;


        // ---------- 护盾次数显示 ----------
        GUI.Label(
            new Rect(guiOffset.x, yPos, 200, 30),
            $"护盾次数：{_playerFishData._shieldCount}",
            labelStyle
        );
        yPos += elementSpacing;
    }

    // 辅助方法：创建纯色纹理（用于血条颜色）
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}