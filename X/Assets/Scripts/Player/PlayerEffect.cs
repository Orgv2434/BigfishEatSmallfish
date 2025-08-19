using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffect : FishTierEffect

{  

    public void ChangeHalo(FishTier tier)
    {  
        // 加载对应挡位的光环
        ShowTierHalo(tier);
        // 自动适配模型大小
        AutoFitModelSize();
    }
}
