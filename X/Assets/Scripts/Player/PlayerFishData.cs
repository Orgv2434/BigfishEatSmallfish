using UnityEngine;
using System;
using System.Collections;
using DistantLands;


public class PlayerFishData : MonoBehaviour
{
    public static PlayerFishData Instance { get; private set; }
    public SkillFishData fishData;

    [Header("基础属性")]
    public float maxHealth = 100f;
    public float currentHealth;
    public int currentExp = 0;
    public FishTier currentTier = FishTier.White;
    public float baseSize = 1f; // 初始体型缩放值（模型+碰撞体统一基准）
    public float currentSize;
    public float moveSpeed = 5f;

    [Header("模型与碰撞体配置（必填）")]
    public Transform modelTransform; // 玩家鱼模型根节点（必须赋值）
    public CharacterController characterController; // 玩家碰撞体（必须赋值，强制同步缩放）
    [Tooltip("碰撞体缩放系数（微调碰撞体与模型的匹配度，默认1=完全同步）")]
    public float colliderScaleMultiplier = 1f; // 灵活适配模型与碰撞体的初始比例
    private float baseColliderRadius; // 碰撞体初始半径（记录基准值）
    private float baseColliderHeight; // 碰撞体初始高度（记录基准值）
    private Vector3 baseColliderCenter; // 碰撞体初始中心（记录基准值）

    [Header("生命设置")]
    public float healthLossRate = 1f;
    public float expToHealthRate = 0.2f;

    [Header("经验设置")]
    public float baseExpMultiplier = 0.5f;
    private float _currentExpMultiplier;

    [Header("升级设置")]
    public int[] expToNextTier = { 100, 300, 600, 1000 };

    [Header("体型与速度成长（核心配置）")]
    public float sizeIncreasePerExp = 0.002f; // 每点经验→体型增长
    public float moveSpeedDecreasePerExp = 0.003f; // 每点经验→速度衰减
    public float sizeIncreasePerTier = 0.5f; // 每升1级→额外体型加成
    public float minMoveSpeed = 1f; // 速度下限
    public float rotateSpeedDecreasePerExp = 0.015f; // 每点经验→转向减速
    public float minRotateSpeed = 10f; // 转向速度下限
    public float baseRotateSpeed = 90f;
    private float _currentRotateSpeed;

    [Header("引用配置")]
    public ThirdPersonMove thirdPersonMove;

    // 事件通知
    public Action<float> OnHealthChanged;
    public Action<int, int> OnExpChanged;
    public Action<FishTier> OnTierChanged;
    public Action<float> OnSizeChanged; // 体型变化事件（触发模型+碰撞体缩放）
    public Action<float> OnSpeedChanged;

    // 护盾状态
    public int _shieldCount = 0;
    private FishSkillSystem _skillSystem;
    public event Action<FishTier> OnTierUpgraded;
    public float survivalTime; // 生存时间（秒）
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // 订阅体型变化事件（核心：一次触发，同步模型+碰撞体）
        OnSizeChanged += SyncModelAndCollider;
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentSize = baseSize;
        _currentExpMultiplier = baseExpMultiplier;
        _currentRotateSpeed = baseRotateSpeed;
        _skillSystem = GetComponent<FishSkillSystem>();

        // 强制检查核心组件（避免用户遗漏赋值）
        CheckRequiredComponents();
        // 初始化模型和碰撞体（确保初始状态一致）
        InitModelAndCollider();

        // 初始化移动速度
        if (thirdPersonMove != null)
            thirdPersonMove.UpdateSpeedStats(moveSpeed, _currentRotateSpeed);
        else
            Debug.LogWarning("未赋值ThirdPersonMove脚本！速度更新将失效");

