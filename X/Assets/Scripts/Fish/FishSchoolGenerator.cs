using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FishSettings
{
    public GameObject fishPrefab;
    public int count = 10;
    public float minSize = 0.8f;
    public float maxSize = 1.2f;
    public float minSpeed = 1f;
    public float maxSpeed = 3f;
}

public class FishSchoolGenerator : MonoBehaviour
{
    [Tooltip("生成区域左下角")]
    public Vector3 spawnAreaMin;
    [Tooltip("生成区域右上角")]
    public Vector3 spawnAreaMax;
    [Tooltip("鱼之间的固定间距")]
    public float fixedDistance = 1f;
    [Tooltip("鱼群整体移动方向")]
    public Vector3 schoolGlobalDirection = Vector3.forward;
    [Tooltip("鱼的类型设置")]
    public List<FishSettings> fishTypes;
    [Tooltip("显示生成区域")]
    public bool showGizmos = true;

    // 公开鱼群列表供移动脚本访问
    public List<GameObject> spawnedFish = new List<GameObject>();
    private Vector3 boundsMin;
    private Vector3 boundsMax;

    void Start()
    {
        CalculateBounds();
        GenerateFishSchool();
    }

    public void GenerateFishSchool()
    {
        ClearAllFish();
        CalculateBounds();

        foreach (var fishType in fishTypes)
        {
            if (fishType.fishPrefab != null && fishType.count > 0)
            {
                GenerateFishOfType(fishType);
            }
        }
    }

    private void CalculateBounds()
    {
        boundsMin = spawnAreaMin;
        boundsMax = spawnAreaMax;
    }

    private void GenerateFishOfType(FishSettings settings)
    {
        List<Vector3> positions = new List<Vector3>();

        // 生成第一个鱼作为初始点
        Vector3 firstPos = GetRandomPosition();
        positions.Add(firstPos);
        SpawnFish(settings, firstPos);

        // 生成其余鱼（确保初始间距）
        for (int i = 1; i < settings.count; i++)
        {
            Vector3 pos = GetPositionWithDistance(positions);
            if (pos != Vector3.zero)
            {
                positions.Add(pos);
                SpawnFish(settings, pos);
            }
        }
    }

    // 获取符合间距要求的位置
    private Vector3 GetPositionWithDistance(List<Vector3> existingPositions)
    {
        int attempts = 0;
        while (attempts < 50)
        {
            attempts++;
            // 在已有位置附近生成候选点
            Vector3 basePos = existingPositions[Random.Range(0, existingPositions.Count)];
            Vector3 randomOffset = new Vector3(
                Random.Range(-fixedDistance * 1.2f, fixedDistance * 1.2f),
                Random.Range(-fixedDistance * 0.5f, fixedDistance * 0.5f),
                Random.Range(-fixedDistance * 1.2f, fixedDistance * 1.2f)
            );
            Vector3 candidate = basePos + randomOffset;

            // 检查是否在生成区域内
            candidate = ClampToBounds(candidate);

            // 检查是否符合间距要求
            bool valid = true;
            foreach (var pos in existingPositions)
            {
                if (Vector3.Distance(candidate, pos) < fixedDistance * 0.8f)
                {
                    valid = false;
                    break;
                }
            }

            if (valid) return candidate;
        }
        return Vector3.zero; // 多次尝试失败
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(boundsMin.x, boundsMax.x),
            Random.Range(boundsMin.y, boundsMax.y),
            Random.Range(boundsMin.z, boundsMax.z)
        );
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        return new Vector3(
            Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y),
            Mathf.Clamp(pos.z, boundsMin.z, boundsMax.z)
        );
    }

    private void SpawnFish(FishSettings settings, Vector3 position)
    {
        GameObject fish = Instantiate(settings.fishPrefab, position, Quaternion.identity, transform);

        // 设置尺寸
        float scale = Random.Range(settings.minSize, settings.maxSize);
        fish.transform.localScale = Vector3.one * scale;

        // 配置移动组件
        FishMovement movement = fish.GetComponent<FishMovement>();
        if (movement == null) movement = fish.AddComponent<FishMovement>();

        movement.baseSpeed = Random.Range(settings.minSpeed, settings.maxSpeed);
        movement.fixedDistance = fixedDistance;
        movement.globalDirection = schoolGlobalDirection;
        movement.schoolGenerator = this;

        spawnedFish.Add(fish);
    }

    public void ClearAllFish()
    {
        foreach (var fish in spawnedFish)
        {
            if (fish != null) Destroy(fish);
        }
        spawnedFish.Clear();
    }

    void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 center = (spawnAreaMin + spawnAreaMax) / 2;
            Vector3 size = spawnAreaMax - spawnAreaMin;
            Gizmos.DrawCube(center, size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}