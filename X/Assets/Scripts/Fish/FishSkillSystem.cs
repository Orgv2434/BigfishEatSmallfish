using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

public class FishSkillSystem : MonoBehaviour
{
    private PlayerEffect playerEffect;
    private PlayerFishData _playerData;
    private ThirdPersonMove _playerMove;

    // 修改：事件携带使用的技能类型
    public event Action<FishSkillType> OnOneTimeSkillUsed;

    // 永久技能冷却管理
    private Dictionary<FishSkillType, float> permanentSkillCooldowns = new Dictionary<FishSkillType, float>();
    // 永久技能基础冷却时间（可配置）
    private Dictionary<FishSkillType, float> baseCooldowns = new Dictionary<FishSkillType, float>()
    {
        { FishSkillType.Dash, 5f },
        { FishSkillType.Camouflage, 30f },
        { FishSkillType.Shield, 20f },
        { FishSkillType.ExpMultiplier, 60f }
    };

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

    private void Start()
    {
        foreach (var skill in baseCooldowns.Keys)
        {
            permanentSkillCooldowns[skill] = 0f;
        }

        _playerData.OnPermanentSkillsUpdated += OnPermanentSkillsUpdated;
        _playerData.OnOneTimeSkillsUpdated += OnOneTimeSkillsUpdated;
    }

    private void OnPermanentSkillsUpdated(HashSet<FishSkillType> skills)
    {
        Debug.Log("当前永久技能：" + string.Join("、", skills) + "（按1键使用）");
    }

    private void OnOneTimeSkillsUpdated(List<FishSkillType> skills)
    {
        if (skills.Count > 0)
        {
            Debug.Log("当前一次性技能：" + string.Join("、", skills) + "（按Q键使用）");
        }
    }

    public void EatSkillFish(SkillFishData fish)
    {
        _playerData.GainExp(fish.baseExpValue);

        if (fish.skillType == FishSkillType.Dash)
        {
            if (!_playerData.permanentSkills.Contains(FishSkillType.Dash))
            {
                _playerData.permanentSkills.Add(FishSkillType.Dash);
                _playerMove.haveDush = true;
                _playerData.OnPermanentSkillsUpdated?.Invoke(_playerData.permanentSkills);
                Debug.Log("获得永久技能【冲刺】");
            }
            else
            {
                ReduceCooldown(FishSkillType.Dash, 2f);
                Debug.Log("冲刺技能冷却减少2秒！");
            }
        }
        else
        {
            _playerData.AddOneTimeSkill(fish.skillType, 30f);
        }
    }

    private void UsePermanentSkill()
    {
        if (_playerData.permanentSkills.Count == 0) return;

        var skill = new List<FishSkillType>(_playerData.permanentSkills)[0];
        ActivatePermanentSkill(skill);
    }

    // 修改：触发事件时传递使用的技能类型
    private void UseOneTimeSkill()
    {
        if (_playerData.oneTimeSkills.Count == 0)
        {
            Debug.Log("没有可用的一次性技能！");
            return;
        }

        var skill = _playerData.oneTimeSkills[0];
        ActivateOneTimeSkill(skill);
        _playerData.RemoveOneTimeSkill(skill);
        Debug.Log($"一次性技能【{skill}】已消耗！");

        // 传递使用的技能类型
        OnOneTimeSkillUsed?.Invoke(skill);
    }

    private void ActivatePermanentSkill(FishSkillType skill)
    {
        if (!_playerData.permanentSkills.Contains(skill))
        {
            Debug.Log($"未拥有永久技能：{skill}");
            return;
        }

        if (permanentSkillCooldowns[skill] > 0)
        {
            Debug.Log($"{skill} 冷却中：{permanentSkillCooldowns[skill]:F1}秒");
            return;
        }

        switch (skill)
        {
            case FishSkillType.ExpMultiplier:
                _playerData.SetTemporaryExpMultiplier(2f, 20f);
                Debug.Log("[永久] 经验倍率生效！20秒内×2");
                permanentSkillCooldowns[skill] = baseCooldowns[skill];
                break;
            case FishSkillType.Dash:
                _playerMove.HandleSprintInput();
                Debug.Log("[永久] 冲刺！");
                permanentSkillCooldowns[skill] = baseCooldowns[skill];
                break;
            case FishSkillType.Shield:
                _playerData.AddShield(1);
                Debug.Log("[永久] 获得1个护盾！");
                permanentSkillCooldowns[skill] = baseCooldowns[skill];
                break;
            case FishSkillType.Camouflage:
                StartCoroutine(PermanentCamouflage(15f));
                permanentSkillCooldowns[skill] = baseCooldowns[skill];
                break;
        }
    }

    private void ActivateOneTimeSkill(FishSkillType skill)
    {
        switch (skill)
        {
            case FishSkillType.ExpMultiplier:
                _playerData.SetTemporaryExpMultiplier(1.5f, 10f);
                Debug.Log("[一次性] 经验倍率生效！10秒内×1.5");
                break;
            case FishSkillType.Shield:
                _playerData.AddShield(1);
                Debug.Log("[一次性] 获得1个护盾！");
                break;
            case FishSkillType.Camouflage:
                StartCoroutine(OneTimeCamouflage(8f));
                break;
        }
    }

    private IEnumerator PermanentCamouflage(float duration)
    {
        FishTier originalTier = _playerData.currentTier;
        playerEffect.ChangeHalo(originalTier + 1);
        Debug.Log("[永久] 伪装生效！15秒内外观提升");

        yield return new WaitForSeconds(duration);

        playerEffect.ChangeHalo(originalTier);
        Debug.Log("[永久] 伪装效果已结束！");
    }

    private IEnumerator OneTimeCamouflage(float duration)
    {
        FishTier originalTier = _playerData.currentTier;
        playerEffect.ChangeHalo(originalTier + 1);
        Debug.Log("[一次性] 伪装生效！8秒内外观提升");

        yield return new WaitForSeconds(duration);

        playerEffect.ChangeHalo(originalTier);
        Debug.Log("[一次性] 伪装效果已结束！");
    }

    private void ReduceCooldown(FishSkillType skill, float amount)
    {
        if (permanentSkillCooldowns.ContainsKey(skill))
        {
            permanentSkillCooldowns[skill] = Mathf.Max(0, permanentSkillCooldowns[skill] - amount);
        }
    }

    private void Update()
    {
        UpdatePermanentSkillCooldowns();
        HandleSkillInput();
    }

    private void UpdatePermanentSkillCooldowns()
    {
        foreach (var skill in baseCooldowns.Keys)
        {
            if (permanentSkillCooldowns[skill] > 0)
            {
                permanentSkillCooldowns[skill] -= Time.deltaTime;
            }
            else
            {
                permanentSkillCooldowns[skill] = 0;
            }
        }
    }

    private void HandleSkillInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UsePermanentSkill();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseOneTimeSkill();
        }
    }
}