        survivalTime = 0;
    }

    /// <summary>
    /// 强制检查模型和碰撞体组件（必须赋值，否则提示错误）
    /// </summary>
    private void CheckRequiredComponents()
    {
        if (modelTransform == null)
        {
            Debug.LogError("【PlayerFishData】必须赋值 modelTransform（玩家鱼模型根节点）！否则无法缩放模型");
            enabled = false; // 禁用脚本，避免后续报错
        }

        if (characterController == null)
        {
            Debug.LogError("【PlayerFishData】必须赋值 characterController（CharacterController组件）！否则无法同步碰撞体缩放");
            enabled = false; // 禁用脚本，避免后续报错
        }
    }

    /// <summary>
    /// 初始化模型和碰撞体的基准状态
    /// </summary>
    private void InitModelAndCollider()
    {
        if (modelTransform == null || characterController == null) return;

        // 1. 初始化模型缩放（确保初始大小=baseSize）
        modelTransform.localScale = Vector3.one * currentSize;
        Debug.Log($"【初始化】模型缩放：{currentSize:F2}，碰撞体缩放系数：{colliderScaleMultiplier:F2}");

        // 2. 记录碰撞体初始参数（基准值，后续缩放基于此计算）
        baseColliderRadius = characterController.radius;
        baseColliderHeight = characterController.height;
        baseColliderCenter = characterController.center;

        // 3. 初始化碰撞体大小（与模型初始缩放同步）
        UpdateColliderSize(currentSize);
    }

    private void Update()
    {
        if (modelTransform == null || characterController == null) return;

        // 随时间扣血
        currentHealth = Mathf.Max(0, currentHealth - healthLossRate * Time.deltaTime);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        survivalTime += Time.deltaTime;

        // 死亡检测
        if (currentHealth <= 0)
        {
            DestroyPlayerFish();
        }
    }

    // 加血逻辑
    public void GainHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    // 加经验逻辑（经验→体型+速度+碰撞体同步）
    public void GainExp(int baseExp)
    {
        int actualExp = Mathf.RoundToInt(baseExp * _currentExpMultiplier);
        currentExp += actualExp;

        // 经验通知（UI更新）
        OnExpChanged?.Invoke(currentExp, GetRequiredExpForNextTier());
        // 经验回血
        float restoredHealth = actualExp * expToHealthRate;
        GainHealth(restoredHealth);

        // 输出调试信息（方便观察体型/碰撞体变化）
        Debug.Log($"【获得经验】+{actualExp} Exp → 当前体型：{currentSize:F2} → 碰撞体半径：{characterController.radius:F2}，高度：{characterController.height:F2}");

        // 屏幕特效
        string coloredText =
       $"<color=yellow>+{actualExp} Exp</color>  " +
       $"<color=green>+{restoredHealth} HP</color>";
        ExpPopupManager.Instance.ShowCustomPopup(transform.position, coloredText, Color.white);

        // 经验→体型增长 + 速度衰减 + 碰撞体同步
        UpdateStatsByExp(actualExp);
        // 升级检测
        CheckTierUpgrade();
    }

    /// <summary>
    /// 经验更新属性（体型+速度）
    /// </summary>
    private void UpdateStatsByExp(int addedExp)
    {
        // 1. 体型增长（经验越多，体型越大）
        currentSize += addedExp * sizeIncreasePerExp;
        OnSizeChanged?.Invoke(currentSize); // 触发同步：模型+碰撞体

        // 2. 速度衰减（经验越多，速度越慢）
        float totalMoveSpeedDecrease = currentExp * moveSpeedDecreasePerExp;
        float newMoveSpeed = Mathf.Max(moveSpeed - totalMoveSpeedDecrease, minMoveSpeed);
        float totalRotateDecrease = currentExp * rotateSpeedDecreasePerExp;
        _currentRotateSpeed = Mathf.Max(baseRotateSpeed - totalRotateDecrease, minRotateSpeed);

        // 3. 同步速度到移动脚本
        if (thirdPersonMove != null)
            thirdPersonMove.UpdateSpeedStats(newMoveSpeed, _currentRotateSpeed);

        OnSpeedChanged?.Invoke(newMoveSpeed);
    }

    /// <summary>
    /// 升级检测（体型叠加+碰撞体同步）
    /// </summary>
    private void CheckTierUpgrade()
    {
        int tierIndex = (int)currentTier;
        while (tierIndex < expToNextTier.Length && currentExp >= expToNextTier[tierIndex])
        {
            tierIndex++;
            currentTier = (FishTier)tierIndex;
            OnTierUpgraded?.Invoke(currentTier);

            // 升级特效
            FishTierEffect tierEffect = GetComponent<FishTierEffect>();
            if (tierEffect != null)
            {
                tierEffect.ShowTierHalo(currentTier);
                tierEffect.AutoFitModelSize();
            }

            // 升级属性加成（体型叠加，不重置）
            currentSize += sizeIncreasePerTier;
            moveSpeed += 0.5f; // 平衡速度衰减
            maxHealth += 50;
            currentHealth = Mathf.Min(maxHealth, currentHealth + 50);

            // 重新计算速度
            float finalMoveSpeed = Mathf.Max(moveSpeed - currentExp * moveSpeedDecreasePerExp, minMoveSpeed);
            float finalRotateSpeed = Mathf.Max(_currentRotateSpeed, minRotateSpeed);
            thirdPersonMove?.UpdateSpeedStats(finalMoveSpeed, finalRotateSpeed);

            // 触发同步：模型+碰撞体（升级后体型变化）
            OnSizeChanged?.Invoke(currentSize);
            // 其他事件通知
            OnSpeedChanged?.Invoke(finalMoveSpeed);
            OnTierChanged?.Invoke(currentTier);
            FishEventSystem.BroadcastTierChanged(currentTier);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
            HandleTierUpgrade();

            // 音效
            MusicManager.Instance.Upgrade();
        }
    }

    /// <summary>
    /// 核心方法：同步模型缩放和碰撞体大小（一次触发，双重同步）
    /// </summary>
    private void SyncModelAndCollider(float newSize)
    {
        if (modelTransform == null || characterController == null) return;

        // 1. 缩放模型（均匀缩放）
        modelTransform.localScale = Vector3.one * newSize;

        // 2. 同步碰撞体大小（基于初始基准值×当前体型×缩放系数）
        UpdateColliderSize(newSize);

        // 调试日志（方便验证同步效果）
        Debug.Log($"【同步缩放】体型：{newSize:F2} → 碰撞体半径：{characterController.radius:F2}，高度：{characterController.height:F2}，中心：{characterController.center:F2}");
    }

    /// <summary>
    /// 精确更新碰撞体大小（半径、高度、中心同步调整）
    /// </summary>
    private void UpdateColliderSize(float newSize)
    {
        if (characterController == null) return;

        // 计算当前缩放比例（基于初始体型）
        float scaleRatio = newSize / baseSize;

        // 同步半径（初始半径×缩放比例×适配系数）
        characterController.radius = baseColliderRadius * scaleRatio * colliderScaleMultiplier;
        // 同步高度（初始高度×缩放比例×适配系数）
        characterController.height = baseColliderHeight * scaleRatio * colliderScaleMultiplier;
        // 同步中心（避免碰撞体偏移，保持与模型中心一致）
        characterController.center = baseColliderCenter * scaleRatio * colliderScaleMultiplier;

        // 安全校验（避免碰撞体参数为负数）
        characterController.radius = Mathf.Max(characterController.radius, 0.1f);
        characterController.height = Mathf.Max(characterController.height, 0.2f);
    }

    // 以下为原有逻辑（保留）
    public FishTier GetCurrentTier() => currentTier;

    public void HandleFishCollision(FishTierEffect otherFish)
    {
        if (otherFish == null) return;

        FishTier otherTier = otherFish.fishData.fishTier;
        int otherExp = otherFish.fishData.baseExpValue;
        string otherTag = otherFish.gameObject.tag;

        if (otherTier > currentTier)
        {
            if (!UseShield())
            {
                currentHealth = 0;
                Debug.Log("玩家鱼被更大挡位的鱼吃掉，死亡！");
            }
            else
            {
                Debug.Log("玩家鱼使用护盾抵挡了更大挡位鱼的攻击！");
            }
            return;
        }

        if (otherTier < currentTier || (otherTier == currentTier && otherTag != "head"))
        {
            Debug.Log(otherTier < currentTier ? "吃掉更小挡位的鱼" : "吃掉同挡位鱼的尾部/身体");

            GlobalFlock fishFlock = otherFish.gameObject.transform.parent.parent?.GetComponent<GlobalFlock>();
            if (fishFlock != null)
                fishFlock.OnFishEaten(otherFish.gameObject);
            else
                Destroy(otherFish.gameObject);

            _skillSystem?.EatSkillFish(otherFish.fishData);
            GainExp(otherExp);
        }
        else if (otherTier == currentTier && otherTag == "head")
        {
            Debug.Log("碰到同挡位鱼的头部，无视碰撞！");
        }
    }

    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        StartCoroutine(ResetExpMultiplier(multiplier, duration));
        MusicManager.Instance.PlusExp();
    }

    private IEnumerator ResetExpMultiplier(float tempMulti, float duration)
    {
        _currentExpMultiplier = tempMulti;
        yield return new WaitForSeconds(duration);
        _currentExpMultiplier = baseExpMultiplier;
    }

    public void SetTemporaryVisualTier(FishTier visualTier, float duration)
    {
        StartCoroutine(ResetVisualTier(visualTier, duration));
    }

    private IEnumerator ResetVisualTier(FishTier visualTier, float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void AddShield(int count = 1) => _shieldCount += count;

    public bool UseShield()
    {
        if (_shieldCount > 0)
        {
            _shieldCount--;
            MusicManager.Instance.HuDun();
            
            return true;
        }
        return false;
    }

    public int GetRequiredExpForNextTier()
    {
        int tierIndex = (int)currentTier;
        return tierIndex < expToNextTier.Length ? expToNextTier[tierIndex] : 0;
    }

    public void HandleTierUpgrade()
    {
        Debug.Log($"玩家鱼升级到 {currentTier} 挡位！当前体型：{currentSize:F2}，碰撞体半径：{characterController.radius:F2}");
    }

    public void DestroyPlayerFish()
    {
        Debug.Log("玩家鱼死亡！");
        MusicManager.Instance.Die();
        MusicManager.Instance.StopBGM();

        FishGameFlowManager.Instance.tex_survivalTime.text = $"{Mathf.FloorToInt(survivalTime)} ";
        FishGameFlowManager.Instance.tex_finalLevel.text = $"{(int)currentTier+1} ";
        FishGameFlowManager.Instance.GetVerdictText(survivalTime, (int)currentTier);
        FishGameFlowManager.Instance.TriggerGameOver();
        // 清空相关UI
       FishSkillSystem skillSystem = GetComponent<FishSkillSystem>();
        if (skillSystem != null)
        {
            skillSystem.ClearAllSkillIcons();
        }
        else Debug.Log("没有找到FishSkillSystem");
        ThirdPersonMove thirdPersonMove = GetComponent<ThirdPersonMove>();
        if (thirdPersonMove != null)
        {
            thirdPersonMove.ClearSliderUI();
        }
        else Debug.Log("没有找到thirdPersonMove");
  
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 取消事件订阅（避免内存泄漏）
        OnSizeChanged -= SyncModelAndCollider;
    }
}