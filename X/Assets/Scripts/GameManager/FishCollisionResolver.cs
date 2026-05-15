using UnityEngine;
using DistantLands.Core;

/// <summary>
/// 碰撞解决器 - 纯逻辑处理，不修改任何状态
/// 职责：根据规则判断碰撞结果（谁吃谁、是否有效等）
/// 返回碰撞结果，由其他系统根据结果决定后续行为
/// </summary>
public class FishCollisionResolver : MonoBehaviour, ICollisionResolver
{
    [Header("碰撞规则配置")]
    [SerializeField] private bool allowSameTierCollision = true;

    private EventBus eventBus;

    private void Awake()
    {
        eventBus = EventBus.Instance;
    }

    /// <summary>
    /// 解决碰撞 - 根据玩家等级和对方等级判断碰撞结果
    /// </summary>
    /// <param name="playerTier">玩家等级</param>
    /// <param name="otherTier">对方等级</param>
    /// <param name="otherTag">对方标签（用于判断是否吃头部）</param>
    /// <returns>true 表示进行了吃鱼操作，false 表示无效碰撞</returns>
    public bool ResolveCollision(FishTier playerTier, FishTier otherTier, string otherTag)
    {
        // 规则 1：对方比玩家大 - 检查护盾
        if (otherTier > playerTier)
        {
            // 护盾检查由 PlayerFishData 处理
            return false; // 碰撞解决器不处理护盾逻辑
        }

        // 规则 2：玩家比对方大 - 可以吃掉
        if (otherTier < playerTier)
        {
            return true;
        }

        // 规则 3：同等级 - 仅吃尾部，不吃头部
        if (otherTier == playerTier)
        {
            // 碰到头部则无法吃掉
            if (otherTag == "head")
            {
                Debug.Log("[CollisionResolver] 碰到同等级鱼的头部，无法吃掉");
                return false;
            }

            // 其他部分可以吃掉
            return allowSameTierCollision;
        }

        return false;
    }

    /// <summary>
    /// 获取吃掉对方时的经验奖励
    /// 这里返回基础值，实际乘数由 ExpSystem 处理
    /// </summary>
    public int GetExpReward(FishTier otherTier)
    {
        // 根据对方等级返回基础经验值
        return (int)otherTier * 50 + 50;
    }

    /// <summary>
    /// 处理护盾保护碰撞（调试用）
    /// </summary>
    public bool CanBeSavedByShield(FishTier playerTier, FishTier otherTier)
    {
        return otherTier > playerTier;
    }
}
