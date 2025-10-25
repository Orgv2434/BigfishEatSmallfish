using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DistantLands
{
    public abstract class FishManagerBase : MonoBehaviour
    {
        #region 配置参数（Inspector可配置）
        [Header("=== 基础生成配置 ===")]
        [Tooltip("鱼预制体列表（直接赋值）")]
        public List<GameObject> fishPrefabs = new List<GameObject>();
        [Tooltip("预制体加载路径（备选）")]
        public string prefabPath = "";
        [Tooltip("最大活跃鱼数量")]
        public int maxFishCount = 20;
        [Tooltip("生成间隔（秒）")]
        public float spawnInterval = 1f;
        [Tooltip("活动范围半径")]
        public float areaRadius = 10f;
        [Tooltip("重生延迟（秒）")]
        public float respawnDelay = 2f;

        [Header("=== 挡位与视觉配置 ===")]
        public int maxTier = 5;
        public int[] expValues = { 4, 10, 30, 80, 200, 500 };
        public GameObject[] tierEffects;
        public float baseSize = 0.8f;
        public float sizeIncreasePerTier = 0.3f;
        public float sizeVariance = 0.1f;

        [Header("=== 对象池配置 ===")]
        public int maxPoolSizePerPrefab = 5;
        public GameObject fishParent; // 鱼的父物体（用于层级管理）

        [Header("=== 测试模式 ===")]
        public bool testMode = false;
        #endregion


        #region 私有数据（运行时维护）
        // 活跃鱼对象列表
        public List<GameObject> activeFishObjects = new List<GameObject>();
        // 对象池（键：预制体标识，值：闲置对象队列）
        protected Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
        // 记录所有闲置对象（用于快速查找）
        protected HashSet<GameObject> inactiveObjects = new HashSet<GameObject>();
        // 生成计时器
        protected float spawnTimer;
        #endregion


        #region 生命周期方法（Unity回调）
        protected virtual void Awake()
        {
            LoadPrefabs();
            InitializeObjectPools();
            if (fishParent == null) 
                fishParent = new GameObject($"{GetType().Name}_FishParent");
        }

        protected virtual void Start()
        {
            CheckSettings();
            if (fishPrefabs.Count == 0)
            {
                Debug.LogError($"[{GetType().Name}] 未加载到鱼预制体！");
                return;
            }
            StartCoroutine(CleanupInvalidFishCoroutine());
        }

        protected virtual void Update()
        {
            if (CanSpawnFish())
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnInterval && activeFishObjects.Count < maxFishCount)
                {
                    SpawnFish();
                    spawnTimer = 0;
                }
            }
        }
        #endregion


        #region 核心共性方法（生成/回收/重生）
        /// <summary>
        /// 加载预制体（优先使用直接赋值，备选从Resources加载）
        /// </summary>
        protected virtual void LoadPrefabs()
        {
            if (fishPrefabs.Count > 0) return;

            if (!string.IsNullOrEmpty(prefabPath))
            {
                var loaded = Resources.LoadAll<GameObject>(prefabPath);
                fishPrefabs = loaded.Where(p => p != null).ToList();
                Debug.Log($"[{GetType().Name}] 从Resources加载 {fishPrefabs.Count} 个预制体");
            }
        }

        /// <summary>
        /// 初始化对象池（为每个预制体创建队列）
        /// </summary>
        protected virtual void InitializeObjectPools()
        {
            foreach (var prefab in fishPrefabs)
            {
                string key = GetPrefabKey(prefab);
                if (!objectPools.ContainsKey(key))
                    objectPools[key] = new Queue<GameObject>();
            }
        }

        /// <summary>
        /// 生成单条鱼（共性逻辑：选预制体→取对象→设属性→加组件→入列表）
        /// </summary>
        protected virtual void SpawnFish()
        {
            // 1. 选择预制体
            var prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
            if (prefab == null) return;

            // 2. 从对象池获取或实例化
            var fishObj = GetFromPoolOrInstantiate(prefab);
            fishObj.transform.parent = fishParent.transform;

            // 3. 生成挡位数据
            var fishData = GenerateFishData();

            // 4. 设置基础属性（位置、缩放）
            SetupFishTransform(fishObj, (int)fishData.fishTier);

            // 5. 添加碰撞体和挡位特效（共性组件）
            AddCollisionAndEffect(fishObj, fishData);

            // 6. 子类实现：添加特定组件（Fish/SkillFishAI）
            SetupFishSpecificComponents(fishObj, fishData);

            // 7. 添加到活跃列表
            if (!activeFishObjects.Contains(fishObj))
                activeFishObjects.Add(fishObj);
        }

        /// <summary>
        /// 从对象池获取或实例化鱼（复用闲置对象，减少GC）
        /// </summary>
        protected virtual GameObject GetFromPoolOrInstantiate(GameObject prefab)
        {
            string key = GetPrefabKey(prefab);
            if (objectPools.TryGetValue(key, out var pool) && pool.Count > 0)
            {
                var fish = pool.Dequeue();
                fish.SetActive(true);
                inactiveObjects.Remove(fish);
                return fish;
            }

            var newFish = Instantiate(prefab);
            newFish.name = $"{prefab.name}_Instance";
            return newFish;
        }

        /// <summary>
        /// 销毁/回收鱼并触发重生（统一回收逻辑，支持对象池复用）
        /// </summary>
        public virtual void DespawnFish(GameObject fishObj)
        {
            if (fishObj == null || !activeFishObjects.Contains(fishObj)) return;

            // 1. 从活跃列表移除
            activeFishObjects.Remove(fishObj);

            // 2. 回收或销毁
            string key = GetPrefabKey(fishObj);
            if (objectPools.TryGetValue(key, out var pool) && pool.Count < maxPoolSizePerPrefab)
            {
                fishObj.SetActive(false);
                pool.Enqueue(fishObj);
                inactiveObjects.Add(fishObj);
            }
            else
            {
                Destroy(fishObj);
            }

            // 3. 延迟重生
            StartCoroutine(RespawnAfterDelay());
        }

        /// <summary>
        /// 延迟重生协程（控制重生时机，避免瞬间生成）
        /// </summary>
        protected virtual IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (CanSpawnFish() && activeFishObjects.Count < maxFishCount)
                SpawnFish();
        }
        #endregion


        #region 辅助方法（属性设置/组件添加）
        /// <summary>
        /// 生成挡位数据（基础逻辑，支持测试模式强制0挡）
        /// </summary>
        protected virtual SkillFishData GenerateFishData()
        {
            var data = new SkillFishData();
            int tier = testMode ? 0 : GetGaussianTier();
            tier = Mathf.Clamp(tier, 0, maxTier - 1);

            data.baseExpValue = (tier < expValues.Length) ? expValues[tier] : 10;
            data.fishTier = (FishTier)tier;
            return data;
        }

        /// <summary>
        /// 设置鱼的位置、旋转和缩放（共性Transform配置）
        /// </summary>
        protected virtual void SetupFishTransform(GameObject fishObj, int tier)
        {
            // 随机位置（活动范围内）
            fishObj.transform.position = transform.position + Random.insideUnitSphere * areaRadius * 0.8f;
            // 随机旋转（Y轴）
            fishObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            // 计算缩放（随挡位增长+随机波动）
            float scale = baseSize + (tier * sizeIncreasePerTier);
            scale *= Random.Range(1 - sizeVariance, 1 + sizeVariance);
            fishObj.transform.localScale = Vector3.one * Mathf.Max(scale, baseSize * 0.8f);
        }

        /// <summary>
        /// 添加碰撞体和挡位特效（共性交互与视觉组件）
        /// </summary>
        protected virtual void AddCollisionAndEffect(GameObject fishObj, SkillFishData data)
        {
            // 添加碰撞体（用于检测交互）
            AddMeshCollider(fishObj);

            // 初始化挡位特效组件
            var tierEffect = fishObj.GetComponent<FishTierEffect>() ?? fishObj.AddComponent<FishTierEffect>();
            tierEffect.fishData = data;
            UpdateTierEffect(fishObj, data);
        }

        /// <summary>
        /// 更新挡位特效（清除旧特效+添加新特效）
        /// </summary>
        protected virtual void UpdateTierEffect(GameObject fishObj, SkillFishData data)
        {
            // 清除旧特效
            foreach (var child in fishObj.transform)
            {
                var t = (Transform)child;
                if (t.CompareTag("TierEffect"))
                    Destroy(t.gameObject);
            }

            // 添加新特效
            int tier = (int)data.fishTier;
            if (tierEffects != null && tier < tierEffects.Length && tierEffects[tier] != null)
            {
                var effect = Instantiate(tierEffects[tier], fishObj.transform);
                effect.tag = "TierEffect";
                effect.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// 添加碰撞体（确保交互可检测）
        /// </summary>
        protected virtual void AddMeshCollider(GameObject fishObj)
        {
            var meshFilter = fishObj.GetComponent<MeshFilter>() ?? fishObj.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning($"[{GetType().Name}] {fishObj.name} 无MeshFilter，无法添加碰撞体");
                return;
            }

            var collider = fishObj.GetComponent<MeshCollider>() ?? fishObj.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.mesh;
            collider.convex = true;
            collider.isTrigger = true;
        }

        /// <summary>
        /// 定时清理无效鱼对象（避免空引用）
        /// </summary>
        protected virtual IEnumerator CleanupInvalidFishCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);
                activeFishObjects.RemoveAll(obj => obj == null);
            }
        }
        #endregion


        #region 工具方法（通用计算/校验）
        /// <summary>
        /// 获取预制体唯一标识（用于对象池键）
        /// </summary>
        protected virtual string GetPrefabKey(GameObject obj)
        {
            return obj.name.Split('(')[0].Trim();
        }

        /// <summary>
        /// 高斯分布生成挡位（低挡位概率更高）
        /// </summary>
        protected virtual int GetGaussianTier()
        {
            float mean = 1f;
            float stdDev = 0.8f;
            float gauss = mean + Mathf.Sqrt(-2f * Mathf.Log(Random.value)) * Mathf.Sin(2f * Mathf.PI * Random.value) * stdDev;
            return Mathf.RoundToInt(gauss);
        }

        /// <summary>
        /// 校验配置参数（避免无效值）
        /// </summary>
        protected virtual void CheckSettings()
        {
            if (areaRadius <= 0) areaRadius = 10f;
            if (maxTier <= 0) maxTier = 5;
            if (respawnDelay <= 0) respawnDelay = 2f;
        }
        #endregion


        #region 抽象与判断方法（子类扩展点）
        /// <summary>
        /// 是否允许生成鱼（子类可重写，如判断游戏状态）
        /// </summary>
        protected virtual bool CanSpawnFish() => true;

        /// <summary>
        /// 子类实现：添加鱼的特定组件（如Fish/SkillFishAI）
        /// </summary>
        protected abstract void SetupFishSpecificComponents(GameObject fishObj, SkillFishData fishData);
        #endregion
    }
}