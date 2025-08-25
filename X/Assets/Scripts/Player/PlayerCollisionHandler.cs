using UnityEngine;
using Unity.Netcode;


    public class PlayerCollisionHandler : NetworkBehaviour
    {
        // 用于存储玩家鱼数据的组件引用
        private PlayerFishData playerFishData;
      

        /// <summary>
        /// 初始化时获取 PlayerFishData 组件
        /// </summary>
        private void Awake()
        {
            playerFishData = GetComponent<PlayerFishData>();
            
            if (playerFishData == null)
            {
                Debug.LogError("当前对象上缺少 PlayerFishData 组件！请检查挂载对象。");
            }
        }

        /// <summary>
        /// 触发进入事件：检测碰撞对象的父物体中是否有 Fish 组件
        /// </summary>
        /// <param name="other">碰撞到的对象</param>
        private void OnTriggerEnter(Collider other)
        {
            // 只有本地玩家（自己控制的客户端）才处理碰撞逻辑，避免重复处理
            if (!IsOwner)
                return;

            Debug.Log($"检测到与 {other.gameObject.name} 的碰撞");

            FishTierEffect otherFish = other.GetComponentInParent<FishTierEffect>();

            if (otherFish != null)
            {
                Debug.Log($"找到父物体上的 Fish 组件：{otherFish.gameObject.name}");
                // 获取鱼的 SkillFishData ，从中拿到鱼的 id 等信息
                SkillFishData fishData = otherFish.fishData;
                if (fishData != null)
                {
                    playerFishData.HandleFishCollision(otherFish);

                }
                
            }
            else
            {
                Debug.LogWarning($"未在 {other.gameObject.name} 的父物体中找到 Fish 组件");
                otherFish = other.GetComponent<FishTierEffect>();
                if (otherFish != null)
                {
                    SkillFishData fishData = otherFish.fishData;
                    if (fishData != null)
                    {
                        playerFishData.HandleFishCollision(otherFish);
                    }
                }
            }
        }

    
    }
