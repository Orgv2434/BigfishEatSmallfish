using UnityEngine;

public class FishTierEffect : MonoBehaviour
{
    // 鱼的技能数据（在Inspector拖拽配置）
    public SkillFishData fishData;
    // 各挡位对应的光环预制件（代码自动加载，无需手动拖入）
    public GameObject[] tierPrefabs;

    // 缓存当前显示的光环
    protected GameObject currentHalo;
    public float scalePlus = 0.8f;

    // 定死特效预制体的路径（Resources文件夹下的路径，需确保FishEffects在Resources里）
    private const string EffectPath = "FishEffects/";
    // 按顺序定义特效名称，与“白、黄、紫、黑、红”对应
    private string[] effectNames = {
        "Area_circles_white",
        "Area_circles_yellow",
        "Area_circles_purple",
        "Area_circles_black",
        "Area_circles_red"
    };

    public virtual void Start()
    {
        // 自动加载特效到tierPrefabs数组
        LoadTierPrefabs();

        // 加载对应挡位的光环
        ShowTierHalo(fishData.fishTier);

        // 自动适配模型大小
        AutoFitModelSize();
    }

    private void LoadTierPrefabs()
    {
        tierPrefabs = new GameObject[effectNames.Length];
        for (int i = 0; i < effectNames.Length; i++)
        {
            string fullPath = $"{EffectPath}{effectNames[i]}";
            GameObject prefab = Resources.Load<GameObject>(fullPath);

            // 强制打印路径，看实际加载的地址
            Debug.Log($"尝试加载：{fullPath} → 是否找到？ {prefab != null}");

            if (prefab == null)
            {
                Debug.LogError($"未找到特效预制件：{fullPath}");
                continue;
            }
            tierPrefabs[i] = prefab;
        }
    }

    // 根据挡位显示对应光环预制件
    public void ShowTierHalo(FishTier tier)
    {
        // 销毁之前的光环
        if (currentHalo != null) Destroy(currentHalo);

        // 检查数组有效性（核心修复：先判空再用）
        if (tierPrefabs == null || tierPrefabs.Length == 0)
        {
            Debug.LogError("tierPrefabs数组未初始化或为空！");
            return;
        }

        // 限制索引范围（双重保险）
        int tierIndex = Mathf.Clamp((int)tier, 0, tierPrefabs.Length - 1);

        // 实例化预制件
        currentHalo = Instantiate(
            tierPrefabs[tierIndex],
            new Vector3(
                transform.position.x - (transform.localScale.x * 0.5f),
                transform.position.y - (transform.localScale.y * 0.5f),
                transform.position.z
            ),
            Quaternion.identity,
            transform
        );
    }
    // 自动适配模型大小（修改预制件缩放）
    public virtual void AutoFitModelSize()
    {
        // 查找第一个子物体上的 Renderer 组件
        Renderer modelRenderer = GetComponentInChildren<Renderer>();
        if (modelRenderer == null)
        {
            Debug.LogError("未找到模型的Renderer组件");
            return;
        }

        // 获取模型的包围盒大小
        Bounds bounds = modelRenderer.bounds;
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

        // 计算缩放比例（让光环比模型大20%）
        float scaleRatio = maxExtent * scalePlus;

        // 调整光环的缩放（假设预制件的原始大小是1）
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