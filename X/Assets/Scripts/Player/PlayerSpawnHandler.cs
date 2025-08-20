using Unity.Netcode;
using UnityEngine;

namespace DistantLands
{
    // 挂载到Player预制体上，监听玩家网络生成事件
    public class PlayerSpawnHandler : NetworkBehaviour
    {
        // 当玩家网络对象生成完成时调用（Netcode核心回调）
        public override void OnNetworkSpawn()
        {
            // 只在本地玩家生成时执行（避免重复处理）
            if (IsOwner)
            {
                Debug.Log("本地玩家已生成，通知技能鱼管理器");
                // 查找技能鱼管理器并触发玩家查找
                SkillFishManager fishManager = FindObjectOfType<SkillFishManager>();
                if (fishManager != null)
                {
                  
                }
                else
                {
                    Debug.LogError("未找到SkillFishManager实例！");
                }
            }
        }
    }
}