using UnityEngine;
using UnityEngine.UI;

namespace TripoForUnity
{
    public class TripoController : MonoBehaviour
    {
        public string myApiKey = "tsk_ztER2NfJqGkP3fFqdcnbQRSKhqe3sMf97mk-7M2uNpr";

        public Button btnLoadImage;
        public Button btnImageToMdelGenerate;
        public Text ImageToMdelPercentage;
        public Image ImagePreview;
        public GameObject SimpleModel;

        public float ModelRotationSpeed = 50f;

        void Start()
        {
            btnLoadImage.onClick.AddListener(OnLoadImage);
            btnImageToMdelGenerate.onClick.AddListener(OnImageToMdelGenerate);

            GetComponent<TripoRuntimeCore>().OnDownloadComplete.AddListener(OnFbxDownloadComplete);
        }

        void OnConfirmApiKey()
        {
            //GetComponent<TripoRuntimeCore>().set_api_key(ApiKeyInputField.text);
        }

        void OnTextToModelGenerate()
        {

            //GetComponent<TripoRuntimeCore>().textPrompt = TextPromptInputField.text;
            //GetComponent<TripoRuntimeCore>().Text_to_Model_func();

        }

        void OnImageToMdelGenerate()
        {
            GetComponent<TripoRuntimeCore>().set_api_key(myApiKey);
            //GetComponent<TripoRuntimeCore>().imagePath = ImagePathInputField.text;
            GetComponent<TripoRuntimeCore>().Image_to_Model_func();
        }

        void OnLoadImage()
        {
            //GetComponent<TripoRuntimeCore>().imagePath = ImagePathInputField.text;
            LoadImagePreview();
        }

        private void LoadImagePreview()
        {
            string imagePath = GetComponent<SketchDemo>().GetImagePath();
            if (!string.IsNullOrEmpty(imagePath))
            {
                // “Ï≤Ωº”‘ÿÕº∆¨
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
                Sprite imageSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                ImagePreview.sprite = imageSprite;
            }
            else
            {
                Debug.LogError("Image path is empty or null.");
            }
        }
        void OnFbxDownloadComplete(string GltfUrl)
        {
            if (SimpleModel.GetComponent<GLTFast.GltfAsset>())
            {
                Destroy(SimpleModel.GetComponent<GLTFast.GltfAsset>());
            }
            var gltfModel = SimpleModel.AddComponent<GLTFast.GltfAsset>();
            gltfModel.Url = GltfUrl;
            gltfModel.GetMaterial();
        }

        void Update()
        {
            
            ImageToMdelPercentage.text = Mathf.RoundToInt(100f * GetComponent<TripoRuntimeCore>().imageToModelProgress) + "%";
            SimpleModel.transform.Rotate(0, ModelRotationSpeed * Time.deltaTime, 0);
        }
    }
}