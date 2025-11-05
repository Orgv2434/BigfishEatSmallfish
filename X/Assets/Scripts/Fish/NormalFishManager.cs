using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DistantLands;
using System.Linq;

[RequireComponent(typeof(Transform))]
public class NormalFishManager : GlobalFlock
{
    [Header("=== 普通鱼特有设置 ===")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;

    #region 生命周期
    protected override void Start()
    {
        base.Start();
        if (fishSchool == null || fishPrefabs.Count == 0)
        {
            Debug.LogError("鱼群父物体或预制体未赋值！");
            return;
        }
    }

    private void Update()
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0 || !isPlayerAssigned)
            return;

        if (FishGameFlowManager.Instance.CurrentState == FishGameFlowManager.GameState.GamePlaying)
        {
            if (Time.frameCount % 30 == 0)
                CleanupNullReferences();
        }
    }
    #endregion

    #region 抽象方法实现
    protected override void InitializeFishAI(GameObject fish, FishSchoolSetting setting)
    {
        // 普通鱼AI组件
        Fish fishAI = fish.GetComponent<Fish>() ?? fish.AddComponent<Fish>();
        fishAI.flock = this;
    }

    protected override void ResetFishSpecificComponents(GameObject fish, SkillFishData data, FishSchoolSetting setting)
    {
        // 普通鱼特有组件重置
        FishTierEffect normalEffect = fish.GetComponent<FishTierEffect>() ?? fish.AddComponent<FishTierEffect>();
        normalEffect.fishData = data;
    }
    #endregion

    #region 辅助方法
    private void CleanupNullReferences()
    {
        allFish.RemoveAll(fish => fish == null);
        foreach (var key in schoolFishMap.Keys.ToList())
            schoolFishMap[key].RemoveAll(fish => fish == null);
    }
    #endregion
}




