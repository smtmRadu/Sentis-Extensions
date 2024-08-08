using kbradu;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using static kbradu.YOLOScript;

public class VRYOLO : MonoBehaviour
{
    [SerializeField] private ModelRuntime modelRuntime;
    [SerializeField] private CameraRuntime vrCamera;
    [SerializeField] private DisplayRuntime displayRuntime;
    [SerializeField] private TMP_Text text;
    [SerializeField] private TextAsset labels;
    [SerializeField] private float confidence_tresh = 0.5f;


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
        Detect();
    }

    public void Detect()
    {
        Texture2D input_view = vrCamera.GetCamTexture(false,~(1<<3));

        Color init_cam_color = vrCamera.cam.backgroundColor;
        vrCamera.cam.backgroundColor = Color.black;
        Texture2D controllers_view = vrCamera.GetCamTexture(false, 1<<3);
        vrCamera.cam.backgroundColor = init_cam_color;

        TensorFloat input = TensorFloatExtensions.FromTexture(input_view, ImageShape.CHW, OriginLike.OpenCV, multithread: true);
        TensorFloat output = modelRuntime.Forward(input) as TensorFloat;

        PostProcess_yolov10(input, output, Time.deltaTime); // to be optimized
        Texture2D display_view = TensorFloatExtensions.ToTexture(input, ImageShape.CHW);
        PasteControllerOnView(display_view, controllers_view);
        displayRuntime.SetTexture(display_view);

        input.Dispose();
        output.Dispose();
        Destroy(input_view);
        Destroy(controllers_view);
    }
    public void PasteControllerOnView(Texture2D view, Texture2D controllers)
    {
        int width = view.width;
        int height = view.height;

        Color[] viewPixels = view.GetPixels();
        Color[] controllersPixels = controllers.GetPixels();
        Color[] finalPixels = new Color[viewPixels.Length];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                int viewIndex = x + y * width;
                int flippedY = height - 1 - y;
                int controllersIndex = x + flippedY * width;

                if (controllersPixels[controllersIndex] == Color.black)
                {
                    finalPixels[viewIndex] = viewPixels[viewIndex];
                }
                else
                {
                    finalPixels[viewIndex] = controllersPixels[controllersIndex];
                }
            }
        });

        view.SetPixels(finalPixels);
        view.Apply();
    }


    public async void PostProcess_yolov10(TensorFloat input, TensorFloat yolo_output, float deltatime)
    {
        // input 3, 640, 640
        // 1,300, 6 
        ConcurrentBag<DetectedObject> list = new();
        Parallel.For(0, 300, i =>
        {
            float x = yolo_output[0, i, 0];
            float y = yolo_output[0, i, 1];
            float w = yolo_output[0, i, 2];
            float h = yolo_output[0, i, 3];
            float confidence = yolo_output[0, i, 4];
            string classname = classLabels[(int)yolo_output[0, i, 5]];

            if (confidence > confidence_tresh)
                list.Add(new DetectedObject(new Rect(x, y, w, h), confidence, classname));
        });


        Task<string> to_display_string = Task.Run(() =>
        {
            StringBuilder stringBuilder = new StringBuilder($"FPS: {(int)(1f / deltatime)}\nDetections: {list.Count}\n\n");
            foreach (DetectedObject obj in list)
            {
                Color col = classColors[obj.name];
                stringBuilder.Append($"<color={Utils.HexOf(col.r, col.g, col.b)}>");
                stringBuilder.Append(obj.name);
                stringBuilder.Append(" - ");
                stringBuilder.Append($"{(int)(obj.confidence * 100f)}%");
                stringBuilder.Append("</color>\n");
            }
            return stringBuilder.ToString();
        });
       

        Parallel.ForEach(list, obj =>
        {
            DrawBoundingBox(input, obj.bounding_box, classColors[obj.name], obj.confidence);
        });

        text.text = await to_display_string;
    }
}
