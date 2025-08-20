using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class NetworkGameManager : NetworkBehaviour
{
    // 游戏状态枚举
    public enum GameState
    {
        Preparation,   // 准备生成阶段
        WaitingPlayers,// 等候其他玩家加入状态
        InGame,        // 游戏进行中
        GameOver       // 游戏结束
    }

    // 网络同步的当前游戏状态（服务器权威）
    private NetworkVariable<GameState> _currentState = new NetworkVariable<GameState>(
        GameState.Preparation,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 状态切换事件（客户端/服务器分别处理）
    [Header("服务器事件")]
    public UnityEvent OnServerPreparationStart;
    public UnityEvent OnServerWaitingPlayersStart;
    public UnityEvent OnServerGameStart;
    public UnityEvent OnServerGameOver;

    [Header("客户端事件")]
    public UnityEvent OnClientPreparationStart;
    public UnityEvent OnClientWaitingPlayersStart;
    public UnityEvent OnClientGameStart;
    public UnityEvent OnClientGameOver;

    // 单例实例（每个客户端/服务器实例一个）
    public static NetworkGameManager Instance { get; private set; }

    // 玩家相关设置
    [Header("玩家设置")]
    public int minPlayersToStart = 2;
    private NetworkVariable<int> _currentPlayerCount = new NetworkVariable<int>(0);

    // 计时设置
    [Header("计时设置")]
    public float preparationTime = 5f;
    public float waitingTimeOut = 60f;
    private float _currentTimer;

    private void Awake()
    {
        // 单例初始化（每个进程一个实例）
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        // 只在服务器上初始化状态
        if (IsServer)
        {
            _currentState.Value = GameState.Preparation;
            _currentTimer = preparationTime;

            // 监听玩家连接状态
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // 所有端监听状态变化
        _currentState.OnValueChanged += OnGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        _currentState.OnValueChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        // 只有服务器能处理状态逻辑
        if (!IsServer) return;

        switch (_currentState.Value)
        {
            case GameState.Preparation:
                HandlePreparationState();
                break;
            case GameState.WaitingPlayers:
                HandleWaitingPlayersState();
                break;
            case GameState.InGame:
                HandleInGameState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
        }
    }

    // 状态变化回调（所有端都会触发）
    private void OnGameStateChanged(GameState previous, GameState current)
    {
        Debug.Log($"游戏状态切换为: {current}");

        // 服务器端事件
        if (IsServer)
        {
            switch (current)
            {
                case GameState.Preparation:
                    OnServerPreparationStart?.Invoke();
                    _currentTimer = preparationTime;
                    break;
                case GameState.WaitingPlayers:
                    OnServerWaitingPlayersStart?.Invoke();
                    _currentTimer = waitingTimeOut;
                    break;
                case GameState.InGame:
                    OnServerGameStart?.Invoke();
                    break;
                case GameState.GameOver:
                    OnServerGameOver?.Invoke();
                    break;
            }
        }

        // 客户端事件（包括主机客户端）
        if (IsClient)
        {
            switch (current)
            {
                case GameState.Preparation:
                    OnClientPreparationStart?.Invoke();
                    break;
                case GameState.WaitingPlayers:
                    OnClientWaitingPlayersStart?.Invoke();
                    break;
                case GameState.InGame:
                    OnClientGameStart?.Invoke();
                    break;
                case GameState.GameOver:
                    OnClientGameOver?.Invoke();
                    break;
            }
        }
    }

    // 服务器：设置游戏状态（权威控制）
    [ServerRpc(RequireOwnership = false)]
    public void SetGameStateServerRpc(GameState newState)
    {
        _currentState.Value = newState;
    }

    // 准备阶段逻辑（服务器）
    private void HandlePreparationState()
    {
        _currentTimer -= Time.deltaTime;

        if (_currentTimer <= 0)
        {
            SetGameStateServerRpc(GameState.WaitingPlayers);
        }
    }

    // 等待玩家逻辑（服务器）
    private void HandleWaitingPlayersState()
    {
        _currentTimer -= Time.deltaTime;

        // 检查是否满足最小玩家数
        if (_currentPlayerCount.Value >= minPlayersToStart)
        {
            SetGameStateServerRpc(GameState.InGame);
        }

        // 等待超时
        if (_currentTimer <= 0)
        {
            Debug.LogWarning("玩家等待超时，强制开始游戏");
            SetGameStateServerRpc(GameState.InGame);
        }
    }

    // 游戏中逻辑（服务器）
    private void HandleInGameState()
    {
        // 游戏逻辑（如检查胜利/失败条件）
        // 示例：if (CheckGameOverCondition()) SetGameStateServerRpc(GameState.GameOver);
    }

    // 游戏结束逻辑（服务器）
    private void HandleGameOverState()
    {
        // 游戏结束处理（如统计结果、倒计时返回大厅等）
    }

    // 玩家连接回调（服务器）
    private void OnClientConnected(ulong clientId)
    {
        _currentPlayerCount.Value++;
        Debug.Log($"玩家 {clientId} 加入，当前玩家数: {_currentPlayerCount.Value}");
    }

    // 玩家断开连接回调（服务器）
    private void OnClientDisconnected(ulong clientId)
    {
        _currentPlayerCount.Value = Mathf.Max(0, _currentPlayerCount.Value - 1);
        Debug.Log($"玩家 {clientId} 离开，当前玩家数: {_currentPlayerCount.Value}");

        // 如果游戏中玩家数不足，结束游戏
        if (_currentState.Value == GameState.InGame && _currentPlayerCount.Value < 1)
        {
            SetGameStateServerRpc(GameState.GameOver);
        }
    }

    // 客户端：获取当前状态（本地缓存）
    public GameState GetCurrentState()
    {
        return _currentState.Value;
    }

    // 客户端：获取剩余时间（需从服务器同步，这里简化处理）
    public float GetRemainingTime()
    {
        return _currentTimer;
    }

    // 客户端：请求结束游戏（需服务器验证）
    public void RequestGameOver()
    {
        if (IsServer)
        {
            SetGameStateServerRpc(GameState.GameOver);
        }
        else
        {
            // 客户端只能发送请求，由服务器决定是否结束
            Debug.LogWarning("客户端不能直接结束游戏，需通过服务器验证");
        }
    }
}
