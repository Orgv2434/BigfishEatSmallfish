using UnityEngine;
using System;
using System.Collections;

public class PlayerFishData : MonoBehaviour
{
    // 鱼的技能数据（在Inspector拖拽配置）
    public SkillFishData fishData;
    [Header("基础属性")]
    public float maxHealth = 100f;
    public float currentHealth;
    public int currentExp = 0;
    public FishTier currentTier = FishTier.White;
    public float baseSize = 1f;
    public float currentSize;
    public float moveSpeed = 5f;
    

    [Header("生命设置")]
    public float healthLossRate = 1f; // 每秒扣血量
    [Tooltip("经验转血量的倍率（每点经验回复的血量）")]
    public float expToHealthRate = 0.2f; // 新增：经验回血倍率

    [Header("经验设置")]
    public float baseExpMultiplier = 0.5f; // 基础经验倍率
    private float _currentExpMultiplier;

[Header("升级设置")]
public int[] expToNextTier = { 100, 300, 600, 1000 }; // 各挡位升级所需经验

    [Header("体型设置")]
    public float sizeIncreasePerTier = 0.5f;
    [Tooltip("每1点经验增加的体型系数（控制增长幅度）")]
public float sizeIncreasePerExp = 0.001f; // 每点经验增加的体型（可调整数值控制增长速度）
// 新增：引用移动控制脚本（在Inspector拖拽赋值）
public ThirdPersonMove thirdPersonMove;

[Header("速度衰减设置")]
[Tooltip("每1点经验减少的移动速度系数")]
public float moveSpeedDecreasePerExp = 0.002f; // 每点经验减少的移动速度
[Tooltip("鱼的最小移动速度（防止速度过低）")]
public float minMoveSpeed = 2f; // 移动速度下限
[Tooltip("每1点经验减少的转向速度系数")]
public float rotateSpeedDecreasePerExp = 0.01f; // 每点经验减少的转向速度
[Tooltip("鱼的最小转向速度")]
public float minRotateSpeed = 1f; // 转向速度下限
[Tooltip("初始转向速度（与ThirdPersonMove的初始rotateSpeed一致）")]
public float baseRotateSpeed = 90f; // 初始转向速度（需和ThirdPersonMove配置匹配）
private float _currentRotateSpeed; // 当前实际转向速度
    // 事件通知
    public Action<float> OnHealthChanged;
    public Action<int, int> OnExpChanged;
    public Action<FishTier> OnTierChanged;
    public Action<float> OnSizeChanged;
    public Action<float> OnSpeedChanged;

    // 护盾状态
    public int _shieldCount = 0;
    FishSkillSystem skillSystem;
    
    private void Start()
    {
        currentHealth = maxHealth;
        currentSize = baseSize;
        _currentExpMultiplier = baseExpMultiplier;
        skillSystem = GetComponent<FishSkillSystem>();
            // 新增：初始化转向速度，绑定ThirdPersonMove初始速度
    _currentRotateSpeed = baseRotateSpeed;
    if (thirdPersonMove != null)
    {
        // 初始化ThirdPersonMove的速度（确保初始状态一致）
        thirdPersonMove.UpdateSpeedStats(moveSpeed, _currentRotateSpeed);
    }
    else
    {
        Debug.LogWarning("未赋值ThirdPersonMove脚本！速度更新将失效");
    }
    }

    private void Update()
    {
        // 随时间扣血
        currentHealth = Mathf.Max(0, currentHealth - healthLossRate * Time.deltaTime);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // 死亡检测
        if (currentHealth <= 0)
        {
            Debug.Log("玩家鱼死亡！");
            // 可扩展死亡逻辑，比如触发游戏结束事件等
        }
    }

