using System;

public class GetTaskDetailModel
{
    [Serializable]
    public class RequestBody
    {
        /// <summary>
        /// 结果图片格式，目前支持`jpeg`和`png`两种格式的图片结果返回
        /// jpeg拥有更好的压缩比，图片大小可以明显压缩，但是可能会对图片质量造成损失
        /// </summary>
        public string imgFormat = ImgFormat.jpeg.ToString();

        /// <summary>
        /// 结果图片质量，1~100的值，值越小代表对图片压缩率越大
        /// 注意，在jpeg格式下图片为有损压缩，jpeg图片quality值越小文件也会越小，请调整合适的值用于平衡您对图片大小和图片质量的需求
        /// </summary>
        public int imgQuality = 100;

        /// <summary>
        /// 是否返回预览图，如果不需要预览图，可以传false，用于减少响应体大小
        /// </summary>
        public bool preview = true;

        /// <summary>
        /// 任务ID，由提交任务成功返回的任务ID，通常是paintingSign
        /// </summary>
        public string taskId;

        /// <summary>
        /// 等待直到任务完成再返回，可以利用此功能实现`同步调用`，如果此字段为true则此接口会自动等待直到此任务执行完成（成功或者失败）后再返回
        /// </summary>
        public bool waitUtilEnd = false;
    }

    /// <summary>
    /// 结果图片格式，目前支持`jpeg`和`png`两种格式的图片结果返回
    /// jpeg拥有更好的压缩比，图片大小可以明显压缩，但是可能会对图片质量造成损失
    /// </summary>
    public enum ImgFormat { jpeg, png };

    [Serializable]
    public class ResponseBody
    {
        /// <summary>
        /// API统一响应码，错误码请参考[此文档](https://app.apifox.com/project/3150904)
        /// </summary>
        public int code;

        public ResponseBodyData data;

        /// <summary>
        /// 错误消息，如果本次接口请求错误，则会返回对应的错误描述
        /// </summary>
        public string msg;

        /// <summary>
        /// 是否处理成功，简易字段用于区分本次请求接口是否处理成功
        /// </summary>
        public bool success;
    }

    [Serializable]
    public class ResponseBodyData
    {
        /// <summary>
        /// 图片审核结果，仅在state为`success`才会返回。当生成单图的时候，单图的审核结果会直接在这里返回
        /// </summary>
        public int audit;

        /// <summary>
        /// 绘画预览图，用于表现绘画过程的预览图，base64格式，仅在任务没有完成的时候返回
        /// </summary>
        public string current_image;

        /// <summary>
        /// 本次生成的图片列表，仅在state为`success`才会返回。不管是单图还是多图，都会单独在这个数组里面返回每一个图片
        /// </summary>
        public ResponseBodyImage[] images;

        /// <summary>
        /// 单图URL，仅在state为`success`才会返回。图片URL的有效期为半个小时，请获取后自行进行保存
        /// </summary>
        public string imgUrl;

        /// <summary>
        /// 任务进度，取值范围0~1.0
        /// </summary>
        public float progress;

        /// <summary>
        /// 任务状态，任务状态共有4种：
        /// 1. `in_queue`任务当前已经进入队列，等待执行中
        /// 2. `running`任务当前正在执行中，请继续轮询此接口获取最新状态
        /// 3. `success`任务执行结束，并且结果为成功，可以获取任务结果
        /// 4. `fail` 任务执行结束，但是结果为失败，可以获取失败原因
        /// </summary>
        public string state;
    }

    [Serializable]
    public class ResponseBodyImage
    {
        /// <summary>
        /// 图片审核结果，请参考审核结果说明
        /// </summary>
        public int audit;

        /// <summary>
        /// 图片的URL，图片URL的有效期为半个小时，请获取后自行进行保存
        /// </summary>
        public string imageUrl;
    }
}