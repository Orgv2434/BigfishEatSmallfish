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
using DistantLands;
using Unity.VisualScripting;
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
    public GameObject SurVivalTimeUI,FinalLevelUI,VerdictUI;
    [HideInInspector]public Text tex_survivalTime,tex_finalLevel,tex_verdict;

    [Header("设置面板UI")]
    public GameObject SettingsUI;
    public Button btnSettingsReturn;

    [Header("开发人员UI")]
    public GameObject DevelepersUI;
    public Button btnDevelopersReturn;


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

        // 加载主界面UI
        mainMenuUI.SetActive(true);
        SwitchToState(GameState.MainMenu);
        BindUIEvents();
        BindInputEvents();
        BindEndUI();
        MusicManager.Instance.FindAllButtonsAndBindClickSound();
    }

    private void HideAllUI()
    {
        mainMenuUI.SetActive(false);
        inGameUI.SetActive(false);
        pauseUI.SetActive(false);
        gameOverUI.SetActive(false);
    }

    public void ActiveAllUI()
    {
        mainMenuUI.SetActive(true);
        inGameUI.SetActive(true);
        pauseUI.SetActive(true);
        gameOverUI.SetActive(true);
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
                MusicManager.Instance.PlayBGM();
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
                GameObject player = GameObject.FindWithTag("Player");
                if ( player!= null)
                {
                    Destroy(player);
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
                PageUIAppearEffect pauseEffect = pauseUI.GetComponent<PageUIAppearEffect>();
                if (pauseEffect != null)
                {
                    pauseEffect.Play();
                }
                Time.timeScale = 0f;
                MusicManager.Instance.PauseBGM();

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

        // 新增 Return 按钮事件
        if (btnSettingsReturn != null)
            btnSettingsReturn.onClick.AddListener(CloseSettingsPanel);

        if (btnDevelopersReturn != null)
            btnDevelopersReturn.onClick.AddListener(CloseDevelopersPanel);

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

    private void BindEndUI()
    {
        if(gameOverUI != null)
        {
            tex_survivalTime = SurVivalTimeUI.transform.GetChild(0).GetComponent<Text>();
            tex_finalLevel = FinalLevelUI.transform.GetChild(0).GetComponent<Text>();
            tex_verdict = VerdictUI.transform.GetChild(0).GetComponent<Text>();
        }
        else
        {
            Debug.LogError("GameOverUI未找到！");
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

    private void CloseSettingsPanel()
    {
        if (SettingsUI != null)
            SettingsUI.SetActive(false);
    }

    private void CloseDevelopersPanel()
    {
        if (DevelepersUI != null)
            DevelepersUI.SetActive(false);
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

        // 查找ModelHandler并增强调试
        ModelHandler modelHandler = FindObjectOfType<ModelHandler>();
        if (modelHandler == null)
        {
            Debug.LogError("[ProcessGeneratedModel] 找不到ModelHandler实例！请检查场景中是否存在ModelHandler组件");
            yield break;
        }
        Debug.Log("[ProcessGeneratedModel] 成功找到ModelHandler实例");

        // 获取模型路径并严格校验
        string fullModelPath = modelHandler.GetLastDownloadedModelPath();
        Debug.Log($"[ProcessGeneratedModel] 尝试加载模型路径: {fullModelPath}");

        if (string.IsNullOrEmpty(fullModelPath))
        {
            Debug.Log("[ProcessGeneratedModel] 模型路径为空！ModelHandler未正确记录下载路径");
            yield break;
        }

        if (!File.Exists(fullModelPath))
        {
            Debug.Log($"[ProcessGeneratedModel] 模型文件不存在！路径: {fullModelPath} 请检查文件是否被删除或路径是否正确");
            yield break;
        }
        Debug.Log("[ProcessGeneratedModel] 模型文件存在，开始加载流程");

        // 处理跨平台路径前缀问题
        string fileUrl;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        fileUrl = "file:///" + fullModelPath.Replace("\\", "/"); // Windows需要三个斜杠，替换反斜杠为正斜杠
#else
    fileUrl = "file://" + fullModelPath; // 其他平台使用两个斜杠
#endif
        Debug.Log($"[ProcessGeneratedModel] 处理后的文件URL: {fileUrl}");

        // 初始化GLTF加载器
        var gltf = new GLTFast.GltfImport();
        if (gltf == null)
        {
            Debug.LogError("[ProcessGeneratedModel] GLTFast加载器初始化失败！请检查GLTFast包是否正确导入");
            yield break;
        }

        // 加载GLTF模型并处理任务状态
        var loadTask = gltf.Load(fileUrl);
        yield return new WaitUntil(() => loadTask.IsCompleted);

        // 检查加载任务异常
        if (loadTask.IsFaulted)
        {
            Debug.LogError($"[ProcessGeneratedModel] GLTF加载任务抛出异常: {loadTask.Exception?.InnerException?.Message}");
            yield break;
        }

        // 检查加载结果
        if (!loadTask.Result)
        {
            Debug.LogError($"[ProcessGeneratedModel] GLB模型加载失败！加载器返回失败状态，路径: {fileUrl}");
            yield break;
        }
        Debug.Log("[ProcessGeneratedModel] GLTF模型加载成功");

        // 实例化模型到场景
        GameObject root = new GameObject("GLB_Root");
        var instantiateTask = gltf.InstantiateMainSceneAsync(root.transform);
        yield return new WaitUntil(() => instantiateTask.IsCompleted);

        // 检查实例化任务异常
        if (instantiateTask.IsFaulted)
        {
            Debug.LogError($"[ProcessGeneratedModel] 模型实例化任务抛出异常: {instantiateTask.Exception?.InnerException?.Message}");
            Destroy(root);
            yield break;
        }

        if (!instantiateTask.Result)
        {
            Debug.LogError("[ProcessGeneratedModel] GLTF模型实例化失败！实例化器返回失败状态");
            Destroy(root);
            yield break;
        }
        Debug.Log("[ProcessGeneratedModel] 模型实例化成功");

        // 验证模型结构
        if (root.transform.childCount == 0)
        {
            Debug.LogError("[ProcessGeneratedModel] 实例化的模型根对象没有子物体！模型可能为空或格式错误");
            Destroy(root);
            yield break;
        }
        Debug.Log($"[ProcessGeneratedModel] 模型根对象包含 {root.transform.childCount} 个子物体");

        // 获取实际模型对象（取第一个有效子物体作为主体）
        generatedModel = root.transform.GetChild(0).gameObject;
        generatedModel.transform.SetParent(null); // 解除与根对象的关联
        Destroy(root); // 销毁临时根对象
        generatedModel.SetActive(false);
        Debug.Log($"[ProcessGeneratedModel] 已获取模型主体: {generatedModel.name}");

        // 查找模型中所有Renderer组件（递归查找子物体）
        Renderer[] allRenderers = generatedModel.GetComponentsInChildren<Renderer>(true);
        if (allRenderers.Length == 0)
        {
            Debug.LogError("[ProcessGeneratedModel] 模型及其子物体中未找到任何Renderer组件！无法应用材质");
            yield break;
        }
        Debug.Log($"[ProcessGeneratedModel] 在模型中找到 {allRenderers.Length} 个Renderer组件");

        // 处理材质和贴图
        bool textureApplied = false;
        if (playerMaterial != null)
        {
            // 尝试从第一个有效Renderer获取贴图
            foreach (var renderer in allRenderers)
            {
                if (renderer.material != null && renderer.material.mainTexture != null)
                {
                    Texture2D extractedTex = renderer.material.mainTexture as Texture2D;
                    // 关键：用Shader的_Atlas属性名赋值，而非默认mainTexture
                    playerMaterial.SetTexture("_Atlas", extractedTex);
                    Debug.Log($"[ProcessGeneratedModel] 已提取贴图并赋值到_Atlas属性");
                    textureApplied = true;
                    break;
                }

            }

            if (!textureApplied)
            {
                Debug.LogWarning("[ProcessGeneratedModel] 所有Renderer组件均无有效主贴图，使用playerMaterial默认贴图");
            }

            // 将playerMaterial应用到所有Renderer
            foreach (var renderer in allRenderers)
            {
                renderer.material = playerMaterial;
            }
            Debug.Log("[ProcessGeneratedModel] 已将playerMaterial应用到模型所有Renderer");
        }
        else
        {
            Debug.LogError("[ProcessGeneratedModel] playerMaterial未赋值！无法处理模型材质");
            yield break;
        }

        // 设置预制体并激活按钮
        fishPrefab = generatedModel;
        DontDestroyOnLoad(fishPrefab);
        Debug.Log($"[ProcessGeneratedModel] 已将模型 {generatedModel.name} 设置为fishPrefab");

        if (btnEnterGame != null)
        {
            btnEnterGame.gameObject.SetActive(true);

            // 音效

            MusicManager.Instance.DrawOver();

            Debug.Log("[ProcessGeneratedModel] 已激活进入游戏按钮");
        }
        else
        {
            Debug.LogError("[ProcessGeneratedModel] btnEnterGame未赋值！无法激活进入游戏按钮");
        }
    }

    // 获取评判文本
    public void GetVerdictText(float survivalTime, int currentTier)
    {
        string verdict;
        if (survivalTime >= 300 && currentTier >= 5)
        {
            verdict = "Fishy Overlord";
        }
        else if (survivalTime >= 180 && currentTier >= 4)
        {
            verdict = "Pro Munch Master";
        }
        else if (survivalTime >= 60 && currentTier >= 3)
        {
            verdict = "Fish Explorer";
        }
        else
        {
            verdict = "Tiny Fish Newbie";
        }
        tex_verdict.text = verdict;
    }

    // 新增：场景加载完成后重新获取当前场景引用（核心修改2）
   
    private void OnDestroy()
    {
        Time.timeScale = 1f;
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
        GameObject player = GameObject.FindWithTag("Player");
                
        if (player != null)
        {
            Destroy(player);
        }

        // 再创建新玩家
        if (fishPrefab == null)
        {
            Debug.LogError("请赋值小鱼预制体！");
            return;
        }
        if(playerFish == null)
        {
            Debug.LogError("请赋值玩家鱼预制体！");
            return;
        }
        playerFish = Instantiate(playerFish, playerSpawnPoint.position, Quaternion.identity);
      if(!isDebugMode)
        {

        // 删除旧模型
        Destroy(playerFish.transform.GetChild(0).gameObject);
        // 把新模型设置为第一个子物体

        fishPrefab = Instantiate(fishPrefab, playerSpawnPoint.position, Quaternion.identity);
        fishPrefab.transform.SetParent(playerFish.transform);
        fishPrefab.transform.SetSiblingIndex(0);
        fishPrefab.SetActive(true);
        }
        playerFish.SetActive(true); // 激活模型

        // 相机跟随逻辑保持不变
        ThirdPersonCamera target = FindObjectOfType<ThirdPersonCamera>();
        GameObject cameraObj = GameObject.FindWithTag("ThirdPersonCamera");
        if (cameraObj != null && target != null)
        {
            target.SetCameraTarget(GameObject.Find("LookRoot"), cameraObj);
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
    // 回到主场景
    public void OnLoadMainMenuScene()
    {
        playerSpawnPoint = GameObject.Find("PlayerSpawnPoint").transform;
        CreateNewPlayerFish();
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