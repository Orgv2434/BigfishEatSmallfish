using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 3D大鱼吃小鱼 游戏流程总控制器（单例场景+AI画板版）
/// 集成AI画板生成小鱼功能，支持图片和模型双预览
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
    #endregion

    #region 外部引用
    // 玩家出生地点
    [Header("玩家出生点")]
    public Transform playerSpawnPoint;
    // 主界面UI
    [Header("主界面UI")]
    public GameObject mainMenuUI;
    public Button btnStartGame;
    public Button btnSettings;
    public Button btnDevelopers;

// 准备阶段UI（合并画板和预览UI）
[Header("准备阶段UI（画板+预览）")]
public GameObject prepareStageUI;
public GameObject drawingBoard;        // 画板对象
public Button btnGenerate;             // 生成按钮
public TMP_Text tipText;               // 提示文本
public Image imagePreview;          // 图片预览（在准备阶段显示）
public GameObject modelPreview;        // 模型预览（在准备阶段显示）
public Button btnEnterGame;            // 进入游戏按钮（生成后显示）

    // 游戏中UI
    [Header("游戏中UI")]
    public GameObject inGameUI;
    public Button btnPause;


    // 暂停界面UI
    [Header("暂停界面UI")]
    public GameObject pauseUI;
    public Button btnResume;
    public Button btnReturnMenu;

    // 结算界面UI
    [Header("结算界面UI")]
    public GameObject gameOverUI;
    public Button btnRestart;
    public Button btnQuit;

    // 玩家小鱼
    [Header("玩家小鱼")]
    public GameObject fishPrefab;
    private GameObject playerFish;
    private int currentScore = 0;
    #endregion

    #region 初始化
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

