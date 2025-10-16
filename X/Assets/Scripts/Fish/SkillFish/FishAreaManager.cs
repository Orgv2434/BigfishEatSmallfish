using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DistantLands;

[RequireComponent(typeof(Transform))]
public class FishAreaManager : MonoBehaviour
{
    [Header("=== 生成核心设置 ===")]
    [Tooltip("最大活跃鱼数量（超过后停止生成）")]
    public int maxFishCount = 20;
    [Tooltip("单次生成间隔（秒），控制生成频率")]
    public float spawnInterval = 1f;
    [Tooltip("鱼预制体在Resources文件夹中的路径（例：TropicalFish）")]
    public string prefabPath = "Tropical Fish";
    [Tooltip("直接拖入预制体（优先级高于prefabPath，推荐使用）")]
    public List<GameObject> fishPrefabsDirect = new List<GameObject>();

    [Header("=== 球形活动区域设置 ===")]
    [Tooltip("鱼的活动球形区域半径（与SkillFishAI同步）")]
    public float spawnRadius = 10f;
    [Tooltip("玩家Transform引用（鱼逃窜的目标检测对象）")]
    public Transform player;

    [Header("=== 挡位系统与视觉设置 ===")]
    [Tooltip("最大挡位（0开始，数值越大稀有度越高）")]
    public int maxTier = 5;
    [Tooltip("各挡位对应的基础经验值（索引对应挡位）")]
    public int[] expValues = { 4, 10, 30, 80, 200, 500 };
    [Tooltip("各挡位对应的特效预制体（索引对应挡位）")]
    public GameObject[] tierEffects;

    [Tooltip("挡位0的基础大小（缩放值）")]
    public float baseSize = 0.8f;
    [Tooltip("每提升1个挡位增加的大小比例（例：0.3=30%）")]
    public float sizeIncreasePerTier = 0.3f;
    [Tooltip("同挡位内的大小随机波动范围（0-1）")]
    public float sizeVariance = 0.1f;

    [Header("=== 对象池设置 ===")]
    [Tooltip("每个预制体的最大池容量")]
    public int maxPoolSizePerPrefab = 5;

    private List<SkillFishAI> activeFishes = new List<SkillFishAI>();
    private List<GameObject> fishPrefabs;
    private float spawnTimer;
    
    private Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
    private HashSet<GameObject> inactiveObjects = new HashSet<GameObject>();

    private void Awake()
    {
        LoadFishPrefabs();
        InitializeObjectPools();
    }

    private void Start()
    {
        CheckCriticalSettings();
        
        if (fishPrefabs != null && fishPrefabs.Count > 0)
        {
            StartCoroutine(InitialSpawnRoutine());
        }
    }

