using UnityEngine;
using System;
using System.Collections;
using DistantLands;

public class PlayerFishData : MonoBehaviour
{
    public static PlayerFishData Instance { get; private set; }
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
    public float healthLossRate = 1f;
    public float expToHealthRate = 0.2f;

    [Header("经验设置")]
    public float baseExpMultiplier = 0.5f;
    private float _currentExpMultiplier;

    [Header("升级设置")]
    public int[] expToNextTier = { 100, 300, 600, 1000 };

    [Header("体型与速度设置")]
    public float sizeIncreasePerTier = 0.5f;
    public float sizeIncreasePerExp = 0.001f;
    public float moveSpeedDecreasePerExp = 0.002f;
    public float minMoveSpeed = 2f;
    public float rotateSpeedDecreasePerExp = 0.01f;
    public float minRotateSpeed = 1f;
    public float baseRotateSpeed = 90f;
    private float _currentRotateSpeed;

    [Header("引用配置")]
    public ThirdPersonMove thirdPersonMove;
   

    // 事件通知
    public Action<float> OnHealthChanged;
    public Action<int, int> OnExpChanged;
    public Action<FishTier> OnTierChanged;
    
    public Action<float> OnSizeChanged;
    public Action<float> OnSpeedChanged;

    // 护盾状态
    public int _shieldCount = 0;
    private FishSkillSystem _skillSystem;
    // 定义挡位变化事件（参数为新挡位）
    public event Action<FishTier> OnTierUpgraded;

    //bgm
  
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentSize = baseSize;
        _currentExpMultiplier = baseExpMultiplier;
        _currentRotateSpeed = baseRotateSpeed;
        _skillSystem = GetComponent<FishSkillSystem>();
        

