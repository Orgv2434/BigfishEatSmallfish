using UnityEngine;
using DistantLands.Core;

/// <summary>
/// 独立的属性系统 - 管理体型、速度等属性的计算和同步
/// 职责：属性计算、模型缩放、碰撞体同步
/// 依赖注入：modelTransform、characterController
/// </summary>
public class FishStatsSystem : MonoBehaviour, IFishStatsSystem
{
    [Header("模型与碰撞体配置")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float colliderScaleMultiplier = 1f;

    [Header("体型配置")]
    [SerializeField] private float baseSize = 1f;
    [SerializeField] private float sizeIncreasePerExp = 0.002f;
    [SerializeField] private float sizeIncreasePerTier = 0.5f;

    [Header("速度配置")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float moveSpeedDecreasePerExp = 0.003f;
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float baseRotateSpeed = 90f;
    [SerializeField] private float rotateSpeedDecreasePerExp = 0.015f;
    [SerializeField] private float minRotateSpeed = 10f;

    private float currentSize;
    private float currentMoveSpeed;
    private float currentRotateSpeed;

    // 碰撞体初始参数
    private float baseColliderRadius;
    private float baseColliderHeight;
    private Vector3 baseColliderCenter;

    private EventBus eventBus;
    private int totalExp = 0;

    public float CurrentSize => currentSize;
    public float MoveSpeed => currentMoveSpeed;
    public float RotateSpeed => currentRotateSpeed;

    private void Awake()
    {
        eventBus = EventBus.Instance;
        currentSize = baseSize;
        currentMoveSpeed = baseSpeed;
        currentRotateSpeed = baseRotateSpeed;

        // 检查必需的组件
        if (modelTransform == null)
            Debug.LogError("[FishStatsSystem] 必须赋值 modelTransform");
        if (characterController == null)
            Debug.LogError("[FishStatsSystem] 必须赋值 characterController");
    }

    private void Start()
    {
        // 记录碰撞体初始参数
        if (characterController != null)
        {
            baseColliderRadius = characterController.radius;
            baseColliderHeight = characterController.height;
            baseColliderCenter = characterController.center;
        }

        // 初始化模型和碰撞体
        InitializeModelAndCollider();

        // 订阅等级升级事件
        eventBus.Subscribe<PlayerTierUpgradedEvent>(OnTierUpgraded);
    }

    private void InitializeModelAndCollider()
    {
        if (modelTransform != null)
        {
            modelTransform.localScale = Vector3.one * currentSize;
        }

        if (characterController != null)
        {
            UpdateColliderSize(currentSize);
        }
    }

    /// <summary>
    /// 根据获得的经验更新体型
    /// </summary>
    public void UpdateSizeByExp(int expGained)
    {
        totalExp += expGained;
        currentSize = baseSize + totalExp * sizeIncreasePerExp;

        // 同步模型和碰撞体
        SyncModelAndCollider();

        // 发布体型变化事件
        var sizeEvent = new PlayerSizeChangedEvent
        {
            NewSize = currentSize,
            OldSize = currentSize - expGained * sizeIncreasePerExp
        };
        eventBus.Publish(sizeEvent);
    }

    /// <summary>
    /// 根据获得的经验更新速度
    /// </summary>
    public void UpdateSpeedByExp(int expGained)
    {
        totalExp += expGained;

        // 计算速度衰减
        float totalSpeedDecrease = totalExp * moveSpeedDecreasePerExp;
        currentMoveSpeed = Mathf.Max(baseSpeed - totalSpeedDecrease, minMoveSpeed);

        float totalRotateDecrease = totalExp * rotateSpeedDecreasePerExp;
        currentRotateSpeed = Mathf.Max(baseRotateSpeed - totalRotateDecrease, minRotateSpeed);

        // 发布速度变化事件
        var speedEvent = new PlayerSpeedChangedEvent
        {
            MoveSpeed = currentMoveSpeed,
            RotateSpeed = currentRotateSpeed
        };
        eventBus.Publish(speedEvent);
    }

    /// <summary>
    /// 应用等级升级加成
    /// </summary>
    public void ApplyTierUpgradeBonus(FishTier newTier)
    {
        // 体型增长
        currentSize += sizeIncreasePerTier;

        // 速度恢复（平衡)
        currentMoveSpeed += 0.5f;

        // 同步模型和碰撞体
        SyncModelAndCollider();

        Debug.Log($"[FishStatsSystem] 升级到 {newTier}，体型 {currentSize:F2}");
    }

    /// <summary>
    /// 同步模型缩放和碰撞体大小
    /// </summary>
    private void SyncModelAndCollider()
    {
        if (modelTransform != null)
        {
            modelTransform.localScale = Vector3.one * currentSize;
        }

        UpdateColliderSize(currentSize);
    }

    /// <summary>
    /// 更新碰撞体大小（精确同步）
    /// </summary>
    public void UpdateCollider(float newSize)
    {
        currentSize = newSize;
        UpdateColliderSize(newSize);
    }

    private void UpdateColliderSize(float newSize)
    {
        if (characterController == null)
            return;

        // 计算缩放比例
        float scaleRatio = newSize / baseSize;

        // 同步半径、高度、中心
        characterController.radius = baseColliderRadius * scaleRatio * colliderScaleMultiplier;
        characterController.height = baseColliderHeight * scaleRatio * colliderScaleMultiplier;
        characterController.center = baseColliderCenter * scaleRatio * colliderScaleMultiplier;

        // 安全校验
        characterController.radius = Mathf.Max(characterController.radius, 0.1f);
        characterController.height = Mathf.Max(characterController.height, 0.2f);
    }

    private void OnTierUpgraded(PlayerTierUpgradedEvent evt)
    {
        ApplyTierUpgradeBonus(evt.NewTier);
    }

    private void OnDestroy()
    {
        eventBus.Unsubscribe<PlayerTierUpgradedEvent>(OnTierUpgraded);
    }
}
