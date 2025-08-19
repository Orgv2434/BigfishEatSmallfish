using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
namespace TripoForUnity
{
    

public enum ModelVersion
{
    v2_5_20250123 = 0,
    v2_0_20240919 = 1,
    v1_4_20240625 = 2,
    v1_3_20240522 = 3
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
public class TripoRuntimeCore: MonoBehaviour
{
    private string apiKey = "";
    public ModelVersion selectedModel = ModelVersion.v2_0_20240919;

    public int face_limit = 10000;
    public bool texture_optional = true;
    public bool pbr_optional = true;
    public TextureQuality texture_quality_optional = TextureQuality.Standard;
    public bool autosize_optional = false;
    public ModelStyle style_optional = ModelStyle.Original;
    public Orientation orientation_optional = Orientation.Default;
    public TextureAlignment texture_alignment_optional = TextureAlignment.OriginalImage;
    
    public string textPrompt = "";
    public float textToModelProgress = 0f;
    public float textToModelGenerateProgress = 0f;
    
    public string imagePath = "";
    public Texture2D uploadedImage = null;
    public float imageToModelProgress = 0f;
    public float imageToModelGenerateProgress = 0f;

    public GameObject simpleModel;

    
    private readonly string[] textureQualityOptions = { "standard", "detailed" };
    private readonly string[] modelOptions = { "v2.5-20250123", "v2.0-20240919", "v1.4-20240625", "v1.3-20240522" };
    private readonly string[] orientationOptions = { "default", "align_image" };
    private readonly string[] styleOptions = { "default", "person:person2cartoon", "object:clay", "object:steampunk", "animal:venom", "object:barbie", "object:christmas" };
    private readonly string[] textureAlignmentOptions = { "original_image", "geometry" };

    
    
    private string imageToken = "";


    public UnityEvent<string> OnDownloadComplete = new UnityEvent<string>();
    public void set_api_key(string apiKey)
    {
        this.apiKey = apiKey;
    }
    
    public void Text_to_Model_func()
    {
        if (apiKey == "")
        {
            Debug.LogError("Please enter a valid API Key");
            return;
        }
        string selectedModel = modelOptions[(int)this.selectedModel];
        Debug.Log($"Running Text_to_Model_func with input: {textPrompt} and model: {selectedModel}");
        StartCoroutine(TextPromptsToModel());
    }

