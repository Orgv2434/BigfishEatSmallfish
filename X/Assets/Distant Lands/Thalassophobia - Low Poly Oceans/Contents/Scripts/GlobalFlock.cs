using UnityEngine;
using System.Collections.Generic;

namespace DistantLands
{
    public class GlobalFlock : MonoBehaviour
    {
        [Tooltip("鱼的预制体数组，可添加多种鱼模型")]
        public GameObject[] fishPrefabs;

        [Tooltip("鱼群的父物体，用于层级管理（建议创建空物体作为容器）")]
        public GameObject fishSchool;

        [Tooltip("鱼群活动范围半径（单位：米），超出此范围鱼会转向返回")]
        public float wanderSize = 7;

        [Tooltip("鱼群的参考目标点（可选，可绑定到玩家或空物体）")]
        public GameObject target;

        [Tooltip("鱼群的总数量")]
        public int numFish = 30;

        [Tooltip("存储所有鱼的引用列表，供Fish脚本访问邻居")]
        [HideInInspector]
        public List<GameObject> allFish = new List<GameObject>();

        [Tooltip("鱼群的全局目标位置，会定时随机更新")]
        public static Vector3 goalPos = Vector3.zero;

        // 新增：最大挡位（挡位数字越大，出现概率越低）
        [Tooltip("最大挡位，挡位越高生成概率越低")]
        public int maxTier = 5;

        // 初始化鱼群
        void Start()
        {
            for (int i = 0; i < numFish; i++)
            {
                GameObject randomFishPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * wanderSize;
                GameObject fish = Instantiate(randomFishPrefab, spawnPos, Quaternion.identity);
                fish.transform.parent = fishSchool.transform;
                fish.transform.localScale = Vector3.one * (Random.value * 0.2f + 0.9f);
                // 生成鱼后立即添加带Trigger的MeshCollider
                AddMeshColliderWithTrigger(fish);

                // 1. 生成 SkillFishData 并设置挡位等信息
                SkillFishData skillFishData = GenerateSkillFishData();

                // 2. 初始化 FishTierEffect 组件
                FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>();
                if (tierEffect == null)
                {
                    tierEffect = fish.AddComponent<FishTierEffect>();
                }
                tierEffect.fishData = skillFishData;

                // 3. 关联鱼群管理器到 Fish 脚本
                Fish fishScript = fish.GetComponent<Fish>();
                if (fishScript == null)
                {
                    fishScript = fish.AddComponent<Fish>();
                    Debug.Log($"鱼预制体 {randomFishPrefab.name} 未挂载 Fish 脚本，已自动添加");
                }
                fishScript.flock = this;

                allFish.Add(fish);
            }
        }

        private SkillFishData GenerateSkillFishData()
        {
            SkillFishData data = new SkillFishData();

            // 用高斯分布生成挡位
            float mean = 1f;
            float stdDev = 0.8f;

            int randomTier = Mathf.RoundToInt(GenerateGaussian(mean, stdDev));
            randomTier = Mathf.Clamp(randomTier, 0, maxTier - 1);

            // 根据挡位设置经验值
            int[] expValues = { 4, 10, 30, 80, 200, 500 };
            int actualTier = randomTier;
            if (actualTier >= 0 && actualTier < expValues.Length)
            {
                data.baseExpValue = expValues[actualTier];
            }
            else
            {
                data.baseExpValue = 0;
                Debug.LogError($"挡位 {actualTier} 超出经验值配置，设为0");
            }

            data.fishTier = (FishTier)actualTier;
            return data;
        }

        // 生成高斯分布随机数
        private float GenerateGaussian(float mean, float stdDev)
        {
            float u1 = Random.value;
            float u2 = Random.value;
            float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            return mean + z * stdDev;
        }

        void Update()
        {
            HandleGoalPos();
            // 每帧验证鱼群列表，移除已销毁的鱼
            ValidateFishList();
        }

        void HandleGoalPos()
        {
            if (Random.Range(1, 10000) < 50)
            {
                goalPos = new Vector3(
                    Random.Range(-wanderSize, wanderSize),
                    Random.Range(-wanderSize, wanderSize),
                    Random.Range(-wanderSize, wanderSize)
                );
            }
        }

        /// <summary>
        /// 验证鱼群列表，移除已销毁的鱼对象
        /// </summary>
        private void ValidateFishList()
        {
            if (allFish == null) return;

            // 从后往前遍历，避免删除元素时索引错乱
            for (int i = allFish.Count - 1; i >= 0; i--)
            {
                GameObject fish = allFish[i];
                if (fish == null)
                {
                    // 移除空引用（已销毁的鱼）
                    allFish.RemoveAt(i);
                    Debug.Log($"从鱼群列表中移除已销毁的鱼，当前剩余数量: {allFish.Count}");
                }
            }
        }

        /// <summary>
        /// 为鱼添加 MeshCollider 并设置为Trigger
        /// </summary>
        private void AddMeshColliderWithTrigger(GameObject fishObj)
        {
            MeshFilter meshFilter = fishObj.GetComponent<MeshFilter>() ?? fishObj.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                MeshCollider meshCollider = fishObj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.mesh;
                meshCollider.convex = true;
                meshCollider.isTrigger = true;
            }
            else
            {
                Debug.LogWarning($"鱼 {fishObj.name} 未找到 MeshFilter，无法添加碰撞体");
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, wanderSize);
        }
    }
}