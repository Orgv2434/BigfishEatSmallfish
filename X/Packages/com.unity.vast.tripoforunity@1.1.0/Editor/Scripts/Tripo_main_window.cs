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
using Unity.EditorCoroutines.Editor;
using TripoForUnity;

namespace TripoForUnity
{
    public class Tripo_main_window : EditorWindow
    {
        private string GenerateModelUrl = "https://api.tripo3d.ai/v2/openapi/task";
        private string UploadImageUrl = "https://api.tripo3d.ai/v2/openapi/upload";
        int TIMEOUT_DURATION = 5000;
        float PROGRESS_CHECK_INTERVAL = 2.0f;

        // 窗口的最小尺寸
        private int WindowHeight = 170;
        private int ExtraHeight = 0;

        private string apiKey = "";
        private const string ApiKeyPrefsKey = "ApiKeyPrefsKey";
        private string UserBalance = "";
        private const string UserBalancePrefsKey = "UserBalancePrefsKey";
        private string textPrompt = "";
        private string imagePath = "";
        private Texture2D uploadedImage = null;
        private float textToModelProgress = 0f;
        private float imageToModelProgress = 0f;

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
        private bool rigging_optional = false;
        private TextureQuality texture_quality_optional = TextureQuality.Standard;
        private bool autosize_optional = false;
        private ModelStyle style_optional = ModelStyle.Original;
        private Orientation orientation_optional = Orientation.Default;
        private TextureAlignment texture_alignment_optional = TextureAlignment.OriginalImage;
        private bool quad_optional = false;

        private int selectedModel = 0;
        private readonly string[] textureQualityOptions = { "standard", "detailed" };
        private readonly string[] modelOptions =
        {
            "v2.5-20250123",
            "v2.0-20240919",
            "Turbo-v1.0-20250506",
            "v1.4-20240625",
        };
        private readonly string[] orientationOptions = { "default", "align_image" };

        private readonly string[] styleOptions =
        {
            "default",
            "person:person2cartoon",
            "object:clay",
            "object:steampunk",
            "animal:venom",
            "object:barbie",
            "object:christmas",
        };

        private readonly string[] textureAlignmentOptions = { "original_image", "geometry" };
        private static List<IEnumerator> coroutines = new List<IEnumerator>();

        //# Shift % Ctrl & Alt
        [MenuItem("Window/TripoPlugin #t")]
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
        private GUIStyle LeftHeaderStyle;
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
        private UnityEditor.Editor gameObjectEditor;

        private Vector2 scrollPosition;

