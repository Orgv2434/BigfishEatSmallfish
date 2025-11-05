using System;

public class DrawModel
{
    [Serializable]
    public class RequestBody
    {
        /// <summary>
        /// 描述词条，描述画面中需要出现的内容，支持中英文描述
        /// 详细词条技巧请参考[此文章](https://www.huashi6.com/article/detail-13420.html)
        /// </summary>
        public string prompt;

        /// <summary>
        /// 负面词条，用于排除画面中要出现的内容描述，支持中英文描述
        /// </summary>
        public string negativePrompt = "";

        /// <summary>        ///
        /// 风格ID，指定要生成对应风格预设的图片，触站AI官方会预设大量可以直接进行生产使用的风格预设，部分可用ID请参考[此文档](https://chuzhanai.apifox.cn/doc-3699065)
        /// 此字段也可以使用模型训练API自定义风格产出的结果，详见[模型训练](https://chuzhanai.apifox.cn/api-123833428)
        /// modelstyle 参如下
        /// 模型案例：（模型id-模型名称）
        /// id name
        /// 1二次元        2多彩厚涂       3日系薄涂       4 2.5D写实     5手办风格       6古早风
        /// 7彩漫风        8二次元风景      9日系唯美人像     10水彩      11小清新       12Q版
        /// 13线稿        14国风            15真人写实   18写实甜妹      19二次元男生    20室内设计
        /// 21炫酷机甲      22儿童插画      23 3D卡通     24唯美动漫风  25泼墨           26温暖二次元风
        /// 27可爱中国风   28半写实手绘       29福瑞娘     30儿童绘本
        /// </summary>
        public int modelStyleId;

        /// <summary>
        /// 生成图片宽度，在未使用高清修复的情况下，图片尺寸不建议设置过小，过小的尺寸会导致画面中的物体轮廓生成不完整，会造成比如人脸生成崩坏等问题的产生
        /// </summary>
        public int width = 512;

        /// <summary>
        /// 生成图片高度，在未使用高清修复的情况下，图片尺寸不建议设置过小，过小的尺寸会导致画面中的物体轮廓生成不完整，会造成比如人脸生成崩坏等问题的产生
        /// </summary>
        public int height = 512;

        /// <summary>
        /// 绘图步数，绘图步数与最终图片生成质量有关系，通常模型风格的默认步数为20步，如果有更高的需求或者能够对画面质量降低有一定容忍度，可以尝试提高或者减少步数。注意：部分风格步数为独立配置，为获得最佳风格效果，此参数可以使用默认配置不用传
        /// </summary>
        //public int steps;

        /// <summary>
        /// 参考图片，生成参考图，此字段传值则代表模式为`图生图`，同时支持base64数据格式和url（注意：url必须可以公网访问）
        /// </summary>
        //public string img;

        /// <summary>
        /// 黑白二值遮罩蒙版图，用于局部重绘处理，可以是图片URL或者base64格式字符串。
        /// 注意：遮罩图黑色区域代表需要重绘的部分
        /// 局部重绘功能请参考[此文档](https://chuzhanai.apifox.cn/doc-3934922)
        /// </summary>
        //public string maskImg;

        /// <summary>
        /// 重绘幅度，重绘幅度仅在`img字段`有传值的时候生效，重绘幅度越大代表生成图与原图越不相似，取值范围`0~1`
        /// 取值为0时，代表不会对img进行修改，出来的图与img几乎一致
        /// 取值为1是，代表对img进行完全修改，与img无任何相似
        /// </summary>
        public float denoisingStrength = 0.55f;

        /// <summary>
        /// 高清化倍率，对生成的图片进行高清化处理，仅在`文生图`模式有效，取值范围为1~3之间
        /// </summary>
        //public float hrScale;

        /// <summary>
        /// 高清处理步数，高清迭代步数，（不建议少于15，会严重影响画面生成效果）
        /// </summary>
        //public int hrSteps;

        /// <summary>
        /// ai算法放大倍率，AI算法放大倍数
        /// </summary>
        //public int upscale;

        /// <summary>
        /// 随机种子，随机种子可以用于进行画面重现，默认不传则为随机生成
        /// </summary>
        //public int seed;