        // 初始化移动速度
        if (thirdPersonMove != null)
            thirdPersonMove.UpdateSpeedStats(moveSpeed, _currentRotateSpeed);
        else
            Debug.LogWarning("未赋值ThirdPersonMove脚本！速度更新将失效");
    }

    private void Update()
    {
        // 随时间扣血（保留原有逻辑）
        currentHealth = Mathf.Max(0, currentHealth - healthLossRate * Time.deltaTime);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // 死亡检测
        if (currentHealth <= 0)
        {
            
            Debug.Log("玩家鱼死亡！");
            Destroy(gameObject);

            // 音效
            MusicManager.Instance.Die();
            MusicManager.Instance.StopBGM();
        }
    }

    // 加血逻辑（保留）
    public void GainHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    // 加经验逻辑（保留并优化）
    public void GainExp(int baseExp)
    {
       
        int actualExp = Mathf.RoundToInt(baseExp * _currentExpMultiplier);
        currentExp += actualExp;

        // 经验与回血通知
        OnExpChanged?.Invoke(currentExp, GetRequiredExpForNextTier());
          float restoredHealth = actualExp * expToHealthRate;
        GainHealth(restoredHealth);
        Debug.Log($"获得经验：{actualExp}，回复血量：{restoredHealth}");

        // 屏幕特效
         string coloredText = 
        $"<color=yellow>+{actualExp} Exp</color>  " +  // 经验部分（黄色）
        $"<color=green>+{restoredHealth} HP</color>"; // 血量部分（红色）
        
        // 调用自定义提示方法，传入拼接好的富文本
        ExpPopupManager.Instance.ShowCustomPopup(transform.position, coloredText, Color.white); 
        
        // 实时更新属性
        UpdateStatsByExp(actualExp);
        CheckTierUpgrade();
    }

    // 经验更新属性（保留）
    private void UpdateStatsByExp(int addedExp)
    {
        // 体型更新
        currentSize += addedExp * sizeIncreasePerExp;
        OnSizeChanged?.Invoke(currentSize);

        // 速度更新
        float totalMoveSpeedDecrease = currentExp * moveSpeedDecreasePerExp;
        float newMoveSpeed = Mathf.Max(moveSpeed - totalMoveSpeedDecrease, minMoveSpeed);
        float totalRotateDecrease = currentExp * rotateSpeedDecreasePerExp;
        _currentRotateSpeed = Mathf.Max(baseRotateSpeed - totalRotateDecrease, minRotateSpeed);

        // 同步移动脚本速度
        if (thirdPersonMove != null)
            thirdPersonMove.UpdateSpeedStats(newMoveSpeed, _currentRotateSpeed);
        
        OnSpeedChanged?.Invoke(newMoveSpeed);
    }

    // 升级检测（保留）
    private void CheckTierUpgrade()
    {
        int tierIndex = (int)currentTier;
        while (tierIndex < expToNextTier.Length && currentExp >= expToNextTier[tierIndex])
        {
            tierIndex++;
            currentTier = (FishTier)tierIndex;
             OnTierUpgraded?.Invoke(currentTier);
            // 升级特效与属性提升
            FishTierEffect tierEffect = GetComponent<FishTierEffect>();
            if (tierEffect != null)
            {
                tierEffect.ShowTierHalo(currentTier);
                tierEffect.AutoFitModelSize();
            }

            // 升级属性加成
            currentSize = baseSize + (int)currentTier * sizeIncreasePerTier;
            moveSpeed += 0.5f;
            maxHealth += 50;
            currentHealth = Mathf.Min(maxHealth, currentHealth + 50);

            // 重新计算速度（抵消衰减）
            float finalMoveSpeed = Mathf.Max(moveSpeed - currentExp * moveSpeedDecreasePerExp, minMoveSpeed);
            float finalRotateSpeed = Mathf.Max(_currentRotateSpeed, minRotateSpeed);
            thirdPersonMove?.UpdateSpeedStats(finalMoveSpeed, finalRotateSpeed);

            // 事件通知
            OnSizeChanged?.Invoke(currentSize);
            OnSpeedChanged?.Invoke(finalMoveSpeed);
            OnTierChanged?.Invoke(currentTier);
            FishEventSystem.BroadcastTierChanged(currentTier); 
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
            HandleTierUpgrade();

            // 音效处理
            MusicManager.Instance.Upgrade();
        }
    }
    
    public FishTier GetCurrentTier()
    {
        return currentTier;
    }
    // 核心修改：吃鱼逻辑（通知鱼群管理器处理移除+重生）
    public void HandleFishCollision(FishTierEffect otherFish)
    {
        if (otherFish == null) return;

        FishTier otherTier = otherFish.fishData.fishTier;
        int otherExp = otherFish.fishData.baseExpValue;
        string otherTag = otherFish.gameObject.tag;

        // 比自己大的鱼：护盾抵消或死亡
        if (otherTier > currentTier)
        {
            if (!UseShield())
            {
                currentHealth = 0;
                Debug.Log("玩家鱼被更大挡位的鱼吃掉，死亡！");
            }
            else
            {
                Debug.Log("玩家鱼使用护盾抵挡了更大挡位鱼的攻击！");
            }
            return;
        }

        // 比自己小或同挡位的鱼：吃掉并通知鱼群重生
        if (otherTier < currentTier || (otherTier == currentTier && otherTag != "head"))
        {
         
            Debug.Log(otherTier < currentTier ? "吃掉更小挡位的鱼" : "吃掉同挡位鱼的尾部/身体");
            
            GlobalFlock fishFlock = otherFish.gameObject.transform.parent.parent?.GetComponent<GlobalFlock>();
            if (fishFlock != null)
                fishFlock.OnFishEaten(otherFish.gameObject); 
            else
                Destroy(otherFish.gameObject); // 异常情况：直接销毁

            // 获得经验与技能
            _skillSystem?.EatSkillFish(otherFish.fishData);
            GainExp(otherExp);

        }
        else if (otherTier == currentTier && otherTag == "head")
        {
            Debug.Log("碰到同挡位鱼的头部，无视碰撞！");
        }
    }

    // 以下为原有逻辑（保留）
    public void SetTemporaryExpMultiplier(float multiplier, float duration)
    {
        StartCoroutine(ResetExpMultiplier(multiplier, duration));
        // 音效
        MusicManager.Instance.PlusExp();
    }

    private IEnumerator ResetExpMultiplier(float tempMulti, float duration)
    {
        _currentExpMultiplier = tempMulti;
        yield return new WaitForSeconds(duration);
        _currentExpMultiplier = baseExpMultiplier;
    }

    public void SetTemporaryVisualTier(FishTier visualTier, float duration)
    {
        StartCoroutine(ResetVisualTier(visualTier, duration));
    }

    private IEnumerator ResetVisualTier(FishTier visualTier, float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void AddShield(int count = 1) => _shieldCount += count;

    public bool UseShield()
    {
        if (_shieldCount > 0)
        {
            _shieldCount--;

            // 音效
            MusicManager.Instance.HuDun();
            return true;
        }
        return false;
    }

    public int GetRequiredExpForNextTier()
    {
        int tierIndex = (int)currentTier;
        return tierIndex < expToNextTier.Length ? expToNextTier[tierIndex] : 0;
    }

    public void HandleTierUpgrade()
    {
        Debug.Log($"玩家鱼升级到 {currentTier} 挡位！");
    }
}