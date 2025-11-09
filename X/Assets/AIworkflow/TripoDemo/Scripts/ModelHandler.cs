using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using System;

namespace TripoForUnity
{
    public class ModelHandler : MonoBehaviour
    {

        public Text progressText;    // 可选：显示进度百分比
        private string modelSavePath; // 保存模型的路径（Assets/TripoModels）
        private TripoRuntimeCore tripoRuntimeCore; // TripoRuntimeCore实例
        private const float DownloadTimeout = 100f; // 下载超时时间（可调整）
        private string _lastDownloadedModelPath; // 新增：保存最后一次下载的模型文件路径


        void Start()
        {
            // 初始化保存路径：按时间分文件夹（推荐，避免文件冲突）
            modelSavePath = Path.Combine(
                Application.dataPath, 
                "TripoModels",
                DateTime.Now.ToString("yyyyMMdd-HHmmss") 
            );

            // 确保文件夹存在
            if (!Directory.Exists(modelSavePath))
            {
                Directory.CreateDirectory(modelSavePath);
                Debug.Log($"已创建文件夹：{modelSavePath}");
            }

            Debug.Log($"模型保存路径（项目根目录下）：{modelSavePath}");



            // 获取TripoRuntimeCore实例（原有逻辑不变）
            tripoRuntimeCore = FindObjectOfType<TripoRuntimeCore>();
            if (tripoRuntimeCore != null)
            {
                tripoRuntimeCore.OnModelGenerateComplete.AddListener(DownloadAndSaveModel);
            }
            else
            {
                Debug.LogError("场景中未找到TripoRuntimeCore组件！");
            }
        }

        private void DownloadAndSaveModel(string modelUrl)
        {
            if (string.IsNullOrEmpty(modelUrl))
            {
                Debug.LogError("模型URL为空！");
                UpdateProgressUI(0, "下载失败：URL为空");
                return;
            }

            Debug.Log($"开始下载模型：{modelUrl}");
            StartCoroutine(DownloadModelCoroutine(modelUrl));
        }

        private IEnumerator DownloadModelCoroutine(string url)
        {
            // 初始化UI（原有逻辑不变）
            UpdateProgressUI(0, "准备下载...");

            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = (int)DownloadTimeout; 

            float elapsedTime = 0f;
            bool downloadSuccess = false;
            string errorMessage = "";

            var asyncOp = webRequest.SendWebRequest();

            while (!asyncOp.isDone)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > DownloadTimeout)
                {
                    webRequest.Abort();
                    errorMessage = $"下载超时（{DownloadTimeout}秒）";
                    Debug.LogError(errorMessage);
                    break; 
                }

                float progress = webRequest.downloadProgress;
                if (progress >= 0.98f)
                    progress = 1f;

                UpdateProgressUI(Mathf.Clamp01(progress), $"下载中：{Mathf.RoundToInt(progress * 100)}%");
                yield return null; 
            }

            try
            {
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string fileName = GetFileNameFromUrl(url);
                    string saveFilePath = Path.Combine(modelSavePath, fileName);
                    _lastDownloadedModelPath = saveFilePath; // 记录最后下载的文件路径

                    File.WriteAllBytes(saveFilePath, webRequest.downloadHandler.data);
                    Debug.Log($"模型保存成功：{saveFilePath}");

                    UpdateProgressUI(1f, "下载完成");
                    FishGameFlowManager.Instance.OnModelGenerated(saveFilePath);
                    downloadSuccess = true;
                }
                else
                {
                    errorMessage = $"错误码：{webRequest.responseCode}，原因：{webRequest.error}";
                    Debug.LogError($"下载失败：{errorMessage}");
                }
            }
            catch (Exception e)
            {
                errorMessage = $"下载异常：{e.Message}";
                Debug.LogError(errorMessage);
            }
            finally
            {
                webRequest?.Dispose();
            }

            if (!downloadSuccess && !string.IsNullOrEmpty(errorMessage))
            {
                UpdateProgressUI(0, $"下载失败：{errorMessage}");
            }

            yield return new WaitForSeconds(3f);

        }

        /// <summary>
        /// 对外暴露：获取最后一次下载的模型文件路径
        /// </summary>
        public string GetLastDownloadedModelPath()
        {
            return _lastDownloadedModelPath;
        }

        /// <summary>
        /// 对外暴露：获取模型保存的根目录路径
        /// </summary>
        public string GetModelSaveDirectory()
        {
            return modelSavePath;
        }

        // 原有方法（UpdateProgressUI、GetFileNameFromUrl）保持不变
        private void UpdateProgressUI(float progress, string text)
        {

            if (progressText != null)
                progressText.text = text;
        }

        private string GetFileNameFromUrl(string url)
        {
            string fileName = Path.GetFileNameWithoutExtension(url).Split('?')[0]
                           + Path.GetExtension(url).Split('?')[0];

            if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
            {
                fileName = $"model_{DateTime.Now.Ticks}.glb";
            }

            return fileName;
        }
        // ModelHandler.cs 中增加文件存在性检查的辅助方法
        /// <summary>
        /// 检查模型文件是否存在
        /// </summary>
        public bool IsModelFileExists()
        {
            return !string.IsNullOrEmpty(_lastDownloadedModelPath) && File.Exists(_lastDownloadedModelPath);
        }

    }
}