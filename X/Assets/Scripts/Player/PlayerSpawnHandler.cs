using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DistantLands
{
    // 挂载到Player预制体上，监听玩家网络生成事件
    public class PlayerSpawnHandler : MonoBehaviour
    {
        [Header("自定义出生点（可选，不用则用PlayerStart标签）")]
        [SerializeField] private List<Transform> customSpawnPoints; // 自定义出生点列表
        [SerializeField] private bool useCustomSpawnPoints = true;   // 是否使用自定义出生点
        [SerializeField] private bool useRandomSpawn = true;         // 自定义出生点是否随机
        private int nextSpawnIndex = 0;
        private void Start()
        {
            OnPlayerSpawned();
        }
        private void OnPlayerSpawned()
        {
            // 获取玩家的 NetworkObject（Netcode 自动生成的玩家对象）
           
                    Transform spawnPoint = GetCustomSpawnPoint();
                    if (spawnPoint != null)
                    {
                        // 设置玩家的出生位置和旋转
                        this.transform.position = spawnPoint.position;
                        this.transform.rotation = spawnPoint.rotation;
                        Debug.Log($"为客户端  在自定义出生点 {spawnPoint.name} 生成玩家");
                    }
                    else
                    {
                        // 若没自定义出生点，Netcode 会自动找“PlayerStart”标签的物体
                        Debug.Log($"为客户端  使用默认 PlayerStart 标签出生点");
                    }
                }
            
        

        // 获取自定义出生点（复用你原有的逻辑）
        private Transform GetCustomSpawnPoint()
        {
            // 若不启用自定义出生点，返回 null（交给 Netcode 默认逻辑）
            if (!useCustomSpawnPoints || customSpawnPoints.Count == 0)
                return null;

            if (useRandomSpawn)
            {
                int randomIndex = Random.Range(0, customSpawnPoints.Count);
                return customSpawnPoints[randomIndex];
            }
            else
            {
                Transform targetPoint = customSpawnPoints[nextSpawnIndex];
                nextSpawnIndex = (nextSpawnIndex + 1) % customSpawnPoints.Count;
                return targetPoint;
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (!useCustomSpawnPoints || customSpawnPoints == null || customSpawnPoints.Count == 0)
                return;

            Gizmos.color = Color.cyan;
            foreach (var spawnPoint in customSpawnPoints)
            {
                if (spawnPoint == null) continue;
                Gizmos.DrawSphere(spawnPoint.position, 0.5f);
                Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 1f);
            }
        }
    }


}