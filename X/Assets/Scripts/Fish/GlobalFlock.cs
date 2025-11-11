using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#region 配置类
[System.Serializable]
public class FishSchoolSetting
{
   [Tooltip("鱼群的名称标识")]
    public string schoolName = "Fish School 1";
    [Tooltip("鱼群的生成位置参考点及父物体（用于层级管理）")]
    public Transform spawnTransform;  // 合并spawnTransform和schoolParent为一个
    [Tooltip("该鱼群可使用的鱼预制体列表")]
    public List<GameObject> schoolFishPrefabs = new List<GameObject>();
    [Tooltip("鱼群的漫游范围大小（单位：米）")]
    public float wanderSize = 7f;
    [Tooltip("该鱼群的初始鱼数量")]
    public int numFish = 30;
    [Tooltip("鱼被吃掉后重新生成的延迟时间（秒）")]
    public float respawnDelay = 2f;

    [Header("=== 挡位系统 ===")]  // 移至鱼群配置中
    [Tooltip("鱼的最大挡位等级")]
    public int maxTier = 5;
    [Tooltip("每个挡位对应的经验值（索引对应挡位等级）")]
    public int[] expValues = { 4, 10, 30, 80, 200, 500 };
    [Tooltip("基础大小缩放值")]
    public float baseSize = 0.8f;
    [Tooltip("每个挡位增加的大小缩放值")]
    public float sizeIncreasePerTier = 0.3f;
    [Tooltip("大小的随机变化范围（比例）")]
    public float sizeVariance = 0.1f;
    [Tooltip("测试模式开关（开启后鱼的挡位固定为最低）")]
    public bool testMode = false;

    [Header("=== 技能鱼配置 ===")]  // 移至鱼群配置中
    [Tooltip("是否为技能鱼群")]
    public bool isSkillFishSchool = false;
    [Tooltip("技能类型权重配置")]
    public int[] skillTypeWeights = { 1, 1, 1, 1 }; // 对应四种技能类型的权重
}

public class FishSchoolIdentifier : MonoBehaviour
{
       [Tooltip("所属鱼群的名称（用于关联配置）")]
    public string schoolName;
}
#endregion

namespace DistantLands
{
    public abstract class GlobalFlock : MonoBehaviour
    {
         [Header("=== 多鱼群管理 ===")]
        [Tooltip("所有鱼群的配置列表")]
        public List<FishSchoolSetting> allSchoolSettings = new List<FishSchoolSetting>();
        [Tooltip("当前选中的鱼群索引（用于编辑器测试）")]
        public int selectedSchoolIndex = 0;

        [Header("=== 全局配置 ===")]
        [Tooltip("默认使用的鱼预制体列表")]
        public List<GameObject> fishPrefabs;
        [Tooltip("鱼群的根父物体")]
        public GameObject fishSchool;
        [Tooltip("默认的漫游范围大小（单位：米）")]
        public float wanderSize = 7;
        [Tooltip("默认的鱼群数量")]
        public int numFish = 30;
        [Tooltip("默认的重生延迟时间（秒）")]
        public float respawnDelay = 2f;
        [Tooltip("清理空引用列表的时间间隔（秒）")]
        public float listCleanupInterval = 5f;

        [Tooltip("测试模式开关（开启后鱼的挡位固定为最低）")]
        public bool testMode = false;

        [Header("=== 对象池配置 ===")]
        [Tooltip("预制体资源加载路径（Resources目录下）")]
        public string prefabPath = "Tropical Fish";
        [Tooltip("直接指定的鱼预制体列表（优先级高于路径加载）")]
        public List<GameObject> fishPrefabsDirect = new List<GameObject>();
        [Tooltip("每个预制体的对象池最大容量")]
        public int maxPoolSizePerPrefab = 5;
        [Tooltip("玩家对象的 Transform 引用")]
        public Transform player;
        [Tooltip("是否已分配玩家引用（内部状态）")]
        protected bool isPlayerAssigned = false;

