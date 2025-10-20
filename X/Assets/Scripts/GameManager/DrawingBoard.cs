using UnityEngine;
using System;

public class DrawingBoard : MonoBehaviour
{
  public event Action OnDrawingChanged; // 绘制变化事件
    private int drawingCount = 0;         // 绘制内容计数
    private Texture2D drawingTexture;     // 画板纹理

    public void AddDrawing()
    {
        drawingCount++;
        OnDrawingChanged?.Invoke(); // 触发绘制变化事件
    }


    public void ClearDrawing()
    {
        drawingCount = 0;
        OnDrawingChanged?.Invoke();
    }

    // 获取绘制数量（用于判断是否为空）
    public int GetDrawingCount() => drawingCount;

    // 获取画板纹理（用于AI生成）
    public Texture2D GetDrawingTexture() => drawingTexture;

}