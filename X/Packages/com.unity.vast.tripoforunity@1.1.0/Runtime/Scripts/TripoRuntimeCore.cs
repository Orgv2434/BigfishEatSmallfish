using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace TripoForUnity
{
    public class TripoRuntimeCore : MonoBehaviour
    {
        private string apiKey = "";
        private readonly string GenerateModelUrl = "https://api.tripo3d.ai/v2/openapi/task";
        private readonly string UploadImageUrl = "https://api.tripo3d.ai/v2/openapi/upload";
        public ModelVersion modelVersion = ModelVersion.v2_5_20250123;

        [Header("ModelGenerate")]
        #region Text to Model
        public string textPrompt = "";
        public float textToModelProgress = 0f;
        #endregion

        #region Image to Model
        public string imagePath = "";
        public float imageToModelProgress = 0f;
        #endregion

        #region Advanced Settings
        [Header("Advanced Settings")]
        public int face_limit = 10000;
        public bool rigging_optional = false;
        public bool pbr_optional = true;
        public bool texture_optional = true;
        public TextureQuality texture_quality_optional = TextureQuality.Standard;
        public bool quad_optional = false;
        public bool autosize_optional = false;
        public ModelStyle style_optional = ModelStyle.Original;
        public Orientation orientation_optional = Orientation.Default;
        public TextureAlignment texture_alignment_optional = TextureAlignment.OriginalImage;
        #endregion


        #region NetworkConfig
        const int TIMEOUT_DURATION = 5000;
        const float PROGRESS_CHECK_INTERVAL = 1.0f;
        #endregion

        private readonly string[] textureQualityOptions = { "standard", "detailed" };
        private readonly string[] modelOptions =
        {
            "Turbo-v1.0-20250506",
            "v1.4-20240625",
            "v2.0-20240919",
            "v2.5-20250123",
            "v3.0-20250812",
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

        public UnityEvent<string> OnModelGenerateComplete = new();

        public TripoRuntimeCore SetAPIKey(string apiKey)
        {
            this.apiKey = apiKey;
            return this;
        }

        public TripoRuntimeCore SetTextPrompt(string prompt)
        {
            this.textPrompt = prompt;
            return this;
        }

        public TripoRuntimeCore SetImagePath(string path)
        {
            this.imagePath = path;
            return this;
        }

        public TripoRuntimeCore SetModelVersion(ModelVersion version)
        {
            this.modelVersion = version;
            return this;
        }

        public TripoRuntimeCore SetTextureQuality(TextureQuality quality)
        {
            this.texture_quality_optional = quality;
            return this;
        }

        public TripoRuntimeCore SetFaceLimit(int limit)
        {
            this.face_limit = limit;
            return this;
        }

        public TripoRuntimeCore SetRigging(bool optional)
        {
            this.rigging_optional = optional;
            return this;
        }

        public TripoRuntimeCore SetPbr(bool optional)
        {
            this.pbr_optional = optional;
            return this;
        }

        public TripoRuntimeCore SetTexture(bool optional)
        {
            this.texture_optional = optional;
            return this;
        }

        public TripoRuntimeCore SetQuad(bool optional)
        {
            this.quad_optional = optional;
            return this;
        }

        public TripoRuntimeCore SetAutosize(bool optional)
        {
            this.autosize_optional = optional;
            return this;
        }

        public TripoRuntimeCore SetStyle(ModelStyle style)
        {
            this.style_optional = style;
            return this;
        }

        public TripoRuntimeCore SetOrientation(Orientation orientation)
        {
            this.orientation_optional = orientation;
            return this;
        }

        public TripoRuntimeCore SetTextureAlignment(TextureAlignment alignment)
        {
            this.texture_alignment_optional = alignment;
            return this;
        }

        #region TextToModel
        public void TextToModel()
        {
            StartCoroutine(TextToModelCoroutine());
        }

        private IEnumerator TextToModelCoroutine()
        {
            if (apiKey == "")
            {
                Debug.LogError("Please enter a valid API Key");
                yield break;
            }
            Debug.Log(
                $"Running Text_to_Model_func with input: {textPrompt} and model: {modelOptions[(int)this.modelVersion]}"
            );
            string taskID = null;
            textToModelProgress = 0f;
            string RiggingTaskID = null;
            string ModelUrl = null;

            // 发送文本请求并获取taskID
            yield return StartCoroutine(SendTextRequest((id) => taskID = id));

            // 使用taskID跟踪任务进度
            yield return StartCoroutine(
                GetTaskProgressAndOutput(taskID, true, (url) => ModelUrl = url)
            );

            if (rigging_optional && ModelUrl != null)
            {
                yield return StartCoroutine(SendRiggingRequset(taskID, (id) => RiggingTaskID = id));
                yield return StartCoroutine(
                    GetTaskProgressAndOutput(RiggingTaskID, true, (url) => ModelUrl = url)
                );
                OnModelGenerateComplete?.Invoke(ModelUrl);
            }
            else if (ModelUrl != null)
            {
                OnModelGenerateComplete?.Invoke(ModelUrl);
            }
            else
            {
                Debug.LogError("Task Failed");
            }
        }

        private IEnumerator SendTextRequest(Action<string> onTaskIdReceived)
        {
            string jsonData = BuildTextRequestData();
            Debug.Log($"Sending request: {jsonData}");

            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            yield return StartCoroutine(SendWebRequestTask(jsonData, onTaskIdReceived));
        }
        #endregion

        #region ImageToModel
        public void ImageToModel()
        {
            StartCoroutine(ImageToModelCoroutine());
        }

        private IEnumerator ImageToModelCoroutine()
        {
            if (apiKey == "")
            {
                Debug.LogError("Please enter a valid API Key");
                yield break;
            }
            Debug.Log(
                $"Running Image_to_Model_func with image: {imagePath} and model: {modelOptions[(int)this.modelVersion]}"
            );
            string imgToken = null;
            string taskID = null;
            imageToModelProgress = 0f;
            string RiggingTaskID = null;
            string ModelUrl = null;

            // 发送图片请求并获取taskID
            yield return StartCoroutine(SendImageRequest(imagePath, (token) => imgToken = token));
            yield return StartCoroutine(PostImageTokenToModel(imgToken, (id) => taskID = id));
            yield return StartCoroutine(
                GetTaskProgressAndOutput(taskID, false, (url) => ModelUrl = url)
            );

            if (rigging_optional && ModelUrl != null)
            {
                yield return StartCoroutine(SendRiggingRequset(taskID, (id) => RiggingTaskID = id));
                yield return StartCoroutine(
                    GetTaskProgressAndOutput(RiggingTaskID, false, (url) => ModelUrl = url)
                );
                OnModelGenerateComplete?.Invoke(ModelUrl);
            }
            else if (ModelUrl != null)
            {
                OnModelGenerateComplete?.Invoke(ModelUrl);
            }
            else
            {
                Debug.LogError("Task Failed");
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
            yield return StartCoroutine(SendWebRequestTask(jsonData, onTaskIdReceived));
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

            yield return StartCoroutine(SendWebRequestTask(jsonData, onTaskIdReceived));
        }

        #endregion


        #region Utility

        private string BuildTextRequestData()
        {
            bool isHighVersion = (int)modelVersion <= 1;

            if (!isHighVersion)
            {
                return JsonUtility.ToJson(
                    new TextPromptsRequestData_lowVersion
                    {
                        type = "text_to_model",
                        model_version = modelOptions[(int)modelVersion],
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
                            model_version = modelOptions[Convert.ToInt32(modelVersion)],
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
                            model_version = modelOptions[Convert.ToInt32(modelVersion)],
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
            bool isHighVersion = (int)modelVersion <= 1;
            if (!isHighVersion)
            {
                return JsonUtility.ToJson(
                    new ImagePromptsRequestData_lowVersion()
                    {
                        type = "image_to_model",
                        model_version = modelOptions[Convert.ToInt32(modelVersion)],
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
                            model_version = modelOptions[Convert.ToInt32(modelVersion)],
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
                            model_version = modelOptions[Convert.ToInt32(modelVersion)],
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
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = uwr.downloadHandler.text;
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
            bool isTextToImage,
            Action<string> onModelUrlReceived
        )
        {
            if (string.IsNullOrEmpty(taskId))
            {
                yield break;
            }
            string url = $"https://api.tripo3d.ai/v2/openapi/task/{taskId}";
            float progressRef = isTextToImage ? textToModelProgress : imageToModelProgress;

            while (progressRef < 1f)
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
                        bool isHighVersion = (int)modelVersion <= 1;
                        //Debug.Log(uwr.downloadHandler.text);
                        TaskSearchResponse response = JsonUtility.FromJson<TaskSearchResponse>(
                            uwr.downloadHandler.text
                        );

                        if (response?.code != 0)
                            continue;
                        UpdateProgress(response.data.progress / 100f, isTextToImage);

                        if (response.data.status == "success")
                        {
                            string modelUrl = GetModelUrl(response);
                            Debug.Log(modelUrl);
                            if (!string.IsNullOrEmpty(modelUrl))
                            {
                                onModelUrlReceived?.Invoke(modelUrl);
                                UpdateProgress(0, isTextToImage);
                            }
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Response parsing error: {e.Message}");
                    }
                }
                yield return new WaitForSeconds(PROGRESS_CHECK_INTERVAL);
            }
        }

        private void UpdateProgress(float progress, bool isTextToImage)
        {
            if (isTextToImage)
            {
                textToModelProgress = progress;
            }
            else
            {
                imageToModelProgress = progress;
            }
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
        #endregion
    }
}
