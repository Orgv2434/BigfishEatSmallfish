using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

namespace DistantLands
{
    public class GlobalFlock : NetworkBehaviour
    {
        [Tooltip("鱼的预制体数组，可添加多种鱼模型")]
        public GameObject[] fishPrefabs;

        [Tooltip("鱼群的父物体，用于层级管理")]
        public GameObject fishSchool;

        [Tooltip("鱼群活动范围半径")]
        public float wanderSize = 7;

        [Tooltip("鱼群的参考目标点")]
        public GameObject target;

        [Tooltip("鱼群的总数量")]
        public int numFish = 30;

        [HideInInspector]
        public List<GameObject> allFish = new List<GameObject>();

        [Tooltip("鱼群的全局目标位置")]
        public static Vector3 goalPos = Vector3.zero;

        [Tooltip("最大挡位")]
        public int maxTier = 5;

        // 网络同步鱼群目标位置
        private NetworkVariable<Vector3> networkGoalPos = new NetworkVariable<Vector3>(
            writePerm: NetworkVariableWritePermission.Server
        );

        void Start()
        {
            if (!IsNetworkInitialized())
            {
                SpawnFishSchool();
            }

            // 客户端监听目标位置变化
            if (!IsServer)
            {
                networkGoalPos.OnValueChanged += OnGoalPosChanged;
            }
        }

        // 同步目标位置到静态变量
        private void OnGoalPosChanged(Vector3 oldVal, Vector3 newVal)
        {
            goalPos = newVal;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                SpawnFishSchool();
                // 初始化网络目标位置
                networkGoalPos.Value = goalPos;
            }
        }

        private void SpawnFishSchool()
        {
            if (allFish.Count > 0) return;

            for (int i = 0; i < numFish; i++)
            {
                GameObject randomFishPrefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * wanderSize;
                GameObject fish = Instantiate(randomFishPrefab, spawnPos, Quaternion.identity);
                fish.transform.parent = fishSchool.transform;
                fish.transform.localScale = Vector3.one * (Random.value * 0.2f + 0.9f);

                AddNetworkObject(fish);
                AddMeshColliderWithTrigger(fish);

                SkillFishData skillFishData = GenerateSkillFishData();
                FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>() ?? fish.AddComponent<FishTierEffect>();
                tierEffect.fishData = skillFishData;
                Fish fishScript = fish.GetComponent<Fish>() ?? fish.AddComponent<Fish>();
                fishScript.flock = this;

                allFish.Add(fish);
            }
        }

        private void AddNetworkObject(GameObject fish)
        {
            if (fish.GetComponent<Fish>() == null) return;

            NetworkObject netObj = fish.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                netObj = fish.AddComponent<NetworkObject>();
            }

            if (IsServer && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }
        }

        public SkillFishData GenerateSkillFishData()
        {
            SkillFishData data = new SkillFishData();
            float mean = 1f;
            float stdDev = 0.8f;
            int randomTier = Mathf.RoundToInt(GenerateGaussian(mean, stdDev));
            randomTier = Mathf.Clamp(randomTier, 0, maxTier - 1);
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

        private float GenerateGaussian(float mean, float stdDev)
        {
            float u1 = Random.value;
            float u2 = Random.value;
            float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            return mean + z * stdDev;
        }

        void Update()
        {
            if (IsServer || !IsNetworkInitialized())
            {
                HandleGoalPos();
            }
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

                // 服务器更新网络目标位置
                if (IsServer)
                {
                    networkGoalPos.Value = goalPos;
                }
            }
        }

        private void ValidateFishList()
        {
            if (allFish == null) return;
            for (int i = allFish.Count - 1; i >= 0; i--)
            {
                GameObject fish = allFish[i];
                if (fish == null)
                {
                    allFish.RemoveAt(i);
                    Debug.Log($"从鱼群列表中移除已销毁的鱼，当前剩余数量: {allFish.Count}");
                }
            }
        }

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

        private bool IsNetworkInitialized()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }
    }
}