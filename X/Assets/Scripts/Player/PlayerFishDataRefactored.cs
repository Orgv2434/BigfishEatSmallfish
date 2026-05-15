using UnityEngine;
using System;
using DistantLands.Core;

/// <summary>
/// 重构后的玩家数据容器 - 轻量级聚合者
/// 职责：聚合各个系统（HealthSystem、ExpSystem、FishStatsSystem）
/// 通过事件驱动与其他系统通信，而不是直接调用
/// </summary>
public class PlayerFishDataRefactored : MonoBehaviour, IPlayerData
{
    public static PlayerFishDataRefactored Instance { get; private set; }

    [Header("系统引用（依赖注入）")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private ExpSystem expSystem;
    [SerializeField] private FishStatsSystem statsSystem;
    [SerializeField] private ThirdPersonMove thirdPersonMove;

    [Header("护盾配置")]
    [SerializeField] private int initialShields = 0;
    private int shieldCount = 0;

    [Header("经验配置")]
    [SerializeField] private float expToHealthRate = 0.2f;
    [SerializeField] private int[] expToNextTier = { 100, 300, 600, 1000 };

    private EventBus eventBus;
    private FishSkillSystem skillSystem;
    private ICollisionResolver collisionResolver;

    #region IPlayerData 实现
    public FishTier CurrentTier => expSystem.CurrentTier;
    public float CurrentHealth => healthSystem.CurrentHealth;
    public float MaxHealth => healthSystem.MaxHealth;
    public float CurrentSize => statsSystem.CurrentSize;
    public int CurrentExp => expSystem.CurrentExp;
    public float MoveSpeed => statsSystem.MoveSpeed;
    public float SurvivalTime { get; private set; }
    public int ShieldCount => shieldCount;
    #endregion

    // 旧的事件（为了兼容性暂时保留）
    public Action<float> OnHealthChanged;
    public Action<int, int> OnExpChanged;
    public Action<FishTier> OnTierChanged;
    public Action<float> OnSizeChanged;
    public Action<float> OnSpeedChanged;
    public event Action<FishTier> OnTierUpgraded;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        eventBus = EventBus.Instance;
        shieldCount = initialShields;
    }

    private void Start()
    {
        // 获取其他系统组件
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
        if (expSystem == null)
            expSystem = GetComponent<ExpSystem>();
        if (statsSystem == null)
            statsSystem = GetComponent<FishStatsSystem>();
        if (thirdPersonMove == null)
            thirdPersonMove = GetComponent<ThirdPersonMove>();

        skillSystem = GetComponent<FishSkillSystem>();
        collisionResolver = GetComponent<FishCollisionResolver>();

        // 检查必需组件
        CheckRequiredComponents();

        // 订阅事件
        SubscribeToEvents();

        SurvivalTime = 0;
    }

    private void Update()
    {
        SurvivalTime += Time.deltaTime;
    }

    private void CheckRequiredComponents()
    {
        if (healthSystem == null)
            Debug.LogError("[PlayerFishDataRefactored] 缺少 HealthSystem 组件");
        if (expSystem == null)
            Debug.LogError("[PlayerFishDataRefactored] 缺少 ExpSystem 组件");
        if (statsSystem == null)
            Debug.LogError("[PlayerFishDataRefactored] 缺少 FishStatsSystem 组件");
    }

