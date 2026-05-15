using UnityEngine;
using DistantLands.Core;

/// <summary>
/// 独立的经验系统 - 管理经验和等级升级
/// 职责：经验累计、等级判定、升级事件发布
/// 通过事件驱动，与其他系统解耦
/// </summary>
public class ExpSystem : MonoBehaviour, IExpSystem
{
    [Header("经验配置")]
    [SerializeField] private float baseExpMultiplier = 0.5f;
    [SerializeField] private int[] expToNextTier = { 100, 300, 600, 1000 };
    
    private int currentExp = 0;
    private FishTier currentTier = FishTier.White;
    private float _currentExpMultiplier;
    private EventBus eventBus;

    public int CurrentExp => currentExp;
    public int TotalExp => currentExp;
    public FishTier CurrentTier => currentTier;

    private void Awake()
    {
        eventBus = EventBus.Instance;
        _currentExpMultiplier = baseExpMultiplier;
    }

    /// <summary>
    /// 获得经验
    /// </summary>
    public void AddExp(int baseExp, float multiplier = 1f)
    {
        int actualExp = Mathf.RoundToInt(baseExp * multiplier * _currentExpMultiplier);
        currentExp += actualExp;

        // 发布经验变化事件
        var expEvent = new PlayerExpGainedEvent
        {
            ExpAmount = actualExp,
            CurrentExp = currentExp,
            RequiredExpForNextTier = GetRequiredExpForNextTier()
        };
        eventBus.Publish(expEvent);

        // 检查等级升级
        CheckTierUpgrade();

        Debug.Log($"[ExpSystem] 获得 {actualExp} 经验，总经验 {currentExp}");
    }

    /// <summary>
    /// 获取下一档位所需经验
    /// </summary>
    public int GetRequiredExpForNextTier()
    {
        int tierIndex = (int)currentTier;
        return tierIndex < expToNextTier.Length ? expToNextTier[tierIndex] : 0;
    }

    /// <summary>
    /// 检查等级升级
    /// </summary>
    public void CheckTierUpgrade()
    {
        int tierIndex = (int)currentTier;
        
        while (tierIndex < expToNextTier.Length && currentExp >= expToNextTier[tierIndex])
        {
            FishTier oldTier = currentTier;
            tierIndex++;
            currentTier = (FishTier)tierIndex;

            // 发布升级事件
            var upgradeEvent = new PlayerTierUpgradedEvent
            {
                OldTier = oldTier,
                NewTier = currentTier
            };
            eventBus.Publish(upgradeEvent);

            Debug.Log($"[ExpSystem] 升级到 {currentTier}");
        }
    }

    /// <summary>
    /// 设置临时经验倍数
    /// </summary>
    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        StartCoroutine(ResetExpMultiplierCoroutine(multiplier, duration));
    }

    private System.Collections.IEnumerator ResetExpMultiplierCoroutine(float tempMulti, float duration)
    {
        _currentExpMultiplier = tempMulti;
        yield return new WaitForSeconds(duration);
        _currentExpMultiplier = baseExpMultiplier;
    }

    /// <summary>
    /// 直接设置经验值（调试或特殊情况）
    /// </summary>
    public void SetExp(int exp)
    {
        currentExp = exp;
        CheckTierUpgrade();
    }

    /// <summary>
    /// 直接设置等级（调试）
    /// </summary>
    public void SetTier(FishTier tier)
    {
        FishTier oldTier = currentTier;
        currentTier = tier;

        var upgradeEvent = new PlayerTierUpgradedEvent
        {
            OldTier = oldTier,
            NewTier = currentTier
        };
        eventBus.Publish(upgradeEvent);
    }

    /// <summary>
    /// 获取当前经验倍数
    /// </summary>
    public float GetCurrentMultiplier()
    {
        return _currentExpMultiplier;
    }
}
