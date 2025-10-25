using UnityEngine;

public class FishTierEffect : MonoBehaviour
{
    // 鱼的技能数据（在Inspector拖拽配置）
    public SkillFishData fishData;
    
    // 各挡位对应的光环预制件（代码自动加载，无需手动拖入）
    public GameObject[] tierPrefabs;

    // 缓存当前显示的光环
    public GameObject currentHalo;
    
    public float scalePlus = 0.5f;
    // 光环在模型顶部的额外偏移量，可根据需要调整
    public float topOffset = 0.1f;

    // 定死特效预制体的路径（Resources文件夹下的路径，需确保FishEffects在Resources里）
    protected const string EffectPath = "FishEffects/";
    // 按顺序定义特效名称，与“白、黄、紫、黑、红”对应
    private string[] effectNames = {
        "Area_circles_white 1",
        "Area_circles_yellow 1",
        "Area_circles_purple 1",
        "Area_circles_black 1",
        "Area_circles_red 1"
    };

    public virtual void Start()
    {
        BindEvent();
        // 自动加载特效到tierPrefabs数组
        LoadTierPrefabs();

        // 加载对应挡位的光环
        ShowTierHalo(fishData.fishTier);

        // 自动适配模型大小
        AutoFitModelSize();
    }

    protected virtual void LoadTierPrefabs()
    {
        tierPrefabs = new GameObject[effectNames.Length];
        for (int i = 0; i < effectNames.Length; i++)
        {
            string fullPath = $"{EffectPath}{effectNames[i]}";
            GameObject prefab = Resources.Load<GameObject>(fullPath);

            if (prefab == null)
            {
                Debug.LogError($"未找到特效预制件：{fullPath}");
                continue;
            }
            tierPrefabs[i] = prefab;
        }
    }
    private void BindEvent()
    {
        // 注册事件监听
        FishEventSystem.OnPlayerTierChanged += OnPlayerTierUpdated;
        // 初始更新一次显示状态
        if (PlayerFishData.Instance != null)
        UpdateEffectVisibility(PlayerFishData.Instance.GetCurrentTier());
    }
    private void OnDisable()
    {
        // 移除事件监听
        FishEventSystem.OnPlayerTierChanged -= OnPlayerTierUpdated;
    }
    private void OnPlayerTierUpdated(FishTier newPlayerTier)
    {
        // 当玩家挡位变化时更新特效显示
        UpdateEffectVisibility(newPlayerTier);
    }


    // 根据挡位显示对应光环预制件
    public void ShowTierHalo(FishTier tier)
    {
        // 销毁之前的光环
        if (currentHalo != null) Destroy(currentHalo);

        if (tier == FishTier.White) // 假设White对应0挡位
        {
            currentHalo = null;
            return;
        }
    

    // 检查特效预制体
    if (tierPrefabs == null || tierPrefabs.Length == 0)
    {
        Debug.LogError("tierPrefabs数组未初始化或为空！");
        return;
    }

        // 限制索引范围
        int tierIndex = Mathf.Clamp((int)tier, 0, tierPrefabs.Length - 1);

        // 获取模型渲染组件以计算准确位置
        Renderer modelRenderer = GetComponent<Renderer>();
        if (modelRenderer == null)
        {
            modelRenderer = GetComponentInChildren<Renderer>();
        }

        Vector3 haloPosition;
        
        // 如果找到渲染组件，使用模型实际顶部位置
        if (modelRenderer != null)
        {
            Bounds bounds = modelRenderer.bounds;
            // 光环位置 = 模型包围盒顶部 + 额外偏移
            haloPosition = new Vector3(
                transform.position.x + bounds.center.x - transform.position.x,
                transform.position.y,
                transform.position.z + bounds.center.z - transform.position.z
            );
        }
        else
        {
            // 未找到渲染组件时使用 fallback 位置
            Debug.LogWarning("未找到Renderer组件，使用默认位置计算");
            haloPosition = new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z
            );
        }

        // 实例化预制件
        currentHalo = Instantiate(
            tierPrefabs[tierIndex],
            haloPosition,
            Quaternion.identity,
            transform
        );
    }
public void UpdateEffectVisibility(FishTier playerTier)
{
    // 0挡位特效本身就不存在，直接返回
    if (fishData.fishTier == FishTier.White) return;
    
    if (currentHalo == null) return;
    
    // 只显示挡位不低于玩家当前挡位的特效
    currentHalo.SetActive(fishData.fishTier > playerTier);
}
    // 自动适配模型大小（修改预制件缩放）
    public virtual void AutoFitModelSize()
    {
        // 查找第一个子物体上的 Renderer 组件
        Renderer modelRenderer = GetComponent<Renderer>();
        if (modelRenderer == null)
        {
            modelRenderer = GetComponentInChildren<Renderer>();
            if (modelRenderer == null)
            {
                Debug.LogWarning("未找到 Renderer 组件，无法自动适配模型大小。");
                return;
            }
        }

        // 获取模型的包围盒大小
        Bounds bounds = modelRenderer.bounds;

        // 空检查：bounds.size为零时说明没有有效包围盒
        if (bounds.size == Vector3.zero)
        {
            Debug.LogWarning("Renderer 的 bounds 无效（size为零），无法自动适配模型大小。");
            return;
        }

        // 使用模型在X轴上的最大范围来计算光环大小，更适合水平方向的模型
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);

        // 计算缩放比例（让光环比模型大20%）
        float scaleRatio = maxExtent * scalePlus;

        // 调整光环的缩放
        if (currentHalo != null)
        {
            currentHalo.transform.localScale = Vector3.one * scaleRatio;
        }
    }

    private void OnDestroy()
    {
        // 清理当前光环
        if (currentHalo != null)
        {
            Destroy(currentHalo);
        }
    }
}
    