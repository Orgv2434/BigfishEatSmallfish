using UnityEditor;
using UnityEngine;
using Unity.Netcode;

public class AddFishToNetworkPrefabs : MonoBehaviour
{
    [MenuItem("Tools/Add Fish To Network Prefabs")]
    static void AddFishPrefabs()
    {
        // 1. 找到场景中的NetworkManager
        NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("场景中未找到NetworkManager！请先在场景中添加NetworkManager");
            return;
        }

        // 2. 获取网络预制体配置
        NetworkPrefabs networkPrefabs = networkManager.NetworkConfig.Prefabs;
        if (networkPrefabs == null)
        {
            Debug.LogError("NetworkManager的NetworkConfig中未找到Prefabs配置！");
            return;
        }

        // 3. 遍历鱼预制体文件夹（替换为你的实际路径）
        string fishPrefabsPath = "Assets/Resources/Tropical Fish";
        string[] fishPrefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { fishPrefabsPath });
        int addedCount = 0;

        foreach (string guid in fishPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (fishPrefab == null)
            {
                Debug.LogWarning($"跳过无效预制体：{path}");
                continue;
            }

            // 4. 尝试添加预制体（通过捕获异常判断是否已存在）
            try
            {
                // 尝试添加预制体
                networkPrefabs.Add(new NetworkPrefab { Prefab = fishPrefab });
                Debug.Log($"已添加预制体: {fishPrefab.name}");
                addedCount++;
            }
            catch (System.Exception ex)
            {
                // 捕获"已存在"的异常（不同版本错误信息可能不同）
                if (ex.Message.Contains("already exists") ||
                    ex.Message.Contains("已存在") ||
                    ex.Message.Contains("重复"))
                {
                    // 已存在则跳过，不输出错误
                    continue;
                }
                else
                {
                    // 其他错误才输出
                    Debug.LogError($"添加预制体 {fishPrefab.name} 失败: {ex.Message}");
                }
            }
        }

        // 5. 保存修改
        EditorUtility.SetDirty(networkManager);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"批量添加完成！共处理 {fishPrefabGuids.Length} 个预制体，新增 {addedCount} 个");
    }
}

