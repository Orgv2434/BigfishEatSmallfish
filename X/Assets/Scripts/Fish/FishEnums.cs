using UnityEngine;

// 鱼的挡位枚举（从小到大）
public enum FishTier
{
    White,   // 白
    Yellow,  // 黄
    Purple,  // 紫
    Black,   // 黑
    Red      // 红
}

// 技能类型枚举
public enum FishSkillType
{
    None,
    ExpMultiplier,  // 经验倍率
    Dash,           // 冲刺
    Shield,         // 护盾
    Camouflage      // 伪装
}
[System.Serializable]
// 鱼的属性类
public class SkillFishData
{
    public int id;                   // 鱼的专属id
    public int baseExpValue;         // 鱼本身的经验值
    public FishTier fishTier;        // 鱼的挡位
    public FishSkillType skillType;  // 技能类型
    public GameObject originalPrefab; // 新增：存储鱼的原始预制体
}