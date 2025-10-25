using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DistantLands;

[RequireComponent(typeof(Transform))]
public class SkillFishGenerate : GlobalFlock
{
    #region 配置参数（仅保留子类特有设置）
    [Header("=== 生成核心设置 ===")]
    [Tooltip("最大活跃鱼数量（超过后停止生成）")]
    public int maxFishCount = 20;
    [Tooltip("单次生成间隔（秒），控制生成频率")]
    public float spawnInterval = 1f;

    [Header("=== 球形活动区域设置 ===")]
    [Tooltip("鱼的活动球形区域半径（与SkillFishAI同步）")]
    public float spawnRadius = 10f;
    private Transform playerTransform;
    #endregion


    #region 私有变量（运行时状态）
    private List<SkillFishAI> activeFishes = new List<SkillFishAI>();
    private float spawnTimer;
    #endregion


    #region 生命周期方法
    protected override void Start()
    {
        if (fishSchool == null || fishPrefabs.Count == 0)
            {
                Debug.LogError("鱼群父物体或预制体未赋值，无法生成鱼！");
                return;
            }
            StartCoroutine(CleanupFishListCoroutine());
    }

    private void Update()
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0 || !isPlayerAssigned)
            return;

        if (FishGameFlowManager.Instance.CurrentState == FishGameFlowManager.GameState.GamePlaying)
        {
            UpdateSpawnLogic();
            // 定期清理活跃列表空引用
            if (Time.frameCount % 30 == 0)
            {
                activeFishes.RemoveAll(fish => fish == null);
            }
        }
    }
    #endregion


    #region 游戏状态变化事件（保持子类特有逻辑）


    protected override void AssignPlayerReference()
    {
        if (isPlayerAssigned) return;

        if (FishGameFlowManager.Instance != null && FishGameFlowManager.Instance.playerFish != null)
        {   
            player = FishGameFlowManager.Instance.playerFish.transform;
            isPlayerAssigned = true;
            Debug.Log("[FishAreaManager] 已从游戏管理器获取玩家引用");



            if (fishPrefabs != null && fishPrefabs.Count > 0 && activeFishes.Count == 0)
            {
                StartCoroutine(InitialSpawnRoutine());
            }
            
                        if (player != null)
            {
                UpdateAllFishPlayerReference();
            }
        }
        else
        {
            Debug.LogWarning("[FishAreaManager] 游戏已开始，但未找到玩家对象！");
        }
    }

    private void UpdateAllFishPlayerReference()
    {
        foreach (var fishAI in activeFishes)
        {
            if (fishAI != null)
            {
                fishAI.playerTransform = player;
            }
        }
    }

    private void ResetPlayerReference()
    {
        if (isPlayerAssigned)
        {
            player = null;
            isPlayerAssigned = false;
            Debug.Log("[FishAreaManager] 已重置玩家引用");
        }
    }
    #endregion


    #region 鱼生成逻辑（复用父类对象池）
    private IEnumerator InitialSpawnRoutine()
    {
        int initialCount = Mathf.Min(5, maxFishCount);
        for (int i = 0; i < initialCount; i++)
        {
            if (activeFishes.Count < maxFishCount)
            {
                SpawnFish();
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void UpdateSpawnLogic()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && activeFishes.Count < maxFishCount)
        {
            SpawnFish();
            spawnTimer = 0;
        }
    }

    private void SpawnFish()
    {
        if (!isPlayerAssigned )
    {
        Debug.LogWarning("player未就绪，暂不生成鱼");
        return;
    }
        // 随机选择预制体
        GameObject selectedPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        if (selectedPrefab == null)
        {
            Debug.LogError("[FishAreaManager] 选中的预制体为空！");
            return;
        }

        // 复用父类对象池获取鱼（不再自己实现对象池逻辑）
        GameObject fishObj = GetFishFromPool(selectedPrefab);

        // 生成挡位数据（复用父类逻辑，扩展技能类型）
        SkillFishData fishData = GenerateFishTierData();
        int fishTier = (int)fishData.fishTier;

        // 基础属性设置（位置、旋转、缩放）
        SetupFishTransform(fishObj, fishTier);

        // 重置组件状态（复用父类方法，确保数据正确）
        ResetFishComponents(fishObj, fishData);

        // 初始化AI（子类特有逻辑）
        SetupFishAI(fishObj);

    }

    // 设置位置和旋转（缩放由父类CalculateFishScale处理）
    private void SetupFishTransform(GameObject fishObj, int tier)
    {
        fishObj.transform.position = GetRandomSpawnPosition();
        fishObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        fishObj.transform.parent = fishSchool.transform; // 统一父物体（与父类一致）
        fishObj.transform.localScale = CalculateFishScale(tier); // 复用父类缩放逻辑
    }
       protected override void ResetFishComponents(GameObject fish, SkillFishData data)
        {
            // 重置挡位数据
            SkillFishTierEffect tierEffect = fish.GetComponent<SkillFishTierEffect>() ?? fish.AddComponent<SkillFishTierEffect>();
            tierEffect.fishData = data;

            // 确保碰撞体存在
            AddMeshColliderWithTrigger(fish);
        }
    #endregion


    #region AI初始化（子类特有逻辑）
    private void SetupFishAI(GameObject fishObj)
    {
        if (!fishObj.TryGetComponent<SkillFishAI>(out var fishAI))
        {
            fishAI = fishObj.AddComponent<SkillFishAI>(); // 确保组件存在
        }

        SkillFishAI prefabAI = fishObj.GetComponent<SkillFishAI>();
        if (prefabAI != null)
        {
            float randomSpeed = Random.Range(
                prefabAI.originalSpeed * 0.8f,
                prefabAI.originalSpeed * 1.2f
            );

            float randomDirInterval = Random.Range(
                prefabAI.directionChangeIntervalRange.x,
                prefabAI.directionChangeIntervalRange.y
            );

            fishAI.Initialize(
                speed: randomSpeed,
                rotSpeed: prefabAI.rotationSpeed,
                detectDist: prefabAI.detectDistance,
                safeDist: prefabAI.safeDistance,
                fleeTime: prefabAI.fleeDuration,
                fleeMultiplier: prefabAI.fleeSpeedMultiplier,
                playerTransform: player,
                center: transform.position,
                radius: spawnRadius
            );
        }
        else
        {
            Debug.LogWarning($"[FishAreaManager] {fishObj.name} 未配置SkillFishAI，使用默认参数");
            fishAI.Initialize(
                speed: Random.Range(1.5f, 2.5f),
                rotSpeed: 5f,
                detectDist: 5f,
                safeDist: 8f,
                fleeTime: 3f,
                fleeMultiplier: 1.8f,
                playerTransform: player,
                center: transform.position,
                radius: spawnRadius
            );
        }

        // 添加到子类活跃列表
        if (!activeFishes.Contains(fishAI))
            activeFishes.Add(fishAI);
    }
    #endregion


    #region 挡位系统（复用父类逻辑，扩展技能类型）
    private SkillFishData GenerateFishTierData()
    {
        SkillFishData data = GenerateFishData(); // 复用父类基础数据生成
        data.skillType = (FishSkillType)Random.Range(1, 5); // 子类扩展技能类型
        return data;
    }
    #endregion


    #region 回收逻辑（复用父类对象池）
    public void DespawnFish(GameObject fishObj)
    {
        if (fishObj == null) return;

        // 从列表移除
        var fishAI = fishObj.GetComponent<SkillFishAI>();
        if (fishAI != null)
            activeFishes.Remove(fishAI);
        allFish.Remove(fishObj);

        // 调用父类方法回收至对象池（不再自己实现回收逻辑）
        ReturnFishToPool(fishObj);
    }
    #endregion


    #region 辅助方法
    private Vector3 GetRandomSpawnPosition()
    {
        return transform.position + Random.insideUnitSphere * spawnRadius * 0.8f;
    }
    #endregion


    #region 编辑器可视化
    // protected override void OnDrawGizmos()
    // {
    //     Gizmos.color = new Color(0, 1, 0, 0.3f);
    //     Gizmos.DrawSphere(transform.position, spawnRadius);

    //     Gizmos.color = new Color(0, 1, 0, 0.8f);
    //     Gizmos.DrawWireSphere(transform.position, spawnRadius);
    // }
    #endregion
}