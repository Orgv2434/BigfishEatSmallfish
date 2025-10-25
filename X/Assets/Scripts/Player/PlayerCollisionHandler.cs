using UnityEngine;



    public class PlayerCollisionHandler : MonoBehaviour
    {
        private PlayerFishData _playerFishData;

        private void Awake()
        {
            // 提前获取引用（避免每帧查找）
            _playerFishData = GetComponent<PlayerFishData>();
            if (_playerFishData == null)
                Debug.LogError("当前对象上缺少 PlayerFishData 组件！请检查挂载对象。");
        }

        // 仅在触发时执行逻辑（避免每帧检测）
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || _playerFishData == null)
                return;

            // 优先查找鱼自身的FishTierEffect（优化查找逻辑）
            FishTierEffect otherFish = other.GetComponent<FishTierEffect>();
            // 未找到时再查父对象（兼容层级结构）
            if (otherFish == null)
                otherFish = other.GetComponentInParent<FishTierEffect>();

            if (otherFish != null)
            {
                // Debug.Log($"检测到与 {otherFish.gameObject.name} 的碰撞");
                _playerFishData.HandleFishCollision(otherFish); // 调用玩家吃鱼逻辑
            }
            // 移除冗余的else Debug（减少日志开销，仅异常时输出）
        }
    }
