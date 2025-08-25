using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;
using System.Linq;

[DisallowMultipleComponent]
public class GlobalFlock : NetworkBehaviour
{
    public static GlobalFlock Instance { get; private set; }

    [Tooltip("全局通用的鱼预制体（如果生成点未指定则使用这个）")]
    public List<GameObject> defaultFishPrefabs = new List<GameObject>();

    [Tooltip("鱼群父物体（所有生成的鱼都会放在这里）")]
    public GameObject fishSchool;

    [Tooltip("所有生成区域配置（在编辑器中添加多个）")]
    public List<FishSpawnZone> spawnZones = new List<FishSpawnZone>();

    [Tooltip("全局最大挡位（生成点未指定时使用）")]
    public int globalMaxTier = 5;

    [HideInInspector]
    public List<GameObject> allFish = new List<GameObject>();
    public static Vector3 goalPos = Vector3.zero;

    // 网络同步目标位置
    private NetworkVariable<Vector3> networkGoalPos = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private int fishIdCounter = 0;
    private Dictionary<int, GameObject> fishIdToObjectMap = new Dictionary<int, GameObject>();
    public FishObjectPool fishObjectPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

  
        void Start()
        {
            if (!IsNetworkInitialized())
            {
                // 单机模式：直接初始化
                LoadDefaultPrefabs();
                SpawnAllZones();
            }
            // 网络模式下不在Start中执行，等待OnNetworkSpawn
        }

    

