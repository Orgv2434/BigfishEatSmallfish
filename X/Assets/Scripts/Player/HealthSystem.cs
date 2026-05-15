using UnityEngine;
using DistantLands.Core;

/// <summary>
/// 独立的健康系统 - 管理玩家生命值
/// 职责：血量管理、扣血、回血
/// 通过事件发布，与其他系统解耦
/// </summary>
public class HealthSystem : MonoBehaviour, IHealthSystem
{
    [Header("生命配置")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthLossRate = 1f;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => MaxHealth > 0 ? currentHealth / MaxHealth : 0;

    private EventBus eventBus;

    private void Awake()
    {
        eventBus = EventBus.Instance;
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 每帧扣血
        currentHealth = Mathf.Max(0, currentHealth - healthLossRate * Time.deltaTime);
        
        // 发布健康值变化事件
        var healthEvent = new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        };
        eventBus.Publish(healthEvent);

        // 检测死亡
        if (currentHealth <= 0)
        {
            OnDead();
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        var healthEvent = new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        };
        eventBus.Publish(healthEvent);
    }

    /// <summary>
    /// 受伤
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        
        var healthEvent = new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        };
        eventBus.Publish(healthEvent);

        if (currentHealth <= 0)
        {
            OnDead();
        }
    }

    /// <summary>
    /// 设置血量（用于初始化或特殊情况）
    /// </summary>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        
        var healthEvent = new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        };
        eventBus.Publish(healthEvent);
    }

    /// <summary>
    /// 增加最大生命值
    /// </summary>
    public void IncreaseMaxHealth(float amount)
    {
        maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        var healthEvent = new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        };
        eventBus.Publish(healthEvent);
    }

    private void OnDead()
    {
        // 只发布一次死亡事件
        if (currentHealth <= 0 && currentHealth + healthLossRate * Time.deltaTime > 0)
        {
            var deadEvent = new PlayerDeadEvent
            {
                SurvivalTime = Time.timeSinceLevelLoad
            };
            eventBus.Publish(deadEvent);
        }
    }
}
