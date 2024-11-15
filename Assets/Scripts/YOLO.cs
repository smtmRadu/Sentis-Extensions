using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using SentisExtensions;
namespace kbradu
{
    public class YOLOScript : MonoBehaviour
    {
        [SerializeField] private ModelRuntime modelRuntime;
        [SerializeField] private CameraRuntime cam;
        [SerializeField] private DisplayRuntime cameraDisplay;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Sprite testImage;
        [SerializeField] private TextAsset labels;
        [SerializeField] private float confidence_tresh = 0.5f;
        [SerializeField] private int inferenceFreq = 5;

        [Header("View Only")]
        [SerializeField] private int CURRENT_DETECTIONS = 0;

        // https://github.com/szaza/android-yolo-v2/blob/master/assets/tiny-yolo-voc-labels.txt
        Dictionary<int, string> classLabels;
        Dictionary<string, Color> classColors;


        private void Start()
        {
            classLabels = new();
            classColors = new();

            string dep = labels.text;
            string[] pairs = dep.Trim('{', '}').Split(new[] { ", " }, StringSplitOptions.None);
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split(new[] { ": " }, StringSplitOptions.None);

                if (keyValue.Length == 2)
                {
                    int key = int.Parse(keyValue[0].Trim());
                    string value = keyValue[1].Trim('\'');
                    classLabels.Add(key, value);
                    classColors.Add(value, UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                }
            }
        }
        public void Update()
        {
            if(Time.frameCount % inferenceFreq == 0)
                Detect();
        }

        public void Detect()
        {
            // with multithread 0.5 around


    
            Utils.Benckmark.Start();
            Texture2D view = testImage.texture;
            Tensor<float> input = TensorExtensions.FromTexture(view, ImageShape.HWC, OriginCoord.OpenCV);

            input = input.CenterCropBHWC();
            input = input.ResizeBHWC(640, 640);
            input = input.HWC2CHW();



            Tensor<float> output = modelRuntime.Forward(input) as Tensor<float>;
       

            var result = PostProcess_yolov10n(input, output); // to be optimized
           
            // this takes 0.06
            cameraDisplay.SetTexture(result.ToTexture(ImageShape.CHW));

            input.Dispose();
            output.Dispose();
            result.Dispose();
            Utils.Benckmark.Stop();
        }
        public Tensor<float> PostProcess_yolov10n(Tensor<float> input, Tensor<float> result)
        {
            // input 3, 640, 640
            // 1,300, 6

            LinkedList<DetectedObject> list = new();

            for (int i = 0; i < 300; i++)
            {
                float x = result[0, i, 0];
                float y = result[0, i, 1];
                float w = result[0, i, 2];
                float h = result[0, i, 3];
                float confidence = result[0, i, 4];
                string classname = classLabels[(int)result[0, i, 5]];

                if(confidence > confidence_tresh)         
                    list.AddLast(new DetectedObject(new Rect(x, y, w, h), confidence, classname));
            }
            
            StringBuilder stringBuilder = new StringBuilder($"(FPS:{1f/Time.deltaTime}) Detections : {list.Count}\n\n");
            foreach (DetectedObject obj in list)
            {
                Color col = classColors[obj.name];
                stringBuilder.Append($"<color={Utils.HexOf(col.r, col.g, col.b)}>");
                stringBuilder.Append(obj.name);
                stringBuilder.Append(" - ");
                stringBuilder.Append($"{(int)(obj.confidence * 100f)}%");
                stringBuilder.Append("</color>\n");
               
                
            }
            text.text = stringBuilder.ToString();
            CURRENT_DETECTIONS = list.Count;

            Parallel.ForEach(list, obj =>
            {
                DrawBoundingBox(input, obj.bounding_box, classColors[obj.name], obj.confidence);
            });
            
            return input;
        }
        public static void DrawBoundingBox(Tensor<float> imageCHW, Rect box, Color color, float confidence, float line_thickness = 5f)
        {
            int channels = imageCHW.shape[1];
            int height = imageCHW.shape[2];
            int width = imageCHW.shape[3];

            int x = (int)box.x;
            int y = (int)box.y;
            int x2 = (int)(box.x + box.width/2);
            int y2 = (int)(box.y + box.height/2);

            float r = color.r;
            float g = color.g;
            float b = color.b;

            int lineThickness = (int)(confidence * line_thickness); 

            for (int i = x; i <= x2; i++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    if (y + t < height) DrawPixel(imageCHW, i, y + t, r, g, b);
                    if (y2 - t >= 0) DrawPixel(imageCHW, i, y2 - t, r, g, b);
                }
            }

            for (int j = y; j <= y2; j++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    if (x + t < width) DrawPixel(imageCHW, x + t, j, r, g, b);
                    if (x2 - t >= 0) DrawPixel(imageCHW, x2 - t, j, r, g, b);
                }
            }
        }

        // Helper method to draw a single pixel
        private static void DrawPixel(Tensor<float> imageCHW, int x, int y, float r, float g, float b)
        {
            int channels = imageCHW.shape[1];
            int height = imageCHW.shape[2];
            int width = imageCHW.shape[3];

            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                imageCHW[0, 0, y, x] = r; 
                if (channels > 1) imageCHW[0, 1, y, x] = g; 
                if (channels > 2) imageCHW[0, 2, y, x] = b;
            }
        }
    

        public static List<DetectedObject> NonMaxSuppression(List<DetectedObject> detections, float iouThreshold)
        {
            List<DetectedObject> result = new List<DetectedObject>();

            // Sort detections by confidence score - highest first
            detections.Sort((a, b) => b.confidence.CompareTo(a.confidence));

            while (detections.Count > 0)
            {
                DetectedObject current = detections[0];
                result.Add(current);
                detections.RemoveAt(0);

                for (int i = detections.Count - 1; i >= 0; i--)
                {
                    if (CalculateIoU(current.bounding_box, detections[i].bounding_box) > iouThreshold)
                    {
                        detections.RemoveAt(i);
                    }
                }
            }

            return result;
        }

        private static float CalculateIoU(Rect box1, Rect box2)
        {
            float intersectionArea = IntersectionArea(box1, box2);
            float unionArea = box1.width * box1.height + box2.width * box2.height - intersectionArea;
            return intersectionArea / unionArea;
        }

        private static float IntersectionArea(Rect box1, Rect box2)
        {
            float x1 = Mathf.Max(box1.x, box2.x);
            float y1 = Mathf.Max(box1.y, box2.y);
            float x2 = Mathf.Min(box1.x + box1.width, box2.x + box2.width);
            float y2 = Mathf.Min(box1.y + box1.height, box2.y + box2.height);

            float width = Mathf.Max(0, x2 - x1);
            float height = Mathf.Max(0, y2 - y1);

            return width * height;


        }

        public class DetectedObject
        {
            public Rect bounding_box;
            public float confidence;
            public string name;

            public DetectedObject(Rect bounding_box, float confidence, string classname)
            {
                this.bounding_box = bounding_box;
                this.confidence = confidence;
                this.name = classname;
            }

        }


    }


}