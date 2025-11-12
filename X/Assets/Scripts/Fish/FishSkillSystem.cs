using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;


public class SkillIconData
{
    public FishSkillType skillType;
    public Image iconImage;
    public Animator iconAnimator;
    public bool isPermanent;
    public Coroutine expireCoroutine;
}

public class FishSkillSystem : MonoBehaviour
{
    private PlayerEffect playerEffect;
    private PlayerFishData _playerData;
    private ThirdPersonMove _playerMove;

    [Header("技能图标配置")]
    public Transform skillIconContainer;
    public GameObject skillIconPrefab;
    public Sprite expMultiplierIcon;
    public Sprite dashIcon;
    public Sprite shieldIcon;
    public Sprite healIcon;

    [Header("技能配置")]

    public float healAmount = 50f;
    private List<SkillIconData> activeSkills = new List<SkillIconData>();
    private int maxSkillCount = 4;

    private const string ANIM_SHOW = "Show";
    private const string ANIM_HIDE = "Hide";


    private void Awake()
    {
        playerEffect = GetComponent<PlayerEffect>();
        _playerData = GetComponent<PlayerFishData>();
        _playerMove = GetComponent<ThirdPersonMove>();

        if (_playerData == null)
        {
            Debug.LogError("缺少 PlayerFishData 组件！");
            enabled = false;
        }
    }


    // 外部调用：玩家吃掉技能鱼
    public void EatSkillFish(SkillFishData fish)
    {
        _playerData.GainExp(fish.baseExpValue);
        ActivateSkill(fish.skillType);
    }

    // 修改后的 ClearAllSkillIcons 方法（仅改遍历部分）
    public void ClearAllSkillIcons()
    {
        // 1. 处理列表中的技能图标：停止计时+隐藏动画+强制销毁
        if (activeSkills.Count > 0)
        {
            foreach (var skillData in activeSkills)
            {
                if (skillData == null) continue;

                if (skillData.expireCoroutine != null)
                {
                    StopCoroutine(skillData.expireCoroutine);
                    skillData.expireCoroutine = null;
                }

                // 关键修改：传 forceDestroy = true，强制销毁图标
                HideSkillIcon(skillData, forceDestroy: true);
            }
            activeSkills.Clear();
        }

        // 2. 销毁容器下所有子物体（兜底：确保漏网之鱼被销毁）
        DestroyAllSkillContainerChildren();

        Debug.Log("玩家死亡，已清除所有技能图标及容器子物体");
    }

   // 优化后的 DestroyAllSkillContainerChildren 方法
private void DestroyAllSkillContainerChildren()
{
    Transform container = skillIconContainer;
    if (container == null)
    {
        GameObject containerObj = GameObject.Find("SkillIcons"); // 注意名称拼写
        if (containerObj != null)
        {
            container = containerObj.transform;
        }
        else
        {
            Debug.LogWarning("未找到 SkillIcons 容器，无需销毁子物体");
            return;
        }
    }

    // 反向遍历：强制销毁所有子物体（不管是否激活）
    for (int i = container.childCount - 1; i >= 0; i--)
    {
        Transform child = container.GetChild(i);
        if (child == null) continue;

        // 停止所有动画（包括隐藏动画）
        DOTween.Kill(child.gameObject);
        DOTween.Kill(child.GetComponent<Image>());
        DOTween.Kill(child.GetComponent<RectTransform>());

        // 强制销毁：不管是否隐藏、是否永久
        Destroy(child.gameObject);
    }
}

    private void ActivateSkill(FishSkillType skill)
    {
        switch (skill)
        {
            case FishSkillType.ExpMultiplier:
                _playerData.SetTemporaryExpMultiplier(1.5f, 10f);
                ShowSkillIcon(skill, expMultiplierIcon, isPermanent: false, duration: 10f);
                Debug.Log("经验倍率生效！10秒内经验×1.5");
                break;

            case FishSkillType.Dash:
                if (!IsSkillActive(FishSkillType.Dash))
                {
                    _playerMove.haveDush = true;
                    _playerMove.InitDushSlider();
                    ShowSkillIcon(skill, dashIcon, isPermanent: true);
                    Debug.Log("学会冲刺技能！");
                }
                break;

            case FishSkillType.Shield:
                _playerData.AddShield();
                ShowSkillIcon(skill, shieldIcon, isPermanent: false, duration: 10f);
                StartCoroutine(ExpireShieldEffect(10f));
                break;

            case FishSkillType.Heal:
                 healAmount = 50f;
                _playerData.GainHealth(healAmount);
                ShowSkillIcon(skill, healIcon, isPermanent: false, duration: 5f);
                Debug.Log($"获得加血效果！恢复{healAmount}点生命值");
                break;

            default:
                Debug.Log("无技能效果");
                break;
        }
    }