    private void Update()
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0 || player == null)
            return;

        UpdateSpawnLogic();
        CleanupInvalidReferences();
    }

    #region 预制体加载与初始化
    private void LoadFishPrefabs()
    {
        // 优先使用直接拖入的预制体列表
        if (fishPrefabsDirect != null && fishPrefabsDirect.Count > 0)
        {
            fishPrefabs = fishPrefabsDirect.Where(p => p != null).ToList();
            Debug.Log($"[FishAreaManager] 从直接赋值加载 {fishPrefabs.Count} 个预制体");
            return;
        }

        // 从Resources加载作为备选
        if (!string.IsNullOrEmpty(prefabPath))
        {
            var loadedPrefabs = Resources.LoadAll<GameObject>(prefabPath);
            fishPrefabs = loadedPrefabs.Where(p => p != null).ToList();

            if (fishPrefabs.Count > 0)
            {
                Debug.Log($"[FishAreaManager] 从Resources/{prefabPath} 加载 {fishPrefabs.Count} 个预制体");
            }
            else
            {
                Debug.LogError($"[FishAreaManager] 在Resources/{prefabPath} 未找到预制体！");
            }
        }
        else
        {
            Debug.LogError($"[FishAreaManager] 未配置预制体路径且未直接赋值预制体！");
        }
    }

    private void InitializeObjectPools()
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0) return;

        foreach (var prefab in fishPrefabs)
        {
            string prefabKey = GetPrefabKey(prefab);
            if (!objectPools.ContainsKey(prefabKey))
            {
                objectPools[prefabKey] = new Queue<GameObject>();
            }
        }
    }

    private void CheckCriticalSettings()
    {
        if (player == null)
        {
            Debug.LogWarning("[FishAreaManager] 未指定玩家引用，鱼将无法触发逃窜行为！");
        }

        if (spawnRadius <= 0)
        {
            Debug.LogWarning("[FishAreaManager] 活动区域半径不能为0，已自动设置为10");
            spawnRadius = 10f;
        }

        if (maxTier <= 0)
        {
            Debug.LogWarning("[FishAreaManager] 最大挡位不能小于1，已自动设置为5");
            maxTier = 5;
        }
    }
    #endregion

    #region 鱼生成逻辑
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
        GameObject selectedPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        if (selectedPrefab == null)
        {
            Debug.LogError("[FishAreaManager] 选中的预制体为空！");
            return;
        }

        string prefabKey = GetPrefabKey(selectedPrefab);
        GameObject fishObj = GetFishFromPoolOrInstantiate(selectedPrefab, prefabKey);

        // 生成挡位数据
        SkillFishData fishData = GenerateFishTierData();
        int fishTier = (int)fishData.fishTier;

        // 设置鱼的基础属性
        SetupFishTransform(fishObj, fishTier);

        // 初始化挡位特效
        SetupFishTierEffect(fishObj, fishData);

        // 初始化AI（核心：与SkillFishAI协同）
        SetupFishAI(fishObj);

        // 添加到活跃列表
        if (fishObj.TryGetComponent<SkillFishAI>(out var fishAI) && !activeFishes.Contains(fishAI))
        {
            activeFishes.Add(fishAI);
        }
    }

    private GameObject GetFishFromPoolOrInstantiate(GameObject prefab, string prefabKey)
    {
        // 尝试从对象池获取
        if (objectPools.TryGetValue(prefabKey, out var pool) && pool.Count > 0)
        {
            GameObject fishObj = pool.Dequeue();
            fishObj.SetActive(true);
            inactiveObjects.Remove(fishObj);
            return fishObj;
        }

        // 对象池无闲置，实例化新对象
        GameObject newFish = Instantiate(prefab, transform);
        newFish.name = $"{prefab.name}_Instance";
        
        // 添加必要组件
        AddRequiredComponents(newFish);
        return newFish;
    }

    private void AddRequiredComponents(GameObject fishObj)
    {
        // 添加AI组件
        if (!fishObj.TryGetComponent<SkillFishAI>(out _))
        {
            fishObj.AddComponent<SkillFishAI>();
        }

        // 添加挡位特效组件
        if (!fishObj.TryGetComponent<FishTierEffect>(out _))
        {
            fishObj.AddComponent<FishTierEffect>();
        }

        // 添加碰撞体（用于交互检测）
        AddMeshColliderWithTrigger(fishObj);
    }

    private void SetupFishTransform(GameObject fishObj, int tier)
    {
        // 随机位置（球形区域内）
        fishObj.transform.position = GetRandomSpawnPosition();
        
        // 随机旋转（Y轴）
        fishObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        // 根据挡位设置大小
        fishObj.transform.localScale = CalculateFishScale(tier);
    }
    #endregion

    #region AI初始化（与SkillFishAI协同）
    private void SetupFishAI(GameObject prefab)
    {
        if (!prefab.TryGetComponent<SkillFishAI>(out var fishAI))
        {
            Debug.LogError($"[FishAreaManager] {prefab.name} 缺少SkillFishAI组件！");
            return;
        }

        // 从预制体获取AI参数（优先使用预制体配置）
        SkillFishAI prefabAI = prefab.GetComponent<SkillFishAI>();
        if (prefabAI != null)
        {
            // 速度在预制体基础上随机波动±20%
            float randomSpeed = Random.Range(
                prefabAI.originalSpeed * 0.8f,
                prefabAI.originalSpeed * 1.2f
            );

            // 变向间隔在预制体设置的范围内随机
            float randomDirInterval = Random.Range(
                prefabAI.directionChangeIntervalRange.x,
                prefabAI.directionChangeIntervalRange.y
            );

            // 初始化AI（参数与SkillFishAI保持一致）
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
            // 容错：预制体无AI组件时使用默认值
            Debug.LogWarning($"[FishAreaManager] 预制体 {prefab.name} 未配置SkillFishAI组件，使用默认参数");
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
    }
    #endregion

    #region 挡位系统与特效
    private SkillFishData GenerateFishTierData()
    {
        SkillFishData data = new SkillFishData();

        // 高斯分布生成挡位（集中在低挡位）
        float mean = 1f;
        float stdDev = 0.8f;
        int randomTier = Mathf.RoundToInt(GenerateGaussian(mean, stdDev));
        randomTier = Mathf.Clamp(randomTier, 0, maxTier - 1);

        // 设置经验值
        data.baseExpValue = (expValues != null && randomTier < expValues.Length) 
            ? expValues[randomTier] 
            : 10;

        data.fishTier = (FishTier)randomTier;
        data.skillType = (FishSkillType)Random.Range(1, 5);
        return data;
    }

    private void SetupFishTierEffect(GameObject fishObj, SkillFishData data)
    {
        if (!fishObj.TryGetComponent<FishTierEffect>(out var tierEffect))
        {
            Debug.LogError($"[FishAreaManager] {fishObj.name} 缺少FishTierEffect组件！");
            return;
        }

        tierEffect.fishData = data;
        int tierIndex = (int)data.fishTier;

        // 清除旧特效
        ClearOldTierEffects(fishObj);

        // 应用新特效
        if (tierEffects != null && tierIndex < tierEffects.Length && tierEffects[tierIndex] != null)
        {
            GameObject effect = Instantiate(tierEffects[tierIndex], fishObj.transform);
            effect.tag = "TierEffect";
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
        }
    }

    private void ClearOldTierEffects(GameObject fishObj)
    {
        foreach (Transform child in fishObj.transform)
        {
            if (child.CompareTag("TierEffect"))
            {
                Destroy(child.gameObject);
            }
        }
    }
    #endregion

    #region 对象池与回收
    public void DespawnFish(GameObject fishObj)
    {
        if (fishObj == null) return;

        // 从活跃列表移除
        var fishAI = fishObj.GetComponent<SkillFishAI>();
        if (fishAI != null)
        {
            activeFishes.Remove(fishAI);
        }

        // 回收或销毁
        string prefabKey = GetPrefabKey(fishObj);
        if (objectPools.TryGetValue(prefabKey, out var pool) && pool.Count < maxPoolSizePerPrefab)
        {
            fishObj.SetActive(false);
            pool.Enqueue(fishObj);
            inactiveObjects.Add(fishObj);
        }
        else
        {
            Destroy(fishObj);
        }
    }

    private string GetPrefabKey(GameObject obj)
    {
        return obj.name.Split('(')[0].Trim();
    }
    #endregion

    #region 辅助方法
    private float GenerateGaussian(float mean, float stdDev)
    {
        float u1 = Random.value;
        float u2 = Random.value;
        float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + z * stdDev;
    }

    private Vector3 CalculateFishScale(int tier)
    {
        float baseScale = baseSize + (tier * sizeIncreasePerTier);
        float randomScale = baseScale * Random.Range(1 - sizeVariance, 1 + sizeVariance);
        return Vector3.one * Mathf.Max(randomScale, baseSize * 0.8f);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return transform.position + Random.insideUnitSphere * spawnRadius * 0.8f;
    }

    private void AddMeshColliderWithTrigger(GameObject fishObj)
    {
        MeshFilter meshFilter = fishObj.GetComponent<MeshFilter>() ?? fishObj.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            MeshCollider collider = fishObj.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = fishObj.AddComponent<MeshCollider>();
            }
            collider.sharedMesh = meshFilter.mesh;
            collider.convex = true;
            collider.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"[FishAreaManager] {fishObj.name} 未找到MeshFilter，无法添加碰撞体");
        }
    }

    private void CleanupInvalidReferences()
    {
        if (Time.frameCount % 30 == 0)
        {
            activeFishes.RemoveAll(fish => fish == null);
        }
    }
    #endregion

    #region 编辑器可视化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, spawnRadius);

        Gizmos.color = new Color(0, 1, 0, 0.8f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
    #endregion
}