        /// <summary>
        /// 花纹贴图，是否生成花纹贴图，一般用于纹理生成使用
        /// </summary>
        //public bool tiling = false;

        /// <summary>
        /// 单次批量生成数量，单次生成图片数量不可以超过6个
        /// </summary>
        public int batchSize = 1;

        /// <summary>
        /// 脸部修复开关，开启后系统将会对生成图片人物脸部进行高级修复处理，大大改善人物脸部崩坏情况，开启需要单独`扣减2积分`
        /// </summary>
        //public bool faceFix = false;

        /// <summary>
        /// 细节倍率，取值范围为`1~9`小于5则画面偏向简单扁平话、草稿化大于5则画面偏向添加更多光影、服饰、头发等细节详细效果请参考[此文章](https://www.huashi6.com/article/detail-17448.html)
        /// </summary>
        //public int detailsLevel = 5;

        /// <summary>
        /// 引导系数，用于引导画面与描述词符合程度，取值`1~30`，取值越大越符合描述词，但是会限制AI发挥空间，取值小越不像描述词。（不同风格模型对此值有不同的预设，一般情况下此值不需要传，用风格默认即可）
        /// </summary>
        //public float cfgScale;

        /// <summary>
        /// 图片高级功能参数，可以通过一系列参数实现高级效果
        /// </summary>
        //public ImgOptions imgOptions;

        /// <summary>
        /// controlnet功能实现，详细使用请参考[此文档](https://chuzhanai.apifox.cn/doc-3672565)
        /// </summary>
        public Controlnet controlnet;

        /// <summary>
        /// 预测模拟积分消耗，如果传值为true则代表本次是测试模拟积分消耗量，不进行真正的绘画操作，仅用于计算同参数下积分消耗量，不扣减任何积分
        /// </summary>
        //public bool predictConsume = false;

        /// <summary>
        /// 结果回调地址，支持IP等URL[请参考回调使用指南](https://chuzhanai.apifox.cn/doc-3556414)
        /// </summary>
        //public string callback;

        /// <summary>
        /// 自定义请求回调标识，任意长度不超过32的字符串，具体[请参考回调使用指南](https://chuzhanai.apifox.cn/doc-3556414)
        /// </summary>
        //public string nonce;
    }

    /// <summary>
    /// 图片高级功能参数，可以通过一系列参数实现高级效果
    /// </summary>
    [Serializable]
    public class ImgOptions
    {
        /// <summary>
        /// 移除画面背景，是否自动移除图片中的人物背景，一般用于重绘背景有效，`注意：开启此功能需额外扣除2积分`
        /// </summary>
        public bool removeBackground = false;

        /// <summary>
        /// 是否保留画面中的主体，仅绘制画面中的背景`注意：开启此功能需额外扣除2积分`
        /// </summary>
        public bool redrawBackground = false;

        /// <summary>
        /// 从原始背景中重绘，是否完全清除原始背景，不从原始背景中重绘。
        /// 需要denoisingStrength设置为1，且搭配controlnet获得最好的效果
        /// </summary>
        public bool redrawBackgroundFromOrigin = true;

        /// <summary>
        /// 自定识别画面中的主体，用于背景相关处理。取值：
        /// - 当`removeBackground`或者`redrawBackground`为true的时候，画面中的主体类型，请根据您的画面中的实际情况传值
        /// </summary>
        public MainObjectType mainObjectType = MainObjectType.Human;

        /// <summary>
        /// 是否自动检测图片中的人物性别，尽量使生成前后的人物性别不发生变化，如果图片中有多个人物性别则效果会不准确。
        /// `注意，开启此功能需额外扣除1积分`
        /// </summary>
        public bool genderDetect = false;

        /// <summary>
        /// 自动识别图片中的词条，用于生图的时候，自动检测图片中的词条生成（如果prompt有传，则会覆盖掉）
        /// 如果`genderDetect` 字段为true，则此字段不会生效
        /// </summary>
        public bool promptDetect = false;

        /// <summary>
        /// 自动检测图片中的人脸，使生成后的人脸与原始人脸保持高度相似。
        /// `注意，开启此功能需额外扣除2积分`
        /// </summary>
        public bool facePreservation = false;

        /// <summary>
        /// 开启人脸保持后，画面中最多处理的人脸数量，默认为1，最大值可以为5
        /// </summary>
        public int facePreservationCount = 1;