    private void OnGoalPosChanged(Vector3 oldVal, Vector3 newVal)
    {
        goalPos = newVal;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 网络模式：确保网络就绪后再初始化
            LoadDefaultPrefabs();
            // 过滤无效预制体（关键修复）
            FilterInvalidPrefabs();
            SpawnAllZones();
            networkGoalPos.Value = goalPos;
        }
    }
    // 过滤所有空预制体，避免传入对象池
    private void FilterInvalidPrefabs()
    {
        // 过滤全局默认预制体
        defaultFishPrefabs = defaultFishPrefabs.Where(p => p != null).ToList();

        // 过滤每个区域的自定义预制体
        foreach (var zone in spawnZones)
        {
            zone.customPrefabs = zone.customPrefabs.Where(p => p != null).ToList();
        }
    }
    // 初始化对象池（添加空检查）
    private void InitializeObjectPool()
    {
        if (fishObjectPool == null) return;

        HashSet<GameObject> allPrefabs = new HashSet<GameObject>(defaultFishPrefabs);
        foreach (var zone in spawnZones)
        {
            if (zone.enabled && zone.customPrefabs != null)
            {
                foreach (var prefab in zone.customPrefabs)
                {
                    if (prefab != null) // 跳过空预制体
                    {
                        allPrefabs.Add(prefab);
                    }
                }
            }
        }

        int totalPrefabs = allPrefabs.Count;
        if (totalPrefabs == 0)
        {
            Debug.LogError("没有有效的鱼预制体，无法初始化对象池！");
            return; // 避免后续错误
        }

        int totalFish = spawnZones.Sum(z => z.enabled ? z.spawnCount : 0);
        int perPrefabCount = Mathf.Max(1, totalFish / totalPrefabs);

        foreach (var prefab in allPrefabs)
        {
            if (prefab != null)
            {
                fishObjectPool.InitializePool(prefab, perPrefabCount);
            }
        }
    }
    // 加载全局默认预制体
    private void LoadDefaultPrefabs()
    {
        if (defaultFishPrefabs.Count > 0) return;

        // 从Resources加载（保持原有逻辑）
        defaultFishPrefabs = FishPrefabLoader.LoadFishPrefabsFromResources("Tropical Fish");
    }

    // 生成所有区域的鱼群
    private void SpawnAllZones()
    {
        if (allFish.Count > 0) return;

        // 初始化对象池（合并所有生成点的预制体需求）
        InitializeObjectPool();

        // 遍历所有生成区域并生成鱼
        foreach (var zone in spawnZones)
        {
            if (zone.enabled) // 跳过禁用的区域
            {
                SpawnFishInZone(zone);
            }
        }
    }



    private void SpawnFishInZone(FishSpawnZone zone)
    {
        // 区域配置验证
        if (zone.spawnCenter == null)
        {
            Debug.LogWarning($"生成区域 {zone.name} 未设置中心点，已跳过");
            return;
        }
        if (zone.spawnCount <= 0)
        {
            Debug.LogWarning($"生成区域 {zone.name} 数量设置为0，已跳过");
            return;
        }

        // 确定当前区域使用的预制体（优先使用区域自定义的，否则用全局默认）
        List<GameObject> usedPrefabs = zone.customPrefabs.Count > 0
            ? zone.customPrefabs
            : defaultFishPrefabs;

        // 额外检查：确保有可用预制体
        if (usedPrefabs.Count == 0)
        {
            Debug.LogError($"生成区域 {zone.name} 没有可用预制体（已过滤空对象）");
            return;
        }

        // 生成该区域的鱼（合并重复的循环逻辑）
        for (int i = 0; i < zone.spawnCount; i++)
        {
            GameObject fish = null;
            // 随机选择该区域允许的预制体
            GameObject randomPrefab = usedPrefabs[UnityEngine.Random.Range(0, usedPrefabs.Count)];

            // 关键：检查预制体是否为空
            if (randomPrefab == null)
            {
                Debug.Log("随机选中的预制体为空，跳过生成");
                continue;
            }

            // 从对象池获取或实例化
            if (fishObjectPool != null)
            {
                fish = fishObjectPool.GetFishFromPool(randomPrefab);
            }
            else
            {
                fish = Instantiate(randomPrefab);
            }

            // 安全检查：确保鱼对象创建成功
            if (fish == null)
            {
                Debug.LogError($"无法创建鱼对象，预制体: {randomPrefab.name}");
                continue;
            }

            // 设置鱼的初始位置（基于区域配置）
            Vector3 spawnPos = zone.spawnCenter.position +
                              UnityEngine.Random.insideUnitSphere * zone.spawnRadius;
            spawnPos.y = zone.lockYPosition ? zone.spawnCenter.position.y : spawnPos.y;

            fish.transform.position = spawnPos;
            fish.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            fish.transform.localScale = Vector3.one * (UnityEngine.Random.value * 0.2f + 0.9f);

            AddMeshColliderWithTrigger(fish);

            // 生成鱼数据（使用区域指定的挡位范围）
            SkillFishData fishData = GenerateSkillFishData(zone);
            fishData.id = fishIdCounter++;
            fishData.originalPrefab = randomPrefab;

            // 初始化鱼组件
            FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>() ?? fish.AddComponent<FishTierEffect>();
            tierEffect.fishData = fishData;
            Fish fishScript = fish.GetComponent<Fish>() ?? fish.AddComponent<Fish>();
            fishScript.flock = this;
            fishScript.myZone = zone; // 绑定鱼到所属区域

            // 网络生成
            NetworkObject fishNetworkObj = fish.GetComponent<NetworkObject>();
            if (fishNetworkObj == null)
            {
                fishNetworkObj = fish.AddComponent<NetworkObject>();
            }
            if (IsServer && !fishNetworkObj.IsSpawned)
            {
                fishNetworkObj.Spawn();
            }

            // 添加到管理列表（避免重复添加检查）
            if (!allFish.Contains(fish))
            {
                allFish.Add(fish);
                fishIdToObjectMap[fishData.id] = fish;
            }
        }

        Debug.Log($"已在区域 {zone.name} 生成 {zone.spawnCount} 条鱼");
    }

    // 生成鱼数据（支持区域自定义挡位范围）
    public SkillFishData GenerateSkillFishData(FishSpawnZone zone)
    {
        SkillFishData data = new SkillFishData();

        // 确定挡位范围（区域有配置则用区域的，否则用全局的）
        int minTier = zone.overrideTierRange ? zone.minTier : 0;
        int maxTier = zone.overrideTierRange ? zone.maxTier : globalMaxTier - 1;
        maxTier = Mathf.Clamp(maxTier, minTier, globalMaxTier - 1); // 不超过全局最大

        // 随机挡位（在区域指定的范围内）
        float mean = (minTier + maxTier) / 2f; // 均值居中
        float stdDev = (maxTier - minTier) / 3f; // 标准差控制分散度
        int randomTier = Mathf.RoundToInt(GenerateGaussian(mean, stdDev));
        randomTier = Mathf.Clamp(randomTier, minTier, maxTier);

        // 经验值（保持原有逻辑）
        int[] expValues = { 4, 10, 30, 80, 200 };
        data.baseExpValue = expValues[Mathf.Min(randomTier, expValues.Length - 1)];
        data.fishTier = (FishTier)randomTier;

        // 随机技能
        Array skillTypes = Enum.GetValues(typeof(FishSkillType));
        data.skillType = (FishSkillType)skillTypes.GetValue(UnityEngine.Random.Range(0, skillTypes.Length));

        return data;
    }

    // 以下为原有代码（未修改部分）
    private float GenerateGaussian(float mean, float stdDev)
    {
        float u1 = UnityEngine.Random.value;
        float u2 = UnityEngine.Random.value;
        float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + z * stdDev;
    }

    void Update()
    {
        ValidateFishList();
    }

    private void ValidateFishList()
    {
        if (allFish == null) return;
        for (int i = allFish.Count - 1; i >= 0; i--)
        {
            if (allFish[i] == null)
            {
                allFish.RemoveAt(i);
            }
        }
    }

    private void AddMeshColliderWithTrigger(GameObject fishObj)
    {
        MeshFilter meshFilter = fishObj.GetComponent<MeshFilter>() ?? fishObj.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            MeshCollider collider = fishObj.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.mesh;
            collider.convex = true;
            collider.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"鱼 {fishObj.name} 缺少MeshFilter，无法添加碰撞体");
        }
    }

    private void OnDrawGizmos()
    {
        // 绘制所有生成区域
        foreach (var zone in spawnZones)
        {
            if (zone.enabled && zone.spawnCenter != null)
            {
                // 每个区域用不同颜色区分
                Gizmos.color = zone.gizmoColor;
                Gizmos.DrawWireSphere(zone.spawnCenter.position, zone.spawnRadius);

                // 绘制区域名称标签
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    zone.spawnCenter.position + Vector3.up * (zone.spawnRadius + 0.5f),
                    $"{zone.name} ({zone.spawnCount})"
                );
#endif
            }
        }
    }
    private bool IsNetworkInitialized()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    public void RemoveFishFromMap(int fishId)
    {
        if (fishIdToObjectMap.ContainsKey(fishId))
        {
            fishIdToObjectMap.Remove(fishId);
        }
    }

    public GameObject GetFishByID(int fishId)
    {
        if (fishIdToObjectMap.TryGetValue(fishId, out GameObject fish))
        {
            return fish;
        }
        return null;
    }

    public void HandleFishDestruction(int fishId)
    {
        GameObject fish = GetFishByID(fishId);
        if (fish == null) return;

        FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>();
        if (tierEffect != null)
        {
            GameObject originalPrefab = tierEffect?.fishData?.originalPrefab;
            allFish.Remove(fish);
            RemoveFishFromMap(fishId);

            if (fishObjectPool != null && originalPrefab != null)
            {
                fishObjectPool.ReturnFishToPool(fish, originalPrefab);
                fish.SetActive(false);
            }
        }
        else
        {
            Debug.Log("没有找到预制体");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyServerToDestroyFishServerRpc(int fishId)
    {
        ServerHandleFishDestruction(fishId);
    }

    public void ServerHandleFishDestruction(int fishId)
    {
        GameObject fish = GetFishByID(fishId);
        if (fish == null) return;

        if (!IsServer) return;

        NetworkObject fishNetworkObj = fish.GetComponent<NetworkObject>();
        if (fishNetworkObj == null)
        {
            Debug.LogError($"鱼 {fishId} 没有 NetworkObject 组件，无法网络同步");
            return;
        }

        allFish.Remove(fish);
        RemoveFishFromMap(fishId);
        fishNetworkObj.Despawn(true);
        Debug.Log($"服务器：鱼 {fishId} 已销毁，同步至所有客户端");
    }
}

// 生成区域配置（可在编辑器中添加多个）
[Serializable]
public class FishSpawnZone
{
    [Tooltip("区域名称（用于识别）")]
    public string name = "Spawn Zone";

    [Tooltip("是否启用该区域")]
    public bool enabled = true;

    [Tooltip("生成区域中心点")]
    public Transform spawnCenter;

    [Tooltip("生成区域半径")]
    public float spawnRadius = 5f;

    [Tooltip("该区域生成的鱼数量")]
    public int spawnCount = 10;

    [Tooltip("是否锁定Y轴位置（避免上下漂浮）")]
    public bool lockYPosition = true;

    [Tooltip("该区域专用的鱼预制体（空则使用全局默认）")]
    public List<GameObject> customPrefabs = new List<GameObject>();

    [Tooltip("是否覆盖全局挡位范围")]
    public bool overrideTierRange = false;

    [Tooltip("最小挡位（仅当覆盖范围时生效）")]
    public int minTier = 0;

    [Tooltip("最大挡位（仅当覆盖范围时生效）")]
    public int maxTier = 2;

    [Tooltip("编辑器中Gizmo的颜色（用于区分区域）")]
    public Color gizmoColor = new Color(0, 1, 0, 0.5f);
}
