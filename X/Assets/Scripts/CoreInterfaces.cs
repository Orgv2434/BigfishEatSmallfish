using UnityEngine;

namespace DistantLands.Core
{
    /// <summary>
    /// 玩家数据接口 - 定义玩家数据的对外契约
    /// </summary>
    public interface IPlayerData
    {
        FishTier CurrentTier { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float CurrentSize { get; }
        int CurrentExp { get; }
        float MoveSpeed { get; }
        float SurvivalTime { get; }
        int ShieldCount { get; }

        void GainHealth(float amount);
        void LoseHealth(float amount);
        void GainExp(int baseExp);
        void AddShield(int count);
        bool UseShield();
    }

    /// <summary>
    /// 健康系统接口
    /// </summary>
    public interface IHealthSystem
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float HealthPercent { get; }

        void Heal(float amount);
        void TakeDamage(float amount);
        void SetHealth(float health);
    }

    /// <summary>
    /// 经验系统接口
    /// </summary>
    public interface IExpSystem
    {
        int CurrentExp { get; }
        int TotalExp { get; }
        FishTier CurrentTier { get; }
        int GetRequiredExpForNextTier();
        
        void AddExp(int amount, float multiplier = 1f);
        void CheckTierUpgrade();
    }

    /// <summary>
    /// 属性系统接口 - 管理体型、速度等属性
    /// </summary>
    public interface IFishStatsSystem
    {
        float CurrentSize { get; }
        float MoveSpeed { get; }
        float RotateSpeed { get; }

        void UpdateSizeByExp(int expGained);
        void UpdateSpeedByExp(int expGained);
        void ApplyTierUpgradeBonus(FishTier newTier);
        void UpdateCollider(float newSize);
    }

    /// <summary>
    /// 技能提供者接口
    /// </summary>
    public interface ISkillProvider
    {
        void ActivateSkill(FishSkillType skillType);
        bool IsSkillActive(FishSkillType skillType);
        void ClearAllSkills();
    }

    /// <summary>
    /// 碰撞处理接口
    /// </summary>
    public interface ICollisionHandler
    {
        void HandleFishCollision(FishTierEffect otherFish);
    }

    /// <summary>
    /// 碰撞解决器接口 - 纯逻辑，不修改状态
    /// </summary>
    public interface ICollisionResolver
    {
        /// <summary>
        /// 解决碰撞，返回是否继续处理
        /// </summary>
        bool ResolveCollision(FishTier playerTier, FishTier otherTier, string otherTag);
        
        /// <summary>
        /// 获取可获得的经验值
        /// </summary>
        int GetExpReward(FishTier otherTier);
    }

    /// <summary>
    /// 玩家移动接口
    /// </summary>
    public interface IPlayerMovement
    {
        void SetMovementSpeed(float moveSpeed, float rotateSpeed);
        void EnableDash();
    }
}
