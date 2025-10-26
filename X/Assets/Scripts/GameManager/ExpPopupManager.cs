using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ExpPopupManager : MonoBehaviour
{
    public static ExpPopupManager Instance { get; private set; }

    [Header("配置")]
    public TMP_Text popupPrefab;
    public float moveSpeed = 30f;
    public float lifeTime = 1.5f;
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = Vector3.zero;

    [Header("玩家相对偏移（世界坐标，与屏幕无关）")]
    [Tooltip("相对于玩家的X偏移（负值=左，正值=右）")]
    public float playerOffsetX = -1f; 
    [Tooltip("相对于玩家的Y偏移（正值=上，负值=下）")]
    public float playerOffsetY = 1f;
    public float playerOffsetZ = 0f;

    [Header("UI 引用")]
    public Canvas uiCanvas;
    // 新增：主相机引用（用于坐标转换）
    public Camera mainCamera; 

    private Queue<TMP_Text> textPool = new Queue<TMP_Text>();
    private List<PopupItem> activePopups = new List<PopupItem>();
    [Header("对象池设置")]
    public int maxPoolSize = 20; 
    

    private struct PopupItem
    {
        public TMP_Text text;
        public float lifeTimer;
        public Vector3 startPosition;
    }
    private void OnEnable()
    {
        // 监听场景卸载事件
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    private void OnDisable()
    {
        // 取消监听（避免内存泄漏）
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    private void OnSceneUnloaded(Scene scene)
    {
        ClearPool();
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (uiCanvas == null)
                uiCanvas = FindObjectOfType<Canvas>();
            if (mainCamera == null)
                mainCamera = Camera.main; // 自动获取主相机
        }
        else
            Destroy(gameObject);
    }

    // 核心修改：基于屏幕比例的坐标转换
    public void ShowCustomPopup(Vector3 playerWorldPosition, string textContent, Color textColor)
    {
        if (Instance == null || uiCanvas == null || popupPrefab == null || mainCamera == null)
        {
            Debug.LogError("ExpPopupManager 配置错误（缺少Canvas/相机/预制体）");
            return;
        }

        TMP_Text text = GetTextFromPool();
        if (text != null)
        {
            // 1. 计算玩家左上方向的世界坐标（仅与玩家位置相关，与屏幕无关）
            Vector3 popupWorldPos = new Vector3(
                playerWorldPosition.x + playerOffsetX,
                playerWorldPosition.y + playerOffsetY,
                playerWorldPosition.z + playerOffsetZ
            );

            // 2. 关键：将世界坐标转换为屏幕比例坐标（0-1范围），适配任何分辨率
            // 屏幕坐标（像素）→ 比例坐标（0-1）：(x/Screen.width, y/Screen.height)
            Vector3 screenPos = mainCamera.WorldToScreenPoint(popupWorldPos);
            Vector2 normalizedPos = new Vector2(
                screenPos.x / Screen.width,  // X轴比例（0=左，1=右）
                screenPos.y / Screen.height // Y轴比例（0=下，1=上）
            );

            // 3. 将比例坐标转换为Canvas的UI坐标（适配拉伸）
            RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
            // Canvas的实际宽高（考虑缩放）
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;
            // 比例坐标 → Canvas内像素坐标
            Vector2 canvasPos = new Vector2(
                normalizedPos.x * canvasWidth - canvasWidth / 2,  // 居中偏移
                normalizedPos.y * canvasHeight - canvasHeight / 2
            );

            // 4. 赋值UI位置
            text.rectTransform.anchoredPosition = canvasPos; // 使用anchoredPosition而非position
            text.text = textContent;
            text.color = textColor;
            text.rectTransform.localScale = startScale;
            text.gameObject.SetActive(true);

            activePopups.Add(new PopupItem
            {
                text = text,
                lifeTimer = 0,
                startPosition = canvasPos // 记录Canvas内的起始位置
            });
        }
    }

    private void Update()
    {
        if (activePopups.Count == 0) return;

        float deltaTime = Time.deltaTime;
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            var item = activePopups[i];
            item.lifeTimer += deltaTime;
            float t = item.lifeTimer / lifeTime;

            // 基于Canvas坐标的飘移动画（避免受屏幕拉伸影响）
            Vector2 newPos = (Vector2)item.startPosition + Vector2.up * moveSpeed * t;
            item.text.rectTransform.anchoredPosition = newPos;

            // 缩放和透明度动画
            item.text.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            var color = item.text.color;
            color.a = 1 - t;
            item.text.color = color;

            activePopups[i] = item;

            if (t >= 1f)
            {
                RecycleText(item.text);
                activePopups.RemoveAt(i);
            }
        }
    }

    // 对象池相关方法保持不变
    private TMP_Text GetTextFromPool()
    {
        if (textPool.Count > 0)
        {
            var text = textPool.Dequeue();
            text.gameObject.SetActive(true);
            return text;
        }

        TMP_Text newText = Instantiate(popupPrefab, uiCanvas.transform);
        newText.rectTransform.localScale = Vector3.one;
        return newText;
    }

    private void RecycleText(TMP_Text text)
{
    text.gameObject.SetActive(false);
    
    // 重置文本状态（避免下次复用出错）
    text.text = ""; // 清空文本
    text.color = Color.white; // 重置颜色
    text.rectTransform.anchoredPosition = Vector2.zero; // 重置位置到Canvas中心
    
    // 池容量判断（原有逻辑）
    if (textPool.Count < maxPoolSize)
    {
        textPool.Enqueue(text);
    }
    else
    {
        Destroy(text.gameObject);
    }
}
    public void ClearPool()
    {
        foreach (var text in textPool)
        {
            Destroy(text.gameObject); // 彻底销毁池内所有对象
        }
        textPool.Clear();
        activePopups.Clear();
        Debug.Log("对象池已清空，释放内存");
    }
}