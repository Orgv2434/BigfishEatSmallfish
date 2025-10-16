using UnityEngine;

namespace YourNamespace // 可根据项目需求替换为实际命名空间
{
    public class PlayerCollisionHandler : MonoBehaviour
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
            Debug.Log($"检测到与 {other.gameObject.name} 的碰撞");

           
            FishTierEffect otherFish = other.GetComponentInParent<FishTierEffect>();

            if (otherFish != null)
            {
                Debug.Log($"找到父物体上的 FishTierEffect 组件：{otherFish.gameObject.name}");
                playerFishData?.HandleFishCollision(otherFish); // 安全调用碰撞处理逻辑
            }
            else
            {
                Debug.LogWarning($"未在 {other.gameObject.name} 的父物体中找到 FishTierEffect 组件");
              otherFish = other.GetComponent<FishTierEffect>();
                playerFishData?.HandleFishCollision(otherFish); // 安全调用碰撞处理逻辑
            }
        }
    }
}