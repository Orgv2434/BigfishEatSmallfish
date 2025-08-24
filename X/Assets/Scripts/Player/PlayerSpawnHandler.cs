using System.Collections.Generic;
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

        private int nextSpawnIndex = 0;
        // 存储服务器分配的最终出生点（用于调试）
        private Vector3 assignedSpawnPos;
        private Quaternion assignedSpawnRot;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 只在服务器端执行出生点分配（确保逻辑唯一来源）
            if (IsServer)
            {
                AssignSpawnPoint();
            }
        }

        // 服务器端分配出生点并同步
        private void AssignSpawnPoint()
        {
            // 1. 服务器计算出生点
            Transform spawnPoint = GetCustomSpawnPoint();
            if (spawnPoint != null)
            {
                assignedSpawnPos = spawnPoint.position;
                assignedSpawnRot = spawnPoint.rotation;
                Debug.Log($"服务器为玩家 {OwnerClientId} 分配自定义出生点: {spawnPoint.name}，位置: {assignedSpawnPos}");
            }
            else
            {
                // 找不到自定义出生点时，使用场景中"PlayerStart"标签物体
                var defaultSpawns = GameObject.FindGameObjectsWithTag("PlayerStart");
                if (defaultSpawns.Length > 0)
                {
                    var randomSpawn = defaultSpawns[Random.Range(0, defaultSpawns.Length)];
                    assignedSpawnPos = randomSpawn.transform.position;
                    assignedSpawnRot = randomSpawn.transform.rotation;
                    Debug.Log($"服务器为玩家 {OwnerClientId} 分配默认出生点: {randomSpawn.name}，位置: {assignedSpawnPos}");
                }
                else
                {
                    // 极端情况：无任何出生点，使用原点
                    assignedSpawnPos = Vector3.zero;
                    assignedSpawnRot = Quaternion.identity;
                    Debug.LogWarning($"服务器未找到任何出生点，玩家 {OwnerClientId} 生成在原点");
                }
            }

            // 2. 同步位置到所有客户端（包括主机和远程客户端）
            SyncSpawnPositionClientRpc(assignedSpawnPos, assignedSpawnRot);
        }

        // 客户端接收服务器同步的位置并强制应用
        [ClientRpc]
        private void SyncSpawnPositionClientRpc(Vector3 position, Quaternion rotation)
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

        private Transform GetCustomSpawnPoint()
        {
            if (!useCustomSpawnPoints || customSpawnPoints == null || customSpawnPoints.Count == 0)
                return null;

            if (useRandomSpawn)
            {
                return customSpawnPoints[Random.Range(0, customSpawnPoints.Count)];
            }
            else
            {
                Transform target = customSpawnPoints[nextSpawnIndex];
                nextSpawnIndex = (nextSpawnIndex + 1) % customSpawnPoints.Count;
                return target;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!useCustomSpawnPoints || customSpawnPoints == null || customSpawnPoints.Count == 0)
                return;

            Gizmos.color = Color.cyan;
            foreach (var spawn in customSpawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawSphere(spawn.position, 0.5f);
                    Gizmos.DrawRay(spawn.position, spawn.forward * 1f);
                }
            }
        }
    }
}