        #region 内部状态
        [HideInInspector] 
        [Tooltip("所有活跃鱼的列表")]
        public List<GameObject> allFish = new List<GameObject>();
        
        [Tooltip("对象池字典（键：预制体标识，值：对象队列）")]
        protected Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
        
        [Tooltip("鱼群-鱼列表映射（键：鱼群名称，值：该鱼群包含的鱼）")]
        protected Dictionary<string, List<GameObject>> schoolFishMap = new Dictionary<string, List<GameObject>>();

        #endregion

        protected virtual void Start()
        {
            InitializeAllFishSchools();
            StartCoroutine(CleanupFishListCoroutine());
        }

        #region 抽象方法（由子类实现具体鱼类型逻辑）
        protected abstract void InitializeFishAI(GameObject fish, FishSchoolSetting setting);
        protected abstract void ResetFishSpecificComponents(GameObject fish, SkillFishData data, FishSchoolSetting setting);
        #endregion

        #region 多鱼群管理
        
        protected virtual void InitializeAllFishSchools()
        {
            if (allSchoolSettings == null || allSchoolSettings.Count == 0)
            {
                Debug.LogWarning("未配置任何鱼群！");
                return;
            }

            foreach (var setting in allSchoolSettings)
            {
                if (!IsSchoolSettingValid(setting))
                {
                    Debug.LogError($"鱼群 [{setting.schoolName}] 配置不完整");
                    continue;
                }

                if (!schoolFishMap.ContainsKey(setting.schoolName))
                    schoolFishMap[setting.schoolName] = new List<GameObject>();

                for (int i = 0; i < setting.numFish; i++)
                {
                    SpawnSingleFishForSchool(setting);
                }
            }
        }

        [ContextMenu("生成当前选中鱼群")]
        public virtual void SpawnSelectedSchool()
        {
            if (selectedSchoolIndex < 0 || selectedSchoolIndex >= allSchoolSettings.Count) return;

            var selectedSetting = allSchoolSettings[selectedSchoolIndex];
            if (!IsSchoolSettingValid(selectedSetting)) return;

            ClearSchoolFish(selectedSetting);
            for (int i = 0; i < selectedSetting.numFish; i++)
            {
                SpawnSingleFishForSchool(selectedSetting);
            }
        }

        protected virtual void ClearSchoolFish(FishSchoolSetting setting)
        {
            if (schoolFishMap.ContainsKey(setting.schoolName))
            {
                foreach (var fish in schoolFishMap[setting.schoolName])
                {
                    if (fish != null)
                    {
                        allFish.Remove(fish);
                        ReturnFishToPool(fish);
                    }
                }
                schoolFishMap[setting.schoolName].Clear();
            }
        }

        protected virtual bool IsSchoolSettingValid(FishSchoolSetting setting)
        {
            if (setting.spawnTransform == null) return false;  // 仅检查合并后的spawnTransform
            if ((setting.schoolFishPrefabs == null || setting.schoolFishPrefabs.Count == 0) &&
                (fishPrefabs == null || fishPrefabs.Count == 0))
                return false;
            return true;
        }
        
        #endregion

        #region 对象池核心
        protected virtual GameObject GetFishFromPool(GameObject prefab)
        {
            string prefabKey = GetPrefabKey(prefab);
            if (objectPools.TryGetValue(prefabKey, out var pool) && pool.Count > 0)
            {
                GameObject fish = pool.Dequeue();
                fish.SetActive(true);
                return fish;
            }

            GameObject newFish = Instantiate(prefab);
            newFish.name = $"{prefab.name}_Instance";
            return newFish;
        }

        protected virtual void ReturnFishToPool(GameObject fish)
        {
            if (fish == null) return;

            string prefabKey = GetPrefabKey(fish);
            if (!objectPools.ContainsKey(prefabKey))
                objectPools[prefabKey] = new Queue<GameObject>();

            var pool = objectPools[prefabKey];
            if (pool.Count < maxPoolSizePerPrefab)
            {
                fish.SetActive(false);
                pool.Enqueue(fish);
            }
            else
            {
                Destroy(fish);
            }
        }

