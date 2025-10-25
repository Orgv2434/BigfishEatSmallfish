using UnityEngine;
using System;

public static class FishEventSystem
{
    // 定义玩家挡位变化事件
    public static event Action<FishTier> OnPlayerTierChanged;

    public static void BroadcastTierChanged(FishTier newTier)
    {
        OnPlayerTierChanged?.Invoke(newTier);
    }
}