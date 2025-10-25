using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillFishTierEffect : FishTierEffect
{   
     private string[] skillFishEffectName = {
        "Area_star_white",
        "Area_star_yellow",
        "Area_star_purple"
    };
    protected override void LoadTierPrefabs()
    {
         tierPrefabs = new GameObject[skillFishEffectName.Length];
        for (int i = 0; i < skillFishEffectName.Length; i++)
        {
            string fullPath = $"{EffectPath}{skillFishEffectName[i]}";
            GameObject prefab = Resources.Load<GameObject>(fullPath);

            if (prefab == null)
            {
                Debug.LogError($"未找到特效预制件：{fullPath}");
                continue;
            }
            tierPrefabs[i] = prefab;
        }
    }
}