    // 加血
    public void GainHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    // 加经验（新增：同时计算回血）
    public void GainExp(int baseExp)
    {
        int actualExp = Mathf.RoundToInt(baseExp * _currentExpMultiplier);
        currentExp += actualExp;
        OnExpChanged?.Invoke(currentExp, GetRequiredExpForNextTier());
        CheckTierUpgrade();

        // 新增：根据获得的实际经验值回复血量
        float healthRecover = actualExp * expToHealthRate;
        GainHealth(healthRecover);
        Debug.Log($"获得经验：{actualExp}，回复血量：{healthRecover}");
        
    // 新增：每点经验实时修改体型、移动速度、转向速度
    UpdateStatsByExp(actualExp);
    }
/// <summary>
/// 根据新增经验实时更新体型、速度属性
/// </summary>
/// <param name="addedExp">本次新增的经验值</param>
private void UpdateStatsByExp(int addedExp)
{
    // 2. 移动速度衰减：基于当前总经验计算（累计衰减）
    float totalMoveSpeedDecrease = currentExp * moveSpeedDecreasePerExp;
    float newMoveSpeed = moveSpeed - totalMoveSpeedDecrease;
    newMoveSpeed = Mathf.Max(newMoveSpeed, minMoveSpeed); // 限制最小值

    // 3. 转向速度衰减：基于当前总经验计算（累计衰减）
    float totalRotateSpeedDecrease = currentExp * rotateSpeedDecreasePerExp;
    _currentRotateSpeed = baseRotateSpeed - totalRotateSpeedDecrease;
    _currentRotateSpeed = Mathf.Max(_currentRotateSpeed, minRotateSpeed); // 限制最小值

    // 4. 调用ThirdPersonMove的速度更新方法（同步速度变化）
    if (thirdPersonMove != null)
    {
        thirdPersonMove.UpdateSpeedStats(newMoveSpeed, _currentRotateSpeed);
    }

    // 触发速度变化事件（原有逻辑保留）
    OnSpeedChanged?.Invoke(newMoveSpeed);

}
    // 检查升级
private void CheckTierUpgrade()
{
    int tierIndex = (int)currentTier;
    while (tierIndex < expToNextTier.Length && currentExp >= expToNextTier[tierIndex])
    {
        tierIndex++;
        currentTier = (FishTier)tierIndex;

        // 新增：触发FishTierEffect的升级特效更新（获取当前物体上的FishTierEffect组件）
        FishTierEffect fishTierEffect = GetComponent<FishTierEffect>();
        if (fishTierEffect != null)
        {
            fishTierEffect.ShowTierHalo(currentTier); // 调用特效类的更新光环方法
            fishTierEffect.AutoFitModelSize(); // 重新适配光环大小（因体型变化）
        }

        // 升级属性提升（保留原有逻辑，可根据需要调整）
        currentSize = baseSize + (int)currentTier * sizeIncreasePerTier;
        moveSpeed += 0.5f;
        maxHealth += 50;
            currentHealth = Mathf.Min(maxHealth, currentHealth + 50);
        
                // 新增：重新计算升级后的实际速度（抵消累计衰减）
        float totalMoveSpeedDecrease = currentExp * moveSpeedDecreasePerExp;
        float finalMoveSpeed = Mathf.Max(moveSpeed - totalMoveSpeedDecrease, minMoveSpeed);
        float finalRotateSpeed = Mathf.Max(_currentRotateSpeed - totalMoveSpeedDecrease, minRotateSpeed);

        // 同步更新ThirdPersonMove的速度
        if (thirdPersonMove != null)
        {
            thirdPersonMove.UpdateSpeedStats(finalMoveSpeed, finalRotateSpeed);
        }

        // 触发事件（原有逻辑不变）
        OnSizeChanged?.Invoke(currentSize);
        OnSpeedChanged?.Invoke(moveSpeed);
        OnTierChanged?.Invoke(currentTier);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }
}

    // 临时经验倍率
    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        StartCoroutine(ResetExpMultiplier(multiplier, duration));
    }

    private IEnumerator ResetExpMultiplier(float tempMulti, float duration)
    {
        _currentExpMultiplier = tempMulti;
        yield return new WaitForSeconds(duration);
        _currentExpMultiplier = baseExpMultiplier;
    }

    // 临时外观挡位（伪装技能用）
    public void SetTemporaryVisualTier(FishTier visualTier, float duration)
    {
        StartCoroutine(ResetVisualTier(visualTier, duration));
    }

    private IEnumerator ResetVisualTier(FishTier visualTier, float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    // 护盾管理
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

    // 获取下一等级所需经验
    public int GetRequiredExpForNextTier()
    {
        int tierIndex = (int)currentTier;
        return tierIndex < expToNextTier.Length ? expToNextTier[tierIndex] : 0;
    }
    // 处理玩家升级
    public void HandleTierUpgrade()
    {
        // 这里可以添加升级时的特效、音效等
        Debug.Log($"玩家鱼升级到 {currentTier} 挡位！");
    }
    // 处理与其他鱼的碰撞逻辑
    public void HandleFishCollision(FishTierEffect otherFish)
    {
        FishTier otherFishTier = otherFish.fishData.fishTier;
        int otherFishExpValue = otherFish.fishData.baseExpValue;
        string otherFishTag = otherFish.gameObject.tag;
        if (otherFishTier > currentTier)
        {
            // 碰到比自己挡位大的鱼
            if (!UseShield())
            {
                // 没有护盾，死亡
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
            // 同挡位的鱼，检测tag
            if (otherFishTag == "tail")
            {
                Debug.Log("玩家鱼吃掉同挡位鱼的尾部，获得经验！");
                Destroy(otherFish.gameObject);
                skillSystem.EatSkillFish(otherFish.fishData);
            }
            else if (otherFishTag == "head")
            {
                // 是head，无视
                Debug.Log("玩家鱼碰到同挡位鱼的头部，无视该碰撞！");
            }
            else
            {
                Debug.Log("吃到");
                skillSystem.EatSkillFish(otherFish.fishData);
                Destroy(otherFish.gameObject);

            }
        }
    }

}