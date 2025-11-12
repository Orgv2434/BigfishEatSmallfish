using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TripoForUnity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SketchDemo : MonoBehaviour
{
    [Header("Settings")]
    public string setPrompts_forward;
    public string setPrompts_back;
    public string setNegativePrompts;

    public DrawConfig config;
    public SpriteRenderer drawSprite;
    public SpriteRenderer resultSprite;
    public Sprite defaultSprite;
    public Sprite[] preSprites;

    public Transform penPanel;
    private InputField promptInput;
    private InputField negativePromptInput;
    private Text progressText;

    public string currentImagePath;



    void Start()
    {
        InitUI();
        InitDrawingSettings();
    }

    void InitDrawingSettings()
    {
        //预设提示词
        if (string.IsNullOrEmpty(setPrompts_forward))
            setPrompts_forward = "A fish, (low-poly fish), simple geometric shapes, " +
        "\r\nstylized polygonal model, clean silhouette, minimal details," +
        "\r\nmuted color palette, desaturated tones, soft pastel colors," +
        "\r\nMorandi color scheme, soft harmonious colors," +
        "\r\nlow color contrast, gentle tonal transitions, soothing color palette," +
        "\r\nsolid colors, smooth surface, soft lighting," +
        "\r\nfish facing left, head on left side, tail on right side, oriented to the left"+
        "\r\ncompletely lateral view, direct side angle, pure side profile";        // 强化角度描述

        if (string.IsNullOrEmpty(setPrompts_back))
            setPrompts_back = "pure white background, single fish subject only, no background elements," +
                "\r\nno outlines, no sketch lines, smooth surface, clean shading," +
                "\r\ndo not include lineart, no bold edges, isolated on white";

        if (string.IsNullOrEmpty(setNegativePrompts))
            setNegativePrompts = "realistic style, detailed background, environment, plants, rocks, corals," +
                "\r\nvibrant colors, high saturation, bright neon colors, intense coloration," +
                "\r\nhigh contrast colors, bold color contrasts, sharp tonal transitions," +
                "\r\ngarish colors, electric colors, fluorescent tones, pure primary colors," +
                "\r\nlineart, outline, bold lines, draw lines, sketch style, ink, cartoon outline," +
                "\r\nfish facing right, head on right side, tail on left side, reversed orientation";
                
    }

    void InitUI()
    {
        List<string> options = new List<string>();
        for (int i = 0; i < preSprites.Length; i++)
        {
            options.Add(preSprites[i].name);
        }
        Dropdown textureDropdown = penPanel.Find("PreTextures Dropdown").GetComponent<Dropdown>();
        textureDropdown.ClearOptions();
        textureDropdown.AddOptions(options);
        textureDropdown.onValueChanged.AddListener((index) =>
        {
            Texture2D originalTexture = preSprites[index].texture;
            Texture2D copiedTexture = new Texture2D(1, 1);
            copiedTexture.LoadImage(originalTexture.EncodeToPNG());
            Sprite copiedSprite = Sprite.Create(copiedTexture, defaultSprite.rect, Vector2.one * 0.5f);

            drawSprite.sprite = copiedSprite;
            Drawable.drawable.UpdateDrawableSprite();
        });

        Transform colorGrid = penPanel.Find("Color Grid");
        for (int i = 0; i < colorGrid.childCount; i++)
        {
            Button btn = colorGrid.GetChild(i).GetComponent<Button>();
            btn.onClick.AddListener(() => { OnColorBtnClick(btn); });
        }
        //penPanel.Find("PenWidth Slider").GetComponent<Slider>().onValueChanged.AddListener(OnPenWidthSliderValueChanged);
        penPanel.Find("ReDraw Button").GetComponent<Button>().onClick.AddListener(OnReDrawBtnClick);
        penPanel.Find("Request Button").GetComponent<Button>().onClick.AddListener(OnRequestBtnClick);
        promptInput = penPanel.Find("Prompt InputField").GetComponent<InputField>();
        negativePromptInput = penPanel.Find("NegativePrompt InputField").GetComponent<InputField>();
        progressText = penPanel.Find("绘制进度").GetComponent<Text>();
        progressText.text = "";
    }

    void SetImagePath(string filepath)
    {
        currentImagePath=filepath;
        //GetComponent<TripoRuntimeCore>().imagePath = filepath;  // 设置给 Tripo3D 的输入
        Debug.Log("image save path : "+filepath);
        //Debug.Log("tripo image save path : "+ GetComponent<TripoRuntimeCore>().imagePath);
    }

    public string GetImagePath()
    {
        return currentImagePath;
    }

    public void OnClickGenerateModel()
    {
        var tripo = GetComponent<TripoRuntimeCore>();
        //tripo.set_api_key(myApiKey);
        //GetComponent<TripoRuntimeCore>().imagePath
        //在绘制完成时设置
        //tripo.Image_to_Model_func();

        //v1.1
        FindObjectOfType<TripoController>().OnImageToModelGenerate();
        Debug.Log("tripo image save path : " + GetComponent<TripoRuntimeCore>().imagePath);

        Debug.Log($"[按钮触发] 当前 imagePath = {tripo.imagePath}");

    }

    void OnColorBtnClick(Button btn)
    {
        Drawable.Pen_Colour = btn.GetComponent<Image>().color;
    }

    void OnPenWidthSliderValueChanged(float v)
    {
        Drawable.Pen_Width = (int)v;
    }

    void OnReDrawBtnClick()
    {
        drawSprite.sprite = defaultSprite;
        Drawable.drawable.UpdateDrawableSprite();
        Drawable.drawable.Reset_Colour = Color.white;
        Drawable.drawable.ResetCanvas();
        resultSprite.enabled = false;
    }

    void OnRequestBtnClick()
    {
        //if (string.IsNullOrEmpty(promptInput.text))
        //    return;
        //把玩家的绘画（drawSprite.sprite.texture）转成 JPG → base64
        //byte[] bytes = drawSprite.sprite.texture.EncodeToJPG();
        //修正方向版：
        Texture2D fixedTex = Drawable.EnsureConsistentOrientation(drawSprite.sprite.texture);
        byte[] bytes = fixedTex.EncodeToJPG();

        string base64String = Convert.ToBase64String(bytes);
        //保存草图
        string filepath = Path.GetDirectoryName(Application.dataPath) + "/Images/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg";
        if (!Directory.Exists(Path.GetDirectoryName(filepath))) Directory.CreateDirectory(Path.GetDirectoryName(filepath));
        File.WriteAllBytes(filepath, bytes);
        Debug.LogFormat("保存草图成功：{0}", filepath);
        //开始绘制
        penPanel.GetComponent<CanvasGroup>().interactable = false;
        progressText.text = "Begin Draw...";
        StartCoroutine(AIDraw(promptInput.text, negativePromptInput.text, base64String, b =>
         {
             progressText.text = "";
             penPanel.GetComponent<CanvasGroup>().interactable = true;
         }));
    }

    IEnumerator AIDraw(string _prompt, string _negativePrompt, string base64String, Action<bool> _callback)
    {
        string url = config.BaseUrl + config.DrawUrl;
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            DrawModel.RequestBody postData = new DrawModel.RequestBody
            {
                prompt = setPrompts_forward + _prompt + setPrompts_back,
                negativePrompt = setNegativePrompts + _negativePrompt,
                modelStyleId = config.DrawModelStyleId,
                controlnet = new DrawModel.Controlnet
                {
                    units = new DrawModel.Unit[]
                    {
                        new DrawModel.Unit
                        {
                            type = config.ControlnetType,
                            image = base64String,
                            preprocess = config.ControlnetPreprocess
                        }
                    }
                }
            };

            string jsonText = JsonUtility.ToJson(postData).Trim();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonText);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Auth-Token", config.Token);

            Debug.LogFormat("Draw, url={0}, data={1}", url, jsonText);
            yield return request.SendWebRequest();
            Debug.LogFormat("Draw Complete，responseCode={0}，message={1}", request.responseCode, request.downloadHandler.text);

            if (request.responseCode == 200)
            {
                DrawModel.ResponseBody response = JsonUtility.FromJson<DrawModel.ResponseBody>(request.downloadHandler.text);
                if (response != null && response.success)
                {
                    string paintingSign = response.data.paintingSign;
                    StartCoroutine(GetTaskDetail(paintingSign, _callback));
                }
                else
                {
                    Debug.LogErrorFormat("Draw Fail，message={0}", request.downloadHandler.text);
                    _callback(false);
                }
            }
            else
            {
                Debug.LogErrorFormat("Draw Fail，message={0}", request.downloadHandler.text);
                _callback(false);
            }
        }
    }

    IEnumerator GetTaskDetail(string _taskId, Action<bool> _callback)
    {
        string url = config.BaseUrl + config.TaskDetailUrl;
        while (true)
        {
            bool complete = false;
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                GetTaskDetailModel.RequestBody postData = new GetTaskDetailModel.RequestBody
                {
                    taskId = _taskId
                };

                string jsonText = JsonUtility.ToJson(postData).Trim();
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonText);
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Auth-Token", config.Token);

                Debug.LogFormat("GetTaskDetail, url={0}, data={1}", url, jsonText);
                yield return request.SendWebRequest();
                Debug.LogFormat("GetTaskDetail responseCode：{0}，message={1}", request.responseCode, request.downloadHandler.text);

                if (request.responseCode == 200)
                {
                    GetTaskDetailModel.ResponseBody response = JsonUtility.FromJson<GetTaskDetailModel.ResponseBody>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        GetTaskDetailModel.ResponseBodyData data = response.data;
                        progressText.text = string.Format("绘制进度：{0}%", (int)(data.progress * 100));
                        if (data.state == "fail")
                        {
                            Debug.LogFormat("GetTaskDetail Fail，stata:{0},progress：{1}", data.state, data.progress);
                            _callback(false);
                            complete = true;
                            break;
                        }
                        else if (data.state == "success") 
                        {
                            //如果成功：拿到 imgUrl，调用 DownloadImage(...) 下载结果图。
                            Debug.LogFormat("GetTaskDetail Success，imgUrl={0}", data.imgUrl);
                            yield return StartCoroutine(DownloadImage(data.imgUrl));
                            _callback(true);
                            complete = true;
                            break;
                        }
                        else
                        {
                            //如果进行中：显示当前进度，同时还会解析 current_image（base64）作为中间预览。
                            Debug.LogFormat("stata:{0},progress：{1}", data.state, data.progress);
                            byte[] current_bytes = Convert.FromBase64String(data.current_image);
                            Texture2D tex = new Texture2D(1, 1);
                            tex.LoadImage(current_bytes);
                            resultSprite.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                            resultSprite.enabled = true;
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("GetTaskDetail Fail，message={0}", request.downloadHandler.text);
                        _callback(false);
                        complete = true;
                        break;
                    }
                }
                else
                {
                    Debug.LogErrorFormat("GetTaskDetail Fail，message={0}", request.downloadHandler.text);
                    _callback(false);
                    complete = true;
                    break;
                }
            }
            if (complete) break;
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator DownloadImage(string url)
    {
        //从 imgUrl 下载最终图片 → 转换成 Texture2D → 显示到 resultSprite。
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            //最终结果图
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            resultSprite.sprite = sprite;
            resultSprite.enabled = true;
            Debug.Log("DownloadImage Complete.");
            //保存绘画
            //string filepath = Path.GetDirectoryName(Application.dataPath) + "/Images/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg";
            string filepath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Images",
                DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg"
            );

            if (!Directory.Exists(Path.GetDirectoryName(filepath))) Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllBytes(filepath, texture.EncodeToJPG());
            Debug.LogFormat("保存绘画成功：{0}", filepath);

            // 👉 自动衔接到 Tripo3D 建模
            SetImagePath(filepath);
           
        }
        else
        {
            Debug.LogError("DownloadImage Fail: " + www.error);
        }
    }
}