        private void OnEnable()
        {
            //InitParam();
            headerImage =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/logo_small.png"
                ) as Texture2D;
            buttonBackground =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/ButtonBackground.png"
                ) as Texture2D;
            textFieldBackground =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/TextToModelInput.png"
                ) as Texture2D;
            SeparationLine =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/SeparationLine.png"
                ) as Texture2D;
            progressBarBackground =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/ProgressBarBackground.png"
                ) as Texture2D;
            modelPreviewBackground =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/modelPreviewBackground.png"
                ) as Texture2D;
            imageUploadBtnBackground =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/imageUploadBtn.png"
                ) as Texture2D;
            imagePreviewTexture =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/PreviewImageDefault.png"
                ) as Texture2D;
            apikeyInstruct =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/apikey_instruct.png"
                ) as Texture2D;
            UserBalanceCoin =
                EditorGUIUtility.Load(
                    "Packages/com.unity.vast.tripoforunity/Editor/EditorTextures/coin.png"
                ) as Texture2D;
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
                alignment = TextAnchor.MiddleCenter, // 设置内容居中
            };
            textFieldStyle = new GUIStyle
            {
                normal = { background = textFieldBackground, textColor = Color.white },
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(10, 10, 10, 10),
                border = new RectOffset(5, 5, 5, 5),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };
            placeholderStyle = new GUIStyle
            {
                normal = { textColor = Color.gray },
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 10, 10),
            };

            Texture2D CreateColoredTexture(Texture2D baseTexture, Color color)
            {
                Texture2D coloredTexture = new Texture2D(
                    baseTexture.width,
                    baseTexture.height,
                    TextureFormat.ARGB32,
                    false
                );
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
                fixedHeight = 150, // 按钮高度
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
                normal = { textColor = Color.white },
            };
            LeftHeaderStyle = new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
            };
            balanceStyle = new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = balanceColor },
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

        private void InitParam()
        {
            EditorPrefs.DeleteKey(ApiKeyPrefsKey);
            EditorPrefs.DeleteKey(UserBalancePrefsKey);
            face_limit = 10000;
            texture_optional = true;
            pbr_optional = true;
            rigging_optional = false;
            texture_quality_optional = TextureQuality.Standard;
            autosize_optional = false;
            style_optional = ModelStyle.Original;
            orientation_optional = Orientation.Default;
            texture_alignment_optional = TextureAlignment.OriginalImage;
            quad_optional = false;
            Repaint();
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

            if (selectedModel == 0 || selectedModel == 1)
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
            else if (selectedModel == 2 || selectedModel == 3)
            {
                // 仅适用于选中模型 2 或 3
                price = 20f; // 文生模型
            }

            return price;
        }

        private float CalculateImageToModelPrice()
        {
            float price = 0f;

            if (selectedModel == 0 || selectedModel == 1)
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
            else if (selectedModel == 2 || selectedModel == 3)
            {
                // 仅适用于选中模型 2 或 3
                price = 30f; // 图生模型
            }

            return price;
        }

        // 判断像素是否在圆角区域内
        private bool IsInRoundedArea(int x, int y, int width, int height, float radius)
        {
            bool inLeftCircle =
                (x - radius) * (x - radius) + (y - radius) * (y - radius) <= radius * radius;
            bool inRightCircle =
                (x - (width - radius)) * (x - (width - radius)) + (y - radius) * (y - radius)
                <= radius * radius;

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
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Width(position.width),
                GUILayout.Height(position.height),
                GUILayout.MinWidth(420), // 这保证了横向滚动条不会出现
                GUILayout.MaxWidth(600)
            );
            // 在这里放置所有需要滚动的内容
            GUILayout.Label(GUIContent.none, EditorStyles.boldLabel);
            minSize = apiKeyConfirmed ? new Vector2(420, 900) : new Vector2(420, WindowHeight);
            maxSize = new Vector2(413, WindowHeight + ExtraHeight);

            #region LogoAndApiKey
            //Logo W/H
            float aspectRatio = 440f / 104f;
            float newHeight = 22; //Logo Hieght
            float newWidth = newHeight * aspectRatio;
            GUILayout.BeginHorizontal();
            Rect headerRect = GUILayoutUtility.GetRect(
                new GUIContent(),
                GUIStyle.none,
                GUILayout.Height(newHeight),
                GUILayout.Height(30)
            );
            headerRect.x = 25;
            headerRect.height = newHeight;
            headerRect.width = newWidth;
            GUILayout.Space(10);
            GUI.DrawTexture(headerRect, headerImage);

            //Icon
            Rect iconAndTextRect = GUILayoutUtility.GetRect(
                new GUIContent(),
                GUIStyle.none,
                GUILayout.Height(newHeight),
                GUILayout.Height(30)
            );
            iconAndTextRect.x = 300; // 使图标和文本右对齐
            iconAndTextRect.width = 20; // 设置图标的宽度
            iconAndTextRect.height = 20; // 设置图标的高度

            // 绘制图标
            GUI.DrawTexture(iconAndTextRect, UserBalanceCoin);

            // 计算文本的位置

            Rect textRect = new Rect(
                iconAndTextRect.x + iconAndTextRect.width + 5,
                iconAndTextRect.y,
                60,
                iconAndTextRect.height
            );
            GUI.Label(textRect, UserBalance == "" ? ": ----" : ": " + UserBalance, balanceStyle);

            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(apiKeyConfirmed);
            GUILayout.BeginHorizontal();

            Rect ApiFieldRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                textFieldStyle,
                GUILayout.Height(20)
            );

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
                    EditorCoroutineUtility.StartCoroutineOwnerless(GetUserBalance());
                }
            }
            #endregion

            #region TextToModel
            #endregion
            GUILayout.Space(10);
            if (!apiKeyConfirmed && !showTextToModelSection)
            {
                EditorGUI.indentLevel++;
                showInstructions = EditorGUILayout.Foldout(
                    showInstructions,
                    "How to Get Your API Key"
                );

                if (showInstructions)
                {
                    WindowHeight = 420;
                    EditorGUI.indentLevel--;
                    EditorGUILayout.HelpBox(
                        "\n1. Click the button below to visit Tripo api platform\n"
                            + "2. Log into your account.\n"
                            + "3. Apply for an API key as shown in the example image.\n",
                        MessageType.Info
                    );
                    EditorGUI.indentLevel++;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10); // Left margin
                    GUILayout.Label(apikeyInstruct, GUILayout.Width(330), GUILayout.Height(140)); // Adjust size as needed
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

                Rect textFieldRect = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    textFieldStyle,
                    GUILayout.Height(30)
                );
                textPrompt = EditorGUI.TextField(textFieldRect, textPrompt, textFieldStyle);
                if (string.IsNullOrEmpty(textPrompt))
                {
                    GUI.Label(textFieldRect, "Enter the text prompt here...", placeholderStyle); // 显示提示文本
                }

                GUI.enabled = !isTextToModelCoroutineRunning;
                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                {
                    Event.current.Use();
                    EditorCoroutineUtility.StartCoroutineOwnerless(TextPromptsToModel());
                    //EditorCoroutineUtility.StartCoroutineOwnerless(TextPromptsToModel());
                    isTextToModelCoroutineRunning = true;
                    TextToModelBtnString = "Uploading...";
                }

                if (
                    GUILayout.Button(
                        new GUIContent(
                            TextToModelBtnString,
                            $"Cost: {CalculateTextToModelPrice()}"
                        ),
                        buttonStyle,
                        GUILayout.Width(100)
                    )
                )
                {
                    if (Convert.ToInt32(UserBalance) - CalculateTextToModelPrice() < 0)
                    {
                        EditorUtility.DisplayDialog(
                            "Insufficient Balance",
                            "The remaining balance is insufficient. Please go to https://platform.tripo3d.ai/billing to recharge.'.",
                            "OK"
                        );
                    }
                    else
                    {
                        EditorCoroutineUtility.StartCoroutineOwnerless(TextPromptsToModel());
                        isTextToModelCoroutineRunning = true;
                        TextToModelBtnString = "Uploading...";
                    }
                }

                GUILayout.EndHorizontal();
                GUI.enabled = true;
                GUILayout.Space(10);
                Rect progressBarRect = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    progressBarBackgroundStyle,
                    GUILayout.Height(7)
                );
                GUI.Box(progressBarRect, GUIContent.none, progressBarBackgroundStyle);
                // 进度条填充

                fillStyle.normal.background = roundedFillTextureText;
                if (textToModelProgress > 0)
                {
                    float fillWidth = 380f * textToModelProgress;
                    Rect fillRect = new Rect(
                        progressBarRect.x,
                        progressBarRect.y,
                        fillWidth,
                        progressBarRect.height
                    );
                    roundedFillTextureText = GenerateRoundedTexture(
                        new Color(248f / 255f, 207f / 255f, 0f),
                        (int)(fillWidth * 10f),
                        70
                    );
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
                if (
                    GUILayout.Button(
                        new GUIContent(
                            ImageToModelBtnString,
                            $"Cost: {CalculateImageToModelPrice()}"
                        ),
                        buttonStyle,
                        GUILayout.Width(100),
                        GUILayout.Height(30)
                    )
                )
                {
                    // 检查 imagePath 是否为空
                    if (string.IsNullOrEmpty(imagePath))
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Image Path",
                            "The image path is empty. Please select an image before proceeding.",
                            "OK"
                        );
                    }
                    else if (Convert.ToInt32(UserBalance) - CalculateImageToModelPrice() < 0)
                    {
                        EditorUtility.DisplayDialog(
                            "Insufficient Balance",
                            "The remaining balance is insufficient. Please go to https://platform.tripo3d.ai/billing to recharge.",
                            "OK"
                        );
                    }
                    else
                    {
                        EditorCoroutineUtility.StartCoroutineOwnerless(ImageToModel());
                        isImageToModelCoroutineRunning = true;
                        ImageToModelBtnString = "Uploading...";
                    }
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUI.enabled = true;
                Rect progressBarRect = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    progressBarBackgroundStyle,
                    GUILayout.Height(7)
                );
                GUI.Box(progressBarRect, GUIContent.none, progressBarBackgroundStyle);

                // 进度条填充
                fillStyle.normal.background = roundedFillTextureImage;
                if (imageToModelProgress > 0)
                {
                    float fillWidth = 380f * imageToModelProgress;
                    Rect fillRect = new Rect(
                        progressBarRect.x,
                        progressBarRect.y,
                        fillWidth,
                        progressBarRect.height
                    );
                    roundedFillTextureImage = GenerateRoundedTexture(
                        new Color(248f / 255f, 207f / 255f, 0f),
                        (int)(fillWidth * 10f),
                        70
                    );
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
                selectedObject = (GameObject)
                    EditorGUILayout.ObjectField(
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
                GUILayout.Space(16);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Space(16);
                GUILayout.Label("Model Selection", LeftHeaderStyle, GUILayout.Width(120));
                selectedModel = EditorGUILayout.Popup(GUIContent.none, selectedModel, modelOptions);
                GUILayout.Space(16);
                GUILayout.EndHorizontal();

                // 骨骼绑定开关
                GUILayout.BeginHorizontal();
                GUILayout.Space(16);
                GUILayout.Label("Rigging", LeftHeaderStyle, GUILayout.Width(120));
                rigging_optional = EditorGUILayout.Toggle(rigging_optional);
                GUILayout.Space(16);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                if (selectedModel is 0 or 1)
                {
                    CheckAndCloseBumpMapSettingsFixingWindow();
                    // GUILayout.Space(10);
                    EditorGUI.indentLevel++;
                    advancedSettingsFoldout = EditorGUILayout.Foldout(
                        advancedSettingsFoldout,
                        "Advanced settings (Optional)",
                        true
                    );
                    ExtraHeight = 60;
                    if (advancedSettingsFoldout)
                    {
                        ExtraHeight = 200;
                        EditorGUI.indentLevel++; // 增加缩进

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(32); // 右侧空隙
                        GUILayout.Label(
                            new GUIContent(
                                "Face Limits",
                                "Limits the number of faces on the output model"
                            ),
                            GUILayout.Width(88), //强行对齐
                            GUILayout.ExpandWidth(false)
                        );
                        face_limit = EditorGUILayout.IntField(
                            face_limit,
                            GUILayout.ExpandWidth(false)
                        );
                        GUILayout.EndHorizontal();

                        quad_optional = EditorGUILayout.Toggle(
                            new GUIContent("Quad-Mesh", "Enable quad-mesh model output."),
                            quad_optional
                        );
                        texture_optional = EditorGUILayout.Toggle(
                            new GUIContent(
                                "Texture",
                                "set False to get a model without any textures"
                            ),
                            texture_optional
                        );
                        pbr_optional = EditorGUILayout.Toggle(
                            new GUIContent("PBR", "set False to get a model without pbr"),
                            pbr_optional
                        );

                        texture_quality_optional = (TextureQuality)
                            EditorGUILayout.EnumPopup(
                                new GUIContent(
                                    "Texture Quality",
                                    "This parameter controls the texture quality"
                                ),
                                texture_quality_optional
                            );
                        texture_alignment_optional = (TextureAlignment)
                            EditorGUILayout.EnumPopup(
                                new GUIContent(
                                    "Texture Alignment",
                                    "Determines the prioritization of texture alignment in the 3D model"
                                ),
                                texture_alignment_optional
                            );
                        autosize_optional = EditorGUILayout.Toggle(
                            new GUIContent(
                                "Auto Size",
                                "Automatically scale the model to real-world dimensions, with the unit in meters"
                            ),
                            autosize_optional
                        );
                        style_optional = (ModelStyle)
                            EditorGUILayout.EnumPopup(
                                new GUIContent(
                                    "Style",
                                    "Defines the artistic style or transformation to be applied to the 3D model"
                                ),
                                style_optional
                            );
                        orientation_optional = (Orientation)
                            EditorGUILayout.EnumPopup(
                                new GUIContent(
                                    "Orientation",
                                    "Set orientation=align_image to automatically rotate the model to align the original image"
                                ),
                                orientation_optional
                            );
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

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    Debug.LogError($"Error downloading file: {uwr.error}");
                    EditorCoroutineUtility.StartCoroutineOwnerless(
                        DownloadGLBModel(fileUrl, savePath, fromTextToModel)
                    );
                    yield break;
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(GetUserBalance());

                    Repaint();
                    AssetDatabase.Refresh();
                    //GLBPostProcessing(uniquePath); // GLB 文件后处理函数
                    //AssetDatabase.Refresh();
                    ShowPreviewModel(uniquePath); // 显示模型预览

                    ResetState(fromTextToModel);
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
                    ResetState(fromTextToModel);
                    Debug.LogError("Download timed out.");
                    yield break;
                }

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    Debug.LogError($"Error downloading file: {uwr.error}");
                    EditorCoroutineUtility.StartCoroutineOwnerless(
                        DownloadFBXModel(fileUrl, savePath, fromTextToModel)
                    );
                    yield break;
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(GetUserBalance());

                    AssetDatabase.Refresh();
                    ModelPostProcessing(uniquePath);
                    AssetDatabase.Refresh();
                    ShowPreviewModel(uniquePath);

                    ResetState(fromTextToModel);
                }
                else
                {
                    ResetState(fromTextToModel);
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
            string newFilePath = Path.Combine(
                newDirectory,
                $"{filePathWithoutExtension}{fileExtension}"
            );

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
                    if (
                        Path.GetExtension(filePath).ToLower() == ".png"
                        || Path.GetExtension(filePath).ToLower() == ".jpg"
                    )
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
                                Debug.Log("Convert: " + unityPath);
                                TextureImporter textureImporter =
                                    TextureImporter.GetAtPath(unityPath) as TextureImporter;
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

        public UnityEvent<string> OnGetBalanceComplete = new UnityEvent<string>();

        private IEnumerator GetUserBalance()
        {
            // 设置请求头
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Authorization", $"Bearer {apiKey}" },
            };

            // 初始化UnityWebRequest，并设置为GET方法
            using (
                UnityWebRequest uwr = UnityWebRequest.Get(
                    "https://api.tripo3d.ai/v2/openapi/user/balance"
                )
            )
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
                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    if (!apiKey.StartsWith("tsk_"))
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid API Key",
                            "The API Key format is incorrect. Please ensure it starts with 'tsk_'.",
                            "OK"
                        );
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(
                            "Connection failed",
                            "We are unable to locate your account balance; please check your internet connection.",
                            "OK"
                        );
                    }
                }
                else
                {
                    try
                    {
                        // 解析响应内容为JSON
                        var responseJson = JsonUtility.FromJson<BalanceResponseDataWrapper>(
                            uwr.downloadHandler.text
                        );

                        // 检查返回的数据是否包含余额信息
                        if (
                            responseJson != null
                            && responseJson.code == 0
                            && responseJson.data != null
                        )
                        {
                            // 获取余额
                            int balance = responseJson.data.balance;
                            OnGetBalanceComplete.Invoke(balance.ToString());
                        }
                        else
                        {
                            if (!apiKey.StartsWith("tsk_"))
                            {
                                EditorUtility.DisplayDialog(
                                    "Invalid API Key",
                                    "The API Key format is incorrect. Please ensure it starts with 'tsk_'.",
                                    "OK"
                                );
                            }
                            else
                            {
                                EditorUtility.DisplayDialog(
                                    "Connection failed",
                                    "We are unable to locate your account balance; please check your internet connection.",
                                    "OK"
                                );
                            }
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
        private void CompleteTask(TaskSearchResponse response, bool isTextToModel)
        {
            if (response == null)
            {
                return;
            }
            if (isTextToModel)
            {
                textToModelProgress = 1f;
                TextToModelBtnString = "Downloading...";
            }
            else
            {
                imageToModelProgress = 1f;
                ImageToModelBtnString = "Downloading...";
            }

            Repaint();

            string fileName;
            string savePath;
            string url = GetModelUrl(response);

            if (quad_optional && selectedModel <= 1)
            {
                fileName = "TripoModel.fbx";
                savePath = Path.Combine(saveDirectory, fileName);
                EditorCoroutineUtility.StartCoroutineOwnerless(
                    DownloadFBXModel(url, savePath, isTextToModel)
                );
            }
            else
            {
                fileName = "TripoModel.glb";
                savePath = Path.Combine(saveDirectory, fileName);
                EditorCoroutineUtility.StartCoroutineOwnerless(
                    DownloadGLBModel(url, savePath, isTextToModel)
                );
            }
        }

        #region RuntimeFuction
        #region TextToModel
        public IEnumerator TextPromptsToModel()
        {
            if (apiKey == "")
            {
                Debug.LogError("Please enter a valid API Key");
                yield break;
            }

            Debug.Log(
                $"Running Text_to_Model_func with input: {textPrompt} and model: {modelOptions[selectedModel]}"
            );

            string taskID = null;
            textToModelProgress = 0f;
            string RiggingTaskID = null;
            TaskSearchResponse response = null;

            // 发送文本请求并获取taskID
            string jsonData = BuildTextRequestData();
            Debug.Log($"Sending request: {jsonData}");

            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                SendWebRequestTask(jsonData, (id) => taskID = id)
            );

            // 使用taskID跟踪任务进度
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                GetTaskProgressAndOutput(taskID, true, (res) => response = res)
            );

            if (rigging_optional)
            {
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                    SendRiggingRequset(taskID, (id) => RiggingTaskID = id)
                );
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                    GetTaskProgressAndOutput(RiggingTaskID, true, (res) => response = res)
                );
                CompleteTask(response, true);
            }
            else if (response != null)
            {
                CompleteTask(response, true);
            }
            else
            {
                Debug.LogError("Task Failed");
                ResetState(true);
            }
        }
        #endregion

        #region ImageToModel
        public IEnumerator ImageToModel()
        {
            if (apiKey == "")
            {
                Debug.LogError("Please enter a valid API Key");
                yield break;
            }
            Debug.Log(
                $"Running Image_to_Model_func with image: {imagePath} and model: {modelOptions[selectedModel]}"
            );
            string imgToken = null;
            string taskID = null;
            imageToModelProgress = 0f;
            string RiggingTaskID = null;
            TaskSearchResponse response = null;

            // 发送图片请求并获取taskID
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                SendImageRequest(imagePath, (token) => imgToken = token)
            );
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                PostImageTokenToModel(imgToken, (id) => taskID = id)
            );

            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                GetTaskProgressAndOutput(taskID, false, (res) => response = res)
            );

            if (rigging_optional && response != null)
            {
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                    SendRiggingRequset(taskID, (id) => RiggingTaskID = id)
                );
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                    GetTaskProgressAndOutput(RiggingTaskID, false, (res) => response = res)
                );
                CompleteTask(response, false);
            }
            else if (response != null)
            {
                CompleteTask(response, false);
            }
            else
            {
                Debug.LogError("Task Failed");
                ResetState(false);
            }
        }

        private IEnumerator SendImageRequest(string imagePath, Action<string> onImageTokenReceived)
        {
            imageToModelProgress = 0f;
            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.LogError("No image selected for upload.");
                yield break;
            }
            byte[] imageData = File.ReadAllBytes(imagePath);

            // Creating a WWWForm
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageData, Path.GetFileName(imagePath), "image/jpeg");

            // Creating the UnityWebRequest
            using (UnityWebRequest uwr = UnityWebRequest.Post(UploadImageUrl, form))
            {
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = TIMEOUT_DURATION / 1000;

                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseText = uwr.downloadHandler.text;
                        ImageResponseData jsonResponse = JsonUtility.FromJson<ImageResponseData>(
                            responseText
                        );

                        if (jsonResponse.code == 0)
                        {
                            Debug.Log($"Image Token: {jsonResponse.data.image_token}");
                            onImageTokenReceived?.Invoke(jsonResponse.data.image_token);
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

        private IEnumerator PostImageTokenToModel(string imgToken, Action<string> onTaskIdReceived)
        {
            string jsonData = BuildImageRequestData(imgToken);
            Debug.Log(jsonData);

            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                SendWebRequestTask(jsonData, onTaskIdReceived)
            );
        }
        #endregion

        #region Rigging
        IEnumerator SendRiggingRequset(string originalTaskId, Action<string> onTaskIdReceived)
        {
            if (string.IsNullOrEmpty(originalTaskId))
            {
                yield break;
            }
            Debug.Log("AnimateRig");
            var req = new RiggingRequestData
            {
                type = "animate_rig",
                original_model_task_id = originalTaskId,
                out_format = "glb",
            };
            string jsonData = JsonUtility.ToJson(req);

            yield return EditorCoroutineUtility.StartCoroutineOwnerless(
                SendWebRequestTask(jsonData, onTaskIdReceived)
            );
        }

        #endregion


        #region Utility
        private string BuildTextRequestData()
        {
            bool isHighVersion = (int)selectedModel <= 1;

            if (!isHighVersion)
            {
                return JsonUtility.ToJson(
                    new TextPromptsRequestData_lowVersion
                    {
                        type = "text_to_model",
                        model_version = modelOptions[(int)selectedModel],
                        prompt = textPrompt,
                    }
                );
            }
            else
            {
                if (style_optional == 0)
                {
                    return JsonUtility.ToJson(
                        new TextPromptsRequestData
                        {
                            type = "text_to_model",
                            model_version = modelOptions[Convert.ToInt32(selectedModel)],
                            prompt = textPrompt,
                            face_limit = face_limit,
                            texture = texture_optional,
                            pbr = pbr_optional,
                            texture_quality = textureQualityOptions[(int)texture_quality_optional],
                            texture_alignment = textureAlignmentOptions[
                                (int)texture_alignment_optional
                            ],
                            auto_size = autosize_optional,
                            orientation = orientationOptions[(int)orientation_optional],
                            quad = quad_optional,
                        }
                    );
                }
                else
                {
                    return JsonUtility.ToJson(
                        new TextPromptsRequestData_WithStyle
                        {
                            type = "text_to_model",
                            model_version = modelOptions[Convert.ToInt32(selectedModel)],
                            prompt = textPrompt,
                            face_limit = face_limit,
                            texture = texture_optional,
                            pbr = pbr_optional,
                            texture_quality = textureQualityOptions[(int)texture_quality_optional],
                            texture_alignment = textureAlignmentOptions[
                                (int)texture_alignment_optional
                            ],
                            auto_size = autosize_optional,
                            style = styleOptions[(int)style_optional],
                            orientation = orientationOptions[(int)orientation_optional],
                            quad = quad_optional,
                        }
                    );
                }
            }
        }

        private string BuildImageRequestData(string imgToken)
        {
            bool isHighVersion = (int)selectedModel <= 1;
            if (!isHighVersion)
            {
                return JsonUtility.ToJson(
                    new ImagePromptsRequestData_lowVersion()
                    {
                        type = "image_to_model",
                        model_version = modelOptions[Convert.ToInt32(selectedModel)],
                        file = new ImagePromptsRequestfile { type = "jpg", file_token = imgToken },
                    }
                );
            }
            else
            {
                if (style_optional == 0)
                {
                    return JsonUtility.ToJson(
                        new ImagePromptsRequestData
                        {
                            type = "image_to_model",
                            model_version = modelOptions[Convert.ToInt32(selectedModel)],
                            file = new ImagePromptsRequestfile
                            {
                                type = "jpg",
                                file_token = imgToken,
                            },
                            face_limit = face_limit,
                            texture = texture_optional,
                            pbr = pbr_optional,
                            texture_alignment = textureAlignmentOptions[
                                (int)texture_alignment_optional
                            ],
                            texture_quality = textureQualityOptions[(int)texture_quality_optional],
                            auto_size = autosize_optional,
                            orientation = orientationOptions[(int)orientation_optional],
                            quad = quad_optional,
                        }
                    );
                }
                else
                {
                    return JsonUtility.ToJson(
                        new ImagePromptsRequestData_WithStyle
                        {
                            type = "image_to_model",
                            model_version = modelOptions[Convert.ToInt32(selectedModel)],
                            file = new ImagePromptsRequestfile
                            {
                                type = "jpg",
                                file_token = imgToken,
                            },
                            face_limit = face_limit,
                            texture = texture_optional,
                            pbr = pbr_optional,
                            texture_alignment = textureAlignmentOptions[
                                (int)texture_alignment_optional
                            ],
                            texture_quality = textureQualityOptions[(int)texture_quality_optional],
                            auto_size = autosize_optional,
                            style = styleOptions[(int)style_optional],
                            orientation = orientationOptions[(int)orientation_optional],
                            quad = quad_optional,
                        }
                    );
                }
            }
        }

        private IEnumerator SendWebRequestTask(string jsonData, Action<string> onTaskIdReceived)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            using (UnityWebRequest uwr = new UnityWebRequest(GenerateModelUrl, "POST"))
            {
                uwr.uploadHandler = new UploadHandlerRaw(postData);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                uwr.timeout = TIMEOUT_DURATION / 1000;

                yield return uwr.SendWebRequest();

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    Debug.LogError($"Error: {uwr.error}, {uwr.result}");
                    Debug.LogError($"Response: {uwr.downloadHandler.text}");
                    yield break;
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = uwr.downloadHandler.text;
                        Debug.Log("Response received: " + jsonResponse);
                        BaseTaskResponse response = JsonUtility.FromJson<BaseTaskResponse>(
                            jsonResponse
                        );

                        if (response.code == 0)
                        {
                            onTaskIdReceived?.Invoke(response.data.task_id);
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

        private IEnumerator GetTaskProgressAndOutput(
            string taskId,
            bool isTextToModel,
            Action<TaskSearchResponse> onModelUrlReceived
        )
        {
            if (taskId == null)
            {
                ResetState(isTextToModel);
                yield break;
            }

            string url = $"https://api.tripo3d.ai/v2/openapi/task/{taskId}";
            while (true)
            {
                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    uwr.timeout = TIMEOUT_DURATION / 1000;

                    yield return uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Request failed: {uwr.error}");
                        break;
                    }
                    try
                    {
                        bool isHighVersion = selectedModel <= 1;
                        TaskSearchResponse response = JsonUtility.FromJson<TaskSearchResponse>(
                            uwr.downloadHandler.text
                        );
                        UpdateProgress(response.data.progress / 100.0f, isTextToModel);
                        if (response?.code != 0)
                            continue;

                        if (response.data.status == "success")
                        {
                            string modelUrl = GetModelUrl(response);
                            Debug.Log(modelUrl);
                            if (!string.IsNullOrEmpty(modelUrl))
                            {
                                onModelUrlReceived?.Invoke(response);
                            }
                            break;
                        }
                        else if (response.data.status == "failed")
                        {
                            Debug.LogError($"Task failed: {response.data}");
                            ResetState(isTextToModel);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Response parsing error: {e.Message}");
                        break;
                    }
                }
                yield return new WaitForSeconds(PROGRESS_CHECK_INTERVAL);
            }
        }

        private void UpdateProgress(float progress, bool isTextToModel)
        {
            if (isTextToModel)
            {
                textToModelProgress = progress;
            }
            else
            {
                imageToModelProgress = progress;
            }
            Repaint();
        }

        private string GetModelUrl(TaskSearchResponse response)
        {
            if (response.data.output.pbr_model != null)
                return response.data.output.pbr_model;
            else if (response.data.output.base_model != null)
                return response.data.output.base_model;
            else if (response.data.output.model != null)
                return response.data.output.model;

            return null;
        }

        private void ResetState(bool isTextToModel)
        {
            if (isTextToModel)
            {
                textToModelProgress = 0f;
                TextToModelBtnString = "Generate";
                isTextToModelCoroutineRunning = false;
            }
            else
            {
                imageToModelProgress = 0f;
                ImageToModelBtnString = "Generate";
                isImageToModelCoroutineRunning = false;
            }
            Repaint();
        }
        #endregion

        #endregion
    }
}
#endif
