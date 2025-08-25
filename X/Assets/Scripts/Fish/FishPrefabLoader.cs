using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 鱼预制体批量加载工具（按文件夹路径读取）
/// </summary>
public static class FishPrefabLoader
{
    /// <summary>
    /// 从Resources文件夹下的指定路径批量加载鱼预制体
    /// </summary>
    /// <param name="Tropical Fish">Resources内的文件夹路径（例："FishPrefabs/Common"）</param>
    /// <returns>加载到的所有鱼预制体列表</returns>
    public static List<GameObject> LoadFishPrefabsFromResources(string resourcesFolderPath)
    {
        List<GameObject> fishPrefabs = new List<GameObject>();

        // 1. 按路径加载该文件夹下的所有预制体（后缀为.prefab）
        Object[] loadedAssets = Resources.LoadAll(resourcesFolderPath, typeof(GameObject));

        // 2. 筛选出有效的鱼预制体（需包含FishTierEffect组件，避免加载非鱼预制体）
        foreach (Object asset in loadedAssets)
        {
            GameObject prefab = asset as GameObject;
            if (prefab != null )
            {
                fishPrefabs.Add(prefab);
                Debug.Log($"成功加载鱼预制体：{prefab.name}");
            }
        }

        // 3. 校验加载结果
        if (fishPrefabs.Count == 0)
        {
            Debug.LogError($"在路径 Resources/{resourcesFolderPath} 下未找到有效的鱼预制体");
        }
        else
        {
            Debug.Log($"共加载 {fishPrefabs.Count} 个鱼预制体");
        }

        return fishPrefabs;
    }


    /// <summary>
    /// （编辑器模式专用）从项目文件夹路径批量加载鱼预制体（需引入UnityEditor命名空间）
    /// 注：仅在编辑器中使用，打包后无效
    /// </summary>
    /// <param name="projectFolderPath">项目内的文件夹路径（例："Assets/Prefabs/Fish"）</param>
    /// <returns>加载到的所有鱼预制体列表</returns>
#if UNITY_EDITOR
    public static List<GameObject> LoadFishPrefabsFromProjectFolder(string projectFolderPath)
    {
        List<GameObject> fishPrefabs = new List<GameObject>();
        // 引入UnityEditor命名空间，按路径查找所有.prefab文件
        string[] prefabPaths = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { projectFolderPath });

        foreach (string prefabGuid in prefabPaths)
        {
            string prefabPath = UnityEditor.AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null && prefab.GetComponent<FishTierEffect>() != null)
            {
                fishPrefabs.Add(prefab);
                Debug.Log($"编辑器模式加载鱼预制体：{prefab.name}（路径：{prefabPath}）");
            }
        }

        if (fishPrefabs.Count == 0)
        {
            Debug.LogError($"在项目路径 {projectFolderPath} 下未找到有效的鱼预制体！");
        }

        return fishPrefabs;
    }
#endif
}


