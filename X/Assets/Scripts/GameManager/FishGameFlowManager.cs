using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 3D大鱼吃小鱼 游戏流程总控制器（单例场景+AI画板版）
/// </summary>
public class FishGameFlowManager : MonoBehaviour
{
    // 单例实例
    public static FishGameFlowManager Instance { get; private set; }

    #region 状态定义
    public enum GameState
    {
        MainMenu,       // 主界面
        PrepareStage,   // 准备阶段（包含AI画板+预览）
        GamePlaying,    // 游戏进行中
        GamePaused,     // 游戏暂停
        GameOver        // 游戏结束
    }
    public GameState CurrentState { get; private set; }
    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;
    #endregion

    #region 外部引用
    [Header("玩家出生点")]
    public Transform playerSpawnPoint;

    [Header("主界面UI")]
    public GameObject mainMenuUI;
    public Button btnStartGame;
    public Button btnSettings;
    public Button btnDevelopers;

    [Header("准备阶段UI（画板+预览）")]
    public GameObject prepareStageUI;
    public GameObject drawingBoard;
    public Button btnGenerate;
    public TMP_Text tipText;
    public Image imagePreview;
    public GameObject modelPreview;
    public Button btnEnterGame;  // 点击此按钮时才创建新玩家

    [Header("游戏中UI")]
    public GameObject inGameUI;
    public Button btnPause;

    [Header("暂停界面UI")]
    public GameObject pauseUI;
    public Button btnResume;
    public Button btnReturnMenu;

    [Header("结算界面UI")]
    public GameObject gameOverUI;
    public Button btnRestart;  // 重新开始时创建新玩家
    public Button btnQuit;

    [Header("设置面板UI")]
    public GameObject SettingsUI;

    [Header("开发人员UI")]
    public GameObject DevelepersUI;

    [Header("玩家小鱼")]
    public GameObject fishPrefab;
    public GameObject playerFish;
    #endregion

