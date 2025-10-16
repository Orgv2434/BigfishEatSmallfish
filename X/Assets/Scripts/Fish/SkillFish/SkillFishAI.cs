using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SkillFishAI : MonoBehaviour
{
    // ==========================================
    // 基础移动参数（Inspector可编辑）
    // ==========================================
    [Header("基础移动设置"), Tooltip("正常移动速度")]
    public float originalSpeed = 2f;
    [Tooltip("转向速度（值越大转向越快）")]
    public float rotationSpeed = 5f;
    [Tooltip("正常状态下随机变向的时间间隔（范围）")]
    public Vector2 directionChangeIntervalRange = new Vector2(2f, 4.5f);
    private float currentDirectionChangeInterval; // 当前生效的变向间隔


    // ==========================================
    // 玩家检测与逃窜参数（Inspector可编辑）
    // ==========================================
    [Header("玩家检测与逃窜设置"), Tooltip("检测到玩家的距离（进入此范围开始逃窜）")]
    public float detectDistance = 5f;
    [Tooltip("脱离玩家的安全距离（超出此范围停止逃窜）")]
    public float safeDistance = 8f;
    [Tooltip("最长逃窜持续时间（超过时间强制停止逃窜）")]
    public float fleeDuration = 3f;
    [Tooltip("逃窜时的速度倍数（正常速度 × 此值）")]
    public float fleeSpeedMultiplier = 1.8f;
    [Tooltip("逃窜方向的更新间隔（秒），值越大方向越稳定")]
    [SerializeField] private float fleeDirUpdateInterval = 0.2f;
    [Tooltip("逃窜方向的随机偏移强度（值越小方向越稳定）")]
    [SerializeField] private float fleeRandomOffsetStrength = 0.2f;
    [Tooltip("逃窜时的转向速度倍数（正常转向速度 × 此值）")]
    [SerializeField] private float fleeRotationMultiplier = 1.5f;
    [Tooltip("逃窜→正常状态的缓冲期（秒），避免状态反复切换")]
    [SerializeField] private float fleeCooldown = 0.5f;


    // ==========================================
    // 逃窜后休息参数（新增）
    // ==========================================
    [Header("逃窜后休息设置"), Tooltip("逃窜结束后的休息时间（秒）")]
    [SerializeField] private float restDuration = 2f;
    [Tooltip("休息时的速度比例（正常速度 × 此值）")]
    [SerializeField] private float restSpeedRatio = 0.5f;
    [Tooltip("休息时的变向间隔延长比例（正常间隔 × 此值）")]
    [SerializeField] private float restDirChangeMultiplier = 1.5f;


    // ==========================================
    // 球形区域与回归参数（Inspector可编辑）
    // ==========================================
    [Header("球形区域与回归设置"), Tooltip("鱼的活动球形区域中心")]
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    [Tooltip("鱼的活动球形区域半径")]
    [SerializeField] private float spawnRadius = 10f;
    [Tooltip("超出区域后，向中心回归的方向过渡速度（值越大回归越积极）")]
    [SerializeField] private float returnToAreaSpeed = 0.1f;
    [Tooltip("超出区域过远时的强制转向速度（值越大转向越快）")]
    [SerializeField] private float returnToAreaRotationSpeed = 2f;
    [Tooltip("超出区域多少倍半径后，触发强制转向（如1.5=超出半径50%）")]
    [SerializeField] private float farFromAreaMultiplier = 1.5f;


    // ==========================================
    // 随机方向参数（Inspector可编辑）
    // ==========================================
    [Header("随机方向设置"), Tooltip("Y轴方向的随机波动范围（限制上下移动幅度）")]
    [SerializeField] private Vector2 randomYRange = new Vector2(-0.15f, 0.15f);
    [Tooltip("正常变向时，旧方向的保留比例（0=完全新方向，1=不改变方向）")]
    [SerializeField] private float normalDirLerpWeight = 0.5f; // 保留50%旧方向
    [Tooltip("停止逃窜时，旧方向的保留比例（0=完全新方向，1=不改变方向）")]
    [SerializeField] private float fleeEndDirLerpWeight = 0.3f; // 保留70%旧方向


    // ==========================================
    // 私有运行时变量（无需在Inspector显示）
    // ==========================================
    private float moveSpeed;
    private Transform player;
    private Vector3 targetDirection;
    private float directionTimer;
    private float fleeTimer;
    private bool isFleeing;
    private bool isOutsideArea = false;
    private float lastFleeEndTime;
    private Vector3 stableFleeDir;
    private float lastFleeDirUpdateTime;
    
    // 休息状态变量（新增）
    private bool isResting;
    private float restTimer;
    private float originalDirectionChangeInterval; // 保存原始变向间隔


    // 初始化方法（若需通过代码动态赋值，仍可调用此方法覆盖Inspector值）
    public void Initialize(
        float speed = -1f,
        float rotSpeed = -1f,
        float detectDist = -1f,
        float safeDist = -1f,
        float fleeTime = -1f,
        float fleeMultiplier = -1f,
        Transform playerTransform = null,
        Vector3 center = default,
        float radius = -1f
    )
    {
        // 仅当传入有效值时，才覆盖Inspector设置（-1表示使用Inspector值）
        moveSpeed = (speed > 0) ? speed : originalSpeed;
        originalSpeed = (speed > 0) ? speed : originalSpeed;
        rotationSpeed = (rotSpeed > 0) ? rotSpeed : rotationSpeed;
        detectDistance = (detectDist > 0) ? detectDist : detectDistance;
        safeDistance = (safeDist > 0) ? safeDist : safeDistance;
        fleeDuration = (fleeTime > 0) ? fleeTime : fleeDuration;
        fleeSpeedMultiplier = (fleeMultiplier > 0) ? fleeMultiplier : fleeSpeedMultiplier;
        spawnCenter = (center != default) ? center : spawnCenter;
        spawnRadius = (radius > 0) ? radius : spawnRadius;
        player = playerTransform ?? player; // 若未传玩家，保留Inspector可能赋值的player

        // 初始化变向间隔和方向
        currentDirectionChangeInterval = Random.Range(directionChangeIntervalRange.x, directionChangeIntervalRange.y);
        originalDirectionChangeInterval = currentDirectionChangeInterval;
        targetDirection = GetRandomDirection();
        stableFleeDir = targetDirection;
    }


    private void Awake()
    {
        // 初始化解码时的参数（避免未调用Initialize时参数异常）
        moveSpeed = originalSpeed;
        currentDirectionChangeInterval = Random.Range(directionChangeIntervalRange.x, directionChangeIntervalRange.y);
        originalDirectionChangeInterval = currentDirectionChangeInterval;
        targetDirection = GetRandomDirection();
        stableFleeDir = targetDirection;
    }


    private void Update()
    {
        if (player == null)
        {
            MoveNormally();
            CheckIfOutsideArea();
            if (isOutsideArea) ReturnToArea();
            return;
        }

        // 玩家距离检测（带缓冲逻辑）
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool isInDetectRange = distanceToPlayer <= detectDistance;
        bool isInSafeRange = distanceToPlayer >= safeDistance;

        // 如果在休息中检测到玩家，立即结束休息并开始逃窜
        if (isResting && isInDetectRange)
        {
            EndResting();
            StartFleeing();
            lastFleeDirUpdateTime = Time.time;
            stableFleeDir = (transform.position - player.position).normalized;
        }

        // 逃窜状态切换（带缓冲期）
        if (!isFleeing && !isResting)
        {
            if (isInDetectRange && Time.time - lastFleeEndTime >= fleeCooldown)
            {
                StartFleeing();
                lastFleeDirUpdateTime = Time.time;
                stableFleeDir = (transform.position - player.position).normalized;
            }
        }
        else if (isFleeing)
        {
            fleeTimer += Time.deltaTime;
            if (isInSafeRange || fleeTimer >= fleeDuration)
            {
                StopFleeing();
                StartResting(); // 逃窜结束后开始休息
                lastFleeEndTime = Time.time;
            }
        }
        else if (isResting)
        {
            // 休息计时
            restTimer += Time.deltaTime;
            if (restTimer >= restDuration)
            {
                EndResting();
            }
        }

        CheckIfOutsideArea();

        // 运动逻辑
        if (isFleeing)
        {
            FleeMovement();
        }
        else
        {
            MoveNormally();
            if (isOutsideArea) ReturnToArea();
        }
    }


    // 开始休息（新增方法）
    private void StartResting()
    {
        isResting = true;
        restTimer = 0;
        // 降低移动速度
        moveSpeed = originalSpeed * restSpeedRatio;
        // 延长变向间隔，让鱼更"慵懒"
        currentDirectionChangeInterval = originalDirectionChangeInterval * restDirChangeMultiplier;
        Debug.Log("开始休息");
    }


    // 结束休息（新增方法）
    private void EndResting()
    {
        isResting = false;
        // 恢复正常速度
        moveSpeed = originalSpeed;
        // 恢复正常变向间隔
        currentDirectionChangeInterval = originalDirectionChangeInterval;
        Debug.Log("结束休息，恢复正常状态");
    }


    // 检查是否超出球形区域
    private void CheckIfOutsideArea()
    {
        if (spawnRadius <= 0)
        {
            isOutsideArea = false;
            return;
        }
        float distanceToCenter = Vector3.Distance(transform.position, spawnCenter);
        isOutsideArea = distanceToCenter > spawnRadius;
    }


    // 缓慢返回球形区域
    private void ReturnToArea()
    {  
        Vector3 toCenter = spawnCenter - transform.position;
        float distanceToCenter = toCenter.magnitude;
        Vector3 returnDirection = toCenter.normalized;

        // 平滑过渡回归方向
        targetDirection = Vector3.Lerp(targetDirection, returnDirection, returnToAreaSpeed);

        // 距离过远时强制转向
        if (distanceToCenter > spawnRadius * farFromAreaMultiplier)
        {
            Quaternion targetRot = Quaternion.LookRotation(returnDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                returnToAreaRotationSpeed * Time.deltaTime
            );
        }

        // 回到区域内后重置状态
        if (distanceToCenter <= spawnRadius)
        {
            isOutsideArea = false;
        }
    }


    // 正常移动逻辑
    private void MoveNormally()
    {
        directionTimer += Time.deltaTime;

        // 到时间随机变向（平滑过渡）
        if (directionTimer >= currentDirectionChangeInterval)
        {
            targetDirection = Vector3.Lerp(
                targetDirection,
                GetRandomDirection(),
                normalDirLerpWeight // 保留旧方向比例（Inspector可调整）
            ).normalized;
            directionTimer = 0;
            // 重新随机下次变向间隔
            currentDirectionChangeInterval = Random.Range(directionChangeIntervalRange.x, directionChangeIntervalRange.y);
            originalDirectionChangeInterval = currentDirectionChangeInterval;
            
            // 如果在休息中，保持更长的变向间隔
            if (isResting)
            {
                currentDirectionChangeInterval *= restDirChangeMultiplier;
            }
        }

        // 平滑转向
        Quaternion targetRot = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        // 移动
        transform.position += targetDirection * moveSpeed * Time.deltaTime;
    }


    // 逃窜移动逻辑
    private void FleeMovement()
    {
        // 定时更新逃窜方向（避免每帧抖动）
        if (Time.time - lastFleeDirUpdateTime >= fleeDirUpdateInterval)
        {
            Vector3 baseFleeDir = (transform.position - player.position).normalized;
            // 随机偏移（强度由Inspector控制）
            Vector3 randomOffset = new Vector3(
                Random.Range(-fleeRandomOffsetStrength, fleeRandomOffsetStrength),
                Random.Range(-fleeRandomOffsetStrength * 0.5f, fleeRandomOffsetStrength * 0.5f), // Y轴偏移减半，更稳定
                Random.Range(-fleeRandomOffsetStrength, fleeRandomOffsetStrength)
            ).normalized * fleeRandomOffsetStrength;

            // 平滑过渡到新逃窜方向
            stableFleeDir = Vector3.Lerp(stableFleeDir, baseFleeDir + randomOffset, 0.3f).normalized;
            lastFleeDirUpdateTime = Time.time;
        }

        // 逃窜转向（速度倍数由Inspector控制）
        Quaternion targetRot = Quaternion.LookRotation(stableFleeDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * fleeRotationMultiplier * Time.deltaTime
        );

        // 逃窜移动
        transform.position += stableFleeDir * moveSpeed * Time.deltaTime;
    }


    // 开始逃窜
    private void StartFleeing()
    {  
        Debug.Log("检测到玩家，开始逃窜");
        isFleeing = true;
        fleeTimer = 0;
        moveSpeed = originalSpeed * fleeSpeedMultiplier;
    }


    // 停止逃窜
    private void StopFleeing()
    {  
        Debug.Log("脱离玩家，停止逃窜");
        isFleeing = false;
        // 保留逃窜惯性（比例由Inspector控制）
        targetDirection = Vector3.Lerp(
            stableFleeDir,
            GetRandomDirection(),
            fleeEndDirLerpWeight
        ).normalized;
    }


    // 获取随机方向（Y轴范围由Inspector控制）
    private Vector3 GetRandomDirection()
    {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(randomYRange.x, randomYRange.y),
            Random.Range(-1f, 1f)
        ).normalized;
    }


    // 视野外回收
    private void OnBecameInvisible()
    {
        Invoke(nameof(Despawn), 5f);
    }


    private void OnBecameVisible()
    {
        CancelInvoke(nameof(Despawn));
    }


    private void Despawn()
    {
        var spawner = GetComponentInParent<FishAreaManager>();
        if (spawner != null)
        {
            spawner.DespawnFish(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // ==========================================
    // Gizmos：在Scene视图绘制球形区域（方便调试）
    // ==========================================
    private void OnDrawGizmosSelected()
    {
        // 绘制球形活动区域（蓝色）
        if (spawnRadius > 0)
        {
            Gizmos.color = new Color(0, 0.8f, 1, 0.3f); // 半透明蓝色
            Gizmos.DrawSphere(spawnCenter, spawnRadius);

            // 绘制“远距离强制回归”边界（青色）
            Gizmos.color = new Color(0, 1, 1, 0.2f); // 半透明青色
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius * farFromAreaMultiplier);
        }

        // 绘制玩家检测范围（红色）
        if (player != null && detectDistance > 0)
        {
            Gizmos.color = new Color(1, 0.3f, 0.3f, 0.2f); // 半透明红色
            Gizmos.DrawWireSphere(transform.position, detectDistance);
        }

        // 绘制玩家安全范围（绿色）
        if (player != null && safeDistance > 0)
        {
            Gizmos.color = new Color(0.3f, 1, 0.3f, 0.2f); // 半透明绿色
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }

        // 绘制当前移动方向（白色线段）
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + targetDirection * 2f);

        // 休息状态可视化（黄色球体）
        if (isResting)
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); // 半透明黄色
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
    