using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using GLTFast;
using System;
using System.Net.Http;   // 需要先在 Unity Package Manager 安装 GLTFast

namespace TripoForUnity
{

    public class ModelHandler : MonoBehaviour
    {
        public Slider progressBar;   // 在 Inspector 拖一个 UI Slider 进来
        public Text progressText;    // 可选：显示进度百分比

        private string modelSavePath;

        void Start()
        {
            // 监听 Tripo 的事件
            //GetComponent<TripoRuntimeCore>().OnDownloadComplete.AddListener(OnFbxDownloadComplete);

            // 模型保存路径
            //modelSavePath = Path.Combine(Application.persistentDataPath, "Models");
            string modelSavePath = Path.Combine(
                Application.dataPath,
                "TripoModels",
                DateTime.Now.ToString("yyyyMMdd-HHmmss") 
            );
            if (!Directory.Exists(modelSavePath))
                Directory.CreateDirectory(modelSavePath);

            Debug.Log($"模型将保存到: {modelSavePath}");

            // 初始化进度条
            if (progressBar != null) progressBar.gameObject.SetActive(false);
        }

        //void OnFbxDownloadComplete(string gltfUrl)
        //{
        //    Debug.Log($"模型生成完成，下载地址: {gltfUrl}");
        //    StartCoroutine(DownloadAndLoadGLB(gltfUrl));
        //}

        //private IEnumerator DownloadAndLoadGLB(string url)
        //{
        //    string fileName = Path.GetFileName(url);
        //    string localFile = Path.Combine(modelSavePath, fileName);

        //    using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        //    {
        //        if (progressBar != null) progressBar.gameObject.SetActive(true);
        //        uwr.SendWebRequest();

        //        while (!uwr.isDone)
        //        {
        //            if (progressBar != null)
        //            {
        //                progressBar.value = uwr.downloadProgress;
        //                if (progressText != null)
        //                    progressText.text = $"下载中 {(uwr.downloadProgress * 100f):F1}%";
        //            }
        //            yield return null;
        //        }

        //        if (uwr.result != UnityWebRequest.Result.Success)
        //        {
        //            Debug.LogError($"下载模型失败: {uwr.error}");
        //            yield break;
        //        }

        //        File.WriteAllBytes(localFile, uwr.downloadHandler.data);
        //        Debug.Log($"模型已保存到本地: {localFile}");
        //    }
        //}

        //async void OnFbxDownloadComplete(string gltfUrl)
        //{
        //    Debug.Log($"Tripo 模型生成完成: {gltfUrl}");

        //    // 拼接本地保存文件名
        //    string saveFile = Path.Combine(modelSavePath, "model.glb");

        //    // 下载文件到本地
        //    using (HttpClient client = new HttpClient())
        //    {
        //        var data = await client.GetByteArrayAsync(gltfUrl);
        //        await File.WriteAllBytesAsync(saveFile, data);
        //        Debug.Log($"模型已保存到本地: {saveFile}");
        //    }

        //    //// ✅ 在 Unity 里加载 GLB
        //    //var oldAsset = SimpleModel.GetComponent<GLTFast.GltfAsset>();
        //    //if (oldAsset) Destroy(oldAsset);

        //    //var gltfAsset = SimpleModel.AddComponent<GLTFast.GltfAsset>();
        //    //gltfAsset.Url = "file:///" + saveFile.Replace("\\", "/");
        //}

    }

}
