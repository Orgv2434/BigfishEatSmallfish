using UnityEngine;
using System.Collections;
using DG.Tweening;

[System.Serializable]
public class UISequenceElement
{
    [Header("UI 本体")]
    public UIAppearEffect ui;

    [Header("播放延迟")]
    public float delay = 0f;

    [Header("动画参数（独立）")]
    public float dropDistance = 200f;
    public float duration = 0.8f;

    public Ease moveEase = Ease.OutCubic;
    public Ease fadeEase = Ease.OutQuad;
}

public class UISequenceAnimator : MonoBehaviour
{
    [Header("测试功能")]
    public bool replayOnValidate = false;   // inspector 勾选用


    [Header("UI 动画序列（顺序播放）")]
    public UISequenceElement[] elements;



    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        foreach (var elem in elements)
        {
            if (elem.ui != null)
            {
                // 先等延迟
                yield return new WaitForSeconds(elem.delay);

                // 播放独立参数的动画
                elem.ui.Play(elem.dropDistance, elem.duration, elem.moveEase, elem.fadeEase);
            }
        }
    }

    private void OnValidate()
    {
        // 只有当你勾选 replayOnValidate 时才触发
        if (replayOnValidate)
        {
            replayOnValidate = false; // 自动关掉，防止重复触发
            ReplayInEditor();
        }
    }

    private void ReplayInEditor()
    {
        // 防止在 Prefab 编辑模式报错
        if (!Application.isPlaying)
            return;

        StopAllCoroutines();

        // 强制把 UI 位置与透明度重置
        foreach (var elem in elements)
        {
            if (elem.ui != null)
            {
                elem.ui.ResetState();   // 我们稍后会写这个
            }
        }

        // 从头播放动画序列
        StartCoroutine(PlaySequence());
    }


}
