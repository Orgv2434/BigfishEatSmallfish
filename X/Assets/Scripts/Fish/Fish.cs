using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 鱼的行为控制类，负责处理鱼的移动、群体行为和网络同步
/// 优化后：鱼会在所属生成区域内活动，避免单向运动
/// </summary>
public class Fish : NetworkBehaviour
{
    // 鱼的当前移动速度
    private float speed;
    // 鱼的平均移动速度基准值
    public float averageSpeed = 1.0f;
    // 周围鱼群的平均朝向（用于群体行为计算）
    Vector3 averageHeading;
    // 周围鱼群的平均位置（用于群体行为计算）
    Vector3 averagePosition;
    // 判定为"邻居"的最大距离（范围内的鱼会影响当前鱼的行为）
    float neighborDistance = 3.0f;
    // 行为更新频率参数（值越大，行为变化越频繁）
    public int performance = 5;
    // 引用鱼群管理器（全局控制类）
    [HideInInspector] public GlobalFlock flock;
    // 当前鱼所属的生成区域（关键：用于限定活动范围）
    [HideInInspector] public FishSpawnZone myZone;

    // 是否正在转向（用于边界限制时的转向逻辑）
    bool turning = false;

    // 网络同步变量 - 位置（仅服务器可写，客户端只读）
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server
    );
    // 网络同步变量 - 旋转（仅服务器可写，客户端只读）
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        writePerm: NetworkVariableWritePermission.Server
    );
    // 网络同步变量 - 速度（仅服务器可写，客户端只读）
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// 初始化鱼的速度和网络同步回调
    /// </summary>
    void Start()
    {
        // 随机初始化速度（在平均速度的0.5-1.5倍之间）
        speed = Random.Range(0.5f, 1.5f) * averageSpeed;

        // 仅客户端注册网络变量变更回调（服务器不需要，因为服务器直接控制状态）
        if (!IsServer)
        {
            networkPosition.OnValueChanged += OnPositionChanged;
            networkRotation.OnValueChanged += OnRotationChanged;
            networkSpeed.OnValueChanged += OnSpeedChanged;
        }
    }

    /// <summary>
    /// 位置网络变量变更时的回调（客户端执行）
    /// </summary>
    private void OnPositionChanged(Vector3 oldVal, Vector3 newVal)
    {
        transform.position = newVal;
    }

    /// <summary>
    /// 旋转网络变量变更时的回调（客户端执行）
    /// </summary>
    private void OnRotationChanged(Quaternion oldVal, Quaternion newVal)
    {
        transform.rotation = newVal;
    }

    /// <summary>
    /// 速度网络变量变更时的回调（客户端执行）
    /// </summary>
    private void OnSpeedChanged(float oldVal, float newVal)
    {
        speed = newVal;
    }

    /// <summary>
    /// 帧更新方法，区分服务器和客户端逻辑
    /// </summary>
    void Update()
    {
        // 如果没有鱼群管理器引用，直接返回（避免空引用错误）
        if (flock == null) return;

        // 服务器负责计算所有运动逻辑并同步给客户端
        if (IsServer)
        {
            CalculateMovement();

            // 更新网络变量，自动同步到所有客户端
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            networkSpeed.Value = speed;
        }
        // 客户端仅执行基础移动，保持视觉流畅性
        else
        {
            transform.Translate(0, 0, Time.deltaTime * speed);
        }
    }

    /// <summary>
    /// 计算鱼的移动逻辑（仅服务器执行）
    /// 包含边界检查和群体行为决策，以所属区域为中心活动
    /// </summary>
    private void CalculateMovement()
    {
        ApplySpawnZoneBoundary();

        if (turning)
        {
            // 转向目标：所属区域的中心（而非全局固定点），加入随机偏移避免机械感
            Vector3 targetPos = GetZoneCenter();
            Vector3 direction = targetPos + Vector3.up * Random.Range(-0.5f, 0.5f) - transform.position;

            // 确保方向在水平面上（避免垂直方向过度移动）
            direction.y = Mathf.Clamp(direction.y, -0.1f, 0.1f);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                TurnSpeed() * Time.deltaTime
            );
        }
        else
        {
            // 随机触发群体行为规则计算
            if (Random.Range(0, performance + 1) < 1)
            {
                ApplyRules();
            }
        }

        // 沿当前朝向移动，加入微小随机偏移增强自然感
        Vector3 moveOffset = new Vector3(
            Random.Range(-0.02f, 0.02f),
            Random.Range(-0.01f, 0.01f),
            0
        );
        transform.Translate((Vector3.forward + moveOffset) * Time.deltaTime * speed);
    }

    /// <summary>
    /// 获取所属区域的中心位置（带空值保护）
    /// </summary>
    private Vector3 GetZoneCenter()
    {
        if (myZone != null && myZone.spawnCenter != null)
        {
            return myZone.spawnCenter.position;
        }
        //  fallback：如果没有所属区域，使用自身当前位置作为参考
        return transform.position;
    }

    /// <summary>
    /// 检查鱼是否超出所属生成区域的边界
    /// 超出则标记需要转向返回区域内
    /// </summary>
    void ApplySpawnZoneBoundary()
    {
        if (flock == null || myZone == null || !myZone.enabled || myZone.spawnCenter == null)
        {
            turning = false;
            return;
        }

        // 仅检查当前鱼所属的生成区域（而非所有区域）
        float distanceToCenter = Vector3.Distance(transform.position, myZone.spawnCenter.position);
        // 超出区域半径+缓冲范围时需要转向
        turning = distanceToCenter > myZone.spawnRadius + 0.5f;
    }

    /// <summary>
    /// 应用鱼群行为规则（聚集、避碰、速度匹配）
    /// 以所属区域为中心，模拟自然群体运动
    /// </summary>
    void ApplyRules()
    {
        if (flock == null || flock.allFish == null) return;

        GameObject[] gos = flock.allFish.ToArray();
        speed = Random.Range(0.5f, 1.5f) * averageSpeed;

        // 群体中心参考：所属区域的中心
        Vector3 zoneCenter = GetZoneCenter();
        Vector3 vCenter = zoneCenter;
        Vector3 vAvoid = Vector3.zero;
        float gSpeed = 0;
        Vector3 goalPos = zoneCenter;

        int groupSize = 0;

        // 遍历所有鱼，计算群体影响
        foreach (GameObject go in gos)
        {
            if (go == this.gameObject) continue;

            float dist = Vector3.Distance(go.transform.position, transform.position);
            if (dist <= neighborDistance)
            {
                vCenter += go.transform.position;
                groupSize++;

                // 近距离避碰
                if (dist < 0.75f)
                {
                    vAvoid += (transform.position - go.transform.position);
                }

                // 速度匹配
                Fish anotherFish = go.GetComponent<Fish>();
                if (anotherFish != null)
                {
                    gSpeed += anotherFish.speed;
                }
            }
        }

        // 计算最终行为方向
        if (groupSize > 0)
        {
            // 聚集中心 = 平均位置 + 少量区域中心偏向（权重0.3）
            vCenter = (vCenter / groupSize) * 0.7f + (goalPos - transform.position) * 0.3f;
            speed = gSpeed / groupSize;

            Vector3 direction = (vCenter + vAvoid) - transform.position;
            if (direction != Vector3.zero)
            {
                // 平滑转向
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    TurnSpeed() * Time.deltaTime
                );
            }
        }

        // 小概率随机转向（5%概率），增强自然感
        if (Random.Range(0, 100) < 5)
        {
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.2f, 0.2f),
                Random.Range(-1f, 1f)
            ).normalized;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(randomDir),
                TurnSpeed() * 0.5f * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// 计算转向速度（影响转向的平滑程度）
    /// </summary>
    /// <returns>转向速度系数</returns>
    float TurnSpeed()
    {
        return Random.Range(0.2f, 0.4f) * speed;
    }

    /// <summary>
    /// 销毁时的清理逻辑
    /// 从鱼群管理器中移除自身引用
    /// </summary>
    new void OnDestroy()
    {
        if (flock != null && flock.allFish != null && flock.allFish.Contains(gameObject))
        {
            flock.allFish.Remove(gameObject);
        }
    }
}