    #region 技能图标显示/隐藏逻辑
    private void ShowSkillIcon(FishSkillType skillType, Sprite iconSprite, bool isPermanent, float duration = 0)
    {
        int slotIndex = FindEmptySkillSlot();
        if (slotIndex >= maxSkillCount)
        {
            slotIndex = maxSkillCount - 1;
            HideSkillIcon(activeSkills[slotIndex]);
        }

        GameObject iconObj;
        SkillIconData iconData;

        if (slotIndex < activeSkills.Count && activeSkills[slotIndex] != null)
        {
            iconData = activeSkills[slotIndex];
            iconObj = iconData.iconImage.gameObject;
        }
        else
        {
            if (skillIconContainer == null)
            {
                skillIconContainer = GameObject.Find("SkillIcons").transform;
            }
        
            iconObj = Instantiate(skillIconPrefab, skillIconContainer);
            iconData = new SkillIconData
            {
                iconImage = iconObj.GetComponent<Image>(),
                iconAnimator = iconObj.GetComponent<Animator>(),
            };
            if (slotIndex < activeSkills.Count)
                activeSkills[slotIndex] = iconData;
            else
                activeSkills.Add(iconData);
        }

        iconData.skillType = skillType;
        iconData.isPermanent = isPermanent;
        iconData.iconImage.sprite = iconSprite;
        iconData.iconImage.enabled = true;
        iconObj.SetActive(true);

        PlayShowAnimation(iconData);

        if (!isPermanent)
        {
            if (iconData.expireCoroutine != null)
                StopCoroutine(iconData.expireCoroutine);
            iconData.expireCoroutine = StartCoroutine(ExpireSkillIcon(iconData, duration));
        }
        else
        {
            if (iconData.expireCoroutine != null)
            {
                StopCoroutine(iconData.expireCoroutine);
                iconData.expireCoroutine = null;
            }
        }
    }

    // 修改原有 HideSkillIcon 方法（增加 forceDestroy 参数）
    private void HideSkillIcon(SkillIconData iconData, bool forceDestroy = false)
    {
        if (iconData == null || iconData.iconImage == null) return;

        GameObject iconObj = iconData.iconImage.gameObject;
        if (!iconObj.activeInHierarchy && !forceDestroy)
            return;

        // 停止现有动画，避免异步冲突
        DOTween.Kill(iconData.iconImage.rectTransform);
        DOTween.Kill(iconData.iconImage);

        PlayHideAnimation(iconData, () =>
        {
            iconObj.SetActive(false);
            iconData.iconImage.sprite = null;

            // 清理时强制销毁所有图标（不管是否永久）
            if (forceDestroy)
            {
                Destroy(iconObj); // 直接销毁，从场景中移除
            }

            if (iconData.expireCoroutine != null)
            {
                StopCoroutine(iconData.expireCoroutine);
                iconData.expireCoroutine = null;
            }
        });
    }

    private int FindEmptySkillSlot()
    {
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i] == null || !activeSkills[i].iconImage.gameObject.activeInHierarchy)
                return i;
        }
        return activeSkills.Count;
    }

    private bool IsSkillActive(FishSkillType skillType)
    {
        foreach (var skill in activeSkills)
        {
            if (skill != null && skill.skillType == skillType && skill.isPermanent)
                return true;
        }
        return false;
    }
    #endregion


    #region 技能计时与动画
    private IEnumerator ExpireSkillIcon(SkillIconData iconData, float duration)
    {
        yield return new WaitForSeconds(duration);
        HideSkillIcon(iconData);
    }

    private IEnumerator ExpireShieldEffect(float duration)
    {
        yield return new WaitForSeconds(duration);
        Debug.Log("护盾时效已过，后续需重新吞噬护盾鱼获得");
    }

    private void PlayShowAnimation(SkillIconData iconData)
    {
        iconData.iconImage.rectTransform.localScale = Vector3.zero;
        iconData.iconImage.color = new Color(1, 1, 1, 0);
        iconData.iconImage.rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        iconData.iconImage.DOFade(1, 0.3f);
    }

    private void PlayHideAnimation(SkillIconData iconData, Action onComplete)
    {
        iconData.iconImage.rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        iconData.iconImage.DOFade(0, 0.2f).OnComplete(() => onComplete?.Invoke());
    }
    #endregion
}