using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

namespace DistantLands
{
    public class SkillFishManager :NetworkManager
    {
        [Header("技能鱼基础设置")]
        [Tooltip("技能鱼预制体（建议使用不同外观区分普通鱼）")]
        public GameObject[] skillFishPrefabs;

        [Tooltip("技能鱼父物体（用于层级管理）")]
        public GameObject skillFishParent;

        [Tooltip("全图技能鱼稳定数量（始终保持此数量）")]
        [Range(3, 8)] public int stableSkillFishCount = 5;

        [Header("挡位比例设置")]
        [Tooltip("0 挡位鱼的生成概率（0-100，需高于 50）")]
        [Range(51, 90)] public int tier0Probability = 70;
        [Tooltip("1 挡位鱼的生成概率（自动计算为 100 - tier0Probability）")]
        [ReadOnly] public int tier1Probability;

        [Header("生成范围")]
        public Vector3 spawnRangeMin;
        public Vector3 spawnRangeMax;

        [Header("技能概率设置（总和建议为 100）")]
        [Range(5, 20)] public int dashSkillProbability = 10;
        [Range(20, 30)] public int shieldSkillProbability = 25;
        [Range(20, 30)] public int camouflageSkillProbability = 30;
        [Range(20, 30)] public int expSkillProbability = 35;

        [Header("移动参数预设")]
        public float normalSpeed = 2f;
        public float rotationSmooth = 5f;
        public float playerDetectRange = 3f;
        public float fleeDuration = 4f;
        public float fleeSpeedMultiplier = 1.5f;

        [Header("球体路径参数")]
        public float pathSphereRadius = 5f;
        public Vector2 yAxisRange = new Vector2(-2f, 2f);

        private List<GameObject> activeSkillFish = new List<GameObject>();
        private FishSkillSystem playerSkillSystem;
        private PlayerFishData _playerData;
        private bool isInitialized = false;

        // 暴露初始化状态（供外部判断）
        public bool IsInitialized => isInitialized;

        private void Start()
        {
            tier1Probability = 100 - tier0Probability;

            // 监听网络管理器事件（主机和客户端通用）
            if (NetworkManager.Singleton != null)
            {
                // 主机启动时触发初始生成（主机同时作为服务器）
                NetworkManager.Singleton.OnServerStarted += OnHostStarted;
                // 客户端连接后初始化本地逻辑
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
            else
            {
                Debug.LogError("未找到NetworkManager实例！请确保场景中存在NetworkManager");
            }
        }

        // 主机启动后生成初始技能鱼（主机作为权威端）
        private void OnHostStarted()
        {
            if (NetworkManager.Singleton.IsHost) // 明确判断为主机
            {
                Debug.Log("主机启动，生成初始技能鱼（主机拥有生成权威）");
                SpawnInitialSkillFish();
                FindPlayerSkillSystem();
            }
        }

        // 客户端连接后初始化本地逻辑（仅处理本地显示相关）
        private void OnClientConnected(ulong clientId)
        {
            // 只处理本地客户端（非主机的客户端）
            if (clientId == NetworkManager.Singleton.LocalClientId && !NetworkManager.Singleton.IsHost)
            {
                Debug.Log("客户端已连接，初始化本地技能鱼逻辑（仅同步显示）");
                FindPlayerSkillSystem();
            }
        }

        // 查找玩家技能系统（主机和客户端通用，客户端仅用于事件监听）
        public void FindPlayerSkillSystem()
        {
            if (isInitialized) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerSkillSystem = player.GetComponent<FishSkillSystem>();
                if (playerSkillSystem != null)
                {
                    _playerData = player.GetComponent<PlayerFishData>();
                    Debug.Log($"{(NetworkManager.Singleton.IsHost ? "主机" : "客户端")}成功找到玩家技能系统");
                    SubscribeToSkillEvents();
                    isInitialized = true;
                }
                else
                {
                    Debug.LogError("玩家对象上未找到FishSkillSystem组件！");
                    Invoke(nameof(FindPlayerSkillSystem), 1f); // 重试
                }
            }
            else
            {
                Debug.LogWarning("未找到玩家对象，1秒后重试...");
                Invoke(nameof(FindPlayerSkillSystem), 1f); // 重试
            }
        }

