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
        public Slider progressBar;   // 在 Inspector 拖一个 UI Slider 进来
        public Text progressText;    // 可选：显示进度百分比
        private string modelSavePath; // 保存模型的路径（Assets/TripoModels）
        private TripoRuntimeCore tripoRuntimeCore; // TripoRuntimeCore实例
        private const float DownloadTimeout = 30f; // 下载超时时间（10秒，可调整）

        void Start()
        {
            // 初始化保存路径：强制保存到 Assets/TripoModels
            // 可选：若需按时间分文件夹，取消下面注释（保留子目录）
            modelSavePath = Path.Combine(
                Application.dataPath,  // 对应项目的 Assets 文件夹
                "TripoModels",
                DateTime.Now.ToString("yyyyMMdd-HHmmss") // 时间戳子文件夹（避免文件名冲突）
            );
            // 简化版：直接保存到 Assets/TripoModels 根目录（无时间子文件夹）
            modelSavePath = Path.Combine(
                Application.dataPath,  // 核心：指向项目的 Assets 文件夹
                "TripoModels"
            );

            // 确保 Assets/TripoModels 文件夹存在，不存在则创建
            if (!Directory.Exists(modelSavePath))
            {
                Directory.CreateDirectory(modelSavePath);
                Debug.Log($"已创建文件夹：{modelSavePath}");
            }

            Debug.Log($"模型保存路径（Assets下）：{modelSavePath}");

            // 初始化进度条
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(false);
                progressBar.maxValue = 1;
                progressBar.value = 0;
            }

            // 获取TripoRuntimeCore实例
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
            // 初始化UI
            UpdateProgressUI(0, "准备下载...");
            if (progressBar != null)
                progressBar.gameObject.SetActive(true);

            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.timeout = (int)DownloadTimeout; // 设置超时

            float elapsedTime = 0f;
            bool downloadSuccess = false;
            string errorMessage = "";

            // 发送请求并在外部轮询进度（避免在包含 catch/finally 的 try 块中使用 yield）
            var asyncOp = webRequest.SendWebRequest();

            // 轮询进度（解决98%卡住问题）
            while (!asyncOp.isDone)
            {
                // 超时判断
                elapsedTime += Time.deltaTime;
                if (elapsedTime > DownloadTimeout)
                {
                    webRequest.Abort();
                    errorMessage = $"下载超时（{DownloadTimeout}秒）";
                    Debug.LogError(errorMessage);
                    break; // 跳出循环，后续处理错误
                }

                // 进度优化：98%以上强制显示100%
                float progress = webRequest.downloadProgress;
                if (progress >= 0.98f)
                    progress = 1f;

                UpdateProgressUI(Mathf.Clamp01(progress), $"下载中：{Mathf.RoundToInt(progress * 100)}%");
                yield return null; // 每帧轮询，不阻塞主线程
            }

            // 下面使用 try/catch/finally 处理下载结果与异常，但不包含 yield
            try
            {
                // 处理下载结果
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string fileName = GetFileNameFromUrl(url);
                    string saveFilePath = Path.Combine(modelSavePath, fileName);

                    // 写入模型文件到 Assets/TripoModels
                    File.WriteAllBytes(saveFilePath, webRequest.downloadHandler.data);
                    Debug.Log($"模型保存成功（Assets下）：{saveFilePath}");

                    UpdateProgressUI(1f, $"下载完成：{fileName}");
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
                // 释放资源
                webRequest?.Dispose();
            }

            // 处理下载失败的UI提示
            if (!downloadSuccess && !string.IsNullOrEmpty(errorMessage))
            {
                UpdateProgressUI(0, $"下载失败：{errorMessage}");
            }

            // 3秒后隐藏进度条
            yield return new WaitForSeconds(3f);
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
        }

        /// <summary>
        /// 统一更新进度UI
        /// </summary>
        private void UpdateProgressUI(float progress, string text)
        {
            if (progressBar != null)
                progressBar.value = progress;
            if (progressText != null)
                progressText.text = text;
        }

        /// <summary>
        /// 从URL提取文件名（处理带参数的URL，避免文件名冲突）
        /// </summary>
        private string GetFileNameFromUrl(string url)
        {
            // 处理带参数的URL（例如 "xxx.glb?token=123" → "xxx.glb"）
            string fileName = Path.GetFileNameWithoutExtension(url).Split('?')[0] 
                           + Path.GetExtension(url).Split('?')[0];

            // 若提取失败（无后缀/无文件名），自定义GLB格式文件名（避免覆盖）
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
            {
                fileName = $"model_{DateTime.Now.Ticks}.glb";
            }

            return fileName;
        }
    }
}