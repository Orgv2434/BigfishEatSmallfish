using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerFishGUIManager : MonoBehaviour
{   
    public static PlayerFishGUIManager Instance { get; private set; }
    private PlayerFishData _playerFishData;

    // UI引用
    [Header("UI组件引用")]
    public Slider healthSlider;      // 血条Slider
    public Slider expSlider;         // 经验条Slider

    // 平滑动画参数
    public float smoothTime = 0.3f;
    private float _healthVelocity;
    private float _expVelocity;

    // 挡位颜色映射（白、黄、紫、黑、红）
    private readonly Color[] _tierColors = new Color[]
    {
        Color.white,       // 白
        new Color(1, 1, 0), // 黄
        new Color(0.8f, 0, 1), // 紫
        Color.black,       // 黑
        new Color(1, 0, 0)  // 红
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保单例不被销毁
        }
        else
        {
            Destroy(gameObject); // 销毁重复实例
        }
    }

    private void OnDestroy()
    {
        // 安全取消事件订阅
        if (_playerFishData != null)
        {
            _playerFishData.OnHealthChanged -= OnHealthChanged;
            _playerFishData.OnExpChanged -= OnExpChanged;
            _playerFishData.OnTierChanged -= OnTierChanged;
        }
    }

    private void Update()
    {   
        if(FishGameFlowManager.Instance == null) return;
        if(FishGameFlowManager.Instance.CurrentState != FishGameFlowManager.GameState.GamePlaying)
            return;

        // 平滑更新血条
        if (healthSlider != null && _playerFishData != null)
        {
            float targetHealth = Mathf.Max(0, _playerFishData.currentHealth);
            healthSlider.value = Mathf.SmoothDamp(
                healthSlider.value,
                targetHealth,
                ref _healthVelocity,
                smoothTime
            );
        }

        // 平滑更新经验条
        if (expSlider != null && _playerFishData != null)
        {
            float targetExp = Mathf.Max(0, _playerFishData.currentExp);
            expSlider.value = Mathf.SmoothDamp(
                expSlider.value,
                targetExp,
                ref _expVelocity,
                smoothTime
            );
        }
    }

    public void SetPlayerFishData()
    {
        _playerFishData = FindObjectOfType<PlayerFishData>();
        if (_playerFishData == null)
        {
            Debug.LogError("PlayerFishGUIManager：找不到PlayerFishData组件！");
            enabled = false;
            return;
        }

        UpdateHealthSliderRange();
        UpdateExpSliderRange();
        UpdateHealthDisplay();
        UpdateExpDisplay();

        // 避免重复订阅
        _playerFishData.OnHealthChanged -= OnHealthChanged;
        _playerFishData.OnHealthChanged += OnHealthChanged;
        _playerFishData.OnExpChanged -= OnExpChanged;
        _playerFishData.OnExpChanged += OnExpChanged;
        _playerFishData.OnTierChanged -= OnTierChanged;
        _playerFishData.OnTierChanged += OnTierChanged;
    }

    private void OnHealthChanged(float healthPercent)
    {
        if (_playerFishData == null) return;
        UpdateHealthSliderRange();
        UpdateHealthDisplay();
    }

    private void OnExpChanged(int currentExp, int requiredExp)
    {
        if (_playerFishData == null) return;
        UpdateExpSliderRange();
        UpdateExpDisplay();
    }

    private void OnTierChanged(FishTier newTier)
    {
        UpdateExpBarColors();
    }

    private void UpdateHealthSliderRange()
    {
        if (healthSlider != null && _playerFishData != null)
        {
            healthSlider.maxValue = _playerFishData.maxHealth;
        }
    }

    private void UpdateExpSliderRange()
    {
        if (expSlider != null && _playerFishData != null)
        {
            int requiredExp = _playerFishData.GetRequiredExpForNextTier();
            expSlider.maxValue = requiredExp > 0 ? requiredExp : 0;
        }
    }

    private void UpdateHealthDisplay()
    {
        if (healthSlider != null && _playerFishData != null)
        {
            healthSlider.maxValue = _playerFishData.maxHealth;
            healthSlider.value = Mathf.Max(0, _playerFishData.currentHealth);
        }
    }

    private void UpdateExpDisplay()
    {
        if (_playerFishData == null) return;
        UpdateExpSliderRange();
        if (expSlider != null)
        {
            expSlider.value = Mathf.Max(0, _playerFishData.currentExp);
        }
        UpdateExpBarColors();
    }

    // 核心修改：适配层级结构（Fill Area → Fill / Background）
    private void UpdateExpBarColors()
    {
        // 层层空检查，不中断游戏逻辑
        if (expSlider == null)
        {
            Debug.LogWarning("UpdateExpBarColors：expSlider未赋值！");
            return;
        }
        if (_playerFishData == null)
        {
            Debug.LogWarning("UpdateExpBarColors：_playerFishData为空！");
            return;
        }

        // 1. 先找到 Fill Area（所有子对象的父容器）
        Transform fillArea = expSlider.transform.Find("Fill Area");
        if (fillArea == null)
        {
            Debug.LogError("ExpSlider下未找到'Fill Area'子对象！请检查UI层级");
            return;
        }

        // 2. 从 Fill Area 下找 Fill（填充条）
        Transform fill = fillArea.Find("Fill");
        if (fill == null)
        {
            Debug.LogError("Fill Area下未找到'Fill'子对象！请检查UI层级");
            return;
        }
        if (!fill.TryGetComponent<Image>(out Image fillImage))
        {
            Debug.LogError("Fill对象缺少Image组件！");
            return;
        }

        // 3. 从 Fill Area 下找 Background（背景条）
        Transform background = fillArea.Find("Background");
        if (background == null)
        {
            Debug.LogError("Fill Area下未找到'Background'子对象！请检查UI层级");
            return;
        }
        if (!background.TryGetComponent<Image>(out Image backgroundImage))
        {
            Debug.LogError("Background对象缺少Image组件！");
            return;
        }

        // 4. 应用挡位颜色（逻辑不变）
        int tierIndex = (int)_playerFishData.currentTier;
        if (tierIndex < 0 || tierIndex >= _tierColors.Length)
        {
            Debug.LogWarning($"当前挡位{tierIndex}无对应颜色！");
            return;
        }
        Color tierColor = _tierColors[tierIndex];
        fillImage.color = tierColor; // 填充色：当前挡位颜色
        backgroundImage.color = new Color(tierColor.r * 0.3f, tierColor.g * 0.3f, tierColor.b * 0.3f, 0.5f); // 背景色：暗化半透明
    }
}