using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DistantLands;
using System.Linq;

[RequireComponent(typeof(Transform))]
public class SkillFishManager : GlobalFlock
{
   
    [Header("=== 技能鱼特有设置 ===")]
    [Tooltip("技能鱼的最大活跃数量上限")]
    public int maxFishCount = 20;
    [Tooltip("生成新技能鱼的时间间隔（秒）")]
    public float spawnInterval = 1f;
    [Tooltip("生成技能鱼时的随机范围半径（单位：米）")]
    public float spawnRadius = 10f;

    private List<SkillFishAI> activeFishes = new List<SkillFishAI>();
    private float spawnTimer;

    [Tooltip("单例实例")] 
    public static SkillFishManager Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadFishPrefabs();
        InitializeObjectPools();
    }


    #region 生命周期
    protected override void Start()
    {
        base.Start();
        if (fishSchool == null || fishPrefabs.Count == 0)
        {
            Debug.LogError("鱼群父物体或预制体未赋值！");
            return;
        }
        StartCoroutine(InitialSpawnRoutine());
    }

    private void Update()
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0 || !isPlayerAssigned)
            return;

        if (FishGameFlowManager.Instance.CurrentState == FishGameFlowManager.GameState.GamePlaying)
        {
            UpdateSpawnLogic();
            if (Time.frameCount % 30 == 0)
                activeFishes.RemoveAll(fish => fish == null);
        }
    }
    #endregion

    #region 生成逻辑
    private IEnumerator InitialSpawnRoutine()
    {
        int initialCount = Mathf.Min(5, maxFishCount);
        for (int i = 0; i < initialCount; i++)
        {
            if (activeFishes.Count < maxFishCount)
            {
                SpawnRandomSkillFish();
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    private void UpdateSpawnLogic()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && activeFishes.Count < maxFishCount)
        {
            SpawnRandomSkillFish();
            spawnTimer = 0;
        }
    }

    private void SpawnRandomSkillFish()
    {
        if (!isPlayerAssigned || allSchoolSettings.Count == 0) return;

        var randomSetting = allSchoolSettings[Random.Range(0, allSchoolSettings.Count)];
        if (IsSchoolSettingValid(randomSetting))
            SpawnSingleFishForSchool(randomSetting);
    }
    #endregion

    #region 抽象方法实现
    protected override void InitializeFishAI(GameObject fish, FishSchoolSetting setting)
    {
        SkillFishAI fishAI = fish.GetComponent<SkillFishAI>() ?? fish.AddComponent<SkillFishAI>();
        
        // 从预制体获取基础参数或使用默认值
        SkillFishAI prefabAI = fish.GetComponent<SkillFishAI>();
        if (prefabAI != null)
        {
            fishAI.Initialize(
                speed: Random.Range(prefabAI.originalSpeed * 0.8f, prefabAI.originalSpeed * 1.2f),
                rotSpeed: prefabAI.rotationSpeed,
                detectDist: prefabAI.detectDistance,
                safeDist: prefabAI.safeDistance,
                fleeTime: prefabAI.fleeDuration,
                fleeMultiplier: prefabAI.fleeSpeedMultiplier,
                playerTransform: player,
                center: setting.spawnTransform.position,
                radius: setting.wanderSize
            );
        }
        else
        {
            fishAI.Initialize(
                speed: Random.Range(1.5f, 2.5f),
                rotSpeed: 5f,
                detectDist: 5f,
                safeDist: 8f,
                fleeTime: 3f,
                fleeMultiplier: 1.8f,
                playerTransform: player,
                center: setting.spawnTransform.position,
                radius: setting.wanderSize
            );
        }

        if (!activeFishes.Contains(fishAI))
            activeFishes.Add(fishAI);
    }

    protected override void ResetFishSpecificComponents(GameObject fish, SkillFishData data, FishSchoolSetting setting)
    {
        // 技能鱼特有组件
        SkillFishTierEffect tierEffect = fish.GetComponent<SkillFishTierEffect>() ?? fish.AddComponent<SkillFishTierEffect>();
        tierEffect.fishData = data;
        data.skillType = (FishSkillType)Random.Range(1, 5); // 技能类型初始化
    }
    #endregion

    #region 重写方法
    public override void OnFishEaten(GameObject eatenFish)
    {
                  if (eatenFish == null || !allFish.Contains(eatenFish)) return;

            FishSchoolIdentifier identifier = eatenFish.GetComponent<FishSchoolIdentifier>();
            allFish.Remove(eatenFish);


            if (identifier != null && schoolFishMap.ContainsKey(identifier.schoolName))
                schoolFishMap[identifier.schoolName].Remove(eatenFish);

            var schoolSetting = allSchoolSettings.FirstOrDefault(s => s.schoolName == identifier?.schoolName);
        if (schoolSetting != null)
            StartCoroutine(RespawnFishAfterDelay(schoolSetting));
                

        ReturnFishToPool(eatenFish);
            

        var fishAI = eatenFish.GetComponent<SkillFishAI>();
        if (fishAI != null)
            activeFishes.Remove(fishAI);
        // 音效
        MusicManager.Instance.EatSkillFish();
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
}