using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class FishObjectPool : MonoBehaviour
{
    // 存储每个预制体的对象池
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // 初始化对象池（移除父物体参数）
    public void InitializePool(GameObject prefab, int initialCount)
    {
        if (prefab == null)
        {
            Debug.LogError("初始化对象池失败：预制体为空！");
            return;
        }

        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();

            // 预生成对象
            for (int i = 0; i < initialCount; i++)
            {
                GameObject fish = Instantiate(prefab);
                fish.SetActive(false);
                // 移除所有父物体设置逻辑
                pools[prefab].Enqueue(fish);
            }
        }
    }

    // 获取对象（移除父物体相关逻辑）
    public GameObject GetFishFromPool(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("获取对象失败：预制体为空！");
            return null;
        }

        if (!pools.ContainsKey(prefab) || pools[prefab].Count == 0)
        {
            // 池为空时新建（确保预制体有效）
            GameObject newFish = Instantiate(prefab);
            return newFish;
        }

        // 从池获取
        GameObject fish = pools[prefab].Dequeue();
        fish.SetActive(true);

        return fish;
    }

    // 回收对象
    public void ReturnFishToPool(GameObject fish, GameObject prefab)
    {
        if (fish == null || prefab == null)
        {
            Debug.LogError("回收对象失败：鱼或预制体为空！");
            return;
        }

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogError($"回收失败：不存在 {prefab.name} 的对象池");
            Destroy(fish);
            return;
        }

        fish.SetActive(false);
        pools[prefab].Enqueue(fish);
    }
}

// 移除PooledFish辅助组件，因为不再需要处理父物体设置