    #region 初始化
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保在场景切换时不被销毁
        }
        else
        {
            Destroy(gameObject); // 如果已有实例，销毁重复的实例
        }
    }

    private void Start()
    {
        HideAllUI();
        SwitchToState(GameState.MainMenu);
        BindUIEvents();
        CheckDrawingBoardEmpty();
        imagePreview.gameObject.SetActive(false);
        modelPreview.SetActive(false);
        btnEnterGame.gameObject.SetActive(false);
        BindInputEvents();
    }

    private void HideAllUI()
    {
        mainMenuUI.SetActive(true);
        prepareStageUI.SetActive(false);
        inGameUI.SetActive(false);
        pauseUI.SetActive(false);
        gameOverUI.SetActive(false);
    }
    #endregion

    #region 状态切换
    public void SwitchToState(GameState targetState)
    {
        ExitCurrentState();
        CurrentState = targetState;
        OnGameStateChanged?.Invoke(targetState);
        EnterTargetState(targetState);
    }

    private void ExitCurrentState()
    {
        switch (CurrentState)
        {
            case GameState.MainMenu:
                mainMenuUI.SetActive(false);
                break;
            case GameState.PrepareStage:
                ScreenEffects.Instance.StartCoroutine(ScreenEffects.Instance.LoadScene(0)); // 返回主菜单场景
                break;
            case GameState.GamePlaying:
                inGameUI.SetActive(false);
                break;
            case GameState.GamePaused:
                pauseUI.SetActive(false);
                break;
            case GameState.GameOver:
                gameOverUI.SetActive(false);
                break;  // 游戏结束时不销毁玩家，留到重新开始时处理
        }
    }

    private void EnterTargetState(GameState targetState)
    {
        switch (targetState)
        {
            case GameState.MainMenu:
                mainMenuUI.SetActive(true);
                if (playerFish != null)
                {
                    Destroy(playerFish);
                    playerFish = null;
                }
                
                break;
            case GameState.PrepareStage:
                ScreenEffects.Instance.StartCoroutine(ScreenEffects.Instance.LoadScene(1)); // 加载准备场景

                break;
            case GameState.GamePlaying:
                inGameUI.SetActive(true);
                Time.timeScale = 1f;
                // 进入游戏时不自动创建玩家，仅在明确触发时创建
                break;
            case GameState.GamePaused:
                pauseUI.SetActive(true);
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                gameOverUI.SetActive(true);
                Time.timeScale = 1f;
                break;
        }
    }
    #endregion

    #region 输入事件绑定
    private void BindInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnESCPressed += PauseGame;
        }
        else
        {
            Debug.LogError("InputManager.Instance is null! 请检查InputManager的单例初始化逻辑");
        }
    }
    #endregion

    #region UI事件绑定
    private void BindUIEvents()
    {
        // 主界面
        btnStartGame.onClick.AddListener(() => SwitchToState(GameState.PrepareStage));
        btnSettings.onClick.AddListener(OpenSettingsPanel);
        btnDevelopers.onClick.AddListener(OpenDevelopersPanel);

        // 准备阶段
        btnGenerate.onClick.RemoveAllListeners();
        btnGenerate.onClick.AddListener(TryGeneratePreview);
        var drawingBoardScript = drawingBoard.GetComponent<DrawingBoard>();
        if (drawingBoardScript != null)
        {
            drawingBoardScript.OnDrawingChanged += CheckDrawingBoardEmpty;
        }

        // 关键修改：点击"进入游戏"按钮时，强制创建新玩家（先销毁旧的）
        btnEnterGame.onClick.AddListener(() =>
        {
            CreateNewPlayerFish();  // 明确创建新玩家
            SwitchToState(GameState.GamePlaying);
        });

        // 游戏中
        btnPause.onClick.AddListener(() => SwitchToState(GameState.GamePaused));

        // 暂停界面
        btnResume.onClick.AddListener(() => SwitchToState(GameState.GamePlaying));
        btnReturnMenu.onClick.AddListener(() => SwitchToState(GameState.MainMenu));

        // 结算界面：重新开始时创建新玩家
        btnRestart.onClick.AddListener(() =>
        {
            CreateNewPlayerFish();  // 明确创建新玩家
            SwitchToState(GameState.GamePlaying);
        });
        btnQuit.onClick.AddListener(QuitGame);
    }

    private void OpenSettingsPanel()
    {
        Debug.Log("打开设置面板");
        SettingsUI.SetActive(true);

    }
    private void OpenDevelopersPanel()
    {
        Debug.Log("打开开发人员面板");
        DevelepersUI.SetActive(true);
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region AI画板核心逻辑
    private void CheckDrawingBoardEmpty()
    {
        var drawingBoardScript = drawingBoard.GetComponent<DrawingBoard>();
        bool isEmpty = false; // 调试模式：强制有内容
        btnGenerate.interactable = !isEmpty;
        tipText.gameObject.SetActive(isEmpty);
        tipText.text = "画板上还没有任何东西噢！";
    }

    private void TryGeneratePreview()
    {
        var drawingBoardScript = drawingBoard.GetComponent<DrawingBoard>();
        if (drawingBoardScript == null) return;

        bool isEmpty = drawingBoardScript.GetDrawingCount() == 0;
        if (isEmpty)
        {
            tipText.text = "画板上还没有任何东西噢！";
            tipText.gameObject.SetActive(true);
            return;
        }

        GenerateFishPreview();
        imagePreview.gameObject.SetActive(true);
        modelPreview.gameObject.SetActive(true);
        btnEnterGame.gameObject.SetActive(true);
        tipText.gameObject.SetActive(false);
    }

    private void GenerateFishPreview()
    {
        // 生成预览逻辑
    }
    #endregion

    #region 游戏核心逻辑
    /// <summary>
    /// 强制创建新玩家（先销毁旧玩家）
    /// 仅在点击"进入游戏"或"重新开始"时调用
    /// </summary>
    private void CreateNewPlayerFish()
    {
        // 先销毁已存在的玩家
        if (playerFish != null)
        {
            Destroy(playerFish);
            playerFish = null;
        }

        // 再创建新玩家
        if (fishPrefab == null)
        {
            Debug.LogError("请赋值小鱼预制体！");
            return;
        }
        playerFish = Instantiate(fishPrefab, playerSpawnPoint.position, Quaternion.identity);
        
        // 相机跟随逻辑
        ThirdPersonCamera target = FindObjectOfType<ThirdPersonCamera>();
        GameObject cameraObj = GameObject.FindWithTag("ThirdPersonCamera");
        if (cameraObj != null && target != null)
        {  
            target.SetCameraTarget(playerFish.transform.GetChild(1).gameObject, cameraObj);
        }
        else
        {
            Debug.LogError("场景中找不到ThirdPersonCamera组件！");
        }
    }

    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GamePlaying)
        {
            SwitchToState(GameState.GameOver);
        }
    }
    #endregion
    
    // 游戏没开始时回到主界面
    public void BackToMainMenu(GameObject obj)
    {
       obj.SetActive(false);
       SwitchToState(GameState.MainMenu);
    }

    #region 应用焦点处理
    // private void OnApplicationFocus(bool hasFocus)
    // {
    //     if (CurrentState == GameState.GamePlaying && !hasFocus)
    //     {
    //         SwitchToState(GameState.GamePaused);
    //     }
    // }

    private void OnDestroy() => Time.timeScale = 1f;
    #endregion

    #region 暂停功能
    public void PauseGame(bool isPaused)
    {
        if (isPaused && CurrentState == GameState.GamePlaying)
        {
            SwitchToState(GameState.GamePaused);
            Debug.Log("游戏已暂停");
        }
    }
    #endregion
    
}