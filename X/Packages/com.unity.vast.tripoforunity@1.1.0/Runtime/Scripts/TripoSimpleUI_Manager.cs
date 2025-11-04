using UnityEngine;
using UnityEngine.UI;

namespace TripoForUnity
{
    public class TripoSimpleUI_Manager : MonoBehaviour
    {
        public Button btnConfirmApiKey;
        public Button btnTextToModelGenerate;
        public Button btnLoadImage;
        public Button btnImageToMdelGenerate;

        public InputField ApiKeyInputField;
        public InputField TextPromptInputField;
        public InputField ImagePathInputField;
        public Text TextToModelPercentage;
        public Text ImageToMdelPercentage;
        public Image ImagePreview;
        public GameObject SimpleModel;

        public Dropdown ModelVersionDropdown;

        public float ModelRotationSpeed = 50f;

        void Start()
        {
            btnConfirmApiKey.onClick.AddListener(OnConfirmApiKey);
            btnTextToModelGenerate.onClick.AddListener(OnTextToModelGenerate);
            btnLoadImage.onClick.AddListener(OnLoadImage);
            btnImageToMdelGenerate.onClick.AddListener(OnImageToModelGenerate);
            ModelVersionDropdown.onValueChanged.AddListener(OnModelVersionChange);

            GetComponent<TripoRuntimeCore>()
                .OnModelGenerateComplete.AddListener(OnModelDownloadComplete);
        }

        void OnConfirmApiKey()
        {
            GetComponent<TripoRuntimeCore>().SetAPIKey(ApiKeyInputField.text);
        }

        void OnModelVersionChange(int index)
        {
            GetComponent<TripoRuntimeCore>().SetModelVersion((ModelVersion)index);
        }

        void OnTextToModelGenerate()
        {
            GetComponent<TripoRuntimeCore>().SetTextPrompt(TextPromptInputField.text).TextToModel();
        }

        void OnImageToModelGenerate()
        {
            GetComponent<TripoRuntimeCore>().SetImagePath(ImagePathInputField.text).ImageToModel();
        }

        void OnLoadImage()
        {
            GetComponent<TripoRuntimeCore>().imagePath = ImagePathInputField.text;
            LoadImagePreview();
        }

        private void LoadImagePreview()
        {
            string imagePath = ImagePathInputField.text;
            if (!string.IsNullOrEmpty(imagePath))
            {
                // 异步加载图片
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
                Sprite imageSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                ImagePreview.sprite = imageSprite;
            }
            else
            {
                Debug.LogError("Image path is empty or null.");
            }
        }

        void OnModelDownloadComplete(string GltfUrl)
        {
            if (SimpleModel.GetComponent<GLTFast.GltfAsset>())
            {
                Destroy(SimpleModel.GetComponent<GLTFast.GltfAsset>());
            }
            var gltfModel = SimpleModel.AddComponent<GLTFast.GltfAsset>();
            gltfModel.Url = GltfUrl;
        }

        void Update()
        {
            TextToModelPercentage.text =
                Mathf.RoundToInt(100f * GetComponent<TripoRuntimeCore>().textToModelProgress) + "%";
            ImageToMdelPercentage.text =
                Mathf.RoundToInt(100f * GetComponent<TripoRuntimeCore>().imageToModelProgress)
                + "%";
            SimpleModel.transform.Rotate(0, ModelRotationSpeed * Time.deltaTime, 0);
        }
    }
}
