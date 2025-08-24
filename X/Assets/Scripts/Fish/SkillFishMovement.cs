using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace DistantLands
{
    // 确保网络变换组件使用服务器授权模式
    [RequireComponent(typeof(NetworkTransform))]
    public class SkillFishMovement : NetworkBehaviour
    {
        [Header("基础移动参数")]
        [Tooltip("正常移动速度（必须大于0）")]
        public float normalSpeed = 2f;
        [Tooltip("转向平滑系数")]
        public float rotationSmooth = 5f;

        [Header("玩家交互参数")]
        [Tooltip("玩家对象的标签")]
        public string playerTag = "Player";
        [Tooltip("玩家靠近触发逃窜的距离")]
        public float playerDetectRange = 3f;
        [Tooltip("逃窜持续时间")]
        public float fleeDuration = 4f;
        [Tooltip("逃窜速度倍率")]
        public float fleeSpeedMultiplier = 1.5f;

        [Header("球体路径参数")]
        [Tooltip("路径点之间的最小距离阈值")]
        public float waypointThreshold = 0.5f;
        [Tooltip("路径点生成的球体半径（建议5-10）")]
        public float pathSphereRadius = 5f;
        [Tooltip("Y轴高度限制（相对于自身位置）")]
        public Vector2 yAxisRange = new Vector2(-2f, 2f);

        // 状态变量
        private Vector3 currentVelocity;
        private Vector3[] pathPoints;
        private int currentPathIndex = 0;
        private bool isFleeing = false;
        private float fleeTimer = 0;
        private List<Transform> playerTransforms = new List<Transform>();

        // 网络同步变量（使用NetworkVariable的默认设置：服务器写入，所有人读取）
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<bool> networkIsFleeing = new NetworkVariable<bool>();
        private NetworkVariable<float> networkFleeTimer = new NetworkVariable<float>();
        private NetworkVariable<int> networkPathIndex = new NetworkVariable<int>();
        private NetworkList<Vector3> networkPathPoints = new NetworkList<Vector3>();

        // 主机标识属性（区分主机和纯客户端）
       new private bool IsHost => NetworkManager.Singleton.IsHost;
        // 客户端标识属性（包括主机作为客户端的角色）
       new private bool IsClient => NetworkManager.Singleton.IsClient;

        private void Start()
        {
            // 只有主机负责初始化路径和速度
            if (IsHost)
            {
                GenerateSphericalRandomPath();

                // 验证路径点并初始化速度
                if (pathPoints == null || pathPoints.Length == 0)
                {
                  
                }
                else
                {
                   
                    currentVelocity = (pathPoints[0] - transform.position).normalized * normalSpeed;
                  
                }
            }

            // 所有客户端（包括主机）都需要查找玩家
            if (IsClient)
            {
                FindPlayers();
                // 每5秒刷新一次玩家列表（防止玩家加入/离开）
                InvokeRepeating(nameof(FindPlayers), 5f, 5f);
            }
        }

        public override void OnNetworkSpawn()
        {
            // 主机初始化网络数据
            if (IsHost)
            {
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
                networkPathPoints.Clear();

                if (pathPoints != null)
                {
                    foreach (var point in pathPoints)
                    {
                        networkPathPoints.Add(point);
                    }
                }
                networkPathIndex.Value = currentPathIndex;
            }

            // 所有客户端（包括主机）监听网络变量
            if (IsClient)
            {
                networkPosition.OnValueChanged += OnPositionChanged;
                networkRotation.OnValueChanged += OnRotationChanged;
                networkIsFleeing.OnValueChanged += OnFleeingChanged;
                networkFleeTimer.OnValueChanged += OnFleeTimerChanged;
                networkPathIndex.OnValueChanged += OnPathIndexChanged;
                networkPathPoints.OnListChanged += OnPathPointsChanged;
            }
        }

        private void Update()
        {
            // 只有客户端（包括主机）执行更新
            if (!IsClient) return;

            // 路径点为空时不执行移动逻辑
            if (pathPoints == null || pathPoints.Length == 0)
                return;

            // 主机负责计算移动逻辑
            if (IsHost)
            {
                HostUpdate();
            }
            // 纯客户端仅做同步表现
            else
            {
                ClientUpdate();
            }
        }

        /// <summary>
        /// 主机更新逻辑（负责所有移动计算）
        /// </summary>
        private void HostUpdate()
        {
            HandlePlayerDetection();

            if (isFleeing)
            {
                FleeBehavior();
                fleeTimer -= Time.deltaTime;

                if (fleeTimer <= 0)
                {
                    isFleeing = false;
                    currentVelocity = Vector3.Lerp(
                        currentVelocity,
                        (pathPoints[currentPathIndex] - transform.position).normalized * normalSpeed,
                        0.1f
                    );
                }
            }
            else
            {
                MoveAlongPath();
            }

            MoveAndRotate();
            if (IsSpawned)
            {
                // 主机同步状态到网络
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
                networkIsFleeing.Value = isFleeing;
                networkFleeTimer.Value = fleeTimer;
                networkPathIndex.Value = currentPathIndex;
            }
        }

        /// <summary>
        /// 纯客户端更新逻辑（仅同步位置和旋转）
        /// </summary>
        private void ClientUpdate()
        {
            // 平滑同步位置和旋转
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 15f);
        }

        /// <summary>
        /// 玩家检测（仅主机执行）
        /// </summary>
        private void HandlePlayerDetection()
        {
            if (!IsHost || playerTransforms.Count == 0)
                return;

            Transform closestPlayer = GetClosestPlayer();
            if (closestPlayer != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, closestPlayer.position);
                if (!isFleeing && distanceToPlayer < playerDetectRange)
                {
                    isFleeing = true;
                    fleeTimer = fleeDuration;
                   
                }
            }
        }

        /// <summary>
        /// 生成路径点（仅主机执行）
        /// </summary>
        private void GenerateSphericalRandomPath()
        {
            if (!IsHost) return;

            int pathLength = Random.Range(3, 6);
            pathPoints = new Vector3[pathLength];
            Vector3 originPos = transform.position;

            for (int i = 0; i < pathLength; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere;
                randomDir = randomDir.normalized * Random.Range(pathSphereRadius * 0.5f, pathSphereRadius);

                float yPos = Mathf.Clamp(
                    originPos.y + randomDir.y,
                    originPos.y + yAxisRange.x,
                    originPos.y + yAxisRange.y
                );

                pathPoints[i] = new Vector3(
                    originPos.x + randomDir.x,
                    yPos,
                    originPos.z + randomDir.z
                );
               
            }
        }

        /// <summary>
        /// 沿路径移动（仅主机执行）
        /// </summary>
        private void MoveAlongPath()
        {
            if (!IsHost || pathPoints.Length == 0) return;

            if (Vector3.Distance(transform.position, pathPoints[currentPathIndex]) < waypointThreshold)
            {
                currentPathIndex = (currentPathIndex + 1) % pathPoints.Length;
             
            }

            Vector3 targetDir = (pathPoints[currentPathIndex] - transform.position);
            if (targetDir.sqrMagnitude < 0.01f)
            {
                targetDir = Random.insideUnitSphere.normalized;
              
            }
            else
            {
                targetDir = targetDir.normalized;
            }

            currentVelocity = Vector3.Lerp(currentVelocity, targetDir * normalSpeed, Time.deltaTime * 2f);
            if (currentVelocity.sqrMagnitude < 0.1f)
            {
                currentVelocity = targetDir * normalSpeed;
               
            }
        }

        /// <summary>
        /// 逃窜行为（仅主机执行）
        /// </summary>
        private void FleeBehavior()
        {
            if (!IsHost) return;

            Transform closestPlayer = GetClosestPlayer();
            if (closestPlayer == null) return;

            Vector3 fleeDir = (transform.position - closestPlayer.position).normalized;
            Vector3 randomDir = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.3f, 0.3f)
            );

            currentVelocity = (fleeDir + randomDir).normalized * normalSpeed * fleeSpeedMultiplier;
        }

        /// <summary>
        /// 获取最近玩家（仅客户端执行，包括主机）
        /// </summary>
        private Transform GetClosestPlayer()
        {
            if (!IsClient || playerTransforms.Count == 0) return null;

            Transform closest = null;
            float minDistance = Mathf.Infinity;
            Vector3 currentPosition = transform.position;

            foreach (var player in playerTransforms)
            {
                if (player == null) continue;

                float distance = Vector3.Distance(currentPosition, player.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = player;
                }
            }

            return closest;
        }

        /// <summary>
        /// 移动和旋转（仅主机执行）
        /// </summary>
        private void MoveAndRotate()
        {
            if (!IsHost) return;

            transform.position += currentVelocity * Time.deltaTime;
           

            if (currentVelocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentVelocity);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Mathf.Min(Time.deltaTime * rotationSmooth, 1f)
                );
            }
        }

        /// <summary>
        /// 查找玩家（所有客户端执行）
        /// </summary>
        public void FindPlayers()
        {
            if (!IsClient) return;

            playerTransforms.Clear();
            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);

            foreach (var player in players)
            {
                // 确保玩家有网络标识
                if (player.TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
                {
                    playerTransforms.Add(player.transform);
                }
            }
           
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, playerDetectRange);

            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, pathSphereRadius);

            if (pathPoints != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < pathPoints.Length; i++)
                {
                    Gizmos.DrawSphere(pathPoints[i], 0.2f);
                    if (i < pathPoints.Length - 1)
                    {
                        Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
                    }
                    else
                    {
                        Gizmos.DrawLine(pathPoints[i], pathPoints[0]);
                    }
                }
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2);
        }

        // 网络变量变更回调（仅客户端执行）
        private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
        {
            if (!IsHost) // 纯客户端才需要日志
            {
               
            }
        }

        private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
        {
            // 旋转同步回调
        }

        private void OnFleeingChanged(bool oldValue, bool newValue)
        {
            if (IsClient)
            {
                isFleeing = newValue;
            }
        }

        private void OnFleeTimerChanged(float oldValue, float newValue)
        {
            if (IsClient)
            {
                fleeTimer = newValue;
            }
        }

        private void OnPathIndexChanged(int oldValue, int newValue)
        {
            if (IsClient)
            {
                currentPathIndex = newValue;
            }
        }

        private void OnPathPointsChanged(NetworkListEvent<Vector3> changeEvent)
        {
            if (IsClient)
            {
                pathPoints = new Vector3[networkPathPoints.Count];
                for (int i = 0; i < networkPathPoints.Count; i++)
                {
                    pathPoints[i] = networkPathPoints[i];
                }
                currentPathIndex = 0;

                if (pathPoints.Length > 0)
                {
                    currentVelocity = (pathPoints[0] - transform.position).normalized * normalSpeed;
                }
              
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                networkPosition.OnValueChanged -= OnPositionChanged;
                networkRotation.OnValueChanged -= OnRotationChanged;
                networkIsFleeing.OnValueChanged -= OnFleeingChanged;
                networkFleeTimer.OnValueChanged -= OnFleeTimerChanged;
                networkPathIndex.OnValueChanged -= OnPathIndexChanged;
                networkPathPoints.OnListChanged -= OnPathPointsChanged;
                CancelInvoke(nameof(FindPlayers));
            }

            // 新增：释放 NetworkList
            if (networkPathPoints!=null)
            {
                networkPathPoints.Dispose();
            }
        }
    }
}