    public void Image_to_Model_func()
    {
        if (apiKey == "")
        {
            Debug.LogError("Please enter a valid API Key");
            return;
        }
        string selectedModel = modelOptions[(int)this.selectedModel];
        Debug.Log($"Running Image_to_Model_func with image: {imagePath} and model: {selectedModel}");
        StartCoroutine(ImageToModelCoroutine());
    }
    private IEnumerator ImageToModelCoroutine()
    {
        imageToModelGenerateProgress = 0f;
        imageToModelProgress = 0f;
        if (string.IsNullOrEmpty(imagePath))
        {
            Debug.LogError("No image selected for upload.");
            yield break;
        }

        string url = "https://api.tripo3d.ai/v2/openapi/upload";
        
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
                        Debug.Log($"Image Token: {imageToken}");
                        StartCoroutine(PostImageToModel());
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
        string jsonData;
        // 构建要发送的数据对象
        if (Convert.ToInt32(selectedModel) is 0 or 1)
        {
            if (style_optional == ModelStyle.Original)
            {
                TextPromptsRequestData requestData = new TextPromptsRequestData
                {
                    type = "text_to_model",
                    model_version = modelOptions[Convert.ToInt32(selectedModel)],
                    prompt = textPrompt, 
                    face_limit = face_limit,
                    texture = texture_optional,
                    pbr = pbr_optional, 
                    texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                    texture_quality = textureQualityOptions[(int)texture_quality_optional],
                    auto_size = autosize_optional,
                    orientation = orientationOptions[(int)orientation_optional]
                    
                };
                jsonData = JsonUtility.ToJson(requestData);
            }
            else
            {
                TextPromptsRequestData_withStyle requestData = new TextPromptsRequestData_withStyle()
                {
                    type = "text_to_model",
                    model_version = modelOptions[Convert.ToInt32(selectedModel)],
                    prompt = textPrompt, 
                    face_limit = face_limit,
                    texture = texture_optional,
                    pbr = pbr_optional, 
                    texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                    texture_quality = textureQualityOptions[(int)texture_quality_optional],
                    auto_size = autosize_optional,
                    style = styleOptions[(int)style_optional],
                    orientation = orientationOptions[(int)orientation_optional]
                };
                jsonData = JsonUtility.ToJson(requestData);
            }
        }
        else
        {
            TextPromptsRequestData_lowVersion requestData = new TextPromptsRequestData_lowVersion()
            {
                type = "text_to_model",
                model_version = modelOptions[Convert.ToInt32(selectedModel)],
                prompt = textPrompt, 
            };
            jsonData = JsonUtility.ToJson(requestData);
        }

        Debug.Log(jsonData);
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
                            
                            StartCoroutine(GetTaskProgressAndOutput(response.data.task_id, true));
                            
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
        if (Convert.ToInt32(selectedModel) is 0 or 1)
        {
            if (style_optional==ModelStyle.Original)
            {
                ImagePromptsRequestData requestData = new ImagePromptsRequestData
                {
                    type = "image_to_model",
                    model_version = modelOptions[Convert.ToInt32(selectedModel)],
                    file = new ImagePromptsRequestfile{
                        type = "jpg",
                        file_token = imageToken
                    },
                    face_limit = face_limit,
                    texture = texture_optional,
                    pbr = pbr_optional, 
                    texture_alignment = textureAlignmentOptions[(int)texture_alignment_optional],
                    texture_quality = textureQualityOptions[(int)texture_quality_optional],
                    auto_size = autosize_optional,
                    orientation = orientationOptions[(int)orientation_optional]
                };
                jsonData = JsonUtility.ToJson(requestData);
            }
            else
            {
                ImagePromptsRequestData_withStyle requestData = new ImagePromptsRequestData_withStyle()
                {
                    type = "image_to_model",
                    model_version = modelOptions[Convert.ToInt32(selectedModel)],
                    file = new ImagePromptsRequestfile{
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
                    orientation = orientationOptions[(int)orientation_optional]
                };
                jsonData = JsonUtility.ToJson(requestData);
            }
        }
        else
        {
            ImagePromptsRequestData_lowVersion requestData = new ImagePromptsRequestData_lowVersion()
            {
                type = "image_to_model",
                model_version = modelOptions[Convert.ToInt32(selectedModel)],
                file = new ImagePromptsRequestfile{
                    type = "jpg",
                    file_token = imageToken
                },
            };
            jsonData = JsonUtility.ToJson(requestData);
        }
        Debug.Log(jsonData);
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
                        
                        StartCoroutine(GetTaskProgressAndOutput(response.data.task_id, false));
                        
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
        string url = $"https://api.tripo3d.ai/v2/openapi/task/{taskId}";
        while ((isTextToImage && textToModelProgress<1f)||(!isTextToImage && imageToModelProgress<1f))
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
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Error: {uwr.error}");
                    break; // Exit the loop on error
                }
                else if (uwr.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        if (Convert.ToInt32(selectedModel)!=0)
                        {
                            
                            string jsonResponse = uwr.downloadHandler.text;

                            TaskSearchResponse response = JsonUtility.FromJson<TaskSearchResponse>(jsonResponse);

                            if (response != null && response.code == 0)
                            {
                                float progress = response.data.progress;
                            
                            
                                if (isTextToImage)
                                {
                                    textToModelGenerateProgress = progress / 100;
                                    textToModelProgress = (textToModelGenerateProgress);
                                }
                                else
                                {
                                    imageToModelGenerateProgress = progress / 100;
                                    imageToModelProgress = (imageToModelGenerateProgress);
                                }

                                if (progress > 99)
                                {
                                    if (isTextToImage)
                                    {
                                        textToModelGenerateProgress = 1f;
                                        textToModelProgress = (textToModelGenerateProgress);
                                    }
                                    else
                                    {
                                        imageToModelGenerateProgress = 1f;
                                        imageToModelProgress = (imageToModelGenerateProgress);
                                    }

                                    TaskSearchOutputData output = response.data.output;
                                    if (output.model!="")
                                    {
                                        Debug.Log($"model: {output.model}");
                                        OnDownloadComplete.Invoke(output.model);
                                        if (isTextToImage)
                                        {
                                            textToModelGenerateProgress = 0f;
                                            textToModelProgress = 0f;
                                        }
                                        else
                                        {
                                            imageToModelGenerateProgress = 0f;
                                            imageToModelProgress = 0f;
                                        }
                                    }
                                    else if (output.pbr_model!="")
                                    {
                                        OnDownloadComplete.Invoke(output.pbr_model);
                                        if (isTextToImage)
                                        {
                                            textToModelGenerateProgress = 0f;
                                            textToModelProgress = 0f;
                                        }
                                        else
                                        {
                                            imageToModelGenerateProgress = 0f;
                                            imageToModelProgress = 0f;
                                        }
                                    }
                                    break; 
                                }
                            }
                            else
                            {
                                Debug.LogError("Error in response data.");
                            }
                        }
                        else
                        {
                            
                            string jsonResponse = uwr.downloadHandler.text;

                            TaskSearchResponse_2_0 response = JsonUtility.FromJson<TaskSearchResponse_2_0>(jsonResponse);

                            if (response != null && response.code == 0)
                            {
                                float progress = response.data.progress;
                            
                            
                                if (isTextToImage)
                                {
                                    textToModelGenerateProgress = progress / 100;
                                    textToModelProgress = (textToModelGenerateProgress);
                                }
                                else
                                {
                                    imageToModelGenerateProgress = progress / 100;
                                    imageToModelProgress = (imageToModelGenerateProgress);
                                }

                                if (progress > 99)
                                {
                                    if (isTextToImage)
                                    {
                                        textToModelGenerateProgress = 1f;
                                        textToModelProgress = (textToModelGenerateProgress);
                                    }
                                    else
                                    {
                                        imageToModelGenerateProgress = 1f;
                                        imageToModelProgress = (imageToModelGenerateProgress);
                                    }

                                    TaskSearchOutputData_2_0 output = response.data.output;
                                    if (output.pbr_model!="")
                                    {
                                        Debug.Log($"pbr: {output.pbr_model}");
                                        OnDownloadComplete.Invoke(output.pbr_model);
                                        if (isTextToImage)
                                        {
                                            textToModelGenerateProgress = 0f;
                                            textToModelProgress = 0f;
                                        }
                                        else
                                        {
                                            imageToModelGenerateProgress = 0f;
                                            imageToModelProgress = 0f;
                                        }
                                    }
                                    break; 
                                }
                            }
                            else
                            {
                                Debug.LogError("Error in response data.");
                            }
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
    private class TaskSearchResponse_2_0
    {
        public int code;
        public TaskSearchData_2_0 data;
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
    private class TaskSearchData_2_0
    {
        public string task_id;
        public string type;
        public string status;
        public TaskSearchInput input;
        public TaskSearchOutputData_2_0 output;
        public int progress;
        public long create_time;
    }

    [System.Serializable]
    private class TaskSearchInput
    {
        public string prompt;
    }

    [System.Serializable]
    private class TaskSearchOutputData
    {
        public string model;
        public string base_model;
        public string pbr_model;
        public string rendered_image;
    }
    
    [System.Serializable]
    private class TaskSearchOutputData_2_0
    {
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