    private void SubscribeToEvents()
    {
        // 订阅健康值变化（用于 UI 更新）
        eventBus.Subscribe<PlayerHealthChangedEvent>(evt =>
        {
            OnHealthChanged?.Invoke(evt.HealthPercent);
        });

        // 订阅经验变化（用于 UI 更新）
        eventBus.Subscribe<PlayerExpGainedEvent>(evt =>
        {
            OnExpChanged?.Invoke(evt.CurrentExp, evt.RequiredExpForNextTier);
        });

        // 订阅等级升级（用于 UI 更新）
        eventBus.Subscribe<PlayerTierUpgradedEvent>(evt =>
        {
            OnTierChanged?.Invoke(evt.NewTier);
            OnTierUpgraded?.Invoke(evt.NewTier);

            // 升级后更新移动速度
            if (thirdPersonMove != null)
            {
                thirdPersonMove.UpdateSpeedStats(statsSystem.MoveSpeed, statsSystem.RotateSpeed);
            }
        });

        // 订阅体型变化（用于 UI 更新）
        eventBus.Subscribe<PlayerSizeChangedEvent>(evt =>
        {
            OnSizeChanged?.Invoke(evt.NewSize);
        });

        // 订阅速度变化（用于 UI 更新）
        eventBus.Subscribe<PlayerSpeedChangedEvent>(evt =>
        {
            OnSpeedChanged?.Invoke(evt.MoveSpeed);
            if (thirdPersonMove != null)
            {
                thirdPersonMove.UpdateSpeedStats(evt.MoveSpeed, evt.RotateSpeed);
            }
        });

        // 订阅死亡事件
        eventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    /// <summary>
    /// 外部调用：获得经验（会自动触发体型、速度、升级等一系列更新）
    /// </summary>
    public void GainExp(int baseExp)
    {
        if (expSystem == null) return;

        float multiplier = 1f;
        // 如果有 ExpSystem 的乘数，则应用
        // 具体乘数管理由 ExpSystem 负责

        expSystem.AddExp(baseExp, multiplier);

        // 同步体型和速度更新
        statsSystem.UpdateSizeByExp(baseExp);
        statsSystem.UpdateSpeedByExp(baseExp);

        // 回血
        int restoredHealth = Mathf.RoundToInt(baseExp * expToHealthRate);
        healthSystem.Heal(restoredHealth);

        // 显示经验弹窗
        string popupText = $"<color=yellow>+{baseExp} Exp</color>  <color=green>+{restoredHealth} HP</color>";
        if (ExpPopupManager.Instance != null)
        {
            ExpPopupManager.Instance.ShowCustomPopup(transform.position, popupText, Color.white);
        }
    }

    /// <summary>
    /// 处理鱼的碰撞（由 PlayerCollisionHandler 调用）
    /// </summary>
    public void HandleFishCollision(FishTierEffect otherFish)
    {
        if (otherFish == null || collisionResolver == null)
            return;

        FishTier otherTier = otherFish.fishData.fishTier;
        int otherExp = otherFish.fishData.baseExpValue;
        string otherTag = otherFish.gameObject.tag;

        // 使用碰撞解决器判断碰撞结果
        bool shouldEat = collisionResolver.ResolveCollision(CurrentTier, otherTier, otherTag);

        if (otherTier > CurrentTier)
        {
            // 被吃掉的逻辑 - 检查护盾
            if (!UseShield())
            {
                healthSystem.SetHealth(0); // 死亡
                Debug.Log("[PlayerFishDataRefactored] 被更大的鱼吃掉");
            }
            else
            {
                Debug.Log("[PlayerFishDataRefactored] 护盾保护");
                eventBus.Publish(new ShieldUsedEvent { RemainingShields = shieldCount });
            }
            return;
        }

        if (shouldEat)
        {
            // 吃掉对方
            Debug.Log("[PlayerFishDataRefactored] 吃掉 " + otherFish.gameObject.name);

            // 销毁被吃的鱼
            GlobalFlock fishFlock = otherFish.gameObject.transform.parent?.parent?.GetComponent<GlobalFlock>();
            if (fishFlock != null)
            {
                fishFlock.OnFishEaten(otherFish.gameObject);
            }
            else
            {
                Destroy(otherFish.gameObject);
            }

            // 吃掉特殊技能鱼
            if (skillSystem != null)
            {
                skillSystem.EatSkillFish(otherFish.fishData);
            }

            // 获得经验
            GainExp(otherExp);
        }
    }

    #region 护盾系统
    public void AddShield(int count = 1)
    {
        shieldCount += count;
    }

    public bool UseShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.HuDun();
            }
            return true;
        }
        return false;
    }
    #endregion

    #region 兼容性方法
    public FishTier GetCurrentTier() => CurrentTier;

    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        if (expSystem != null)
        {
            expSystem.SetTemporaryExpMultiplier(multiplier, duration);
        }
    }

    public int GetRequiredExpForNextTier()
    {
        if (expSystem != null)
            return expSystem.GetRequiredExpForNextTier();
        return 0;
    }
    #endregion

    private void OnPlayerDead(PlayerDeadEvent evt)
    {
        Debug.Log("[PlayerFishDataRefactored] 玩家死亡");
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.Die();
            MusicManager.Instance.StopBGM();
        }

        if (FishGameFlowManager.Instance != null)
        {
            FishGameFlowManager.Instance.tex_survivalTime.text = $"{Mathf.FloorToInt(SurvivalTime)} ";
            FishGameFlowManager.Instance.tex_finalLevel.text = $"{(int)CurrentTier + 1} ";
            FishGameFlowManager.Instance.GetVerdictText(SurvivalTime, (int)CurrentTier);
            FishGameFlowManager.Instance.TriggerGameOver();
        }

        if (skillSystem != null)
        {
            skillSystem.ClearAllSkillIcons();
        }

        if (thirdPersonMove != null)
        {
            thirdPersonMove.ClearSliderUI();
        }

        Destroy(gameObject);
    }

    public void GainHealth(float amount)
    {
        if (healthSystem != null)
        {
            healthSystem.Heal(amount);
        }
    }

    private void OnDestroy()
    {
        // 取消事件订阅
        eventBus.Unsubscribe<PlayerHealthChangedEvent>(evt => { });
        eventBus.Unsubscribe<PlayerExpGainedEvent>(evt => { });
        eventBus.Unsubscribe<PlayerTierUpgradedEvent>(evt => { });
        eventBus.Unsubscribe<PlayerSizeChangedEvent>(evt => { });
        eventBus.Unsubscribe<PlayerSpeedChangedEvent>(evt => { });
        eventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
    }
}