        // 订阅技能事件（主机处理生成，客户端仅监听用于本地反馈）
        public void SubscribeToSkillEvents()
        {
            if (playerSkillSystem != null)
            {
                playerSkillSystem.OnOneTimeSkillUsed -= OnPlayerUsedOneTimeSkill;
                playerSkillSystem.OnOneTimeSkillUsed += OnPlayerUsedOneTimeSkill;
                Debug.Log($"{(NetworkManager.Singleton.IsHost ? "主机" : "客户端")}已订阅技能使用事件");
            }
        }

        // 取消订阅技能事件（玩家离开时清理）
        public void UnsubscribeFromSkillEvents()
        {
            if (playerSkillSystem != null)
            {
                playerSkillSystem.OnOneTimeSkillUsed -= OnPlayerUsedOneTimeSkill;
                Debug.Log($"{(NetworkManager.Singleton.IsHost ? "主机" : "客户端")}已取消订阅技能使用事件");
            }
        }

        // 初始生成稳定数量的技能鱼（仅主机执行）
        private void SpawnInitialSkillFish()
        {
            if (!NetworkManager.Singleton.IsHost) return; // 非主机不执行

            for (int i = 0; i < stableSkillFishCount; i++)
            {
                SpawnSingleSkillFish();
            }
        }

        private void SpawnSingleSkillFish(FishSkillType? targetSkill = null)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                Debug.LogWarning("客户端无生成权限，技能鱼生成由主机处理");
                return;
            }

            if (skillFishPrefabs.Length == 0)
            {
                Debug.LogError("未设置技能鱼预制体！请在Inspector中赋值skillFishPrefabs");
                return;
            }

            // 1. 随机选择预制体、计算位置
            GameObject prefab = skillFishPrefabs[UnityEngine.Random.Range(0, skillFishPrefabs.Length)];
            Vector3 spawnPos = new Vector3(
                UnityEngine.Random.Range(spawnRangeMin.x, spawnRangeMax.x),
                UnityEngine.Random.Range(spawnRangeMin.y, spawnRangeMax.y),
                UnityEngine.Random.Range(spawnRangeMin.z, spawnRangeMax.z)
            );

