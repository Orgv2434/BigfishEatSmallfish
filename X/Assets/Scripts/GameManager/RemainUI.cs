using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemainUI : MonoBehaviour
{     
    public static RemainUI Instance { get; private set; } // 创建单例实例
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保在场景切换时不被销毁
        }
        else
        {
            Destroy(gameObject); // 如果已有实例，销毁重复的实例
        }
    }
}