private void Start()
{
    HideAllUI();
    SwitchToState(GameState.MainMenu);
    BindUIEvents();
    CheckDrawingBoardEmpty();
    // 初始隐藏预览和进入按钮
    imagePreview.gameObject.SetActive(false);
    modelPreview.SetActive(false);
    btnEnterGame.gameObject.SetActive(false);
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
        EnterTargetState(targetState);
    }

    private void ExitCurrentState()
    {
        switch (CurrentState)
        {
            case GameState.MainMenu:
                mainMenuUI.SetActive(false);
                break;
  // 移除PreviewStage的退出逻辑
        case GameState.PrepareStage:
            prepareStageUI.SetActive(false);
            break;
            case GameState.GamePlaying:
                inGameUI.SetActive(false);
                break;
            case GameState.GamePaused:
                pauseUI.SetActive(false);
                break;
            case GameState.GameOver:
                gameOverUI.SetActive(false);
                if (playerFish != null) Destroy(playerFish);
                playerFish = null;
                break;
        }
    }

    private void EnterTargetState(GameState targetState)
    {
        switch (targetState)
        {
            case GameState.MainMenu:
                mainMenuUI.SetActive(true);
                break;
    case GameState.PrepareStage:
            prepareStageUI.SetActive(true);

            // 进入准备阶段时仅显示画板，隐藏预览
            drawingBoard.SetActive(true);
            imagePreview.gameObject.SetActive(false);
            modelPreview.SetActive(false);
            btnEnterGame.gameObject.SetActive(false);
            break;
            case GameState.GamePlaying:
                inGameUI.SetActive(true);
                Time.timeScale = 1f;
                if (playerFish == null)
                {
                    CreatePlayerFish();
                    currentScore = 0;
                }
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

    #region UI事件绑定
    private void BindUIEvents()
    {
        // 主界面
        btnStartGame.onClick.AddListener(() => SwitchToState(GameState.PrepareStage));
        btnSettings.onClick.AddListener(OpenSettingsPanel);
        btnDevelopers.onClick.AddListener(OpenDevelopersPanel);

        // 准备阶段（AI画板）
            btnGenerate.onClick.RemoveAllListeners(); // 移除重复绑定
        btnGenerate.onClick.AddListener(TryGeneratePreview);
    // 绑定画板变化事件（修复语法错误）
    var drawingBoardScript = drawingBoard.GetComponent<DrawingBoard>();
    if (drawingBoardScript != null)
    {
        drawingBoardScript.OnDrawingChanged += CheckDrawingBoardEmpty;
    }

    // 准备阶段的进入游戏按钮
    btnEnterGame.onClick.AddListener(() => SwitchToState(GameState.GamePlaying));

        // 游戏中
        btnPause.onClick.AddListener(() => SwitchToState(GameState.GamePaused));

        // 暂停界面
        btnResume.onClick.AddListener(() => SwitchToState(GameState.GamePlaying));
        btnReturnMenu.onClick.AddListener(() => SwitchToState(GameState.MainMenu));

        // 结算界面
        btnRestart.onClick.AddListener(() => SwitchToState(GameState.GamePlaying));
        btnQuit.onClick.AddListener(QuitGame);
    }

    private void OpenSettingsPanel() => Debug.Log("打开设置面板");
    private void OpenDevelopersPanel() => Debug.Log("打开开发人员面板");
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region AI画板核心逻辑
    /// <summary>
    /// 检查画板是否为空，控制生成按钮状态
    /// </summary>
private void CheckDrawingBoardEmpty()
{
    var drawingBoardScript = drawingBoard.GetComponent<DrawingBoard>();
    
    // 调试模式：强制默认有内容（忽略实际画板状态）
    bool isEmpty = false; // 强制设为false，即默认有东西
    
    // （如果需要临时测试空状态，可注释上面一行，启用下面的逻辑）
    // if (drawingBoardScript != null)
    // {
    //     isEmpty = drawingBoardScript.GetDrawingCount() == 0;
    // }
    // else
    // {
    //     isEmpty = false; // 没有画板脚本时也默认有内容
    // }
    
    btnGenerate.interactable = !isEmpty; // 生成按钮可点击
    tipText.gameObject.SetActive(isEmpty); // 不显示空提示
    tipText.text = "画板上还没有任何东西噢！";
}
    /// <summary>
    /// 尝试生成预览（先校验画板状态）
    /// </summary>
private void TryGeneratePreview()
{
    var drawingBoardScript = drawingBoard.GetComponent<DrawingBoard>();
    if (drawingBoardScript == null) return;

    // 实际检测画板是否为空
    bool isEmpty = drawingBoardScript.GetDrawingCount() == 0;
    if (isEmpty)
    {
        tipText.text = "画板上还没有任何东西噢！";
        tipText.gameObject.SetActive(true);
        return;
    }

    // 生成预览（在当前阶段内显示）
    GenerateFishPreview();
    // 显示预览和进入按钮，隐藏画板提示
    imagePreview.gameObject.SetActive(true);
    modelPreview.SetActive(true);
    btnEnterGame.gameObject.SetActive(true);
    tipText.gameObject.SetActive(false);
}


    /// <summary>
    /// 生成小鱼的图片和模型预览（对接AI插件逻辑）
    /// </summary>
    private void GenerateFishPreview()
    {
      
    }
    #endregion

    #region 游戏核心逻辑
    private void CreatePlayerFish()
    {
        if (fishPrefab == null)
        {
            Debug.LogError("请赋值小鱼预制体！");
            return;
        }
        if (playerFish != null) Destroy(playerFish);
        playerFish = Instantiate(fishPrefab, playerSpawnPoint.position, Quaternion.identity);
        playerFish.AddComponent<PlayerFishController>();
    }



    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GamePlaying)
        {
            SwitchToState(GameState.GameOver);
        }
    }
    #endregion

    #region 应用焦点处理
    private void OnApplicationFocus(bool hasFocus)
    {
        if (CurrentState == GameState.GamePlaying && !hasFocus)
        {
            SwitchToState(GameState.GamePaused);
        }
    }

    private void OnDestroy() => Time.timeScale = 1f;
    #endregion
}

// 玩家小鱼控制脚本（需单独创建）
public class PlayerFishController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 15f;

    private Camera mainCamera;

    private void Start() => mainCamera = Camera.main;

    private void Update()
    {
        if (FishGameFlowManager.Instance.CurrentState != FishGameFlowManager.GameState.GamePlaying)
            return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDir = new Vector3(horizontal, 0, vertical).normalized;

        if (mainCamera != null && moveDir.magnitude > 0.1f)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            moveDir = (camForward * vertical + camRight * horizontal).normalized;
        }

        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }

}