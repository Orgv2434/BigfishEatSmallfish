using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerFishData : NetworkBehaviour
{
    public SkillFishData fishData;

    // 永久技能
    public HashSet<FishSkillType> permanentSkills = new HashSet<FishSkillType>();
    // 一次性技能
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

    // 事件通知，用于外部监听属性变化
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

        // 服务器生成初始技能（若需要的话，也可根据游戏逻辑调整生成时机）
        if (IsServer)
        {
            GrantRandomPermanentSkill();
        }
    }

    // 初始随机永久技能，给玩家鱼初始技能
    private void GrantRandomPermanentSkill()
    {
        FishSkillType[] pool = { FishSkillType.Shield, FishSkillType.Camouflage, FishSkillType.ExpMultiplier };
        System.Random random = new System.Random();
        FishSkillType skill = pool[random.Next(pool.Length)];
        permanentSkills.Add(skill);
        OnPermanentSkillsUpdated?.Invoke(permanentSkills);
        Debug.Log($"玩家鱼获得初始永久技能：{skill}");
    }

    // 添加一次性技能，外部可调用该方法给鱼添加技能
    public void AddOneTimeSkill(FishSkillType skill, float expireTime = 30f)
    {
        oneTimeSkills.Add(skill);
        OnOneTimeSkillsUpdated?.Invoke(oneTimeSkills);
        StartCoroutine(RemoveOneTimeSkillAfterDelay(skill, expireTime));
        Debug.Log($"玩家鱼获得一次性技能：{skill}，将在 {expireTime} 秒后移除");
    }

    // 移除一次性技能，可内部或外部调用
    public void RemoveOneTimeSkill(FishSkillType skill)
    {
        if (oneTimeSkills.Contains(skill))
        {
            oneTimeSkills.Remove(skill);
            OnOneTimeSkillsUpdated?.Invoke(oneTimeSkills);
            Debug.Log($"玩家鱼移除一次性技能：{skill}");
        }
    }

    // 协程，用于延迟移除一次性技能
    private IEnumerator RemoveOneTimeSkillAfterDelay(FishSkillType skill, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveOneTimeSkill(skill);
    }

    private void Update()
    {
        // 处理生命值自然衰减
        currentHealth = Mathf.Max(0, currentHealth - healthLossRate * Time.deltaTime);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // 处理死亡逻辑
        if (currentHealth <= 0)
        {
            Debug.Log("玩家鱼死亡！");
            // 可在此处添加更多死亡后处理逻辑，比如播放死亡动画、触发游戏结束逻辑等
            Destroy(this.gameObject);
        }
    }

    // 增加生命值的方法，外部可调用给鱼加血
    public void GainHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        Debug.Log($"玩家鱼获得生命值：{amount}，当前生命值：{currentHealth}");
    }

    // 增加经验的方法，外部（比如吃掉其他鱼）调用给鱼加经验
    public void GainExp(int baseExp)
    {
        int actualExp = Mathf.RoundToInt(baseExp * _currentExpMultiplier);
        currentExp += actualExp;
        OnExpChanged?.Invoke(currentExp, GetRequiredExpForNextTier());
        CheckTierUpgrade();

        float healthRecover = actualExp * expToHealthRate;
        GainHealth(healthRecover);
        Debug.Log($"玩家鱼获得经验：{actualExp}，当前经验：{currentExp}");
    }

    // 检查是否满足升级条件，满足则升级
    private void CheckTierUpgrade()
    {
        int tierIndex = (int)currentTier;
        while (tierIndex < expToNextTier.Length && currentExp >= expToNextTier[tierIndex])
        {
            tierIndex++;
            currentTier = (FishTier)tierIndex;

            // 升级后改变体型、移动速度、最大生命值等
            currentSize = baseSize + (int)currentTier * sizeIncreasePerTier;
            moveSpeed += 0.5f;
            maxHealth += 50;
            currentHealth = Mathf.Min(maxHealth, currentHealth + 50);

            // 触发事件通知外部更新UI等
            OnSizeChanged?.Invoke(currentSize);
            OnSpeedChanged?.Invoke(moveSpeed);
            OnTierChanged?.Invoke(currentTier);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);

            Debug.Log($"玩家鱼升级到 {currentTier} 级");
        }
    }

    // 设置临时经验倍数，比如吃了有经验加成的鱼或技能触发
    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        StartCoroutine(ResetExpMultiplier(multiplier, duration));
        Debug.Log($"玩家鱼获得临时经验倍数：{multiplier}，持续 {duration} 秒");
    }

    private IEnumerator ResetExpMultiplier(float tempMulti, float duration)
    {
        _currentExpMultiplier = tempMulti;
        yield return new WaitForSeconds(duration);
        _currentExpMultiplier = baseExpMultiplier;
        Debug.Log($"玩家鱼临时经验倍数结束，恢复为基础倍数：{baseExpMultiplier}");
    }

    // （可选）设置临时视觉挡位，用于表现临时变身等效果，这里简单示例
    public void SetTemporaryVisualTier(FishTier visualTier, float duration)
    {
        StartCoroutine(ResetVisualTier(visualTier, duration));
        Debug.Log($"玩家鱼变为临时挡位 {visualTier}，持续 {duration} 秒");
    }

    private IEnumerator ResetVisualTier(FishTier visualTier, float duration)
    {
        // 这里可添加改变外观等逻辑，比如更换模型、材质等
        yield return new WaitForSeconds(duration);
        // 恢复外观逻辑...
        Debug.Log($"玩家鱼临时挡位 {visualTier} 效果结束，恢复原外观");
    }

    // 添加护盾数量，比如吃了带护盾技能的鱼
    public void AddShield(int count = 1)
    {
        _shieldCount += count;
        Debug.Log($"玩家鱼获得护盾，当前护盾数量：{_shieldCount}");
    }

    // 使用护盾，返回是否成功使用
    public bool UseShield()
    {
        if (_shieldCount > 0)
        {
            _shieldCount--;
            Debug.Log($"玩家鱼使用护盾，剩余护盾数量：{_shieldCount}");
            return true;
        }
        Debug.Log("玩家鱼没有护盾可用");
        return false;
    }

    // 获取升级到下一级所需经验
    public int GetRequiredExpForNextTier()
    {
        int tierIndex = (int)currentTier;
        return tierIndex < expToNextTier.Length ? expToNextTier[tierIndex] : 0;
    }

    // 处理与其他鱼的碰撞逻辑，外部（比如碰撞检测脚本）调用
    public void HandleFishCollision(FishTierEffect otherFish)
    {
        if (otherFish == null || otherFish.fishData == null) return;

        FishTier otherTier = otherFish.fishData.fishTier;
        int otherExp = otherFish.fishData.baseExpValue;
        string otherTag = otherFish.gameObject.tag;

        if (otherTier > currentTier)
        {
            if (!UseShield())
            {
                currentHealth = 0;
                Debug.Log("被更大挡位的鱼吃掉！");
                Destroy(this.gameObject);
            }
            else
            {
                Debug.Log("使用护盾抵挡攻击！");
            }
        }
        else if (otherTier < currentTier)
        {
            Debug.Log("吃掉更小挡位的鱼！");
            skillSystem?.EatSkillFish(otherFish.fishData);
            // 通知服务器销毁被吃掉的鱼（假设你有 GlobalFlock 等管理鱼群的类）
            GlobalFlock flockManager = FindObjectOfType<GlobalFlock>();
            flockManager.NotifyServerToDestroyFishServerRpc(otherFish.fishData.id);
        }
        else
        {
            if (otherTag == "tail")
            {
                Debug.Log("吃掉同挡位鱼的尾部！");
                skillSystem?.EatSkillFish(otherFish.fishData);
                GlobalFlock flockManager = FindObjectOfType<GlobalFlock>();
                flockManager.NotifyServerToDestroyFishServerRpc(otherFish.fishData.id);
            }
            else if (otherTag == "head")
            {
                Debug.Log("碰到同挡位鱼的头部，无视碰撞！");
            }
            else
            {
                skillSystem?.EatSkillFish(otherFish.fishData);
                GlobalFlock flockManager = FindObjectOfType<GlobalFlock>();
                flockManager.NotifyServerToDestroyFishServerRpc(otherFish.fishData.id);
            }
        }
    }


 
}
