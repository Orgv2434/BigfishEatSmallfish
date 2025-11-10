using UnityEngine;
using DG.Tweening;

public class UIAppearEffect : MonoBehaviour
{
    private CanvasGroup cg;
    private RectTransform rt;
    private Vector2 originalPos;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        // 保存 UI 初始位置
        originalPos = rt.anchoredPosition;

        // 完全隐藏 & 禁用交互
        HideCompletely();
    }

    private void HideCompletely()
    {
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 恢复到初始位置（避免重播动画时位置跑掉）
        rt.anchoredPosition = originalPos;
    }

    public void ResetState()
    {
        HideCompletely();
    }

    // 播放动画（所有参数外部传入）
    public void Play(float dropDistance, float duration, Ease moveEase, Ease fadeEase)
    {
        // 开始动画前保持隐藏不可点击
        HideCompletely();

        // 设置从上方开始的初始位置
        rt.anchoredPosition = originalPos + new Vector2(0, dropDistance);

        // 渐显透明度
        cg.DOFade(1, duration)
            .SetEase(fadeEase);

        // 下落动画
        rt.DOAnchorPosY(originalPos.y, duration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                // 动画完成后恢复交互
                cg.interactable = true;
                cg.blocksRaycasts = true;
            });
    }
}
