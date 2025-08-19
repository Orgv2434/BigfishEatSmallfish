#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace TripoForUnity
{
    public enum ModelVersion
    {
        v2_5_20250123 = 0,
        v2_0_20240919 = 1,
        v1_4_20240625 = 2,
        v1_3_20240522 = 3,
        
    }

    public enum TextureQuality
    {
        Standard = 0,
        Detailed = 1
    }

    public enum ModelStyle
    {
        Original = 0,
        Cartoon = 1,
        Clay = 2,
        Steampunk = 3,
        Venom = 4,
        Barbie = 5,
        Christmas = 6
    }

    public enum Orientation
    {
        Default = 0,
        AlignImage = 1
    }

    public enum TextureAlignment
    {
        OriginalImage = 0,
        Geometry = 1
    }

    public class Tripo_main_window : EditorWindow
    {
        // 窗口的最小尺寸
        private int WindowHeight = 170;
        private int ExtraHeight = 0;

        //private Vector2 minWindowSize = new (400, 1000);

        private string apiKey = "";
        private const string ApiKeyPrefsKey = "ApiKeyPrefsKey";
        private string UserBalance = "";
        private const string UserBalancePrefsKey = "UserBalancePrefsKey";
        private string textPrompt = "";
        private string imagePath = "";
        private Texture2D uploadedImage = null;
        private float textToModelProgress = 0f;
        private float imageToModelProgress = 0f;
        private float textToModelGenerateProgress = 0f;
        private float imageToModelGenerateProgress = 0f;
        private float textToModelConvertProgress = 0f;
        private float imageToModelConvertProgress = 0f;
        private bool showModelSelectionSection = false;
        private bool showTextToModelSection = false;
        private bool showImageToModelSection = false;
        private bool showModelPreviewSection = false;
        private bool advancedSettingsFoldout = false;
        private bool apiKeyConfirmed = false;
        private bool showInstructions = false;
        private bool isTextToModelCoroutineRunning = false;
        private bool isImageToModelCoroutineRunning = false;
        private Vector2 rotation = Vector2.zero;
        private static string saveDirectory = "Assets/TripoModels/";

        private int face_limit = 10000;
        private bool texture_optional = true;
        private bool pbr_optional = true;
        private TextureQuality texture_quality_optional = TextureQuality.Standard;
        private bool autosize_optional = false;
        private ModelStyle style_optional = ModelStyle.Original;
        private Orientation orientation_optional = Orientation.Default;
        private TextureAlignment texture_alignment_optional = TextureAlignment.OriginalImage;
        private bool quad_optional = false;


        private int selectedModelIndex = 0;
        private readonly string[] textureQualityOptions = { "standard", "detailed" };
        private readonly string[] modelOptions = { "v2.5-20250123", "v2.0-20240919", "v1.4-20240625", "v1.3-20240522" };
        private readonly string[] orientationOptions = { "default", "align_image" };

        private readonly string[] styleOptions =
        {
            "default", "person:person2cartoon", "object:clay", "object:steampunk", "animal:venom", "object:barbie",
            "object:christmas"
        };

        private readonly string[] textureAlignmentOptions = { "original_image", "geometry" };
        private string imageToken = "";
        private static List<IEnumerator> coroutines = new List<IEnumerator>();

        [MenuItem("Window/TripoPlugin")]
        public static void ShowWindow()
        {
            GetWindow<Tripo_main_window>("Tripo Main Window").Show();
        }

        private Texture2D headerImage;
        private Texture2D buttonBackground;
        private Texture2D textFieldBackground;
        private Texture2D transparentTexture;
        private Texture2D SeparationLine;
        private Texture2D progressBarBackground;
        private Texture2D modelPreviewBackground;
        private Texture2D apikeyInstruct;
        private Texture2D UserBalanceCoin;
        private GUIStyle balanceStyle;

        private Texture2D imageUploadBtnBackground;
        private Texture2D imagePreviewTexture;
        private GUIStyle imageUploadBtnStyle;

        private GUIStyle buttonStyle;
        private GUIStyle textFieldStyle;
        private Texture2D hoverTexture;
        private Texture2D activeTexture;

        private GUIStyle squareButtonStyle;
        private GUIStyle separatorStyle;
        private GUIStyle headerStyle;
        private GUIStyle placeholderStyle;

        private String TextToModelBtnString = "Generate";
        private String ImageToModelBtnString = "Generate";

        private Texture2D roundedFillTextureText;
        private Texture2D roundedFillTextureImage;
        private GUIStyle progressBarBackgroundStyle;
        private GUIStyle fillStyle;

        private GUIStyle transparentBackground;
        private static GameObject gameObject;
        private GameObject selectedObject;
        private Editor gameObjectEditor;

        private Vector2 scrollPosition;

        private void OnEnable()
        {

            headerImage = EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/logo_small.png") as Texture2D;
            buttonBackground = EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/ButtonBackground.png") as Texture2D;
            textFieldBackground =
                EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/TextToModelInput.png") as Texture2D;
            SeparationLine = EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/SeparationLine.png") as Texture2D;
            progressBarBackground =
                EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/ProgressBarBackground.png") as Texture2D;
            modelPreviewBackground =
                EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/modelPreviewBackground.png") as Texture2D;
            imageUploadBtnBackground =
                EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/imageUploadBtn.png") as Texture2D;
            imagePreviewTexture =
                EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/PreviewImageDefault.png") as Texture2D;
            apikeyInstruct = EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/apikey_instruct.png") as Texture2D;
            UserBalanceCoin = EditorGUIUtility.Load("Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/coin.png") as Texture2D;
            gameObject = null;
            selectedObject = null;

            transparentTexture = new Texture2D(1, 1);
            transparentTexture.SetPixel(0, 0, new Color(0, 0, 0, 0f)); // 完全透明
            transparentTexture.Apply();
            buttonStyle = new GUIStyle
            {
                fixedHeight = 30,
                margin = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(5, 5, 5, 5),
                border = new RectOffset(0, 0, 0, 0), // 去掉边框
                alignment = TextAnchor.MiddleCenter // 设置内容居中
            };
            textFieldStyle = new GUIStyle
            {
                normal =
                {
                    background = textFieldBackground,
                    textColor = Color.white
                },
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(10, 10, 10, 10),
                border = new RectOffset(5, 5, 5, 5),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            placeholderStyle = new GUIStyle
            {
                normal =
                {
                    textColor = Color.gray
                },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 10, 10)
            };

            Texture2D CreateColoredTexture(Texture2D baseTexture, Color color)
            {
                Texture2D coloredTexture =
                    new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.ARGB32, false);
                for (int y = 0; y < baseTexture.height; y++)
                {
                    for (int x = 0; x < baseTexture.width; x++)
                    {
                        Color pixelColor = baseTexture.GetPixel(x, y);
                        pixelColor *= color; // 叠加颜色
                        coloredTexture.SetPixel(x, y, pixelColor);
                    }
                }

                coloredTexture.Apply();
                return coloredTexture;
            }

// 定义 hover 和点击时的颜色
            Color hoverColor = new Color(1f, 0.9f, 0.6f, 1f); // 浅黄色
            Color activeColor = new Color(1f, 0.8f, 0.4f, 1f); // 深黄色
            Color balanceColor = new Color(248f / 255f, 207f / 255f, 0f / 255f, 1f);

// 创建 hover 和 active 状态纹理
            hoverTexture = CreateColoredTexture(buttonBackground, hoverColor);
            activeTexture = CreateColoredTexture(buttonBackground, activeColor);

// 设置按钮样式的背景
            buttonStyle.normal.background = buttonBackground;
            buttonStyle.hover.background = hoverTexture;
            buttonStyle.active.background = activeTexture;
            buttonStyle.focused.background = buttonBackground;

// 设置文本颜色（黑色）
            buttonStyle.normal.textColor = Color.black;
            buttonStyle.hover.textColor = Color.black;
            buttonStyle.active.textColor = Color.black;
            buttonStyle.focused.textColor = Color.black;

            squareButtonStyle = new GUIStyle
            {
                fixedHeight = 20,
                fixedWidth = 20,
                margin = new RectOffset(5, 10, 10, 10),
                padding = new RectOffset(5, 5, 5, 5),
                border = new RectOffset(0, 0, 0, 0), // 去掉边框
                alignment = TextAnchor.MiddleCenter, // 设置内容居中

            };
            squareButtonStyle.normal.textColor = Color.white;
            squareButtonStyle.hover.textColor = Color.white;
            squareButtonStyle.active.textColor = Color.white;
            squareButtonStyle.focused.textColor = Color.white;

            imageUploadBtnStyle = new GUIStyle
            {
                normal = { background = imageUploadBtnBackground }, // 按钮背景图片
                alignment = TextAnchor.MiddleCenter, // 内容居中
                border = new RectOffset(10, 10, 10, 0), // 按钮边框
                fixedWidth = 300, // 按钮宽度
                fixedHeight = 150 // 按钮高度
            };

            // 分隔线样式
            separatorStyle = new GUIStyle();
            separatorStyle.fixedHeight = 1;
            separatorStyle.margin = new RectOffset(20, 20, 10, 10);
            separatorStyle.normal.background = SeparationLine;

            // 标题样式
            headerStyle = new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.UpperCenter,
                normal =
                {
                    textColor = Color.white
                }
            };

            balanceStyle = new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = balanceColor
                }
            };
            progressBarBackgroundStyle = new GUIStyle();
            progressBarBackgroundStyle.normal.background = progressBarBackground;
            progressBarBackgroundStyle.fixedHeight = 7;
            progressBarBackgroundStyle.margin = new RectOffset(10, 10, 10, 10);

            fillStyle = new GUIStyle();
            transparentBackground = new GUIStyle();
            transparentBackground.normal.background = modelPreviewBackground;
            OnGetBalanceComplete.AddListener(OnReceiveUserBalance);
            if (EditorPrefs.HasKey(UserBalancePrefsKey))
            {
                UserBalance = EditorPrefs.GetString(UserBalancePrefsKey);
            }

            if (EditorPrefs.HasKey(ApiKeyPrefsKey))
            {
                apiKey = EditorPrefs.GetString(ApiKeyPrefsKey);

                if (!apiKey.StartsWith("tsk_"))
                {
                    EditorPrefs.SetString(ApiKeyPrefsKey, "");
                }
                else
                {
                    apiKey = EditorPrefs.GetString(ApiKeyPrefsKey);
                    apiKeyConfirmed = true;
                    showModelSelectionSection = true;
                    showTextToModelSection = true;
                    showImageToModelSection = true;
                    showModelPreviewSection = true;
                    WindowHeight = 900;
                }
            }
        }

        private void OnDisable()
        {

            if (gameObjectEditor != null)
            {
                DestroyImmediate(gameObjectEditor);
                gameObjectEditor = null;
            }

        }


        private Texture2D GenerateRoundedTexture(Color fillColor, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            float radius = height / 2f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (IsInRoundedArea(x, y, width, height, radius))
                    {
                        texture.SetPixel(x, y, fillColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return texture;
        }
        private float CalculateTextToModelPrice()
        {
            float price = 0f;

            if (selectedModelIndex == 0 || selectedModelIndex == 1)
            {
                // 无纹理，根据收费规则
                if (!texture_optional)
                {
                    price = 10f; // 无纹理文生模型
                }
                else if (texture_quality_optional == TextureQuality.Standard)
                {
                    price = 20f; // 标准纹理文生模型
                }
                else if (texture_quality_optional == TextureQuality.Detailed)
                {
                    price = 30f; // 高清纹理文生模型
                }

                // 处理 quad 和 ModelStyle 的附加费用
                if (quad_optional)
                {
                    price += 5f; // Quad 附加费用
                }
                if (style_optional != ModelStyle.Original)
                {
                    price += 5f; // 其他样式附加费用
                }
            }
            else if (selectedModelIndex == 2 || selectedModelIndex == 3)
            {
                // 仅适用于选中模型 2 或 3
                price = 20f; // 文生模型
            }

            return price;
        }

        private float CalculateImageToModelPrice()
        {
            float price = 0f;

            if (selectedModelIndex == 0 || selectedModelIndex == 1)
            {
                // 无纹理，根据收费规则
                if (!texture_optional)
                {
                    price = 20f; // 无纹理图生模型
                }
                else if (texture_quality_optional == TextureQuality.Standard)
                {
                    price = 30f; // 标准纹理图生模型
                }
                else if (texture_quality_optional == TextureQuality.Detailed)
                {
                    price = 40f; // 高清纹理图生模型
                }

                // 处理 quad 和 ModelStyle 的附加费用
                if (quad_optional)
                {
                    price += 5f; // Quad 附加费用
                }
                if (style_optional != ModelStyle.Original)
                {
                    price += 5f; // 其他样式附加费用
                }
            }
            else if (selectedModelIndex == 2 || selectedModelIndex == 3)
            {
                // 仅适用于选中模型 2 或 3
                price = 30f; // 图生模型
            }

            return price;
        }

        // 判断像素是否在圆角区域内
        private bool IsInRoundedArea(int x, int y, int width, int height, float radius)
        {
            bool inLeftCircle = (x - radius) * (x - radius) + (y - radius) * (y - radius) <= radius * radius;
            bool inRightCircle = (x - (width - radius)) * (x - (width - radius)) + (y - radius) * (y - radius) <=
                                 radius * radius;

            return (x >= radius && x <= width - radius) || inLeftCircle || inRightCircle;
        }


        public void OnReceiveUserBalance(string balance)
        {
            UserBalance = balance;
            EditorPrefs.SetString(ApiKeyPrefsKey, apiKey);
            EditorPrefs.SetString(UserBalancePrefsKey, UserBalance);
            apiKeyConfirmed = true;
            showModelSelectionSection = true;
            showTextToModelSection = true;
            showImageToModelSection = true;
            showModelPreviewSection = true;
            WindowHeight = 900;
            Repaint();
        }

        public void OnGUI()
        {

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width),
                GUILayout.Height(position.height), GUILayout.MinWidth(400), // 这保证了横向滚动条不会出现
                GUILayout.MaxWidth(413));
            // 在这里放置所有需要滚动的内容
            GUILayout.Label(GUIContent.none, EditorStyles.boldLabel);
            minSize = apiKeyConfirmed ? new Vector2(413, 900) : new Vector2(413, WindowHeight);
            maxSize = new Vector2(413, WindowHeight + ExtraHeight);

            GUILayout.Space(10);
            float aspectRatio = 440f / 104f;
            float newHeight = 22;
            float newWidth = newHeight * aspectRatio;
            GUILayout.BeginHorizontal();
            Rect headerRect = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none, GUILayout.Height(newHeight),
                GUILayout.Height(30));
            headerRect.x = 25;
            headerRect.height = newHeight;
            headerRect.width = newWidth;
            GUILayout.Space(10);

            GUI.DrawTexture(headerRect, headerImage);
            Rect iconAndTextRect = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none,
                GUILayout.Height(newHeight), GUILayout.Height(30));
            iconAndTextRect.x = 300; // 使图标和文本右对齐
            iconAndTextRect.width = 20; // 设置图标的宽度
            iconAndTextRect.height = 20; // 设置图标的高度

            // 绘制图标
            GUI.DrawTexture(iconAndTextRect, UserBalanceCoin);

            // 计算文本的位置

            Rect textRect = new Rect(iconAndTextRect.x + iconAndTextRect.width + 5, iconAndTextRect.y, 60,
                iconAndTextRect.height);
            GUI.Label(textRect, UserBalance == "" ? ": ----" : ": " + UserBalance, balanceStyle);

            GUILayout.EndHorizontal();

            //GUILayout.Label("Setup Your API Key", headerStyle);
            EditorGUI.BeginDisabledGroup(apiKeyConfirmed);
            GUILayout.BeginHorizontal();

            Rect ApiFieldRect = GUILayoutUtility.GetRect(GUIContent.none, textFieldStyle, GUILayout.Height(20));

            if (string.IsNullOrEmpty(apiKey) && !apiKeyConfirmed)
            {

                GUI.color = Color.gray;
                apiKey = EditorGUI.PasswordField(ApiFieldRect, apiKey, textFieldStyle);
                GUI.color = Color.white;
                if (string.IsNullOrEmpty(textPrompt))
                {
                    GUI.Label(ApiFieldRect, "API key, begins with tsk_...", placeholderStyle); // 显示提示文本
                }

            }
            else
            {
                apiKey = EditorGUI.PasswordField(ApiFieldRect, apiKey, textFieldStyle);
                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                {
                    Event.current.Use();
                    ConfirmApiKey();
                }
            }

            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(apiKeyConfirmed ? "\u21a9\ufe0e" : "\u221a", squareButtonStyle))
            {
                ConfirmApiKey();
            }

            GUILayout.EndHorizontal();


            void ConfirmApiKey()
            {
                // Check if the apiKey is already confirmed
                if (apiKeyConfirmed)
                {
                    apiKeyConfirmed = false;
                    EditorPrefs.SetString(ApiKeyPrefsKey, "");
                }
                else
                {
                    // Validate the format of apiKey
                    StartEditorCoroutine(GetUserBalance());
                }
            }

            GUILayout.Space(10);
            if (!apiKeyConfirmed && !showTextToModelSection)
            {
                EditorGUI.indentLevel++;
                showInstructions = EditorGUILayout.Foldout(showInstructions, "How to Get Your API Key");

                if (showInstructions)
                {
                    WindowHeight = 420;
                    EditorGUI.indentLevel--;
                    EditorGUILayout.HelpBox(
                        "\n1. Click the button below to visit Tripo api platform\n" +
                        "2. Log into your account.\n" +
                        "3. Apply for an API key as shown in the example image.\n"
                        ,
                        MessageType.Info
                    );
                    EditorGUI.indentLevel++;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10); // Left margin
                    GUILayout.Label(apikeyInstruct, GUILayout.Width(330),
                        GUILayout.Height(140)); // Adjust size as needed
                    GUILayout.Space(10); // Right margin
                    GUILayout.EndHorizontal();
                    // Button to open the URL directly
                    if (GUILayout.Button("Visit API Platform", buttonStyle, GUILayout.Width(125)))
                    {
                        Application.OpenURL("https://platform.tripo3d.ai/api-keys");
                    }

                }
                else
                {
                    WindowHeight = 170;
                }

                EditorGUI.indentLevel--;
            }

            if (showTextToModelSection)
            {
                GUILayout.Label("Text to Model", headerStyle);
                //EditorGUILayout.HelpBox("Enter the text prompt and click 'Generate' to create a model.", MessageType.Info);
                GUILayout.BeginHorizontal();

                Rect textFieldRect = GUILayoutUtility.GetRect(GUIContent.none, textFieldStyle, GUILayout.Height(30));
                textPrompt = EditorGUI.TextField(textFieldRect, textPrompt, textFieldStyle);
                if (string.IsNullOrEmpty(textPrompt))
                {
                    GUI.Label(textFieldRect, "Enter the text prompt here...", placeholderStyle); // 显示提示文本
                }


                GUI.enabled = !isTextToModelCoroutineRunning;
                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                {
                    Event.current.Use();
                    Text_to_Model_func();
                    isTextToModelCoroutineRunning = true;
                    TextToModelBtnString = "Uploading...";
                }

                if (GUILayout.Button(new GUIContent(TextToModelBtnString, $"Cost: {CalculateTextToModelPrice()}"), buttonStyle,
                        GUILayout.Width(100)))
                {

                    if (Convert.ToInt32(UserBalance) - CalculateTextToModelPrice() < 0)
                    {
                        EditorUtility.DisplayDialog("Insufficient Balance",
                            "The remaining balance is insufficient. Please go to https://platform.tripo3d.ai/billing to recharge.'.",
                            "OK");
                    }
                    else
                    {
                        Text_to_Model_func();
                        isTextToModelCoroutineRunning = true;
                        TextToModelBtnString = "Uploading...";
                    }

                }

                GUILayout.EndHorizontal();
                GUI.enabled = true;
                GUILayout.Space(10);
                Rect progressBarRect =
                    GUILayoutUtility.GetRect(GUIContent.none, progressBarBackgroundStyle, GUILayout.Height(7));
                GUI.Box(progressBarRect, GUIContent.none, progressBarBackgroundStyle);
                // 进度条填充

                fillStyle.normal.background = roundedFillTextureText;
                if (textToModelProgress > 0)
                {
                    float fillWidth = 380f * textToModelProgress;
                    Rect fillRect = new Rect(progressBarRect.x, progressBarRect.y, fillWidth, progressBarRect.height);
                    roundedFillTextureText = GenerateRoundedTexture(new Color(248f / 255f, 207f / 255f, 0f),
                        (int)(fillWidth * 10f), 70);
                    GUI.Box(fillRect, GUIContent.none, fillStyle);
                }


                GUILayout.Space(10);
                //GUILayout.Box(GUIContent.none, separatorStyle);
            }

            // Image to Model Section
            if (showImageToModelSection)
            {
                GUILayout.Label("Image to Model", headerStyle);
                //EditorGUILayout.HelpBox("Upload an image and click 'Generate' to convert it into a model.", MessageType.Info);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                if (GUILayout.Button("", imageUploadBtnStyle, GUILayout.Width(280)))
                {
                    string path = EditorUtility.OpenFilePanel("Select an Image", "", "jpg,png");
                    if (!string.IsNullOrEmpty(path))
                    {
                        imagePath = path;
                        uploadedImage = new Texture2D(2, 2);
                        uploadedImage.LoadImage(System.IO.File.ReadAllBytes(imagePath));
                    }
                }

                Rect buttonRect = GUILayoutUtility.GetLastRect(); // 获取按钮的 Rect
                float targetHeight = 110f; // 你指定的目标高度
                if (uploadedImage != null)
                {
                    // 计算等比例缩放后的宽度
                    float previewAspectRatio = (float)uploadedImage.width / uploadedImage.height; // 图片的宽高比
                    float scaledWidth = targetHeight * previewAspectRatio; // 根据目标高度计算宽度

                    // 确保图片水平居中
                    Rect imageRect = new Rect(
                        buttonRect.x + (buttonRect.width - scaledWidth) / 2 + 10, // 水平居中
                        buttonRect.y + (buttonRect.height / 2) - (targetHeight / 2), // 垂直居中
                        scaledWidth, // 等比例缩放后的宽度
                        targetHeight // 固定高度
                    );

                    // 绘制图片
                    GUI.DrawTexture(imageRect, uploadedImage, ScaleMode.ScaleToFit);
                }
                else if (imagePreviewTexture != null)
                {
                    // 计算等比例缩放后的宽度

                    float scaledWidth = 200f;

                    // 确保图片水平居中
                    Rect imageRect = new Rect(
                        buttonRect.x + (buttonRect.width - scaledWidth) / 2 + 10, // 水平居中
                        buttonRect.y + (buttonRect.height / 2) - (targetHeight / 2), // 垂直居中
                        scaledWidth, // 等比例缩放后的宽度
                        targetHeight // 固定高度
                    );

                    // 绘制默认的背景图片
                    GUI.DrawTexture(imageRect, imagePreviewTexture, ScaleMode.ScaleToFit);
                }


                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Space(110); // 精确控制按钮位置，偏移大按钮高度-按钮自身高度
                GUI.enabled = !isImageToModelCoroutineRunning;
                if (GUILayout.Button(new GUIContent(ImageToModelBtnString, $"Cost: {CalculateImageToModelPrice()}"), buttonStyle,
                        GUILayout.Width(100), GUILayout.Height(30)))
                {
                    // 检查 imagePath 是否为空
                    if (string.IsNullOrEmpty(imagePath))
                    {
                        EditorUtility.DisplayDialog("Invalid Image Path",
                            "The image path is empty. Please select an image before proceeding.",
                            "OK");
                    }
                    else if (Convert.ToInt32(UserBalance) - CalculateImageToModelPrice() < 0)
                    {
                        EditorUtility.DisplayDialog("Insufficient Balance",
                            "The remaining balance is insufficient. Please go to https://platform.tripo3d.ai/billing to recharge.",
                            "OK");
                    }
                    else
                    {
                        StartEditorCoroutine(ImageToModelCoroutine());
                        isImageToModelCoroutineRunning = true;
                        ImageToModelBtnString = "Uploading...";
                    }
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.enabled = true;
                Rect progressBarRect =
                    GUILayoutUtility.GetRect(GUIContent.none, progressBarBackgroundStyle, GUILayout.Height(7));
                GUI.Box(progressBarRect, GUIContent.none, progressBarBackgroundStyle);

                // 进度条填充
                fillStyle.normal.background = roundedFillTextureImage;
                if (imageToModelProgress > 0)
                {
                    float fillWidth = 380f * imageToModelProgress;
                    Rect fillRect = new Rect(progressBarRect.x, progressBarRect.y, fillWidth, progressBarRect.height);
                    roundedFillTextureImage = GenerateRoundedTexture(new Color(248f / 255f, 207f / 255f, 0f),
                        (int)(fillWidth * 10f), 70);
                    GUI.Box(fillRect, GUIContent.none, fillStyle);
                }
            }



            if (showModelPreviewSection)
            {
                //GUILayout.Space(10);
                // 选择 FBX 模型
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("Result Preview", headerStyle);
                selectedObject = (GameObject)EditorGUILayout.ObjectField(
                    gameObject,
                    typeof(GameObject),
                    false // 这里设置为 false，允许选择资产（如 FBX）但不包括场景对象
                );
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
                GUILayout.Space(20);
                // 当选择了新的 GameObject，重置 Editor 实例
                if (selectedObject != gameObject)
                {
                    gameObject = selectedObject;
                    if (gameObjectEditor != null)
                    {
                        DestroyImmediate(gameObjectEditor); // 销毁旧的 Editor 实例
                    }

                    if (gameObject != null)
                    {
                        gameObjectEditor = Editor.CreateEditor(gameObject);
                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                Rect previewRect = GUILayoutUtility.GetRect(380, 380); // 固定大小的绘制区域



// 固定尺寸的绘制区域
                Rect fixedRect = new Rect(previewRect.x, previewRect.y, 380, 380);

                if (gameObject != null)
                {
                    gameObjectEditor.OnInteractivePreviewGUI(fixedRect, transparentBackground);
                }
                else
                {
                    GUI.DrawTexture(fixedRect, modelPreviewBackground, ScaleMode.StretchToFill);
                }
                GUILayout.EndHorizontal();
            }


            // Model Selection Section
            if (showModelSelectionSection)
            {
                GUILayout.Space(15);
                GUILayout.BeginHorizontal();
                GUILayout.Space(7);
                GUILayout.Label("Model Selection", headerStyle);
                selectedModelIndex = EditorGUILayout.Popup(GUIContent.none, selectedModelIndex, modelOptions);
                GUILayout.Space(7);
                GUILayout.EndHorizontal();

                // 当selectedModelIndex等于0时，显示高级设置区域
                if (selectedModelIndex is 0 or 1)
                {
                    CheckAndCloseBumpMapSettingsFixingWindow();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(23);
                    GUILayout.BeginVertical();
                    GUILayout.Space(12);
                    GUILayout.Label(
                        new GUIContent("Model Face Limits", "Limits the number of faces on the output model"),
                        headerStyle, GUILayout.Width(120)); // 调整标签宽度
                    GUILayout.EndVertical();
                    Rect faceLimitFieldRect =
                        GUILayoutUtility.GetRect(GUIContent.none, textFieldStyle, GUILayout.Width(100));
                    face_limit = EditorGUI.IntField(faceLimitFieldRect, face_limit, textFieldStyle);
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Space(10);
                    EditorGUI.indentLevel++;
                    EditorGUI.indentLevel++;
                    quad_optional = EditorGUILayout.Toggle(
                        new GUIContent("Quad-Mesh", "Enable quad-mesh model output."),
                        quad_optional);
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                    

                    GUILayout.Space(10);
                    EditorGUI.indentLevel++;
                    advancedSettingsFoldout =
                        EditorGUILayout.Foldout(advancedSettingsFoldout, "Advanced settings (Optional)", true);
                    ExtraHeight = 60;
                    if (advancedSettingsFoldout)
                    {
                        ExtraHeight = 200;
                        EditorGUI.indentLevel++; // 增加缩进
                        texture_optional = EditorGUILayout.Toggle(
                            new GUIContent("Texture", "set False to get a model without any textures"),
                            texture_optional);
                        pbr_optional =
                            EditorGUILayout.Toggle(new GUIContent("pbr", "set False to get a model without pbr"),
                                pbr_optional);
                        texture_quality_optional = (TextureQuality)EditorGUILayout.EnumPopup(
                            new GUIContent("Texture Quality", "This parameter controls the texture quality"),
                            texture_quality_optional);
                        texture_alignment_optional = (TextureAlignment)EditorGUILayout.EnumPopup(
                            new GUIContent("Texture Alignment",
                                "Determines the prioritization of texture alignment in the 3D model"),
                            texture_alignment_optional);
                        autosize_optional = EditorGUILayout.Toggle(
                            new GUIContent("Auto Size",
                                "Automatically scale the model to real-world dimensions, with the unit in meters"),
                            autosize_optional);
                        style_optional = (ModelStyle)EditorGUILayout.EnumPopup(
                            new GUIContent("Style",
                                "Defines the artistic style or transformation to be applied to the 3D model"),
                            style_optional);
                        orientation_optional = (Orientation)EditorGUILayout.EnumPopup(
                            new GUIContent("Orientation",
                                "Set orientation=align_image to automatically rotate the model to align the original image"),
                            orientation_optional);
                        EditorGUI.indentLevel--; // 减少缩进
                    }
                }
                else
                {
                    ExtraHeight = 0;
                }

            }

            GUILayout.Space(15);
            GUILayout.EndScrollView();
        }

        // Placeholder for the Text_to_Model function
        private void Text_to_Model_func()
        {
            string selectedModel = modelOptions[selectedModelIndex];
            StartEditorCoroutine(TextPromptsToModel());
        }


        private static void StartEditorCoroutine(IEnumerator coroutine)
        {
            if (coroutines.Count == 0)
            {
                EditorApplication.update += UpdateCoroutines;
            }

            coroutines.Add(coroutine);
        }

        private static void UpdateCoroutines()
        {
            for (int i = coroutines.Count - 1; i >= 0; i--)
            {
                if (!coroutines[i].MoveNext())
                {
                    coroutines.RemoveAt(i);
                }
            }

            if (coroutines.Count == 0)
            {
                EditorApplication.update -= UpdateCoroutines;
            }
        }
        private IEnumerator DownloadGLBModel(string fileUrl, string savePath, bool fromTextToModel)
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string uniquePath = GetUniqueFilePath(savePath);

            using (UnityWebRequest uwr = UnityWebRequest.Get(fileUrl))
            {
                uwr.downloadHandler = new DownloadHandlerFile(uniquePath);

                uwr.SendWebRequest();

                float timeout = 1000000f;
                float elapsedTime = 0f;

                while (!uwr.isDone && elapsedTime < timeout)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (!uwr.isDone)
                {
                    uwr.Abort();
                    Debug.LogError("Download timed out.");
                    yield break;
                }

                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading file: {uwr.error}");
                    StartEditorCoroutine(DownloadGLBModel(fileUrl, savePath, fromTextToModel));
                    yield break;
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    StartEditorCoroutine(GetUserBalance());
                    if (fromTextToModel)
                    {
                        textToModelProgress = 0;
                        textToModelGenerateProgress = 0;
                        textToModelConvertProgress = 0;
                        isTextToModelCoroutineRunning = false;
                        TextToModelBtnString = "Generate";
                    }
                    else
                    {
                        imageToModelProgress = 0;
                        imageToModelGenerateProgress = 0;
                        imageToModelConvertProgress = 0;
                        isImageToModelCoroutineRunning = false;
                        ImageToModelBtnString = "Generate";
                    }

                    Repaint();
                    AssetDatabase.Refresh();
                    //GLBPostProcessing(uniquePath); // GLB 文件后处理函数
                    //AssetDatabase.Refresh();
                    ShowPreviewModel(uniquePath); // 显示模型预览
                }
                else
                {
                    Debug.LogError($"Unexpected result: {uwr.result}");
                }
            }
        }

        private IEnumerator DownloadFBXModel(string fileUrl, string savePath, bool fromTextToModel)
        {
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string uniquePath = GetUniqueFilePath(savePath);

            using (UnityWebRequest uwr = UnityWebRequest.Get(fileUrl))
            {
                uwr.downloadHandler = new DownloadHandlerFile(uniquePath);

                uwr.SendWebRequest();

                float timeout = 1000000f;
                float elapsedTime = 0f;

                while (!uwr.isDone && elapsedTime < timeout)
                {
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (!uwr.isDone)
                {
                    uwr.Abort();
                    Debug.LogError("Download timed out.");
                    yield break;
                }

                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error downloading file: {uwr.error}");
                    StartEditorCoroutine(DownloadFBXModel(fileUrl, savePath, fromTextToModel));
                    yield break;
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    StartEditorCoroutine(GetUserBalance());
                    if (fromTextToModel)
                    {
                        textToModelProgress = 0;
                        textToModelGenerateProgress = 0;
                        textToModelConvertProgress = 0;
                        isTextToModelCoroutineRunning = false;
                        TextToModelBtnString = "Generate";
                    }
                    else
                    {
                        imageToModelProgress = 0;
                        imageToModelGenerateProgress = 0;
                        imageToModelConvertProgress = 0;
                        isImageToModelCoroutineRunning = false;
                        ImageToModelBtnString = "Generate";
                    }

                    Repaint();
                    AssetDatabase.Refresh();
                    ModelPostProcessing(uniquePath);
                    AssetDatabase.Refresh();
                    ShowPreviewModel(uniquePath);
                }
                else
                {
                    Debug.LogError($"Unexpected result: {uwr.result}");
                }
            }
        }



        private static string GetUniqueFilePath(string originalPath)
        {
            // 获取文件的基本信息
            int count = 1;
            string filePathWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string fileExtension = Path.GetExtension(originalPath);
            string directory = Path.GetDirectoryName(originalPath);

            // 创建新的文件夹名称，例如 TripoModel_1
            string newDirectory = Path.Combine(directory, $"{filePathWithoutExtension}_{count}");

            // 确保该文件夹存在
            while (Directory.Exists(newDirectory))
            {
                // 如果文件夹已存在，增加计数
                count++;
                newDirectory = Path.Combine(directory, $"{filePathWithoutExtension}_{count}");
            }

            // 创建新的文件夹
            Directory.CreateDirectory(newDirectory);

            // 设置新的文件路径
            string newFilePath = Path.Combine(newDirectory, $"{filePathWithoutExtension}{fileExtension}");

            return newFilePath;
        }


        private void ModelPostProcessing(string assetPath)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter != null)
            {
                string directoryPath = Path.GetDirectoryName(assetPath);
                modelImporter.ExtractTextures(directoryPath); // 提取纹理
                AssetDatabase.Refresh();
                // 检查目录中的所有文件
                foreach (string filePath in Directory.GetFiles(directoryPath))
                {
                    if (Path.GetExtension(filePath).ToLower() == ".png" || Path.GetExtension(filePath).ToLower() == ".jpg")
                    {
                        string fileName = Path.GetFileName(filePath);
                        Debug.Log(fileName);
                        if (fileName.StartsWith("Normal"))
                        {
                            // 将文件导入Unity项目中（如果尚未导入）
                            string unityPath = directoryPath.Replace("\\", "/") + "/" + fileName;
                            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(unityPath);
                            
                            // 检查texture是否为null，避免未找到文件时出错
                            if (texture != null)
                            {
                                Debug.Log("Convert: "+unityPath);
                                TextureImporter textureImporter = TextureImporter.GetAtPath(unityPath) as TextureImporter;
                                if (textureImporter != null)
                                {
                                    
                                    textureImporter.textureType = TextureImporterType.NormalMap;
                                    textureImporter.SaveAndReimport();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CheckAndCloseBumpMapSettingsFixingWindow()
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.BumpMapSettingsFixingWindow");
            if (type != null && HasOpenInstances(type))
            {
                var windows = Resources.FindObjectsOfTypeAll(type);
                foreach (var window in windows)
                {
                    if (window is EditorWindow editorWindow)
                    {
                        editorWindow.Close();
                    }
                }
            }
        }

        public static bool HasOpenInstances(Type t)
        {
            UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(t);
            return objectsOfTypeAll != null && objectsOfTypeAll.Length != 0;
        }

        private void ShowPreviewModel(string assetPath)
        {
            // 加载 FBX 模型资源
            GameObject loadedModel = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (loadedModel != null)
            {
                loadedModel.GetComponent<Transform>().localRotation = Quaternion.Euler(90, 0, 90);
                loadedModel.GetComponent<Transform>().localScale = Vector3.one;
                selectedObject = loadedModel;
                gameObject = loadedModel;
                if (gameObjectEditor != null)
                {
                    DestroyImmediate(gameObjectEditor);
                }

                if (gameObject != null)
                {
                    gameObjectEditor = Editor.CreateEditor(gameObject);
                }
            }
            else
            {
                Debug.LogError("Failed to load model at path: " + assetPath);
            }

            GetWindow<Tripo_main_window>().Repaint();
        }

        private IEnumerator ImageToModelCoroutine()
        {
            imageToModelConvertProgress = 0f;
            imageToModelGenerateProgress = 0f;
            imageToModelProgress = 0f;
            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.LogError("No image selected for upload.");
                yield break;
            }

            string url = "https://api.tripo3d.ai/v2/openapi/upload";


            // Load image file
            byte[] imageData = File.ReadAllBytes(imagePath);

            // Creating a WWWForm
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageData, Path.GetFileName(imagePath), "image/jpeg");

            // Creating the UnityWebRequest
            using (UnityWebRequest uwr = UnityWebRequest.Post(url, form))
            {
                // Set the authorization header
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                uwr.downloadHandler = new DownloadHandlerBuffer();
                // Send the request and wait for the response
                yield return uwr.SendWebRequest();
                float timeout = 5000f; // Maximum wait time
                float timeElapsed = 0f;
                float interval = 0.5f; // Check interval
                // Check for errors
                while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                {
                    timeElapsed += interval;
                    yield return new WaitForSeconds(interval);
                }

                if (uwr.result == UnityWebRequest.Result.InProgress)
                {
                    Debug.LogWarning("Request timed out.");
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseText = uwr.downloadHandler.text;

                        ImageResponseData jsonResponse = JsonUtility.FromJson<ImageResponseData>(responseText);

                        if (jsonResponse.code == 0)
                        {
                            imageToken = jsonResponse.data.image_token;
                            StartEditorCoroutine(PostImageToModel());
                        }
                        else
                        {
                            Debug.LogError("Error code received: " + jsonResponse.code);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error parsing response: " + e.Message);
                    }
                }

                uwr.Dispose();
            }
        }

        private IEnumerator TextPromptsToModel()
        {

            string url = "https://api.tripo3d.ai/v2/openapi/task";
            textToModelProgress = 0f;
            textToModelGenerateProgress = 0f;
            textToModelConvertProgress = 0f;
            string jsonData;
            // 构建要发送的数据对象
            if (selectedModelIndex is 0 or 1)
            {
                if (style_optional == ModelStyle.Original)
                {
                    TextPromptsRequestData requestData = new TextPromptsRequestData
                    {
                        type = "text_to_model",
                        model_version = modelOptions[selectedModelIndex],
                        prompt = textPrompt,
                        face_limit = face_limit,
                        texture = texture_optional,
                        pbr = pbr_optional,
                        texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                        texture_quality = textureQualityOptions[(int)texture_quality_optional],
                        auto_size = autosize_optional,
                        orientation = orientationOptions[(int)orientation_optional],
                        quad = quad_optional

                    };
                    jsonData = JsonUtility.ToJson(requestData);
                }
                else
                {
                    TextPromptsRequestData_withStyle requestData = new TextPromptsRequestData_withStyle()
                    {
                        type = "text_to_model",
                        model_version = modelOptions[selectedModelIndex],
                        prompt = textPrompt,
                        face_limit = face_limit,
                        texture = texture_optional,
                        pbr = pbr_optional,
                        texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                        texture_quality = textureQualityOptions[(int)texture_quality_optional],
                        auto_size = autosize_optional,
                        style = styleOptions[(int)style_optional],
                        orientation = orientationOptions[(int)orientation_optional],
                        quad = quad_optional
                    };
                    jsonData = JsonUtility.ToJson(requestData);
                }

            }
            else
            {
                TextPromptsRequestData_lowVersion requestData = new TextPromptsRequestData_lowVersion()
                {
                    type = "text_to_model",
                    model_version = modelOptions[selectedModelIndex],
                    prompt = textPrompt,
                };
                jsonData = JsonUtility.ToJson(requestData);
            }


            // 把JSON字符串转换成字节数据
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // 初始化UnityWebRequest，并设置为POST方法
            using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
            {
                // 设置上传的数据
                uwr.uploadHandler = new UploadHandlerRaw(postData);

                // 设置请求头
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                // 发送请求并等待响应
                yield return uwr.SendWebRequest();
                float timeout = 5000f; // Maximum wait time
                float timeElapsed = 0f;
                float interval = 0.5f; // Check interval
                // Check for errors
                while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                {
                    timeElapsed += interval;
                    yield return new WaitForSeconds(interval);
                }

                // Check the results appropriately
                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {uwr.error}, {uwr.result}");
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (uwr.downloadHandler != null)
                        {
                            string jsonResponse = uwr.downloadHandler.text;

                            // Deserialize and process the response
                            TextTaskResponse response = JsonUtility.FromJson<TextTaskResponse>(jsonResponse);

                            if (response != null && response.code == 0)
                            {

                                StartEditorCoroutine(GetTaskProgressAndOutput(response.data.task_id, true));

                            }
                            else
                            {
                                Debug.LogError("Error in response data.");
                            }
                        }
                        else
                        {
                            Debug.LogError("DownloadHandler is null, failed to retrieve the response.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error parsing response: " + e.Message);
                    }
                }

                // Dispose to release resources
                uwr.Dispose();
            }
        }

        public UnityEvent<string> OnGetBalanceComplete = new UnityEvent<string>();

        private IEnumerator GetUserBalance()
        {
            // 设置请求头
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", $"Bearer {apiKey}" }
            };

            // 初始化UnityWebRequest，并设置为GET方法
            using (UnityWebRequest uwr = UnityWebRequest.Get("https://api.tripo3d.ai/v2/openapi/user/balance"))
            {
                // 设置请求头
                foreach (var header in headers)
                {
                    uwr.SetRequestHeader(header.Key, header.Value);
                }

                // 发送请求并等待响应
                yield return uwr.SendWebRequest();
                float timeout = 5000f; // Maximum wait time
                float timeElapsed = 0f;
                float interval = 0.5f; // Check interval
                // Check for errors
                while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                {
                    timeElapsed += interval;
                    yield return new WaitForSeconds(interval);
                }

                // Check the results appropriately
                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    if (!apiKey.StartsWith("tsk_"))
                    {
                        EditorUtility.DisplayDialog("Invalid API Key",
                            "The API Key format is incorrect. Please ensure it starts with 'tsk_'.", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Connection failed",
                            "We are unable to locate your account balance; please check your internet connection.",
                            "OK");
                    }
                    //Debug.LogError($"Error: {uwr.error}, {uwr.result}");
                }
                else
                {
                    try
                    {
                        // 解析响应内容为JSON
                        var responseJson = JsonUtility.FromJson<BalanceResponseDataWrapper>(uwr.downloadHandler.text);

                        // 检查返回的数据是否包含余额信息
                        if (responseJson != null && responseJson.code == 0 && responseJson.data != null)
                        {
                            // 获取余额
                            int balance = responseJson.data.balance;
                            OnGetBalanceComplete.Invoke(balance.ToString());
                        }
                        else
                        {
                            if (!apiKey.StartsWith("tsk_"))
                            {
                                EditorUtility.DisplayDialog("Invalid API Key",
                                    "The API Key format is incorrect. Please ensure it starts with 'tsk_'.", "OK");
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Connection failed",
                                    "We are unable to locate your account balance; please check your internet connection.",
                                    "OK");
                            }
                            //Debug.LogError("Error in response data.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing response: {e.Message}");
                    }
                }
            }
        }

        // 在Unity编辑器中调用此方法以开始协程

        private IEnumerator GLB_to_FBX_Request(string original_model_task_id, bool isTextToImage)
        {
            string url = "https://api.tripo3d.ai/v2/openapi/task";
            textToModelProgress = 0f;
            // 构建要发送的数据对象
            FBXConversionRequestData requestData = new FBXConversionRequestData
            {
                type = "convert_model",
                format = "FBX",
                original_model_task_id = original_model_task_id
            };
            // 将数据对象序列化为JSON字符串
            string jsonData = JsonUtility.ToJson(requestData);
            // 把JSON字符串转换成字节数据
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

            // 初始化UnityWebRequest，并设置为POST方法
            using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
            {
                // 设置上传的数据
                uwr.uploadHandler = new UploadHandlerRaw(postData);

                // 设置请求头
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                // 发送请求并等待响应
                yield return uwr.SendWebRequest();
                float timeout = 5000f; // Maximum wait time
                float timeElapsed = 0f;
                float interval = 0.5f; // Check interval
                // Check for errors
                while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                {
                    timeElapsed += interval;
                    yield return new WaitForSeconds(interval);
                }

                // Check the results appropriately
                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {uwr.error}, {uwr.result}");
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (uwr.downloadHandler != null)
                        {
                            string jsonResponse = uwr.downloadHandler.text;

                            // Deserialize and process the response
                            TextTaskResponse response = JsonUtility.FromJson<TextTaskResponse>(jsonResponse);

                            if (response != null && response.code == 0)
                            {
                                StartEditorCoroutine(
                                    GetConversionTaskProgressAndOutput(response.data.task_id, isTextToImage));
                            }
                            else
                            {
                                Debug.LogError("Error in response data.");
                            }
                        }
                        else
                        {
                            Debug.LogError("DownloadHandler is null, failed to retrieve the response.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error parsing response: " + e.Message);
                    }
                }

                // Dispose to release resources
                uwr.Dispose();
            }
        }

        private IEnumerator PostImageToModel()
        {
            string url = "https://api.tripo3d.ai/v2/openapi/task";
            string jsonData;
            if (selectedModelIndex is 0 or 1)
            {
                if (style_optional == ModelStyle.Original)
                {
                    ImagePromptsRequestData requestData = new ImagePromptsRequestData
                    {
                        type = "image_to_model",
                        model_version = modelOptions[selectedModelIndex],
                        file = new ImagePromptsRequestfile
                        {
                            type = "jpg",
                            file_token = imageToken
                        },
                        face_limit = face_limit,
                        texture = texture_optional,
                        pbr = pbr_optional,
                        texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                        texture_quality = textureQualityOptions[(int)texture_quality_optional],
                        auto_size = autosize_optional,
                        orientation = orientationOptions[(int)orientation_optional],
                        quad = quad_optional
                    };
                    jsonData = JsonUtility.ToJson(requestData);
                }
                else
                {
                    ImagePromptsRequestData_withStyle requestData = new ImagePromptsRequestData_withStyle()
                    {
                        type = "image_to_model",
                        model_version = modelOptions[selectedModelIndex],
                        file = new ImagePromptsRequestfile
                        {
                            type = "jpg",
                            file_token = imageToken
                        },
                        face_limit = face_limit,
                        texture = texture_optional,
                        pbr = pbr_optional,
                        texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                        texture_quality = textureQualityOptions[(int)texture_quality_optional],
                        auto_size = autosize_optional,
                        style = styleOptions[(int)style_optional],
                        orientation = orientationOptions[(int)orientation_optional],
                        quad = quad_optional
                    };
                    jsonData = JsonUtility.ToJson(requestData);
                }
            }
            else
            {
                ImagePromptsRequestData_lowVersion requestData = new ImagePromptsRequestData_lowVersion()
                {
                    type = "image_to_model",
                    model_version = modelOptions[selectedModelIndex],
                    file = new ImagePromptsRequestfile
                    {
                        type = "jpg",
                        file_token = imageToken
                    },
                };
                jsonData = JsonUtility.ToJson(requestData);
            }
            // Adding form fields

            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
            {
                // Set headers correctly
                uwr.uploadHandler = new UploadHandlerRaw(postData);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                // Send the request and wait for the response
                yield return uwr.SendWebRequest();
                float timeout = 5000f; // Maximum wait time
                float timeElapsed = 0f;
                float interval = 0.5f; // Check interval
                // Check for errors
                while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                {
                    timeElapsed += interval;
                    yield return new WaitForSeconds(interval);
                }

                // Check the results appropriately
                if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                    uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {uwr.error}, {uwr.result}");
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = uwr.downloadHandler.text;
                        ImageTaskResponse response = JsonUtility.FromJson<ImageTaskResponse>(jsonResponse);

                        if (response.code == 0)
                        {

                            StartEditorCoroutine(GetTaskProgressAndOutput(response.data.task_id, false));

                        }
                        else
                        {
                            Debug.LogError("Error in response: " + jsonResponse);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error parsing response: " + e.Message);
                    }
                }

                uwr.Dispose();
            }
        }

        public IEnumerator GetTaskProgressAndOutput(string taskId, bool isTextToImage)
        {
            if (isTextToImage)
            {
                isTextToModelCoroutineRunning = true;
            }
            else
            {
                isImageToModelCoroutineRunning = true;
            }

            string url = $"https://api.tripo3d.ai/v2/openapi/task/{taskId}";
            
            while (textToModelProgress < 1f)
            {
                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                    // Send the request
                    yield return uwr.SendWebRequest();
                    float timeout = 5000f; // Maximum wait time
                    float timeElapsed = 0f;
                    float interval = 0.5f; // Check interval
                    
                    // Check for errors
                    while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                    {
                        timeElapsed += interval;
                        yield return new WaitForSeconds(interval);
                    }

                    // Handle the response
                    if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                        uwr.result == UnityWebRequest.Result.ProtocolError)
                    {
                        HandleError($"Network error: {uwr.error}", isTextToImage);
                        break; // Exit the loop on error
                    }
                    
                    // Successful response
                    try
                    {
                        string jsonResponse = uwr.downloadHandler.text;
                        TaskSearchResponse response = JsonUtility.FromJson<TaskSearchResponse>(jsonResponse);

                        if (response != null && response.code == 0)
                        {
                            float progress = response.data.progress;
                            TaskSearchOutputData output = response.data.output;

                            if (isTextToImage)
                            {
                                textToModelGenerateProgress = progress / 100;
                                textToModelProgress = textToModelGenerateProgress;
                                TextToModelBtnString = (textToModelProgress * 100) + "%";
                            }
                            else
                            {
                                imageToModelGenerateProgress = progress / 100;
                                imageToModelProgress = imageToModelGenerateProgress;
                                ImageToModelBtnString = (imageToModelProgress * 100) + "%";
                            }

                            Repaint();

                            if (progress > 99)
                            {
                                // Finalize progress
                                CompleteTask(response, output, isTextToImage);
                                break; // Exit the loop when the task is complete
                            }
                        }
                        else
                        {
                            HandleError("An error occurred with the response data.", isTextToImage);
                            break;
                        }
                    }
                    catch (System.Exception e)
                    {
                        HandleError($"Error parsing response: {e.Message}", isTextToImage);
                        break; // Exit the loop on parsing error
                    }
                }

                yield return new WaitForSeconds(1f); // Wait before checking again
            }
        }

        private void HandleError(string message, bool isTextToImage)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("Error", message, "OK");
            if (isTextToImage)
            {
                isTextToModelCoroutineRunning = false;
            }
            else
            {
                isImageToModelCoroutineRunning = false;
            }
        }

        private void CompleteTask(TaskSearchResponse response, TaskSearchOutputData output, bool isTextToImage)
        {
            if (isTextToImage)
            {
                textToModelGenerateProgress = 1f;
                textToModelProgress = textToModelGenerateProgress;
                TextToModelBtnString = (textToModelProgress * 100) + "%";
                TextToModelBtnString = "Downloading...";
            }
            else
            {
                imageToModelGenerateProgress = 1f;
                imageToModelProgress = imageToModelGenerateProgress;
                ImageToModelBtnString = (imageToModelProgress * 100) + "%";
                ImageToModelBtnString = "Downloading...";
            }

            Repaint();
            
            string fileName, savePath;

            // Determine the file to download based on the response data
            if (response.data.input.pbr)
            {
                fileName = response.data.input.quad ? "TripoModel.fbx" : "TripoModel.glb";
                savePath = Path.Combine(saveDirectory, fileName);
                StartEditorCoroutine(DownloadFBXModel(output.pbr_model, savePath, isTextToImage));
            }
            else
            {
                fileName = response.data.input.quad ? "TripoModel.fbx" : "TripoModel.glb";
                savePath = Path.Combine(saveDirectory, fileName);
                StartEditorCoroutine(DownloadGLBModel(output.model, savePath, isTextToImage));
            }
        }



        public IEnumerator GetConversionTaskProgressAndOutput(string taskId, bool isTextToModel)
        {

            string url = $"https://api.tripo3d.ai/v2/openapi/task/{taskId}";
            while (textToModelProgress < 1f)
            {
                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                    // Send the request
                    yield return uwr.SendWebRequest();
                    float timeout = 5000f; // Maximum wait time
                    float timeElapsed = 0f;
                    float interval = 0.5f; // Check interval
                    // Check for errors
                    while (uwr.result == UnityWebRequest.Result.InProgress && timeElapsed < timeout)
                    {
                        timeElapsed += interval;
                        yield return new WaitForSeconds(interval);
                    }

                    // Handle the response
                    if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                        uwr.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError($"Error: {uwr.error}");
                        break; // Exit the loop on error
                    }
                    else if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            string jsonResponse = uwr.downloadHandler.text;

                            TaskSearchResponse response = JsonUtility.FromJson<TaskSearchResponse>(jsonResponse);

                            if (response != null && response.code == 0)
                            {
                                float progress = response.data.progress;
                                TaskSearchOutputData output = response.data.output;
                                if (isTextToModel)
                                {
                                    textToModelConvertProgress = progress / 100;
                                    textToModelProgress =
                                        (textToModelGenerateProgress + textToModelConvertProgress) / 2;
                                    TextToModelBtnString = (textToModelProgress * 100) + "%";
                                }
                                else
                                {
                                    imageToModelConvertProgress = progress / 100;
                                    imageToModelProgress =
                                        (imageToModelGenerateProgress + imageToModelConvertProgress) / 2;
                                    ImageToModelBtnString = (imageToModelProgress * 100) + "%";
                                }

                                Repaint();


                                if (progress > 99)
                                {
                                    string fileName = "TripoModel.fbx";
                                    string savePath = Path.Combine(saveDirectory, fileName);
                                    StartEditorCoroutine(DownloadFBXModel(output.model, savePath, isTextToModel));

                                    if (isTextToModel)
                                    {

                                        textToModelConvertProgress = 1f;
                                        textToModelProgress =
                                            (textToModelGenerateProgress + textToModelConvertProgress) / 2;
                                        TextToModelBtnString = "Downloading...";
                                    }
                                    else
                                    {
                                        imageToModelConvertProgress = 1f;
                                        imageToModelProgress =
                                            (imageToModelGenerateProgress + imageToModelConvertProgress) / 2;
                                        ImageToModelBtnString = "Downloading...";
                                    }

                                    Repaint();
                                    break; // Exit the loop when the task is complete
                                }
                            }
                            else
                            {
                                Debug.LogError("Error in response data.");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Error parsing response: " + e.Message);
                        }
                    }
                }

                yield return new WaitForSeconds(1f); // Wait before checking again
            }


        }

        [System.Serializable]
        private class TaskSearchResponse
        {
            public int code;
            public TaskSearchData data;
        }

        [System.Serializable]
        private class TaskSearchData
        {
            public string task_id;
            public string type;
            public string status;
            public TaskSearchInput input;
            public TaskSearchOutputData output;
            public int progress;
            public long create_time;
        }

        [System.Serializable]
        private class TaskSearchInput
        {
            public string prompt;
            public string model_version;
            public bool pbr;
            public bool quad;

        }

        [System.Serializable]
        private class TaskSearchOutputData
        {
            public string model;
            public string base_model;
            public string pbr_model;
            public string rendered_image;
        }

        [Serializable]
        public class FBXConversionRequestData
        {
            public string type;
            public string format;
            public string original_model_task_id;
        }

        [Serializable]
        public class TextPromptsRequestData_lowVersion
        {
            public string type;
            public string model_version;
            public string prompt;
        }

        [Serializable]
        public class TextPromptsRequestData
        {
            public string type;
            public string model_version;
            public string prompt;
            public int face_limit;
            public bool texture;
            public bool pbr;
            public string texture_alignment;
            public string texture_quality;
            public bool auto_size;
            public string orientation;
            public bool quad;

        }

        [Serializable]
        public class BalanceResponseDataWrapper
        {
            public int code;
            public BalanceResponseData data;
        }

        [Serializable]
        public class BalanceResponseData
        {
            public int balance;
            public int frozen;
        }

        [Serializable]

        public class TextPromptsRequestData_withStyle
        {
            public string type;
            public string model_version;
            public string prompt;
            public int face_limit;
            public bool texture;
            public bool pbr;
            public string texture_alignment;
            public string texture_quality;
            public bool auto_size;
            public string style;
            public string orientation;
            public bool quad;
        }

        [Serializable]
        public class ImagePromptsRequestData_lowVersion
        {
            public string type;
            public string model_version;
            public ImagePromptsRequestfile file;
        }

        [Serializable]
        public class ImagePromptsRequestData
        {
            public string type;
            public string model_version;
            public ImagePromptsRequestfile file;
            public int face_limit;
            public bool texture;
            public bool pbr;
            public string texture_alignment;
            public string texture_quality;
            public bool auto_size;
            public string orientation;
            public bool quad;
        }

        [Serializable]
        public class ImagePromptsRequestData_withStyle
        {
            public string type;
            public string model_version;
            public ImagePromptsRequestfile file;
            public int face_limit;
            public bool texture;
            public bool pbr;
            public string texture_alignment;
            public string texture_quality;
            public bool auto_size;
            public string style;
            public string orientation;
            public bool quad;
        }

        [Serializable]
        public class ImagePromptsRequestfile
        {
            public string type;
            public string file_token;
        }


        [Serializable]
        private class ImageResponseData
        {
            public int code;
            public ImageData data;
        }

        [Serializable]
        private class ImageData
        {
            public string image_token;
        }

        [System.Serializable]
        private class ImageTaskResponse
        {
            public int code;
            public ImageTaskData data;
        }

        [System.Serializable]
        private class ImageTaskData
        {
            public string task_id;
        }

        [System.Serializable]
        public class TextTaskResponse
        {
            public int code;
            public TaskData data;
        }

        [System.Serializable]
        public class TaskData
        {
            public string task_id;
        }
    }
}
#endif