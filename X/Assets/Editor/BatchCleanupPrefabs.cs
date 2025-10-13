using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class RemoveMissingComponentsFromPrefabs : EditorWindow
{
    private string prefabPath = "Assets/Resources/Tropical Fish"; // 默认预制体路径
    private Vector2 scrollPosition;
    private List<string> processedPrefabs = new List<string>();
    private int missingComponentsRemoved = 0;

    [MenuItem("Tools/Remove Missing Components From Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<RemoveMissingComponentsFromPrefabs>("Remove Missing Components");
    }

    private void OnGUI()
    {
        GUILayout.Label("预制体失效组件清理工具", EditorStyles.boldLabel);

        // 路径输入
        GUILayout.Label("预制体路径:");
        prefabPath = EditorGUILayout.TextField(prefabPath);

        // 浏览按钮
        if (GUILayout.Button("浏览路径"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择预制体文件夹", prefabPath, "");
            if (!string.IsNullOrEmpty(selectedPath) && selectedPath.Contains(Application.dataPath))
            {
                prefabPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
        }

        EditorGUILayout.Space();

        // 执行按钮
        if (GUILayout.Button("清理失效组件", GUILayout.Height(30)))
        {
            if (Directory.Exists(prefabPath))
            {
                processedPrefabs.Clear();
                missingComponentsRemoved = 0;
                ProcessPrefabsInFolder(prefabPath);
                
                EditorUtility.DisplayDialog(
                    "完成", 
                    $"处理完成!\n共检查 {processedPrefabs.Count} 个预制体\n移除了 {missingComponentsRemoved} 个失效组件", 
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "指定的路径不存在!", "确定");
            }
        }

        EditorGUILayout.Space();
        GUILayout.Label("处理结果:", EditorStyles.boldLabel);

        // 显示处理结果
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        foreach (var prefab in processedPrefabs)
        {
            GUILayout.Label(prefab);
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 处理文件夹中的所有预制体
    /// </summary>
    private void ProcessPrefabsInFolder(string folderPath)
    {
        // 获取所有预制体文件
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            ProcessPrefab(prefabPath);
        }
    }

    /// <summary>
    /// 处理单个预制体，移除其中的失效组件
    /// </summary>
    private void ProcessPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        // 实例化预制体进行修改
        GameObject tempInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (tempInstance == null) return;

        bool hasMissingComponents = false;

        // 获取所有组件并检查失效组件
        Component[] components = tempInstance.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
            {
                hasMissingComponents = true;
                break;
            }
        }

        // 如果有失效组件，则进行清理
        if (hasMissingComponents)
        {
            // 记录移除的组件数量
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tempInstance);
            if (removedCount > 0)
            {
                missingComponentsRemoved += removedCount;
                
                // 保存修改到预制体
                PrefabUtility.SaveAsPrefabAsset(tempInstance, prefabPath);
                processedPrefabs.Add($"{prefabPath} - 移除了 {removedCount} 个失效组件");
            }
            else
            {
                processedPrefabs.Add($"{prefabPath} - 未发现失效组件");
            }
        }
        else
        {
            processedPrefabs.Add($"{prefabPath} - 未发现失效组件");
        }

        // 销毁临时实例
        DestroyImmediate(tempInstance);
        
        // 刷新资源数据库
        AssetDatabase.Refresh();
    }
}
