using DistantLands.Core;

/// <summary>
/// ========== 玩家相关事件 ==========
/// </summary>

/// <summary>
/// 玩家健康值变化事件
/// </summary>
public class PlayerHealthChangedEvent : IGameEvent
{
    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }
    public float HealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
}

/// <summary>
/// 玩家经验获得事件
/// </summary>
public class PlayerExpGainedEvent : IGameEvent
{
    public int ExpAmount { get; set; }
    public int CurrentExp { get; set; }
    public int RequiredExpForNextTier { get; set; }
}

/// <summary>
/// 玩家等级升级事件
/// </summary>
public class PlayerTierUpgradedEvent : IGameEvent
{
    public FishTier NewTier { get; set; }
    public FishTier OldTier { get; set; }
}

/// <summary>
/// 玩家体型变化事件
/// </summary>
public class PlayerSizeChangedEvent : IGameEvent
{
    public float NewSize { get; set; }
    public float OldSize { get; set; }
}

/// <summary>
/// 玩家移动速度变化事件
/// </summary>
public class PlayerSpeedChangedEvent : IGameEvent
{
    public float MoveSpeed { get; set; }
    public float RotateSpeed { get; set; }
}

/// <summary>
/// 玩家死亡事件
/// </summary>
public class PlayerDeadEvent : IGameEvent
{
    public float SurvivalTime { get; set; }
    public FishTier FinalTier { get; set; }
    public int FinalExp { get; set; }
}

// ========== 碰撞相关事件 ==========

/// <summary>
/// 玩家碰撞到鱼的事件
/// </summary>
public class PlayerFishCollisionEvent : IGameEvent
{
    public FishTierEffect OtherFish { get; set; }
    public FishTier OtherTier { get; set; }
    public string OtherTag { get; set; }
}

/// <summary>
/// 碰撞解决结果事件
/// </summary>
public class CollisionResolvedEvent : IGameEvent
{
    public enum CollisionResult
    {
        PlayerWins,      // 玩家吃掉对方
        PlayerLoses,     // 玩家被吃（但可能被护盾挡住）
        PlayerIgnore,    // 无效碰撞
        ShieldProtected  // 护盾保护
    }

    public CollisionResult Result { get; set; }
    public int ExpGained { get; set; }
}

// ========== 技能相关事件 ==========

/// <summary>
/// 技能激活事件
/// </summary>
public class SkillActivatedEvent : IGameEvent
{
    public FishSkillType SkillType { get; set; }
    public float Duration { get; set; }
    public bool IsPermanent { get; set; }
}

/// <summary>
/// 技能过期事件
/// </summary>
public class SkillExpiredEvent : IGameEvent
{
    public FishSkillType SkillType { get; set; }
}

/// <summary>
/// 护盾被使用事件
/// </summary>
public class ShieldUsedEvent : IGameEvent
{
    public int RemainingShields { get; set; }
}

// ========== 游戏流程事件 ==========

/// <summary>
/// 游戏开始事件
/// </summary>
public class GameStartedEvent : IGameEvent
{
}

/// <summary>
/// 游戏结束事件
/// </summary>
public class GameOverEvent : IGameEvent
{
    public float SurvivalTime { get; set; }
    public FishTier FinalTier { get; set; }
}

/// <summary>
/// 游戏暂停事件
/// </summary>
public class GamePausedEvent : IGameEvent
{
    public bool IsPaused { get; set; }
}
