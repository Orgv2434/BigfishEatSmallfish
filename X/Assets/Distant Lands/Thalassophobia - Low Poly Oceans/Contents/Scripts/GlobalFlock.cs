using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DistantLands
{
    public class GlobalFlock : MonoBehaviour
    {
        #region 配置参数（新增对象池核心配置）
        [Tooltip("鱼的预制体数组，可添加多种鱼模型")]
        public List<GameObject> fishPrefabs;
        [Tooltip("鱼群的父物体，用于层级管理")]
        public GameObject fishSchool;
        [Tooltip("鱼群活动范围半径（单位：米）")]
        public float wanderSize = 7;
        [Tooltip("鱼群的初始总数量")]
        public int numFish = 30;
        [Tooltip("鱼被吃掉后的重生延迟时间（单位：秒）")]
        public float respawnDelay = 2f;
        [Tooltip("鱼群列表定时清理间隔（单位：秒）")]
        public float listCleanupInterval = 5f;

        [Header("=== 挡位系统与视觉设置 ===")]
        [Tooltip("最大挡位，挡位越高生成概率越低")]
        public int maxTier = 5;
        [Tooltip("各挡位对应的基础经验值（索引对应挡位）")]
        public int[] expValues = { 4, 10, 30, 80, 200, 500 };
        [Tooltip("挡位0的基础大小（缩放值）")]
        public float baseSize = 0.8f;
        [Tooltip("每提升1个挡位增加的大小比例")]
        public float sizeIncreasePerTier = 0.3f;
        [Tooltip("同挡位内的大小随机波动范围（0-1）")]
        public float sizeVariance = 0.1f;
        [Tooltip("开启测试模式后，所有生成的鱼都将是0档")]
        public bool testMode = false;

        [Header("=== 对象池配置（父类核心） ===")]
        [Tooltip("鱼预制体在Resources文件夹中的路径（例：TropicalFish）")]
        public string prefabPath = "Tropical Fish";
        [Tooltip("直接拖入预制体（优先级高于prefabPath）")]
        public List<GameObject> fishPrefabsDirect = new List<GameObject>();
        [Tooltip("每个预制体的最大池容量（父类对象池限制）")]
        public int maxPoolSizePerPrefab = 5; // 新增：控制对象池最大容量
        [Tooltip("玩家对象")]
        public Transform player;
        protected bool isPlayerAssigned = false;
    
        #endregion


        #region 内部状态（对象池核心变量）
        [HideInInspector] public List<GameObject> allFish = new List<GameObject>();
        protected Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>(); // 父类对象池
        #endregion


        void Awake()
        {
            LoadFishPrefabs();
            InitializeObjectPools(); // 初始化对象池
        }

        protected virtual void Start()
        {
            if (fishSchool == null || fishPrefabs.Count == 0)
            {
                Debug.LogError("鱼群父物体或预制体未赋值，无法生成鱼！");
                return;
            }
            // 初始生成鱼群（使用对象池）
            for (int i = 0; i < numFish; i++)
            {
                SpawnSingleFish();
            }
            StartCoroutine(CleanupFishListCoroutine());
        }


        #region 核心：对象池化生成/回收（父类实现）
        /// <summary>
        /// 从对象池获取或实例化鱼（父类核心方法）
        /// </summary>
        protected virtual GameObject GetFishFromPool(GameObject prefab)
        {
            string prefabKey = GetPrefabKey(prefab);
            // 1. 尝试从对象池获取
            if (objectPools.TryGetValue(prefabKey, out var pool) && pool.Count > 0)
            {
                GameObject fish = pool.Dequeue();
                fish.SetActive(true); // 激活对象
                return fish;
            }
            // 2. 对象池无闲置，实例化新对象
            GameObject newFish = Instantiate(prefab);
            newFish.name = $"{prefab.name}_Instance"; // 统一命名
            return newFish;
        }

        /// <summary>
        /// 将鱼回收至对象池（父类核心方法）
        /// </summary>
        protected virtual void ReturnFishToPool(GameObject fish)
        {
            if (fish == null) return;

            string prefabKey = GetPrefabKey(fish);
            // 1. 检查对象池是否存在，不存在则创建
            if (!objectPools.ContainsKey(prefabKey))
            {
                objectPools[prefabKey] = new Queue<GameObject>();
            }
            var pool = objectPools[prefabKey];

            // 2. 若池未满则回收，否则销毁（避免内存溢出）
            if (pool.Count < maxPoolSizePerPrefab)
            {
                fish.SetActive(false); // 隐藏对象
                pool.Enqueue(fish); // 加入池
            }
            else
            {
                Destroy(fish); // 池已满，销毁多余对象
            }
        }

        /// <summary>
        /// 单条鱼生成逻辑（使用对象池）
        /// </summary>
        protected virtual void SpawnSingleFish()
        {
            // 随机选择预制体
            GameObject randomPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
            // 从对象池获取鱼（复用或新实例）
            GameObject fish = GetFishFromPool(randomPrefab);

            // 重置位置（避免复用旧位置）
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * wanderSize;
            fish.transform.position = spawnPos;
            fish.transform.rotation = Quaternion.identity;

            // 生成挡位数据并初始化
            SkillFishData fishData = GenerateFishData();
            fish.transform.parent = fishSchool.transform; // 统一父物体
            fish.transform.localScale = CalculateFishScale((int)fishData.fishTier); // 重置缩放

            // 添加/重置必要组件（确保状态正确）
            ResetFishComponents(fish, fishData);

            // 加入管理列表
            if (!allFish.Contains(fish))
                allFish.Add(fish);
        }

        /// <summary>
        /// 重置鱼的组件状态（避免复用旧数据）
        /// </summary>
        protected virtual void ResetFishComponents(GameObject fish, SkillFishData data)
        {
            // 重置挡位数据
            FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>() ?? fish.AddComponent<FishTierEffect>();
            tierEffect.fishData = data;

            // 重置移动组件
            AssignFishMovement(fish);

            // 确保碰撞体存在
            AddMeshColliderWithTrigger(fish);
        }

        /// <summary>
        /// 鱼被吃掉时的处理（回收至对象池而非销毁）
        /// </summary>
        public virtual void OnFishEaten(GameObject eatenFish)
        {
            if (eatenFish == null || !allFish.Contains(eatenFish))
                return;

            // 从管理列表移除
            allFish.Remove(eatenFish);
            // 回收至对象池（替代Destroy）
            ReturnFishToPool(eatenFish);
            // 延迟重生
            StartCoroutine(RespawnFishAfterDelay());
        }
        #endregion
        #region 订阅游戏变化事件
        private void OnEnable()
        {
            if (FishGameFlowManager.Instance != null)
            {
                FishGameFlowManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }
    

        private void OnDisable()
        {
            if (FishGameFlowManager.Instance != null)
            {
                FishGameFlowManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }
    #endregion

        #region 其他核心逻辑（复用并适配对象池）
        /// <summary>
        /// 延迟重生协程（复用对象池生成逻辑）
        /// </summary>
        protected virtual IEnumerator RespawnFishAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnSingleFish(); // 直接调用对象池化生成方法
        }

        /// <summary>
        /// 初始化对象池（为每个预制体创建队列）
        /// </summary>
        protected virtual void InitializeObjectPools()
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
        private void OnGameStateChanged(FishGameFlowManager.GameState newState)
        {
            switch (newState)
            {
                case FishGameFlowManager.GameState.GamePlaying:
                    AssignPlayerReference();
                    break;
                case FishGameFlowManager.GameState.MainMenu:
                case FishGameFlowManager.GameState.PrepareStage:
                case FishGameFlowManager.GameState.GameOver:
                    ResetPlayerReference();
                    break;
            }
        }

        protected virtual void AssignPlayerReference()
        {
            if (isPlayerAssigned) return;

            if (FishGameFlowManager.Instance != null && FishGameFlowManager.Instance.playerFish != null)
            {
               
                
            }
            else
            {
                Debug.LogWarning("[FishAreaManager] 游戏已开始，但未找到玩家对象！");
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
    
    
        /// <summary>
        /// 加载预制体（与对象池关联）
        /// </summary>
        protected virtual void LoadFishPrefabs()
        {
            if (fishPrefabsDirect != null && fishPrefabsDirect.Count > 0)
            {
                fishPrefabs = fishPrefabsDirect.Where(p => p != null).ToList();
                Debug.Log($"[GlobalFlock] 从直接赋值加载 {fishPrefabs.Count} 个预制体（对象池关联）");
                return;
            }

            if (!string.IsNullOrEmpty(prefabPath))
            {
                var loadedPrefabs = Resources.LoadAll<GameObject>(prefabPath);
                fishPrefabs = loadedPrefabs.Where(p => p != null).ToList();
                Debug.Log($"[GlobalFlock] 从Resources加载 {fishPrefabs.Count} 个预制体（对象池关联）");
            }
            else
            {
                Debug.LogError("[GlobalFlock] 未配置预制体路径且未直接赋值预制体！");
            }
        }

        /// <summary>
        /// 获取预制体唯一标识（用于对象池key）
        /// </summary>
        protected virtual string GetPrefabKey(GameObject obj)
        {
            return obj.name.Split('(')[0].Trim(); // 去除实例化后缀（如"(Clone)"）
        }
        #endregion


        #region 挡位系统与组件管理（保持不变，适配对象池）
        protected virtual SkillFishData GenerateFishData()
        {
            SkillFishData data = new SkillFishData();
            int actualTier = testMode ? 0 : GetGaussianTier();
            data.baseExpValue = (actualTier >= 0 && actualTier < expValues.Length) ? expValues[actualTier] : 0;
            data.fishTier = (FishTier)actualTier;
            return data;
        }

        protected virtual int GetGaussianTier()
        {
            float mean = 1f;
            float stdDev = 0.8f;
            float gaussian = mean + Mathf.Sqrt(-2.0f * Mathf.Log(Random.value)) * Mathf.Sin(2.0f * Mathf.PI * Random.value) * stdDev;
            return Mathf.Clamp(Mathf.RoundToInt(gaussian), 0, maxTier - 1);
        }

        protected virtual Vector3 CalculateFishScale(int tier)
        {
            float baseScale = baseSize + (tier * sizeIncreasePerTier);
            float randomScale = baseScale * Random.Range(1 - sizeVariance, 1 + sizeVariance);
            return Vector3.one * Mathf.Max(randomScale, baseSize * 0.8f);
        }

        protected virtual void AssignFishMovement(GameObject fish)
        {
            Fish fishScript = fish.GetComponent<Fish>() ?? fish.AddComponent<Fish>();
            fishScript.flock = this;
        }

        protected virtual void AddMeshColliderWithTrigger(GameObject fishObj)
        {
            MeshFilter meshFilter = fishObj.GetComponent<MeshFilter>() ?? fishObj.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                MeshCollider collider = fishObj.GetComponent<MeshCollider>() ?? fishObj.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.mesh;
                collider.convex = true;
                collider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning($"鱼 {fishObj.name} 未找到MeshFilter，无法添加碰撞体");
            }
        }
        #endregion


        #region 列表维护与可视化
        protected virtual IEnumerator CleanupFishListCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(listCleanupInterval);
                if (allFish == null) continue;

                // 清理空引用
                for (int i = allFish.Count - 1; i >= 0; i--)
                {
                    if (allFish[i] == null)
                        allFish.RemoveAt(i);
                }
            }
        }

        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, wanderSize);
        }
        #endregion
    }
}