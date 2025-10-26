using UnityEngine;

[CreateAssetMenu(fileName = "Draw_Config", menuName = "ScriptableObject/Draw_Config", order = 0)]
public class DrawConfig : ScriptableObject
{
    public string BaseUrl = "https://ai.huashi6.com/aiapi/v1";
    public string DrawUrl = "/draw";
    public string TaskDetailUrl = "/task/detail";
    public int DrawModelStyleId = 12;
    public string ControlnetType = "canny";
    public bool ControlnetPreprocess = true;
    [Multiline]
    public string Token = "";
}