        protected virtual void SpawnSingleFishForSchool(FishSchoolSetting setting)
        {
            List<GameObject> targetPrefabs = setting.schoolFishPrefabs.Count > 0
                ? setting.schoolFishPrefabs
                : fishPrefabs;

            if (targetPrefabs == null || targetPrefabs.Count == 0) return;

            GameObject randomPrefab = targetPrefabs[Random.Range(0, targetPrefabs.Count)];
            GameObject fish = GetFishFromPool(randomPrefab);

            Vector3 spawnPos = setting.spawnTransform.position + Random.insideUnitSphere * setting.wanderSize;
            fish.transform.position = spawnPos;
            fish.transform.rotation = Quaternion.identity;
            fish.transform.parent = setting.spawnTransform;

            SkillFishData fishData = GenerateFishData(setting);  // 传入鱼群配置
   
            fish.transform.localScale = CalculateFishScale((int)fishData.fishTier, setting);  // 传入鱼群配置


            ResetFishComponents(fish, fishData, setting);
            InitializeFishAI(fish, setting);

            if (!allFish.Contains(fish))
                allFish.Add(fish);
            if (schoolFishMap.ContainsKey(setting.schoolName) && !schoolFishMap[setting.schoolName].Contains(fish))
                schoolFishMap[setting.schoolName].Add(fish);
        }

        protected virtual void ResetFishComponents(GameObject fish, SkillFishData data, FishSchoolSetting setting)
        {
            FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>() ?? fish.AddComponent<FishTierEffect>();
            tierEffect.fishData = data;

            FishSchoolIdentifier identifier = fish.GetComponent<FishSchoolIdentifier>() ?? fish.AddComponent<FishSchoolIdentifier>();
            identifier.schoolName = setting.schoolName;

            AddMeshColliderWithTrigger(fish);
            ResetFishSpecificComponents(fish, data, setting);
        }

        public virtual void OnFishEaten(GameObject eatenFish)
        {
            if (eatenFish == null || !allFish.Contains(eatenFish)) return;

            FishSchoolIdentifier identifier = eatenFish.GetComponent<FishSchoolIdentifier>();
            allFish.Remove(eatenFish);
             MusicManager.Instance.Exp();

            if (identifier != null && schoolFishMap.ContainsKey(identifier.schoolName))
                schoolFishMap[identifier.schoolName].Remove(eatenFish);

            var schoolSetting = allSchoolSettings.FirstOrDefault(s => s.schoolName == identifier?.schoolName);
            if (schoolSetting != null)
                StartCoroutine(RespawnFishAfterDelay(schoolSetting));

            ReturnFishToPool(eatenFish);
        }
        #endregion

        #region 事件与生命周期
        protected virtual void OnEnable()
        {
            if (FishGameFlowManager.Instance != null)
                FishGameFlowManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        protected virtual void OnDisable()
        {
            if (FishGameFlowManager.Instance != null)
                FishGameFlowManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        protected virtual IEnumerator RespawnFishAfterDelay(FishSchoolSetting setting)
        {
            yield return new WaitForSeconds(setting.respawnDelay);
            SpawnSingleFishForSchool(setting);
        }

        protected virtual void InitializeObjectPools()
        {
            if (fishPrefabs == null || fishPrefabs.Count == 0) return;

            foreach (var prefab in fishPrefabs)
            {
                string prefabKey = GetPrefabKey(prefab);
                if (!objectPools.ContainsKey(prefabKey))
                    objectPools[prefabKey] = new Queue<GameObject>();
            }

            if (allSchoolSettings != null)
            {
                foreach (var setting in allSchoolSettings)
                {
                    if (setting.schoolFishPrefabs == null) continue;
                    foreach (var prefab in setting.schoolFishPrefabs)
                    {
                        if (prefab == null) continue;
                        string prefabKey = GetPrefabKey(prefab);
                        if (!objectPools.ContainsKey(prefabKey))
                            objectPools[prefabKey] = new Queue<GameObject>();
                    }
                }
            }
        }

        protected virtual void OnGameStateChanged(FishGameFlowManager.GameState newState)
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
                player = FishGameFlowManager.Instance.playerFish.transform;
                isPlayerAssigned = true;
            }
            else
            {
                Debug.LogWarning("未找到玩家对象！");
            }
        }

