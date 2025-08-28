using System.Collections.Generic;
using System.Globalization;
using System;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class GlobalFlock : NetworkBehaviour
{
    public static GlobalFlock Instance { get; private set; }
    public NetworkVariable<Vector3> NetworkGoalPos { get => networkGoalPos; set => networkGoalPos = value; }

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

    // 新增：网络同步已生成的鱼数据（供延迟加入客户端使用）
    private NetworkVariable<NetworkFishSpawnDataList> syncedFishData = new NetworkVariable<NetworkFishSpawnDataList>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server,
        value: new NetworkFishSpawnDataList()
    );
    private NetworkVariable<Vector3> networkGoalPos = new NetworkVariable<Vector3>(
    writePerm: NetworkVariableWritePermission.Server
);
    private int fishIdCounter = 0;
    private Dictionary<int, GameObject> fishIdToObjectMap = new Dictionary<int, GameObject>();
    public FishObjectPool fishObjectPool;

    // 缓存所有可用预制体（用于索引映射，避免序列化问题）
    private List<GameObject> allAvailablePrefabs = new List<GameObject>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 注册客户端连接回调（辅助延迟加入处理）
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void Start()
    {
        // 仅单机模式/服务器端执行初始化（客户端不自主生成）
        bool isServerOrStandalone = false;
        if (NetworkManager.Singleton == null)
        {
            // 未初始化网络，视为单机模式，当作服务器处理
            isServerOrStandalone = true;
        }
        else
        {
            isServerOrStandalone = IsServer || NetworkManager.Singleton.IsServer;
        }

        if (!IsNetworkInitialized() && isServerOrStandalone)
        {
            LoadDefaultPrefabs();
            InitAllAvailablePrefabs();
            SpawnAllZones();
        }
    }

    public override void OnDestroy()
    {
        // 取消回调订阅，避免内存泄漏
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // 取消网络变量监听
        networkGoalPos.OnValueChanged -= OnGoalPosChanged;
        if (IsClient)
        {
            syncedFishData.OnValueChanged -= OnSyncedFishDataChanged;
        }
    }

    // 客户端连接回调（服务器触发，通知新客户端同步数据）
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer && clientId != NetworkManager.ServerClientId)
        {
            Debug.Log($"客户端 {clientId} 延迟加入，触发鱼数据同步");
            // 主动同步当前所有鱼数据（确保新客户端能获取完整数据）
            SyncAllFishToClientServerRpc(clientId);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkGoalPos.OnValueChanged += OnGoalPosChanged;
        if (IsServer)
        {
            // 服务器端初始化：加载预制体+生成鱼
            LoadDefaultPrefabs();
            FilterInvalidPrefabs();
            InitAllAvailablePrefabs();
            SpawnAllZones();
            NetworkGoalPos.Value = goalPos;
        }
        else if (IsClient)
        {
            // 客户端初始化预制体列表
            LoadDefaultPrefabs();
            FilterInvalidPrefabs();
            InitAllAvailablePrefabs();
            // 监听鱼数据变化，同步服务器已生成的鱼
            syncedFishData.OnValueChanged += OnSyncedFishDataChanged;
            // 立即同步当前已存在的鱼数据
            OnSyncedFishDataChanged(default, syncedFishData.Value);
        }
    }

    // 新增：初始化所有可用预制体列表（用于索引映射）
    private void InitAllAvailablePrefabs()
    {
        allAvailablePrefabs.Clear();
        // 添加全局默认预制体
        allAvailablePrefabs.AddRange(defaultFishPrefabs.Where(p => p != null));
        // 添加所有区域自定义预制体（去重）
        foreach (var zone in spawnZones)
        {
            foreach (var prefab in zone.customPrefabs.Where(p => p != null && !allAvailablePrefabs.Contains(p)))
            {
                allAvailablePrefabs.Add(prefab);
            }
        }
    }

    // 新增：服务器主动同步所有鱼数据到指定客户端
    [ServerRpc(RequireOwnership = false)]
    private void SyncAllFishToClientServerRpc(ulong targetClientId)
    {
        if (!IsServer) return;

        var fishDataList = syncedFishData.Value.Items;
        if (fishDataList.Count == 0) return;

        // 向目标客户端发送完整鱼数据
        SyncFishDataClientRpc(new NetworkFishSpawnDataList(fishDataList), new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { targetClientId } }
        });
    }

    // 新增：客户端接收服务器同步的鱼数据
    [ClientRpc]
    private void SyncFishDataClientRpc(NetworkFishSpawnDataList fishDataList, ClientRpcParams clientRpcParams = default)
    {
        if (!IsClient) return;

        foreach (var data in fishDataList.Items)
        {
            SpawnSyncedFish(data);
        }
    }

    // 新增：鱼数据变化时同步（延迟加入核心逻辑）
    private void OnSyncedFishDataChanged(NetworkFishSpawnDataList previous, NetworkFishSpawnDataList current)
    {
        if (!IsClient) return;

        // 修复：先检查previous是否为null，再安全获取Items
        var previousItems = (previous as NetworkFishSpawnDataList?) != null
     ? previous.Items
     : new List<FishSpawnData>();
        var newFishData = current.Items.Except(previousItems, new FishSpawnDataComparer()).ToList();

        foreach (var data in newFishData)
        {
            SpawnSyncedFish(data);
        }
    }

    // 新增：根据同步数据生成鱼（客户端专用）
    private void SpawnSyncedFish(FishSpawnData data)
    {
        // 检查是否已生成（通过ID映射表，更高效）
        if (fishIdToObjectMap.ContainsKey(data.fishId))
        {
            return;
        }

        // 获取预制体（通过索引映射）
        GameObject prefab = allAvailablePrefabs.ElementAtOrDefault(data.prefabIndex);
        if (prefab == null)
        {
            Debug.LogError($"客户端同步鱼失败：预制体索引 {data.prefabIndex} 无效");
            return;
        }

        // 从对象池获取或实例化（客户端不主动Spawn，仅同步状态）
        GameObject fish = null;
        if (fishObjectPool != null)
        {
            fish = fishObjectPool.GetFishFromPool(prefab);
        }
        else
        {
            fish = Instantiate(prefab);
        }

        if (fish == null)
        {
            Debug.LogError($"客户端生成鱼失败：预制体 {prefab.name} 实例化失败");
            return;
        }

        // 还原鱼的状态（位置、旋转、缩放）
        fish.transform.position = data.position;
        fish.transform.rotation = data.rotation;
        fish.transform.localScale = data.scale;

        // 初始化鱼组件
        FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>();
        tierEffect.fishData = new SkillFishData
        {
            id = data.fishId,
            originalPrefab = prefab,
            fishTier = (FishTier)data.tier,
            baseExpValue = data.baseExp,
            skillType = (FishSkillType)data.skillType
        };

        Fish fishScript = fish.GetComponent<Fish>();
        fishScript.flock = this;
        // 绑定所属区域（通过区域名称匹配）
        fishScript.myZone = spawnZones.FirstOrDefault(z => z.name == data.zoneName);



        // 添加到管理列表
        allFish.Add(fish);
        fishIdToObjectMap[data.fishId] = fish;
        Debug.Log($"客户端同步生成鱼：ID {data.fishId}，预制体 {prefab.name}");
    }

    // 原有逻辑：目标位置变化回调
    private static void OnGoalPosChanged(Vector3 oldVal, Vector3 newVal)
    {
        goalPos = newVal;
    }

    // 原有逻辑：过滤无效预制体
    private void FilterInvalidPrefabs()
    {
        defaultFishPrefabs = defaultFishPrefabs.Where(p => p != null).ToList();
        foreach (var zone in spawnZones)
        {
            zone.customPrefabs = zone.customPrefabs.Where(p => p != null).ToList();
        }
    }

    // 原有逻辑：初始化对象池（仅服务器执行）
    private void InitializeObjectPool()
    {
        if (fishObjectPool == null || !IsServer) return;

        HashSet<GameObject> allPrefabs = new HashSet<GameObject>(defaultFishPrefabs);
        foreach (var zone in spawnZones)
        {
            if (zone.enabled && zone.customPrefabs != null)
            {
                foreach (var prefab in zone.customPrefabs.Where(p => p != null))
                {
                    allPrefabs.Add(prefab);
                }
            }
        }

        int totalPrefabs = allPrefabs.Count;
        if (totalPrefabs == 0)
        {
            Debug.LogError("没有有效的鱼预制体，无法初始化对象池！");
            return;
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

    // 原有逻辑：加载全局默认预制体
    private void LoadDefaultPrefabs()
    {
        if (defaultFishPrefabs.Count > 0) return;
        defaultFishPrefabs = FishPrefabLoader.LoadFishPrefabsFromResources("Tropical Fish");
    }

    // 原有逻辑：生成所有区域的鱼群（仅服务器执行）
    private void SpawnAllZones()
    {
        if (allFish.Count > 0 || !IsServer) return;

        InitializeObjectPool();
        foreach (var zone in spawnZones)
        {
            if (zone.enabled)
            {
                SpawnFishInZone(zone);
            }
        }
    }

    // 核心修改：生成单个区域的鱼（仅服务器执行，添加数据同步）
    private void SpawnFishInZone(FishSpawnZone zone)
    {
        // 仅服务器执行生成逻辑（彻底避免客户端重复生成）
        if (!IsServer) return;

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

        // 确定当前区域使用的预制体
        List<GameObject> usedPrefabs = zone.customPrefabs.Count > 0
            ? zone.customPrefabs
            : defaultFishPrefabs;

        if (usedPrefabs.Count == 0)
        {
            Debug.LogError($"生成区域 {zone.name} 没有可用预制体（已过滤空对象）");
            return;
        }

        // 生成鱼
        for (int i = 0; i < zone.spawnCount; i++)
        {
            GameObject randomPrefab = usedPrefabs[UnityEngine.Random.Range(0, usedPrefabs.Count)];
            if (randomPrefab == null)
            {
                Debug.Log("随机选中的预制体为空，跳过生成");
                continue;
            }

            // 从对象池获取或实例化
            GameObject fish = null;
            if (fishObjectPool != null)
            {
                fish = fishObjectPool.GetFishFromPool(randomPrefab);
            }
            else
            {
                fish = Instantiate(randomPrefab);
            }

            if (fish == null)
            {
                Debug.LogError($"无法创建鱼对象，预制体: {randomPrefab.name}");
                continue;
            }

            // 设置初始状态
            Vector3 spawnPos = zone.spawnCenter.position +
                              UnityEngine.Random.insideUnitSphere * zone.spawnRadius;
            spawnPos.y = zone.lockYPosition ? zone.spawnCenter.position.y : spawnPos.y;

            fish.transform.position = spawnPos;
            fish.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            fish.transform.localScale = Vector3.one * (UnityEngine.Random.value * 0.2f + 0.9f);

            // 生成鱼数据
            SkillFishData fishData = GenerateSkillFishData(zone);
            fishData.id = fishIdCounter++;
            fishData.originalPrefab = randomPrefab;

            // 初始化组件
            FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>();
            tierEffect.fishData = fishData;
            Fish fishScript = fish.GetComponent<Fish>();
            if (fishScript != null)
            {
                fishScript.flock = this;
                fishScript.myZone = zone;
            }
            else Debug.LogWarning(fish.gameObject+"没有找到fish");
            // 网络生成（仅服务器执行）
            NetworkObject fishNetworkObj = fish.GetComponent<NetworkObject>();

            // 确保NetworkObject在设置父对象前已生成
            if (!fishNetworkObj.IsSpawned)
            {
                // 检查Hash冲突（避免重复生成）
                if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsValue(fishNetworkObj))
                {
                    fishNetworkObj.Spawn();
                }
                else
                {
                    Debug.LogWarning($"鱼 {fishData.id} Hash冲突，跳过重复生成");
                    if (fishObjectPool != null)
                    {
                        fishObjectPool.ReturnFishToPool(fish, randomPrefab);
                    }
                    else
                    {
                        Destroy(fish);
                    }
                    continue;
                }
            }


            // 添加到管理列表
            if (!allFish.Contains(fish))
            {
                allFish.Add(fish);
                fishIdToObjectMap[fishData.id] = fish;

                // 新增：记录鱼数据到网络变量（供客户端同步）
                var spawnData = new FishSpawnData
                {
                    fishId = fishData.id,
                    prefabIndex = allAvailablePrefabs.IndexOf(randomPrefab),
                    position = fish.transform.position,
                    rotation = fish.transform.rotation,
                    scale = fish.transform.localScale,
                    tier = (int)fishData.fishTier,
                    baseExp = fishData.baseExpValue,
                    skillType = (int)fishData.skillType,
                    zoneName = zone.name
                };

                // 重要：创建新列表触发NetworkVariable同步
                var updatedFishData = new List<FishSpawnData>(syncedFishData.Value.Items);
                updatedFishData.Add(spawnData);
                syncedFishData.Value = new NetworkFishSpawnDataList(updatedFishData);
            }
        }

        Debug.Log($"服务器已在区域 {zone.name} 生成 {zone.spawnCount} 条鱼");
    }

    // 原有逻辑：生成鱼数据
    public SkillFishData GenerateSkillFishData(FishSpawnZone zone)
    {
        SkillFishData data = new SkillFishData();

        int minTier = zone.overrideTierRange ? zone.minTier : 0;
        int maxTier = zone.overrideTierRange ? zone.maxTier : globalMaxTier - 1;
        maxTier = Mathf.Clamp(maxTier, minTier, globalMaxTier - 1);

        float mean = (minTier + maxTier) / 2f;
        float stdDev = (maxTier - minTier) / 3f;
        int randomTier = Mathf.RoundToInt(GenerateGaussian(mean, stdDev));
        randomTier = Mathf.Clamp(randomTier, minTier, maxTier);

        int[] expValues = { 4, 10, 30, 80, 200 };
        data.baseExpValue = expValues[Mathf.Min(randomTier, expValues.Length - 1)];
        data.fishTier = (FishTier)randomTier;

        Array skillTypes = Enum.GetValues(typeof(FishSkillType));
        data.skillType = (FishSkillType)skillTypes.GetValue(UnityEngine.Random.Range(0, skillTypes.Length));

        return data;
    }

    // 原有逻辑：高斯分布随机数
    private float GenerateGaussian(float mean, float stdDev)
    {
        float u1 = UnityEngine.Random.value;
        float u2 = UnityEngine.Random.value;
        float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + z * stdDev;
    }

    // 原有逻辑：验证鱼列表（清理空对象）
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
                int fishId = -1;
                if (fishIdToObjectMap.ContainsValue(allFish[i]))
                {
                    fishId = fishIdToObjectMap.First(kv => kv.Value == allFish[i]).Key;
                    fishIdToObjectMap.Remove(fishId);
                }
                allFish.RemoveAt(i);
                Debug.Log($"清理空鱼对象，ID: {fishId}");
            }
        }
    }


    // 原有逻辑：检查网络是否初始化
    private bool IsNetworkInitialized()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    // 原有逻辑：从映射中移除鱼
    public void RemoveFishFromMap(int fishId)
    {
        if (fishIdToObjectMap.ContainsKey(fishId))
        {
            fishIdToObjectMap.Remove(fishId);
        }
    }

    // 原有逻辑：通过ID获取鱼
    public GameObject GetFishByID(int fishId)
    {
        if (fishIdToObjectMap.TryGetValue(fishId, out GameObject fish))
        {
            return fish;
        }
        return null;
    }

    // 原有逻辑：客户端处理鱼销毁（仅回收对象池）
    public void HandleFishDestruction(int fishId)
    {
        if (IsServer) return; // 服务器销毁逻辑单独处理

        GameObject fish = GetFishByID(fishId);
        if (fish == null) return;

        FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>();
        if (tierEffect != null)
        {
            GameObject originalPrefab = tierEffect.fishData.originalPrefab;
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
            Debug.Log("没有找到鱼的TierEffect组件，无法回收");
        }
    }

    // 原有逻辑：客户端通知服务器销毁鱼
    [ServerRpc(RequireOwnership = false)]
    public void NotifyServerToDestroyFishServerRpc(int fishId)
    {
        ServerHandleFishDestruction(fishId);
    }

    // 原有逻辑：服务器处理鱼销毁（同步客户端）
    public void ServerHandleFishDestruction(int fishId)
    {
        if (!IsServer) return;

        GameObject fish = GetFishByID(fishId);
        if (fish == null) return;

        NetworkObject fishNetworkObj = fish.GetComponent<NetworkObject>();
        if (fishNetworkObj == null)
        {
            Debug.LogError($"鱼 {fishId} 没有 NetworkObject 组件，无法网络同步销毁");
            return;
        }

        // 从管理列表移除
        allFish.Remove(fish);
        RemoveFishFromMap(fishId);

        // 从网络变量移除（创建新列表触发同步）
        var fishDataToRemove = syncedFishData.Value.Items.FirstOrDefault(d => d.fishId == fishId);
        if (fishDataToRemove.fishId != -1)
        {
            var updatedFishData = new List<FishSpawnData>(syncedFishData.Value.Items);
            updatedFishData.Remove(fishDataToRemove);
            syncedFishData.Value = new NetworkFishSpawnDataList(updatedFishData);
        }

        // 网络销毁
        fishNetworkObj.Despawn(true);
        Debug.Log($"服务器：鱼 {fishId} 已销毁，同步至所有客户端");

        // 回收对象池
        FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>();
        if (tierEffect != null && tierEffect.fishData.originalPrefab != null && fishObjectPool != null)
        {
            fishObjectPool.ReturnFishToPool(fish, tierEffect.fishData.originalPrefab);
        }
    }

    // 新增：支持网络序列化的列表包装类
    [Serializable]
    public struct NetworkFishSpawnDataList : INetworkSerializable, IEquatable<NetworkFishSpawnDataList>
    {
        private List<FishSpawnData> items;

        public List<FishSpawnData> Items => items ??= new List<FishSpawnData>();

        public NetworkFishSpawnDataList(List<FishSpawnData> list)
        {
            items = new List<FishSpawnData>(list);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // 先序列化列表长度
            int length = Items.Count;
            serializer.SerializeValue(ref length);

            if (serializer.IsReader)
            {
                // 读取模式 - 先清空再添加
                Items.Clear();
                for (int i = 0; i < length; i++)
                {
                    FishSpawnData item = new FishSpawnData();
                    serializer.SerializeValue(ref item);
                    Items.Add(item);
                }
            }
            else
            {
                // 写入模式 - 逐个序列化
                for (int i = 0; i < length; i++)
                {
                    FishSpawnData item = Items[i];
                    serializer.SerializeValue(ref item);
                    Items[i] = item;
                }
            }
        }

        public bool Equals(NetworkFishSpawnDataList other)
        {
            if (Items.Count != other.Items.Count) return false;

            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].Equals(other.Items[i])) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkFishSpawnDataList other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Items);
        }
    }

    // 新增：鱼生成数据结构体（网络可序列化）
    [Serializable]
    public struct FishSpawnData : INetworkSerializable, IEquatable<FishSpawnData>
    {
        public int fishId;          // 鱼ID
        public int prefabIndex;     // 预制体索引（映射allAvailablePrefabs）
        public Vector3 position;    // 位置
        public Quaternion rotation; // 旋转
        public Vector3 scale;       // 缩放
        public int tier;            // 挡位
        public int baseExp;         // 基础经验
        public int skillType;       // 技能类型（枚举int值）
        public string zoneName;     // 所属区域名称

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref fishId);
            serializer.SerializeValue(ref prefabIndex);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref scale);
            serializer.SerializeValue(ref tier);
            serializer.SerializeValue(ref baseExp);
            serializer.SerializeValue(ref skillType);
            serializer.SerializeValue(ref zoneName);
        }

        public bool Equals(FishSpawnData other)
        {
            return fishId == other.fishId;
        }

        public override bool Equals(object obj)
        {
            return obj is FishSpawnData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return fishId;
        }
    }

    // 新增：FishSpawnData比较器（用于筛选新数据）
    private class FishSpawnDataComparer : IEqualityComparer<FishSpawnData>
    {
        public bool Equals(FishSpawnData x, FishSpawnData y)
        {
            return x.fishId == y.fishId;
        }

        public int GetHashCode(FishSpawnData obj)
        {
            return obj.fishId;
        }
    }
}

// 生成区域配置（未修改）
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