            // 2. 实例化技能鱼（先不设置父物体）
            GameObject skillFish = Instantiate(prefab, spawnPos, Quaternion.identity);
            skillFish.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f);

            // 3. 先执行网络生成（关键：必须先Spawn()）
            NetworkObject networkObj = skillFish.GetComponent<NetworkObject>();
            if (networkObj != null)
            {
                networkObj.Spawn(); // 先完成网络生成
                Debug.Log($"主机生成技能鱼，已同步到所有客户端（位置：{spawnPos}）");
            }
            else
            {
                Debug.LogWarning("技能鱼预制体缺少NetworkObject组件，无法网络同步");
            }

            // 4. 生成后再设置父物体（遵守Netcode规则）
            skillFish.transform.parent = skillFishParent != null ? skillFishParent.transform : transform;
            Debug.Log($"设置父物体后位置：{skillFish.transform.position}");
            // 5. 后续配置（碰撞体、移动脚本等）
            AddTriggerCollider(skillFish);
            ConfigureSkillData(skillFish, targetSkill);
            AddAndConfigureMovementScript(skillFish);

            activeSkillFish.Add(skillFish);
        }
        // 为技能鱼添加并配置移动脚本（主机配置，客户端同步生效）
        private void AddAndConfigureMovementScript(GameObject fish)
        {
            SkillFishMovement movement = fish.AddComponent<SkillFishMovement>();
            movement.normalSpeed = normalSpeed;
            movement.rotationSmooth = rotationSmooth;
            movement.playerTag = "Player";
            movement.playerDetectRange = playerDetectRange;
            movement.fleeDuration = fleeDuration;
            movement.fleeSpeedMultiplier = fleeSpeedMultiplier;
            movement.pathSphereRadius = pathSphereRadius;
            movement.yAxisRange = yAxisRange;
            movement.FindPlayers();
        }

        // 为技能鱼添加触发碰撞体（主机配置，客户端同步）
        private void AddTriggerCollider(GameObject fish)
        {
            Collider collider = fish.GetComponent<Collider>();
            if (collider == null)
            {
                MeshFilter meshFilter = fish.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    MeshCollider meshCol = fish.GetComponent<MeshCollider>();
                    meshCol.convex = true;
                    meshCol.isTrigger = true;
                }
                else
                {
                    SphereCollider sphereCol = fish.GetComponent<SphereCollider>();
                    sphereCol.radius = 0.4f;
                    sphereCol.isTrigger = true;
                }
            }
            else
            {
                collider.isTrigger = true;
            }
        }

        // 配置技能鱼数据（主机配置，客户端同步）
        private void ConfigureSkillData(GameObject fish, FishSkillType? targetSkill)
        {
            FishTierEffect tierEffect = fish.GetComponent<FishTierEffect>() ?? fish.GetComponent<FishTierEffect>();
            SkillFishData skillData = new SkillFishData();

            skillData.skillType = targetSkill.HasValue ? targetSkill.Value : GetRandomSkillTypeByProbability();

            // 挡位逻辑
            int random = UnityEngine.Random.Range(0, 100);
            int tier = random < tier0Probability ? 0 : 1;
            skillData.fishTier = (FishTier)tier;

            // 经验值设置
            skillData.baseExpValue = tier * 40 + 40;

            tierEffect.fishData = skillData;
        }

        // 按概率随机技能类型（主机计算，客户端同步）
        private FishSkillType GetRandomSkillTypeByProbability()
        {
            int total = dashSkillProbability + shieldSkillProbability + camouflageSkillProbability + expSkillProbability;
            int random = UnityEngine.Random.Range(0, total);

            if (random < dashSkillProbability) return FishSkillType.Dash;
            else if (random < dashSkillProbability + shieldSkillProbability) return FishSkillType.Shield;
            else if (random < dashSkillProbability + shieldSkillProbability + camouflageSkillProbability) return FishSkillType.Camouflage;
            else return FishSkillType.ExpMultiplier;
        }

        // 玩家使用技能后触发（主机处理生成，客户端仅日志反馈）
        public void OnPlayerUsedOneTimeSkill(FishSkillType usedSkill)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("尚未初始化完成，无法处理技能使用事件");
                return;
            }

            Debug.Log($"{(NetworkManager.Singleton.IsHost ? "主机" : "客户端")}检测到玩家使用【{usedSkill}】技能");

            CleanupDestroyedFish();

            // 仅主机处理生成逻辑（权威控制）
            if (NetworkManager.Singleton.IsHost)
            {
                int needToSpawn = stableSkillFishCount - activeSkillFish.Count;
                if (needToSpawn > 0)
                {
                    Debug.Log($"主机补充生成 {needToSpawn} 条【{usedSkill}】类型技能鱼");
                    for (int i = 0; i < needToSpawn; i++)
                    {
                        SpawnSingleSkillFish(usedSkill);
                    }
                }
            }
        }

        // 清理已销毁的技能鱼引用（仅主机维护列表）
        private void CleanupDestroyedFish()
        {
            if (!NetworkManager.Singleton.IsHost) return; // 客户端无需维护

            for (int i = activeSkillFish.Count - 1; i >= 0; i--)
            {
                if (activeSkillFish[i] == null)
                {
                    activeSkillFish.RemoveAt(i);
                }
            }
        }

        private void Update()
        {
            // 仅主机需要每帧清理（客户端通过网络同步自动感知）
            if (NetworkManager.Singleton.IsHost)
            {
                CleanupDestroyedFish();
            }
        }

        // 绘制生成范围Gizmos（编辑模式可见）
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
            Gizmos.DrawCube(
                (spawnRangeMin + spawnRangeMax) / 2,
                spawnRangeMax - spawnRangeMin
            );
        }

        // 只读属性标签
        [AttributeUsage(AttributeTargets.Field)]
        public class ReadOnlyAttribute : Attribute { }

        // 销毁时清理事件订阅
        private void OnDestroy()
        {
            UnsubscribeFromSkillEvents();
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnHostStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }
    }
}