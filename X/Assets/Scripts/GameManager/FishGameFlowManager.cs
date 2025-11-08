using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TripoForUnity;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using GLTFast;
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
     [Header("基础设置")]
    public GameObject fishPrefab; // 已有的小鱼预制体（测试模式用）
    public Transform playerSpawnPoint;
    public GameObject playerFish;

    [Header("材质和模型设置")]
    public Material playerMaterial; // 玩家材质

    private string generatedModelPath; // 生成的模型路径（非测试模式用）
    private GameObject generatedModel; // 加载的生成模型（非测试模式用）


    public event GameStateChanged OnGameStateChanged;
    #endregion

    #region 测试设置
    [Header("=== 测试设置 ===")]
    [Tooltip("启用调试模式，直接进入游戏")]
    public bool isDebugMode = false; // 调试模式开关
    #endregion
    
    #region 外部引用

    [Header("主界面UI")]
    public GameObject mainMenuUI;
    public Button btnStartGame;
    public Button btnSettings;
    public Button btnDevelopers;


    private Button btnEnterGame;  // 点击此按钮时才创建新玩家

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
        BindInputEvents();
    }

    private void HideAllUI()
    {
        mainMenuUI.SetActive(true);
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
            if (isDebugMode)
                {
                    break;
                }
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
                if (isDebugMode)
                {
                    // 调试模式：直接创建玩家并进入游戏
                    CreateNewPlayerFish();
                    SwitchToState(GameState.GamePlaying);
                    return;
                }
                else // 非调试模式
                {

                    ScreenEffects.Instance.StartCoroutine(ScreenEffects.Instance.LoadScene(1)); // 加载准备场景
                    break;
                }

            case GameState.GamePlaying:
                if (inGameUI != null)
                    inGameUI.SetActive(true);
                else inGameUI = GameObject.Find("InGameUI");
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
        if (SettingsUI != null)
        {
            SettingsUI.SetActive(true);
        }
        else
        {
            Debug.LogError("SettingUI未找到！");
        }
    }


    private void OpenDevelopersPanel()
    {
        if (DevelepersUI != null)
        {
            DevelepersUI.SetActive(true);
        }
        else
        {
            Debug.LogError("DevelepersUI未找到！");
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 模型生成完成时的回调（由ModelHandler调用）
    public void OnModelGenerated(string modelPath)
    {
        btnEnterGame.gameObject.SetActive(true);
        if (!isDebugMode) // 非测试模式才保存生成的模型路径
        {
            generatedModelPath = modelPath;
            Debug.Log("非测试模式：已接收生成模型路径");

            // 加载生成的模型并处理材质
            StartCoroutine(ProcessGeneratedModel());
        }
    }
    private IEnumerator ProcessGeneratedModel()
    {
        yield return null;

        ModelHandler modelHandler = FindObjectOfType<ModelHandler>();
        if (modelHandler == null)
        {
            Debug.LogError("找不到ModelHandler实例！");
            yield break;
        }

        string fullModelPath = modelHandler.GetLastDownloadedModelPath();
        if (string.IsNullOrEmpty(fullModelPath) || !File.Exists(fullModelPath))
        {
            Debug.LogError($"模型文件不存在: {fullModelPath}");
            yield break;
        }

        // 关键修改：替换AssetBundle加载方式，改用GLTFast（使用 GltfImport 并在协程中等待 Task 完成）
        // 需要确保已在项目中安装并导入 GLTFast 包
        var gltf = new GLTFast.GltfImport();

        // Load 接受文件路径或 URL，使用 file:// 前缀以确保本地文件加载
        var loadTask = gltf.Load("file://" + fullModelPath);
        // 等待 Task 完成
        yield return new WaitUntil(() => loadTask.IsCompleted);

        // 检查加载结果
        if (!loadTask.Result)
        {
            Debug.LogError($"GLB模型加载失败: {fullModelPath}");
            yield break;
        }

        // 实例化主场景到一个新的根对象上
        GameObject root = new GameObject("GLB_Root");
        var instantiateTask = gltf.InstantiateMainSceneAsync(root.transform);
        yield return new WaitUntil(() => instantiateTask.IsCompleted);

        if (instantiateTask.IsFaulted || !instantiateTask.Result)
        {
            Debug.LogError("GLTF 模型实例化失败");
            Destroy(root);
            yield break;
        }

        // 获取生成的模型实例
        generatedModel = root;
        generatedModel.transform.SetParent(null);
        generatedModel.SetActive(false);

        // 后续材质处理逻辑不变（保持原代码）
        if (generatedModel.transform.childCount == 0)
        {
            Debug.LogError("生成的模型没有子物体！");
            yield break;
        }

        Transform firstChild = generatedModel.transform.GetChild(0);
        Renderer childRenderer = firstChild.GetComponent<Renderer>();

        if (childRenderer != null && childRenderer.material != null && childRenderer.material.mainTexture != null && playerMaterial != null)
        {
            playerMaterial.mainTexture = childRenderer.material.mainTexture;
            Debug.Log("已将模型子物体的基础贴图应用到playerMaterial");
        }
        else
        {
            Debug.LogWarning("第一个子物体没有有效的基础贴图，使用默认材质");
        }

        Renderer modelRenderer = generatedModel.GetComponent<Renderer>();
        if (modelRenderer != null && playerMaterial != null)
        {
            modelRenderer.material = playerMaterial;
            Debug.Log("已将playerMaterial应用到生成的模型");
        }
        else
        {
            Debug.LogWarning("模型没有Renderer组件或playerMaterial未赋值，无法应用材质");
        }

        fishPrefab = generatedModel;
        Debug.Log("已将生成的模型设置为fishPrefab");
    }

    // 新增：场景加载完成后重新获取当前场景引用（核心修改2）
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 主场景（索引0）加载后，重新获取主场景UI引用
        if (scene.buildIndex == 0)
        {
            GameObject mainUIParent = GameObject.Find("MainMenuUI");
            if (mainUIParent != null)
            {
                mainMenuUI = mainUIParent;
                btnStartGame = mainUIParent.transform.Find("main_Start").GetComponent<Button>(); // 原BtnStartGame改为Start
                btnSettings = mainUIParent.transform.Find("main_Settings").GetComponent<Button>(); // 原BtnSettings改为Settings
                btnDevelopers = mainUIParent.transform.Find("main_Developers").GetComponent<Button>(); // 原BtnDevelopers改为Developers
                SettingsUI = GameObject.Find("SettingUI"); // 直接查找SettingUI根物体
                DevelepersUI = GameObject.Find("DevelepersUI"); // 直接查找DevelepersUI根物体
                RebindMainMenuEvents();

            }

            // 游戏中UI
            GameObject inGameUIParent = GameObject.Find("InGameUI");
            if (inGameUIParent != null)
            {
                inGameUI = inGameUIParent;
                btnPause = inGameUIParent.transform.Find("game_Pause").GetComponent<Button>(); // 按钮名称改为Pause
            }

            // 暂停界面
            GameObject pauseUIParent = GameObject.Find("PauseUI");
            if (pauseUIParent != null)
            {
                pauseUI = pauseUIParent;
                btnResume = pauseUIParent.transform.Find("Panel/pau_BackGame").GetComponent<Button>(); // 层级为Panel/Resume
                btnReturnMenu = pauseUIParent.transform.Find("Panel/pau_BackMainMenu").GetComponent<Button>(); // 层级为Panel/ReturnMenu
            }

            // 结算界面
            GameObject endUIParent = GameObject.Find("EndUI");
            if (endUIParent != null)
            {
                gameOverUI = endUIParent;
                btnRestart = endUIParent.transform.Find("end_Restart").GetComponent<Button>(); // 第一个Button为重新开始
                btnQuit = endUIParent.transform.Find("end_BackMainMenu").GetComponent<Button>(); // 第二个Button为退出
            }

            // 玩家生成点
            GameObject spawnPointObj = GameObject.Find("PlayerSpawnPoint");
            if (spawnPointObj != null)
            {
                playerSpawnPoint = spawnPointObj.transform;
            }
            // 重新绑定主界面事件，确保引用有效
            RebindMainMenuEvents();
            CreateNewPlayerFish(); // 重新创建玩家鱼

        }
        else if(scene.buildIndex == 1)
        {
           OnLoadPrepareScene();
        }

    } 
    private void OnDestroy()
    {
        Time.timeScale = 1f;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    // 新增：重新绑定主界面事件（避免引用失效）（核心修改6）
    private void RebindMainMenuEvents()
    {
        if (btnStartGame != null)
        {
            btnStartGame.onClick.RemoveAllListeners();
            btnStartGame.onClick.AddListener(() => SwitchToState(GameState.PrepareStage));
        }
        if (btnSettings != null)
        {
            btnSettings.onClick.RemoveAllListeners();
            btnSettings.onClick.AddListener(OpenSettingsPanel);
        }
        if (btnDevelopers != null)
        {
            btnDevelopers.onClick.RemoveAllListeners();
            btnDevelopers.onClick.AddListener(OpenDevelopersPanel);
        }
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

        // 如果是生成的模型，使用它创建玩家
        playerFish = Instantiate(fishPrefab, playerSpawnPoint.position, Quaternion.identity);
        playerFish.SetActive(true); // 激活模型

        // 相机跟随逻辑保持不变
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

    // 加载完准备场景绑定
    public void OnLoadPrepareScene()
    {
        GameObject parentObj = GameObject.Find("CanvasDrawing"); // 父物体必须激活
        if (parentObj == null)
        {
            Debug.LogError("找不到父物体Canvas/PrepareUI");
            return;
        }

        // 查找子物体（即使隐藏也能找到）
        Transform readyBtnTrans = parentObj.transform.Find("ReadyButton");
        if (readyBtnTrans == null)
        {
            Debug.LogError("父物体下找不到ReadyButton");
            return;
        }
        btnEnterGame = readyBtnTrans.GetComponent<Button>();
        if (btnEnterGame == null)
        {
            Debug.LogError("ReadyButton 上没有 Button 组件！");
            return;
        }

        btnEnterGame.onClick.AddListener(() =>
        {
            SwitchToState(GameState.GamePlaying);
        });

        TripoRuntimeCore tripoCore = FindObjectOfType<TripoRuntimeCore>();
        if (tripoCore == null)
        {
            Debug.LogError("找不到对应的 tripoCore！");
            return;
        }
        tripoCore.OnModelGenerateComplete.AddListener(OnModelGenerated);

    }
    
    
    #region 应用焦点处理
    // private void OnApplicationFocus(bool hasFocus)
    // {
    //     if (CurrentState == GameState.GamePlaying && !hasFocus)
    //     {
    //         SwitchToState(GameState.GamePaused);
    //     }
    // }
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