using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class UltimatePrefabCleaner
{
    // 菜单路径：Tools下的子菜单，方便查找
    [MenuItem("Tools/Ultimate Clean Missing Scripts From Prefabs")]
    static void CleanAllMissingScripts()
    {
        // 目标文件夹路径（请确保与你的预制体路径完全一致）
        string targetFolder = "Assets/Resources/Tropical Fish";

        // 检查文件夹是否存在
        if (!Directory.Exists(targetFolder))
        {
            Debug.LogError($"错误：目标文件夹不存在 - {targetFolder}");
            return;
        }

        // 获取所有预制体GUID
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolder });
        if (prefabGuids.Length == 0)
        {
            Debug.LogWarning("未找到任何预制体，请检查路径");
            return;
        }

        int totalProcessed = 0;
        int totalRemoved = 0;
        List<string> failedPrefabs = new List<string>();

        // 显示进度条
        EditorUtility.DisplayProgressBar("清理预制体", "准备开始...", 0);

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            totalProcessed++;

            // 加载原始预制体
            GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (originalPrefab == null)
            {
                Debug.LogWarning($"跳过无效预制体：{prefabPath}");
                continue;
            }

            // 核心流程：实例化→清理→替换
            GameObject tempInstance = null;
            try
            {
                // 1. 实例化预制体（关键：操作实例而非原始资源）
                tempInstance = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
                if (tempInstance == null)
                {
                    failedPrefabs.Add(prefabPath);
                    continue;
                }

                // 2. 第一次清理Missing脚本
                int removed = CleanGameObjectAndChildren(tempInstance);

                // 3. 二次验证并强制清理（解决残留问题）
                int recheckRemoved = CleanGameObjectAndChildren(tempInstance);
                removed += recheckRemoved;

                // 4. 最终验证是否还有残留
                if (HasMissingScripts(tempInstance))
                {
                    // 使用Unity内置方法进行终极清理
                    RemoveMissingComponents(tempInstance);
                    removed++; // 计数+1表示进行了强制清理
                }

                // 5. 保存清理后的预制体
                if (removed > 0)
                {
                    // 替换原预制体（兼容不同Unity版本）
                    if (PrefabUtility.SaveAsPrefabAssetAndConnect(
                        tempInstance, prefabPath,
                        InteractionMode.AutomatedAction))
                    {
                        totalRemoved += removed;
                        Debug.Log($"已清理：{prefabPath}，移除 {removed} 个Missing脚本");
                    }
                    else
                    {
                        failedPrefabs.Add(prefabPath);
                        Debug.LogError($"保存失败：{prefabPath}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"处理 {prefabPath} 时出错：{ex.Message}");
                failedPrefabs.Add(prefabPath);
            }
            finally
            {
                // 销毁临时实例
                if (tempInstance != null)
                    Object.DestroyImmediate(tempInstance);
            }

            // 更新进度
            float progress = (float)totalProcessed / prefabGuids.Length;
            EditorUtility.DisplayProgressBar("清理中",
                $"{totalProcessed}/{prefabGuids.Length} 个预制体", progress);
        }

        // 完成后操作
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        // 输出结果摘要
        Debug.Log($"=== 清理完成 ===");
        Debug.Log($"总处理预制体：{totalProcessed} 个");
        Debug.Log($"成功移除Missing脚本：{totalRemoved} 个");

        if (failedPrefabs.Count > 0)
        {
            Debug.LogWarning($"以下预制体处理失败（共 {failedPrefabs.Count} 个）：");
            foreach (var path in failedPrefabs)
                Debug.LogWarning($"- {path}");
        }
    }

    // 清理游戏对象及其所有子物体的Missing脚本
    static int CleanGameObjectAndChildren(GameObject go)
    {
        int count = 0;
        // 清理自身
        count += CleanSingleGameObject(go);
        // 递归清理子物体
        foreach (Transform child in go.transform)
            count += CleanGameObjectAndChildren(child.gameObject);
        return count;
    }

    // 清理单个游戏对象上的Missing脚本（核心方法）
    static int CleanSingleGameObject(GameObject go)
    {
        SerializedObject so = new SerializedObject(go);
        SerializedProperty components = so.FindProperty("m_Component");
        if (components == null) return 0;

        List<int> indicesToRemove = new List<int>();

        // 收集所有Missing组件索引
        for (int i = 0; i < components.arraySize; i++)
        {
            SerializedProperty prop = components.GetArrayElementAtIndex(i);
            if (prop.objectReferenceValue == null)
                indicesToRemove.Add(i);
        }

        // 倒序删除（避免索引偏移）
        if (indicesToRemove.Count > 0)
        {
            indicesToRemove.Reverse();
            foreach (int index in indicesToRemove)
                components.DeleteArrayElementAtIndex(index);

            so.ApplyModifiedProperties();
            return indicesToRemove.Count;
        }

        return 0;
    }

    // 检查是否还有Missing脚本（二次验证）
    static bool HasMissingScripts(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) return true;
        }

        // 检查子物体
        foreach (Transform child in go.transform)
        {
            if (HasMissingScripts(child.gameObject))
                return true;
        }

        return false;
    }

    // 使用Unity内置方法强制移除Missing组件（终极手段）
    static void RemoveMissingComponents(GameObject go)
    {
        // 调用Unity内部清理方法
        System.Type editorUtilityType = typeof(EditorUtility);
        var method = editorUtilityType.GetMethod(
            "ClearMissingComponents",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
        );

        if (method != null)
        {
            method.Invoke(null, new object[] { go });
        }
    }
}
