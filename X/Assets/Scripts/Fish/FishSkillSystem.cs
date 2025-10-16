using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;



public class FishSkillSystem : MonoBehaviour
{
    private PlayerEffect playerEffect;
    private PlayerFishData _playerData;
    private ThirdPersonMove _playerMove;
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

    // 外部调用：玩家吃掉一条技能鱼
    public void EatSkillFish(SkillFishData fish)
    {
        // 1. 基础逻辑：获得经验
        _playerData.GainExp(fish.baseExpValue);

        // 2. 触发技能
        ActivateSkill(fish.skillType);
    }

    // 执行技能效果
    private void ActivateSkill(FishSkillType skill)
    {
      
        switch (skill)
        {
            case FishSkillType.ExpMultiplier:
                // 经验倍率鱼：短时间内经验 ×1.5，持续 10 秒
                _playerData.SetTemporaryExpMultiplier(1.5f, 10f);
                Debug.Log("经验倍率生效！10 秒内经验获取 ×1.5");
                break;

            case FishSkillType.Dash:
                // 冲刺鱼：习得主动技能（这里简化，假设你有输入触发逻辑）
                Debug.Log("学会冲刺技能！");
                _playerMove.haveDush = true;
                break;

            case FishSkillType.Shield:
                // 护盾鱼：自动开护盾，可挡 1 次攻击，10 秒后“可获得次数”失效
                _playerData.AddShield();
                StartCoroutine(ExpireShieldEffect(10f));
                break;

            case FishSkillType.Camouflage:
                // 伪装鱼：获得主动技能，临时提升外观挡位
                Debug.Log("学会伪装技能！按 C 键触发（10 秒后失效）");
                StartCoroutine(EnableCamouflageSkill(10f));
                break;

            default:
                Debug.Log("无技能效果");
                break;
        }
    }

    // 冲刺技能：5 秒后“可学习状态”失效（需结合输入系统扩展）
    private IEnumerator DisableDashAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        Debug.Log("冲刺技能时效已过，需重新吞噬冲刺鱼学习");
        // 可扩展：移除冲刺技能输入绑定
    }

    // 护盾技能：10 秒后“护盾效果”（次数保留，但新护盾不会自动加）
    private IEnumerator ExpireShieldEffect(float duration)
    {
        yield return new WaitForSeconds(duration);
        Debug.Log("护盾时效已过，后续需重新吞噬护盾鱼获得");
        // 可扩展：关闭“自动获得护盾”状态
    }

    // 伪装技能：开启后可主动触发（示例：按 C 键）
    private IEnumerator EnableCamouflageSkill(float duration)
    {
        bool skillTriggered = false;
        float skillTimer = duration;

        // 模拟“主动触发”逻辑
        while (skillTimer > 0)
        {
            if (Input.GetKeyDown(KeyCode.C) && !skillTriggered)
            {
                Debug.Log("挡位切换");
               

                // 临时提升外观挡位（实际挡位不变）
                playerEffect.ChangeHalo(_playerData.fishData.fishTier + 1);

                skillTriggered = true;
            }
            skillTimer -= Time.deltaTime;
            yield return null;
        }
        // 临时提升外观挡位（实际挡位不变）
        playerEffect.ChangeHalo(_playerData.fishData.fishTier);
        Debug.Log("伪装技能时效已过，需重新吞噬伪装鱼学习");
    }
}