        /// <summary>        ///
        /// imgOptions参考图，在最外层`img`参数没有传的时候，如果有传imgOptions的其他参数，则此img为必传，否则使用最外层img作为参考图（注意：redrawBackground、removeBackground仍然只在图生图模式下生效）
        /// </summary>
        public string img;
    }

    /// <summary>
    /// controlnet功能实现，详细使用请参考[此文档](https://chuzhanai.apifox.cn/doc-3672565)
    /// </summary>

    [Serializable]
    public class Controlnet
    {
        /// <summary>
        /// 默认控制图，如果此字段传值，可以在controlnet的unit中没有传对应的image时候，用此image进行控制，可以认为是一个默认控制图
        /// 支持base64与图片url（需公网url）
        /// </summary>
        //public string image;

        /// <summary>
        /// controlnet控制单元列表，最多可以设置3个控制单元,最少需要设置一个
        /// </summary>
        public Unit[] units;
    }

    [Serializable]
    public class Unit
    {
        /// <summary>
        /// 控制类型，控制类型用于指定特定的控制模型，可选值请参考[此文档](https://chuzhanai.apifox.cn/doc-3672565#%E5%85%A8%E9%83%A8%E5%8F%AF%E7%94%A8%E6%8E%A7%E5%88%B6%E5%99%A8)
        /// </summary>
        public string type;

        /// <summary>
        /// 参考图，在图生图模式非必传，不传默认为`img`参数，参数可以为`base64`图片或者图片url（请确保公网可以访问）
        /// </summary>
        public string image;

        /// <summary>
        /// 控制模式，* `0`:平衡模式
        /// * `1`: 关键字的效果更强，controlnet本身的控制效果会减弱
        /// * `2`: controlnet的控制会加强，更忽略关键词的效果
        /// </summary>
        public int controlMode = 0;

        /// <summary>
        /// 控制权重，取值范围0~2，控制权重越大，对画面影响效果越明显，默认为1
        /// </summary>
        public float weight = 1;

        /// <summary>
        /// 控制开始时机，在绘制过程中，controlnet干预开始时机点，取值0~1,默认为0
        /// </summary>
        public float controlStart = 0;

        /// <summary>
        /// 结束控制时机，在绘制过程中，controlnet在什么进度结束干预，取值0~1,默认为1
        /// </summary>
        public float controlEnd = 1;

        /// <summary>
        /// 不同控制类型特定参数
        /// </summary>
        //public float paramsA;

        /// <summary>
        /// 不同控制类型特定参数
        /// </summary>
        //public float paramsB;

        /// <summary>
        /// 是否对图片进行预处理，如果您的图片是原始图片则默认是需要对图片进行预处理提取成特定controlnet模型需要的图片，因此默认为true
        /// </summary>
        public bool preprocess = true;
    }

    /// <summary>
    /// 自定识别画面中的主体，用于背景相关处理。取值：
    /// - 当`removeBackground`或者`redrawBackground`为true的时候，画面中的主体类型，请根据您的画面中的实际情况传值
    /// </summary>
    public enum MainObjectType { General, Human };

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
        /// 账户当前余额
        /// </summary>
        public int balance;

        /// <summary>
        /// 参数需要消耗积分明细，predictConsume为true时候返回此值
        /// </summary>
        public ConsumeDetail[] consumeDetail;

        /// <summary>
        /// 预计消耗积分量，predictConsume为true时候返回此值
        /// </summary>
        public int estimateUsed;

        /// <summary>
        /// 绘画任务ID，可以用于[绘画详情接口](https://chuzhanai.apifox.cn/api-123807409)查询进度或者获取结果
        /// 如果`predictConsume`为true则接口不会返回此值
        /// </summary>
        public string paintingSign;

        /// <summary>
        /// 任务限制数，并发任务数
        /// </summary>
        public int taskLimitCount;

        /// <summary>
        /// 本次扣减积分
        /// </summary>
        public int used;

        [Serializable]
        public class ConsumeDetail
        {
            /// <summary>
            /// 积分量
            /// </summary>
            public int count;

            /// <summary>
            /// 积分扣减计算说明
            /// </summary>
            public string desc;
        }
    }
}
