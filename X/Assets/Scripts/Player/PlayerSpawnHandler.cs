using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace DistantLands
{
    public class PlayerSpawnHandler : NetworkBehaviour
    {
        [Header("输入配置")]
        [SerializeField] private PlayerInput playerInput; // 引用玩家预制体上的PlayerInput组件
        [SerializeField] private string defaultControlScheme = "Keyboard&Mouse"; // 与Input Action配置一致的控制方案名

        [Header("自定义出生点配置")]
        [SerializeField] private List<Transform> customSpawnPoints;
        [SerializeField] private bool useCustomSpawnPoints = true;
        [SerializeField] private bool useRandomSpawn = true;
        [SerializeField] private float spawnPointCheckRadius = 0.1f;
        [SerializeField] private bool debugMode = true;
        [SerializeField] private float fallbackMapSize = 20f;
        [SerializeField] private LayerMask groundLayer;

        private static int nextSpawnIndex = 0;
        private static HashSet<Vector3Int> usedSpawnPositions = new HashSet<Vector3Int>();
        private static HashSet<ulong> assignedClients = new HashSet<ulong>();
        private Vector3 assignedSpawnPos;
        private Quaternion assignedSpawnRot;
        private NetworkTransform cachedNetworkTransform;
        private NetworkObject cachedNetworkObject;
        private bool isPositionAssigned = false;
        public event System.Action OnSpawnPositionApplied;
        private CharacterController cachedCharacterController;

        private void Awake()
        {
            cachedNetworkTransform = GetComponent<NetworkTransform>();
            cachedNetworkObject = GetComponent<NetworkObject>();
            cachedCharacterController = GetComponent<CharacterController>();

            // 自动获取PlayerInput组件（如果未在Inspector中指定）
            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>();

            SceneManager.sceneLoaded += OnSceneLoaded;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                LogDebug($"Awake：注册网络回调完成，NetworkManager状态：{NetworkManager.Singleton.IsListening}");
            }
            else
            {
                LogError("Awake：NetworkManager.Singleton为null！请确保场景中存在NetworkManager");
            }
        }

public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();
    LogDebug($"OnNetworkSpawn：当前对象网络状态 - IsServer:{IsServer}，IsClient:{IsClient}，IsOwner:{IsOwner}，ClientId:{OwnerClientId}");

    // 对于主机，只执行一次生成逻辑
    if (IsServer && IsOwner)
    {
        LogDebug($"OnNetworkSpawn：服务器（主机）自身初始化，分配出生点");
        if (!isPositionAssigned)
            AssignSpawnPoint();
    }
    // 确保客户端逻辑只在非服务器端执行
    else if (IsClient && IsOwner && !IsServer)
    {
        LogDebug($"OnNetworkSpawn：客户端初始化，发送备用出生点请求（ServerRpc）");
        RequestSpawnPositionServerRpc();
    }
}

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer)
            {
                LogDebug($"OnClientConnected：非服务器，跳过处理，ClientId:{clientId}");
                return;
            }

            if (clientId == NetworkManager.ServerClientId || assignedClients.Contains(clientId))
            {
                LogDebug($"OnClientConnected：客户端是服务器自身或已分配，跳过处理，ClientId:{clientId}");
                return;
            }

            LogDebug($"OnClientConnected：延迟加入客户端连接，ClientId:{clientId}，服务器主动分配出生点");
            AssignSpawnPoint(clientId);
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;
            LogDebug($"OnClientDisconnected：客户端断开，ClientId:{clientId}，释放出生点");

            assignedClients.Remove(clientId);

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
            {
                var playerObject = networkClient.PlayerObject;
                if (playerObject == null)
                {
                    LogError($"OnClientDisconnected：ClientId:{clientId} 的PlayerObject为null，无法释放出生点");
                    return;
                }

                var spawnHandler = playerObject.GetComponent<PlayerSpawnHandler>();
                if (spawnHandler == null)
                {
                    LogError($"OnClientDisconnected：ClientId:{clientId} 的PlayerObject缺少PlayerSpawnHandler组件");
                    return;
                }

                var precisePos = ToPreciseVector(spawnHandler.assignedSpawnPos);
                if (usedSpawnPositions.Contains(precisePos))
                {
                    usedSpawnPositions.Remove(precisePos);
                    LogDebug($"OnClientDisconnected：ClientId:{clientId} 释放出生点（原位置：{spawnHandler.assignedSpawnPos}，精度位置：{precisePos}）");
                }
                else
                {
                    LogWarning($"OnClientDisconnected：ClientId:{clientId} 的出生点（{precisePos}）未在已占用列表中");
                }
            }
            else
            {
                LogWarning($"OnClientDisconnected：ClientId:{clientId} 不在连接列表中，无需释放");
            }
        }

        [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
        private void RequestSpawnPositionServerRpc(ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (assignedClients.Contains(clientId))
            {
                LogDebug($"RequestSpawnPositionServerRpc：客户端已分配位置，跳过请求，ClientId:{clientId}");
                return;
            }

            LogDebug($"RequestSpawnPositionServerRpc：收到客户端备用请求，ClientId:{clientId}");
            AssignSpawnPoint(clientId);
        }

        private void AssignSpawnPoint(ulong targetClientId = ulong.MaxValue)
        {
            if (!IsServer)
            {
                LogError($"AssignSpawnPoint：非服务器调用！当前状态IsServer:{IsServer}，ClientId:{targetClientId}");
                return;
            }

            var clientId = targetClientId == ulong.MaxValue ? OwnerClientId : targetClientId;

            if (assignedClients.Contains(clientId))
            {
                LogDebug($"AssignSpawnPoint：客户端已分配位置，跳过，ClientId:{clientId}");
                return;
            }

            LogDebug($"AssignSpawnPoint：开始分配，目标ClientId:{clientId}，是否延迟加入:{targetClientId != ulong.MaxValue}");
            Transform spawnPoint = GetAvailableSpawnPoint(out string spawnSource);

            if (spawnPoint != null)
            {
                assignedSpawnPos = spawnPoint.position;
                assignedSpawnRot = spawnPoint.rotation;
                LogDebug($"AssignSpawnPoint：ClientId:{clientId} 分配成功（来源：{spawnSource}），位置:{assignedSpawnPos}，旋转:{assignedSpawnRot.eulerAngles}");
            }
            else
            {
                assignedSpawnPos = GetFallbackSpawnPosition();
                assignedSpawnRot = Quaternion.identity;
                LogWarning($"AssignSpawnPoint：ClientId:{clientId} 无可用出生点，使用备用逻辑（{assignedSpawnPos}）");
            }

            var precisePos = ToPreciseVector(assignedSpawnPos);
            usedSpawnPositions.Add(precisePos);
            assignedClients.Add(clientId);
            isPositionAssigned = true;
            LogDebug($"AssignSpawnPoint：ClientId:{clientId} 标记已占用出生点（精度位置：{precisePos}），当前已占用数量:{usedSpawnPositions.Count}");

            if (targetClientId != ulong.MaxValue)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var networkClient))
                {
                    var playerObject = networkClient.PlayerObject;
                    if (playerObject != null)
                    {
                        GameObject playerGameObject = playerObject.gameObject;
                        SetPlayerPositionOnServer(playerGameObject, assignedSpawnPos, assignedSpawnRot);
                    }
                    else
                    {
                        LogError($"AssignSpawnPoint：ClientId:{targetClientId} 未找到PlayerObject，服务器端无法预设位置");
                    }
                }
            }
            else
            {
                SetPlayerPositionOnServer(gameObject, assignedSpawnPos, assignedSpawnRot);
            }

            // 同步位置和控制方案到客户端
            SyncPlayerDataClientRpc(assignedSpawnPos, assignedSpawnRot, defaultControlScheme,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { clientId } } });
            LogDebug($"AssignSpawnPoint：ClientId:{clientId} 出生点和控制方案同步完成");
        }

        private void SetPlayerPositionOnServer(GameObject playerObject, Vector3 pos, Quaternion rot)
        {
            if (!IsServer) return;

            var targetNetworkTransform = playerObject.GetComponent<NetworkTransform>();
            if (targetNetworkTransform != null && !targetNetworkTransform.enabled)
            {
                targetNetworkTransform.enabled = true;
                LogDebug($"SetPlayerPositionOnServer：启用NetworkTransform，玩家对象:{playerObject.name}");
            }

            if (targetNetworkTransform != null)
            {
                targetNetworkTransform.Teleport(pos, rot, Vector3.one);
                LogDebug($"SetPlayerPositionOnServer：服务器端Teleport，玩家对象:{playerObject.name}，位置:{pos}");
            }
            else
            {
                playerObject.transform.position = pos;
                playerObject.transform.rotation = rot;
                LogDebug($"SetPlayerPositionOnServer：服务器端直接设置，玩家对象:{playerObject.name}，位置:{pos}");
            }

            var characterController = playerObject.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = true;
                LogDebug($"SetPlayerPositionOnServer：服务器端启用CharacterController，玩家对象:{playerObject.name}");
            }
        }
        [ClientRpc(Delivery = RpcDelivery.Reliable)]
        private void SyncPlayerDataClientRpc(Vector3 position, Quaternion rotation, string controlScheme, ClientRpcParams clientRpcParams = default)
        {
            if (!IsClient || !IsOwner)
            {
                LogDebug($"SyncPlayerDataClientRpc：非本地客户端，跳过应用，ClientId:{NetworkManager.Singleton.LocalClientId}");
                return;
            }

            LogDebug($"SyncPlayerDataClientRpc：客户端接收出生点和控制方案，LocalClientId:{NetworkManager.Singleton.LocalClientId}，位置:{position}，控制方案:{controlScheme}");

            // 应用出生位置
            transform.position = position;
            transform.rotation = rotation;

            try
            {
                if (playerInput != null)
                {
                    // 获取第一个可用的输入设备
                    InputDevice device = InputSystem.devices.FirstOrDefault();
                    if (device != null)
                    {
                        // 手动切换控制方案，确保与输入设备匹配
                        playerInput.SwitchCurrentControlScheme(controlScheme, device);
                        LogDebug($"SyncPlayerDataClientRpc: 客户端切换控制方案为 {controlScheme}，设备: {device.name}");
                    }
                    else
                    {
                        LogWarning("SyncPlayerDataClientRpc: 未找到可用的输入设备");
                    }
                }
                else
                {
                    LogError("SyncPlayerDataClientRpc: PlayerInput组件未找到，请检查玩家预制体！");
                }
            }
            catch (System.Exception e)
            {
                LogError($"SyncPlayerDataClientRpc：处理PlayerInput时发生异常 - {e.Message}");
            }

            try
            {
                // 启用CharacterController
                if (cachedCharacterController != null)
                {
                    cachedCharacterController.enabled = true;
                    LogDebug($"SyncPlayerDataClientRpc：客户端启用CharacterController");
                }
            }
            catch (System.Exception e)
            {
                LogError($"SyncPlayerDataClientRpc：启用CharacterController时发生异常 - {e.Message}");
            }

            try
            {
                var clientNetworkTransform = GetComponent<ClientNetworkTransform>();
                if (clientNetworkTransform != null)
                {
                    clientNetworkTransform.MarkSpawnComplete();
                }
                OnSpawnPositionApplied?.Invoke();
            }
            catch (System.Exception e)
            {
                LogError($"SyncPlayerDataClientRpc：处理ClientNetworkTransform或事件时发生异常 - {e.Message}");
            }
        }
        private Transform GetAvailableSpawnPoint(out string spawnSource)
        {
            spawnSource = "未知";
            if (useCustomSpawnPoints && customSpawnPoints != null && customSpawnPoints.Count > 0)
            {
                var validSpawns = customSpawnPoints
                    .Where(p => p != null)
                    .Where(p => !usedSpawnPositions.Contains(ToPreciseVector(p.position)))
                    .Where(p => !Physics.CheckSphere(p.position, spawnPointCheckRadius, groundLayer))
                    .ToList();

                if (validSpawns.Count > 0)
                {
                    spawnSource = $"自定义出生点（总数:{customSpawnPoints.Count}，可用:{validSpawns.Count}）";
                    return useRandomSpawn ? validSpawns[Random.Range(0, validSpawns.Count)] : GetOrderedSpawnPoint(validSpawns);
                }
                else
                {
                    LogWarning($"GetAvailableSpawnPoint：自定义出生点全部不可用（已占用/有碰撞），总数:{customSpawnPoints.Count}");
                    spawnSource = "自定义出生点（全部不可用）";
                }
            }
            else if (useCustomSpawnPoints)
            {
                LogWarning($"GetAvailableSpawnPoint：启用自定义出生点，但列表为空或未勾选");
                spawnSource = "自定义出生点（列表为空）";
            }

            var defaultSpawns = GameObject.FindGameObjectsWithTag("PlayerStart")
                .Where(go => go != null)
                .Select(go => go.transform)
                .ToList();

            if (defaultSpawns.Count > 0)
            {
                var availableSpawn = defaultSpawns
                    .FirstOrDefault(t => !usedSpawnPositions.Contains(ToPreciseVector(t.position))
                                        && !Physics.CheckSphere(t.position, spawnPointCheckRadius, groundLayer));

                if (availableSpawn != null)
                {
                    spawnSource = $"PlayerStart标签（总数:{defaultSpawns.Count}，可用:1）";
                    return availableSpawn;
                }
                else
                {
                    var randomSpawn = defaultSpawns[Random.Range(0, defaultSpawns.Count)];
                    spawnSource = $"PlayerStart标签（全部占用，随机选择，总数:{defaultSpawns.Count}）";
                    return randomSpawn;
                }
            }
            else
            {
                LogWarning($"GetAvailableSpawnPoint：未找到PlayerStart标签的出生点");
                spawnSource = "PlayerStart标签（无）";
            }

            return null;
        }

        private Vector3 GetFallbackSpawnPosition()
        {
            Vector3 fallbackPos = Vector3.zero;
            int attempts = 0;
            const int maxAttempts = 20;

            while (attempts < maxAttempts)
            {
                fallbackPos = new Vector3(
                    Random.Range(-fallbackMapSize, fallbackMapSize),
                    5f,
                    Random.Range(-fallbackMapSize, fallbackMapSize)
                );

                if (Physics.Raycast(fallbackPos, Vector3.down, out RaycastHit hit, 10f, groundLayer)
                    && !Physics.CheckSphere(hit.point + Vector3.up * 0.5f, spawnPointCheckRadius, groundLayer))
                {
                    return hit.point + Vector3.up * 0.5f;
                }

                attempts++;
            }

            LogError($"GetFallbackSpawnPosition：超过{maxAttempts}次尝试，返回场景中心");
            return new Vector3(0, 0.5f, 0);
        }

        private Transform GetOrderedSpawnPoint(List<Transform> validSpawns)
        {
            var index = nextSpawnIndex % validSpawns.Count;
            var target = validSpawns[index];
            nextSpawnIndex = (nextSpawnIndex + 1) % validSpawns.Count;
            LogDebug($"GetOrderedSpawnPoint：顺序分配，当前索引:{index}，下一次索引:{nextSpawnIndex}");
            return target;
        }

        private Vector3Int ToPreciseVector(Vector3 pos)
        {
            const float precisionMultiplier = 100f;
            return new Vector3Int(
                Mathf.RoundToInt(pos.x * precisionMultiplier),
                Mathf.RoundToInt(pos.y * precisionMultiplier),
                Mathf.RoundToInt(pos.z * precisionMultiplier)
            );
        }

        public void RequestDestroySelf()
        {
            if (IsClient && IsOwner)
            {
                LogDebug($"RequestDestroySelf：客户端请求销毁自己");
                DestroyPlayerServerRpc(cachedNetworkObject.NetworkObjectId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DestroyPlayerServerRpc(ulong networkObjectId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
            {
                LogDebug($"DestroyPlayerServerRpc：服务器销毁玩家对象（ID:{networkObjectId}）");
                networkObject.Despawn();
            }
            else
            {
                LogError($"DestroyPlayerServerRpc：未找到ID为{networkObjectId}的网络对象");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            usedSpawnPositions.Clear();
            assignedClients.Clear();
            nextSpawnIndex = 0;
            LogDebug($"OnSceneLoaded：场景[{scene.name}]加载，重置出生点占用记录");
        }
        private void LogDebug(string message)
        {
            if (debugMode) Debug.Log($"[PlayerSpawn][Debug] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PlayerSpawn][Error] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PlayerSpawn][Warning] {message}");
        }
    }
}