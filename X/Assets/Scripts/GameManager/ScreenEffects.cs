using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenEffects : MonoBehaviour
{   public static ScreenEffects Instance { get; private set; } // 创建单例实例
    public Animator transitionAnimator;
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
    public IEnumerator LoadScene(int sceneIndex)
    {
        transitionAnimator.SetBool("Fadeout", true);
        transitionAnimator.SetBool("Fadein", false);
        yield return new WaitForSecondsRealtime(1f);
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneIndex);
        async.completed += OnLoadScene;
    }
    // 准备场景加载完成后的回调
    public void OnLoadScene(AsyncOperation obj)
    {
        transitionAnimator.SetBool("Fadeout", false);
        transitionAnimator.SetBool("Fadein", true);
        FishGameFlowManager.Instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    
}
