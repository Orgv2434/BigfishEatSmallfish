using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace DistantLands
{
    public class PlayerSpawnHandler : NetworkBehaviour
    {
        [Header("自定义出生点配置")]
        [SerializeField] private List<Transform> customSpawnPoints;
        [SerializeField] private bool useCustomSpawnPoints = true;
        [SerializeField] private bool useRandomSpawn = true;
        [SerializeField] private float spawnPointCheckRadius = 0.1f; // 检查出生点是否被占用的半径

        // 静态索引确保服务器全局唯一管理出生点分配，适合延迟加入的客户端
        private static int nextSpawnIndex = 0;
        // 存储所有已使用的出生点，避免重复分配
        private static HashSet<Vector3> usedSpawnPositions = new HashSet<Vector3>();

        // 存储服务器分配的最终出生点（用于调试和重连）
        private Vector3 assignedSpawnPos;
        private Quaternion assignedSpawnRot;

        private void Awake()
        {
            // 注册客户端连接回调，处理延迟加入逻辑
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 只在服务器端执行出生点分配（确保逻辑唯一来源）
            if (IsServer)
            {
                // 为当前玩家分配出生点（包括延迟加入的客户端）
                AssignSpawnPoint();
            }
            // 对于延迟加入的客户端，请求最新的出生点信息
            else if (IsClient && IsOwner)
            {
                RequestSpawnPositionServerRpc();
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            // 服务器端：当新客户端（非服务器自身）连接时
            if (IsServer && clientId != NetworkManager.ServerClientId)
            {
                Debug.Log($"客户端 {clientId} 延迟加入，准备分配出生点");
                // 可以在这里添加额外的延迟加入处理逻辑，如加载玩家数据等
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            // 服务器端：客户端断开连接时释放其出生点
            if (IsServer)
            {
                // 关键修改：先检查客户端是否还在连接列表中
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
                {
                    var playerObject = networkClient.PlayerObject;
                    if (playerObject != null)
                    {
                        var spawnHandler = playerObject.GetComponent<PlayerSpawnHandler>();
                        if (spawnHandler != null && usedSpawnPositions.Contains(spawnHandler.assignedSpawnPos))
                        {
                            usedSpawnPositions.Remove(spawnHandler.assignedSpawnPos);
                            Debug.Log($"客户端 {clientId} 断开连接，释放出生点: {spawnHandler.assignedSpawnPos}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"客户端 {clientId} 已从连接列表中移除，无需释放出生点");
                }
            }
        }

        // 客户端请求出生点（用于延迟加入的客户端）
        [ServerRpc(RequireOwnership = false)]
        private void RequestSpawnPositionServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log($"收到客户端 {clientId} 的出生点请求，重新分配出生点");

            // 为请求的客户端重新分配出生点
            AssignSpawnPoint(clientId);
        }

        // 服务器端分配出生点并同步（支持指定客户端）
        private void AssignSpawnPoint(ulong targetClientId = ulong.MaxValue)
        {
            // 确定目标客户端ID（默认为当前组件所属客户端）
            ulong clientId = targetClientId == ulong.MaxValue ? OwnerClientId : targetClientId;

            // 1. 服务器计算出生点（确保不重复）
            Transform spawnPoint = GetAvailableSpawnPoint();
            if (spawnPoint != null)
            {
                assignedSpawnPos = spawnPoint.position;
                assignedSpawnRot = spawnPoint.rotation;
                Debug.Log($"服务器为玩家 {clientId} 分配自定义出生点: {spawnPoint.name}，位置: {assignedSpawnPos}");
            }
            else
            {
                // 找不到自定义出生点时，使用场景中"PlayerStart"标签物体
                var defaultSpawns = GameObject.FindGameObjectsWithTag("PlayerStart")
                    .Where(go => go != null) // 过滤已销毁的对象
                    .Select(go => go.transform)
                    .ToList();

                if (defaultSpawns.Count > 0)
                {
                    // 找到第一个未被使用的默认出生点
                    var availableSpawn = defaultSpawns.FirstOrDefault(t =>
                        !usedSpawnPositions.Contains(t.position) ||
                        !Physics.CheckSphere(t.position, spawnPointCheckRadius));

                    if (availableSpawn != null)
                    {
                        assignedSpawnPos = availableSpawn.position;
                        assignedSpawnRot = availableSpawn.rotation;
                    }
                    else
                    {
                        // 所有默认出生点都被使用，随机选一个
                        var randomSpawn = defaultSpawns[Random.Range(0, defaultSpawns.Count)];
                        assignedSpawnPos = randomSpawn.position;
                        assignedSpawnRot = randomSpawn.rotation;
                        Debug.LogWarning($"所有默认出生点都被占用，为玩家 {clientId} 分配已使用的出生点: {randomSpawn.name}");
                    }
                    Debug.Log($"服务器为玩家 {clientId} 分配默认出生点: {assignedSpawnPos}");
                }
                else
                {
                    // 极端情况：无任何出生点，使用原点
                    assignedSpawnPos = Vector3.zero;
                    assignedSpawnRot = Quaternion.identity;
                    Debug.LogWarning($"服务器未找到任何出生点，玩家 {clientId} 生成在原点");
                }
            }

            // 标记该出生点为已使用
            usedSpawnPositions.Add(assignedSpawnPos);

            // 2. 同步位置到目标客户端（如果指定）或所有客户端
            if (targetClientId != ulong.MaxValue)
            {
                // 只同步给指定的延迟加入客户端
                SyncSpawnPositionClientRpc(assignedSpawnPos, assignedSpawnRot,
                    new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { targetClientId } } });
            }
            else
            {
                // 同步到所有客户端
                SyncSpawnPositionClientRpc(assignedSpawnPos, assignedSpawnRot);
            }
        }

        // 客户端接收服务器同步的位置并强制应用
        [ClientRpc]
        private void SyncSpawnPositionClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
        {
            // 强制覆盖客户端当前位置（防止默认位置干扰）
            transform.position = position;
            transform.rotation = rotation;

            // 输出客户端日志，确认是否收到正确位置
            if (IsOwner) // 只在本地客户端输出，避免重复日志
            {
                Debug.Log($"客户端 {NetworkManager.Singleton.LocalClientId} 应用出生位置: {position}");
            }
        }

        // 获取可用的出生点（未被占用的）
        private Transform GetAvailableSpawnPoint()
        {
            if (!useCustomSpawnPoints || customSpawnPoints == null)
                return null;

            // 过滤掉无效和已使用的出生点
            var validSpawns = customSpawnPoints
                .Where(p => p != null) // 排除空引用
                .Where(p => !usedSpawnPositions.Contains(p.position) ||
                           !Physics.CheckSphere(p.position, spawnPointCheckRadius)) // 排除已占用的
                .ToList();

            if (validSpawns.Count == 0)
                return null;

            if (useRandomSpawn)
            {
                return validSpawns[Random.Range(0, validSpawns.Count)];
            }
            else
            {
                // 使用静态索引确保全局顺序分配
                int index = nextSpawnIndex % validSpawns.Count;
                Transform target = validSpawns[index];
                nextSpawnIndex = (nextSpawnIndex + 1) % validSpawns.Count;
                return target;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!useCustomSpawnPoints || customSpawnPoints == null)
                return;

            Gizmos.color = Color.cyan;
            foreach (var spawn in customSpawnPoints)
            {
                if (spawn != null)
                {
                    // 已使用的出生点显示为红色，未使用的为青色
                    if (usedSpawnPositions.Contains(spawn.position))
                        Gizmos.color = Color.red;
                    else
                        Gizmos.color = Color.cyan;

                    Gizmos.DrawSphere(spawn.position, 0.5f);
                    Gizmos.DrawRay(spawn.position, spawn.forward * 1f);
                }
            }
        }

        public override void OnDestroy()
        { base.OnDestroy();
            // 取消注册回调，避免内存泄漏
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            // 释放当前出生点
            if (IsServer && usedSpawnPositions.Contains(assignedSpawnPos))
            {
                usedSpawnPositions.Remove(assignedSpawnPos);
            }
        }
    }
}
