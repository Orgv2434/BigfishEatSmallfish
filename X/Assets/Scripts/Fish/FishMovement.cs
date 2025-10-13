using UnityEngine;
using System.Collections.Generic;

public class FishMovement : MonoBehaviour
{
    [Header("鱼群基础参数")]
    [Tooltip("基础移动速度")]
    public float baseSpeed = 2f;
    [Tooltip("转向平滑系数")]
    public float rotationSmooth = 5f;
    [Tooltip("鱼之间的固定间距")]
    public float fixedDistance = 1f;
    [Tooltip("鱼群整体移动方向")]
    public Vector3 globalDirection = Vector3.forward;

    [Header("鱼群行为权重")]
    [Tooltip("聚集行为权重")]
    [Range(0, 2)] public float cohesionWeight = 1f;
    [Tooltip("对齐行为权重")]
    [Range(0, 2)] public float alignmentWeight = 1f;
    [Tooltip("分离行为权重（固定间距核心）")]
    [Range(0, 5)] public float separationWeight = 3f;
    [Tooltip("全局方向遵循权重")]
    [Range(0, 2)] public float directionWeight = 0.5f;

    [Header("玩家交互参数")]
    [Tooltip("玩家检测范围")]
    public float playerDetectRange = 3f;
    [Tooltip("逃窜持续时间")]
    public float fleeDuration = 4f;
    [Tooltip("逃窜速度倍率")]
    public float fleeSpeedMultiplier = 1.5f;

    private List<FishMovement> nearbyFish = new List<FishMovement>();
    private Vector3 currentVelocity;
    private Transform player;
    private bool isFleeing = false;
    private float fleeTimer = 0;

    // 鱼群管理器引用
    public FishSchoolGenerator schoolGenerator { get; set; }

    void Start()
    {
        // 初始化
        currentVelocity = globalDirection * baseSpeed;
        // 查找玩家（使用标签）
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        // 检测玩家接近
        CheckPlayerProximity();

        if (isFleeing)
        {
            FleeBehavior();
            fleeTimer -= Time.deltaTime;

            // 逃窜结束后恢复正常行为
            if (fleeTimer <= 0)
            {
                isFleeing = false;
                // 恢复时重新对齐全局方向
                currentVelocity = Vector3.Lerp(currentVelocity, globalDirection * baseSpeed, 0.1f);
            }
        }
        else
        {
            // 正常鱼群行为
            SchoolBehavior();
        }

        // 应用移动和转向
        MoveAndRotate();
    }

    // 检测玩家是否进入范围
    private void CheckPlayerProximity()
    {
        if (!isFleeing && Vector3.Distance(transform.position, player.position) < playerDetectRange)
        {
            isFleeing = true;
            Debug.Log("玩家接近，鱼群开始逃窜！");
            fleeTimer = fleeDuration;
        }
    }

    // 逃窜行为
    private void FleeBehavior()
    {
        // 远离玩家方向
        Vector3 fleeDir = (transform.position - player.position).normalized;
        // 加入随机扰动模拟混乱逃窜
        Vector3 randomDir = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.1f, 0.1f),
            Random.Range(-0.3f, 0.3f)
        );

        currentVelocity = (fleeDir + randomDir).normalized * baseSpeed * fleeSpeedMultiplier;
    }

    // 鱼群行为核心逻辑
    private void SchoolBehavior()
    {
        // 查找附近的鱼
        FindNearbyFish();

        // 计算三种基本行为向量
        Vector3 cohesion = CalculateCohesion();
        Vector3 alignment = CalculateAlignment();
        Vector3 separation = CalculateSeparation();

        // 综合行为向量（带权重）
        Vector3 targetVelocity =
            cohesion * cohesionWeight +
            alignment * alignmentWeight +
            separation * separationWeight +
            globalDirection * directionWeight;

        // 规范化并应用速度
        targetVelocity = targetVelocity.normalized * baseSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 2f);
    }

    // 聚集行为：向附近鱼群中心移动
    private Vector3 CalculateCohesion()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 center = Vector3.zero;
        foreach (var fish in nearbyFish)
        {
            center += fish.transform.position;
        }
        center /= nearbyFish.Count;

        return (center - transform.position).normalized;
    }

    // 对齐行为：与附近鱼群保持方向一致
    private Vector3 CalculateAlignment()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 avgDirection = Vector3.zero;
        foreach (var fish in nearbyFish)
        {
            avgDirection += fish.currentVelocity.normalized;
        }
        avgDirection /= nearbyFish.Count;

        return avgDirection;
    }

    // 分离行为：保持固定间距（核心算法）
    private Vector3 CalculateSeparation()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        foreach (var fish in nearbyFish)
        {
            float distance = Vector3.Distance(transform.position, fish.transform.position);
            if (distance < fixedDistance && distance > 0)
            {
                // 距离越近，分离力度越大
                separation += (transform.position - fish.transform.position) / (distance * distance);
            }
        }

        return separation.normalized;
    }

    // 查找附近的鱼
    private void FindNearbyFish()
    {
        nearbyFish.Clear();
        if (schoolGenerator == null) return;

        foreach (var fish in schoolGenerator.spawnedFish)
        {
            if (fish == null || fish == gameObject) continue;

            FishMovement other = fish.GetComponent<FishMovement>();
            if (other != null)
            {
                float distance = Vector3.Distance(transform.position, fish.transform.position);
                // 只检测固定距离1.5倍范围内的鱼
                if (distance < fixedDistance * 1.5f)
                {
                    nearbyFish.Add(other);
                }
            }
        }
    }

    // 移动和转向执行
    private void MoveAndRotate()
    {
        // 更新位置
        transform.position += currentVelocity * Time.deltaTime;

        // 平滑转向
        if (currentVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(currentVelocity);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);
        }
    }

    // Gizmos可视化
    private void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fixedDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, playerDetectRange);

        // 绘制移动方向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2);
    }
}
