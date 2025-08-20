using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerFishData : NetworkBehaviour
{
    public SkillFishData fishData;

    // 永久技能（生成时随机1个+吃冲刺鱼获得，可重复使用）
    public HashSet<FishSkillType> permanentSkills = new HashSet<FishSkillType>();
    // 一次性技能（吃技能鱼获得，仅1次使用机会，超时失效）
    public List<FishSkillType> oneTimeSkills = new List<FishSkillType>();

    [Header("基础属性")]
    public float maxHealth = 100f;
    public float currentHealth;
    public int currentExp = 0;
    public FishTier currentTier = FishTier.White;
    public float baseSize = 1f;
    public float currentSize;
    public float moveSpeed = 5f;

    [Header("生命设置")]
    public float healthLossRate = 1f;
    public float expToHealthRate = 0.2f;

    [Header("经验设置")]
    public float baseExpMultiplier = 0.5f;
    private float _currentExpMultiplier;

    [Header("升级设置")]
    public int[] expToNextTier = { 100, 300, 600, 1000 };

    [Header("体型设置")]
    public float sizeIncreasePerTier = 0.5f;

    // 事件通知
    public Action<float> OnHealthChanged;
    public Action<int, int> OnExpChanged;
    public Action<FishTier> OnTierChanged;
    public Action<float> OnSizeChanged;
    public Action<float> OnSpeedChanged;
    public Action<HashSet<FishSkillType>> OnPermanentSkillsUpdated;
    public Action<List<FishSkillType>> OnOneTimeSkillsUpdated;

    public int _shieldCount = 0;
    FishSkillSystem skillSystem;

    private void Start()
    {
        currentHealth = maxHealth;
        currentSize = baseSize;
        _currentExpMultiplier = baseExpMultiplier;
        skillSystem = GetComponent<FishSkillSystem>();

        // 生成时随机获得1个永久技能
        if (IsServer)
        {
            GrantRandomPermanentSkill();
        }
    }

    // 初始随机永久技能（不含冲刺）
    private void GrantRandomPermanentSkill()
    {
        FishSkillType[] permanentSkillPool = new FishSkillType[]
        {
            FishSkillType.Shield,
            FishSkillType.Camouflage,
            FishSkillType.ExpMultiplier
        };

        System.Random random = new System.Random();
        FishSkillType selectedSkill = permanentSkillPool[random.Next(permanentSkillPool.Length)];

        permanentSkills.Add(selectedSkill);
        OnPermanentSkillsUpdated?.Invoke(permanentSkills);
        Debug.Log($"初始获得永久技能：{selectedSkill}（按1键使用）");
    }

    // 添加一次性技能（并触发超时检查）
    public void AddOneTimeSkill(FishSkillType skill, float expireTime = 30f)
    {
        oneTimeSkills.Add(skill);
        OnOneTimeSkillsUpdated?.Invoke(oneTimeSkills);
        Debug.Log($"获得一次性技能：{skill}（按Q键使用，{expireTime}秒内有效）");

        // 启动超时协程（超时未使用则移除）
        StartCoroutine(RemoveOneTimeSkillAfterDelay(skill, expireTime));
    }

    // 移除一次性技能（使用后或超时）
    public void RemoveOneTimeSkill(FishSkillType skill)
    {
        if (oneTimeSkills.Contains(skill))
        {
            oneTimeSkills.Remove(skill);
            OnOneTimeSkillsUpdated?.Invoke(oneTimeSkills);
        }
    }

    // 一次性技能超时移除（添加失效日志）
    private IEnumerator RemoveOneTimeSkillAfterDelay(FishSkillType skill, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (oneTimeSkills.Contains(skill))
        {
            RemoveOneTimeSkill(skill);
            Debug.Log($"一次性技能【{skill}】已超时失效！"); // 超时失效日志
        }
    }

    private void Update()
    {
        currentHealth = Mathf.Max(0, currentHealth - healthLossRate * Time.deltaTime);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("玩家鱼死亡！");
        }
    }

    public void GainHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void GainExp(int baseExp)
    {
        int actualExp = Mathf.RoundToInt(baseExp * _currentExpMultiplier);
        currentExp += actualExp;
        OnExpChanged?.Invoke(currentExp, GetRequiredExpForNextTier());
        CheckTierUpgrade();

        float healthRecover = actualExp * expToHealthRate;
        GainHealth(healthRecover);
        Debug.Log($"获得经验：{actualExp}，回复血量：{healthRecover}");
    }

    private void CheckTierUpgrade()
    {
        int tierIndex = (int)currentTier;
        while (tierIndex < expToNextTier.Length && currentExp >= expToNextTier[tierIndex])
        {
            tierIndex++;
            currentTier = (FishTier)tierIndex;

            currentSize = baseSize + (int)currentTier * sizeIncreasePerTier;
            moveSpeed += 0.5f;
            maxHealth += 50;
            currentHealth = Mathf.Min(maxHealth, currentHealth + 50);

            OnSizeChanged?.Invoke(currentSize);
            OnSpeedChanged?.Invoke(moveSpeed);
            OnTierChanged?.Invoke(currentTier);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }
    }

    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        StartCoroutine(ResetExpMultiplier(multiplier, duration));
    }

    private IEnumerator ResetExpMultiplier(float tempMulti, float duration)
    {
        _currentExpMultiplier = tempMulti;
        yield return new WaitForSeconds(duration);
        _currentExpMultiplier = baseExpMultiplier;
        Debug.Log("经验倍率效果已结束！"); // 经验倍率失效日志
    }

    public void SetTemporaryVisualTier(FishTier visualTier, float duration)
    {
        StartCoroutine(ResetVisualTier(visualTier, duration));
    }

    private IEnumerator ResetVisualTier(FishTier visualTier, float duration)
    {
        yield return new WaitForSeconds(duration);
        // 此处由伪装技能的协程单独处理失效日志
    }

    public void AddShield(int count = 1)
    {
        _shieldCount += count;
    }

    public bool UseShield()
    {
        if (_shieldCount > 0)
        {
            _shieldCount--;
            return true;
        }
        return false;
    }

    public int GetRequiredExpForNextTier()
    {
        int tierIndex = (int)currentTier;
        return tierIndex < expToNextTier.Length ? expToNextTier[tierIndex] : 0;
    }

    public void HandleFishCollision(FishTierEffect otherFish)
    {
        FishTier otherFishTier = otherFish.fishData.fishTier;
        int otherFishExpValue = otherFish.fishData.baseExpValue;
        string otherFishTag = otherFish.gameObject.tag;
        if (otherFishTier > currentTier)
        {
            if (!UseShield())
            {
                currentHealth = 0;
                Debug.Log("玩家鱼被更大挡位的鱼吃掉，死亡！");
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("玩家鱼使用护盾抵挡了更大挡位鱼的攻击！");
            }
        }
        else if (otherFishTier < currentTier)
        {
            Debug.Log("玩家鱼吃掉更小挡位的鱼，获得经验！");
            Destroy(otherFish.gameObject);
            skillSystem.EatSkillFish(otherFish.fishData);
        }
        else
        {
            if (otherFishTag == "tail")
            {
                Debug.Log("玩家鱼吃掉同挡位鱼的尾部，获得经验！");
                Destroy(otherFish.gameObject);
                skillSystem.EatSkillFish(otherFish.fishData);
            }
            else if (otherFishTag == "head")
            {
                Debug.Log("玩家鱼碰到同挡位鱼的头部，无视该碰撞！");
            }
            else
            {
                Debug.Log("吃到");
                Destroy(otherFish.gameObject);
                skillSystem.EatSkillFish(otherFish.fishData);
            }
        }
    }
}