        protected virtual void ResetPlayerReference()
        {
            if (isPlayerAssigned)
            {
                player = null;
                isPlayerAssigned = false;
            }
        }

        protected virtual void LoadFishPrefabs()
        {
            if (fishPrefabsDirect != null && fishPrefabsDirect.Count > 0)
            {
                fishPrefabs = fishPrefabsDirect.Where(p => p != null).ToList();
            }
            else if (!string.IsNullOrEmpty(prefabPath))
            {
                var loadedPrefabs = Resources.LoadAll<GameObject>(prefabPath);
                fishPrefabs = loadedPrefabs.Where(p => p != null).ToList();
            }
            else
            {
                Debug.LogError("未配置鱼预制体！");
            }

            foreach (var setting in allSchoolSettings)
            {
                if (setting.schoolFishPrefabs == null || setting.schoolFishPrefabs.Count == 0)
                    setting.schoolFishPrefabs = fishPrefabs;
            }
        }

        protected virtual string GetPrefabKey(GameObject obj)
        {
            return obj.name.Split('(')[0].Trim();
        }
        #endregion

        #region 挡位系统
        protected virtual SkillFishData GenerateFishData(FishSchoolSetting setting)  // 新增setting参数
        {
            SkillFishData data = new SkillFishData();
            int actualTier = setting.testMode ? 0 : GetGaussianTier(setting);  // 使用鱼群配置的testMode
            data.baseExpValue = (actualTier >= 0 && actualTier < setting.expValues.Length) ? setting.expValues[actualTier] : 0;  // 使用鱼群配置的expValues
            data.fishTier = (FishTier)actualTier;
            return data;
        }
        

       protected virtual int GetGaussianTier(FishSchoolSetting setting)  // 新增setting参数
        {
            float mean = 1f;
            float stdDev = 0.8f;
            float gaussian = mean + Mathf.Sqrt(-2.0f * Mathf.Log(Random.value)) * Mathf.Sin(2.0f * Mathf.PI * Random.value) * stdDev;
            return Mathf.Clamp(Mathf.RoundToInt(gaussian), 0, setting.maxTier);  // 使用鱼群配置的maxTier
        }



        protected virtual Vector3 CalculateFishScale(int tier, FishSchoolSetting setting)  // 新增setting参数
        {
            float baseScale = setting.baseSize + (tier * setting.sizeIncreasePerTier);  // 使用鱼群配置的尺寸参数
            float randomScale = baseScale * Random.Range(1 - setting.sizeVariance, 1 + setting.sizeVariance);  // 使用鱼群配置的随机范围
            return Vector3.one * Mathf.Max(randomScale, setting.baseSize * 0.8f);  // 使用鱼群配置的基础大小
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
        }
        #endregion

        #region 列表维护
        protected virtual IEnumerator CleanupFishListCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(listCleanupInterval);
                if (allFish == null) continue;

                for (int i = allFish.Count - 1; i >= 0; i--)
                    if (allFish[i] == null)
                        allFish.RemoveAt(i);

                foreach (var key in schoolFishMap.Keys.ToList())
                {
                    var schoolFish = schoolFishMap[key];
                    for (int i = schoolFish.Count - 1; i >= 0; i--)
                        if (schoolFish[i] == null)
                            schoolFish.RemoveAt(i);
                }
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (allSchoolSettings == null) return;

            Gizmos.color = Color.blue;
            foreach (var setting in allSchoolSettings)
                if (setting.spawnTransform != null)
                    Gizmos.DrawWireSphere(setting.spawnTransform.position, setting.wanderSize);
        }
        #endregion
    }
}