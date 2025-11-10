using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class PauseUIImageExpand : MonoBehaviour
{
    [Header("展开目标 Image")]
    public RectTransform targetImage;      // PauseUI 的子 Image
    private Vector2 originalSize;

    [Header("展开动画")]
    public float expandDuration = 0.5f;
    public Ease expandEase = Ease.OutCubic;

    [Header("子元素淡入")]
    public CanvasGroup[] childCanvasGroups; // Image 下的按钮/文字
    public float childFadeDuration = 0.3f;
    public float childFadeDelay = 0.1f;
    public Ease fadeEase = Ease.Linear;

    private void Awake()
    {
        if (targetImage == null)
        {
            Debug.LogError("请赋值 targetImage");
            return;
        }

        originalSize = targetImage.sizeDelta;

        // 初始化 Image 高度为 0
        targetImage.sizeDelta = new Vector2(originalSize.x, 0);

        // 初始化内部子元素隐藏
        foreach (var cg in childCanvasGroups)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public void Play()
    {
        // 展开 Image
        targetImage.DOSizeDelta(originalSize, expandDuration)
            .SetEase(expandEase)
            .OnComplete(() =>
            {
                // 展开完成后播放子元素淡入
                PlayChildrenFade();
            });
    }

    private void PlayChildrenFade()
    {
        for (int i = 0; i < childCanvasGroups.Length; i++)
        {
            CanvasGroup cg = childCanvasGroups[i];
            cg.DOFade(1f, childFadeDuration)
                .SetDelay(i * childFadeDelay)
                .SetEase(fadeEase)
                .OnStart(() =>
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                });
        }
    }

    // 可选：重置状态
    public void ResetState()
    {
        targetImage.sizeDelta = new Vector2(originalSize.x, 0);
        foreach (var cg in childCanvasGroups)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }
}
