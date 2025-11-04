// TripoDataModels.cs
using System;

namespace TripoForUnity
{
    #region
    public enum ModelVersion
    {
        Turbo_v1_0_20250506,
        v1_4_20240625,
        v2_0_20240919,
        v2_5_20250123,

        /// <summary>
        /// Note: The current version is still in beta,
        /// and we are continuing to improve it before the final launch.
        /// If you encounter any issues, please let us know.
        /// </summary>
        v3_0_20250812,
    }

    public enum TextureQuality
    {
        Standard = 0,
        Detailed = 1,
    }

    public enum ModelStyle
    {
        Original = 0,
        Cartoon = 1,
        Clay = 2,
        Steampunk = 3,
        Venom = 4,
        Barbie = 5,
        Christmas = 6,
    }

    public enum Orientation
    {
        Default = 0,
        AlignImage = 1,
    }

    public enum TextureAlignment
    {
        OriginalImage = 0,
        Geometry = 1,
    }
    #endregion

    #region Task
    [System.Serializable]
    public class TaskSearchResponse
    {
        public int code;
        public TaskSearchData data;
    }

    [System.Serializable]
    public class TaskSearchData
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
    public class TaskSearchInput
    {
        public string prompt;
        public string model_version;
        public bool pbr;
        public bool quad;
    }

    [System.Serializable]
    public class TaskSearchOutputData
    {
        public string model;
        public string base_model;
        public string pbr_model;
        public string rendered_image;
    }
    #endregion


    #region TextToModel
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

    public class TextPromptsRequestData_WithStyle
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
    #endregion


    #region ImageToModel
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
    public class ImagePromptsRequestData_WithStyle
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
    #endregion


    #region Rigging
    [Serializable]
    public class RiggingRequestData
    {
        public string type;
        public string original_model_task_id;
        public string out_format;
    }
    #endregion

    #region Response
    [Serializable]
    public class ImageResponseData
    {
        public int code;
        public ImageData data;
    }

    [Serializable]
    public class ImageData
    {
        public string image_token;
    }

    [System.Serializable]
    public class BaseTaskResponse
    {
        public int code;
        public BaseTaskData data;
    }

    [System.Serializable]
    public class BaseTaskData
    {
        public string task_id;
    }

    // public class ImageTaskResponse
    // {
    //     public int code;
    //     public ImageTaskData data;
    // }

    // [System.Serializable]
    // public class ImageTaskData
    // {
    //     public string task_id;
    // }

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
    public class FBXConversionRequestData
    {
        public string type;
        public string format;
        public string original_model_task_id;
    }

    #endregion
}
