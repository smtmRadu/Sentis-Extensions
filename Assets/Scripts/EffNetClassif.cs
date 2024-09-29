using System;
using System.Collections.Generic;
using TMPro;
using Unity.Sentis;
using UnityEngine;
using SentisExtensions;
using UnityEngine.Assertions;

namespace kbradu
{
    public class EffNetClassif : MonoBehaviour
    {
        public ModelRuntime modelRuntime;
        public DisplayRuntime display;
        public WebcamRuntime webcamRuntime;
        public TMP_Text text;
        public TextAsset labelsFile;

        private Dictionary<int, string> labelsMap = new();

        private void Start()
        {
            string dep = labelsFile.text;
            string[] pairs = dep.Trim('{', '}').Split(new[] { "\", \"" }, StringSplitOptions.None);

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split(new[] { "\": \"" }, StringSplitOptions.None);

                if (keyValue.Length == 2)
                {
                    int key = int.Parse(keyValue[0].Trim('\"'));
                    string value = keyValue[1].Trim('\"');
                    labelsMap.Add(key, value);
                }
            }

     
        }

        private void Update()
        {
            var cam_view = webcamRuntime.GetCamTexture(true);

            Tensor<float> input = TensorExtensions.FromTexture(cam_view, ImageShape.HWC, OriginCoord.OpenCV);

            input = input.ResizeBHWC(224, 224);
            input = input.AffineTransform(2, -1);

            Tensor<float> output = modelRuntime.Forward(input) as Tensor<float>;
            float[] probs = output.DownloadToArray();
           
            int index =  Utils.Math.ArgMax(probs);
            text.color = Color.Lerp(Color.red, Color.green, probs[index]);
            text.text = $"{labelsMap[index]} ({(int)(probs[index]*100)}%)";

            input.Dispose();
            output.Dispose();

            display.SetTexture(cam_view);

            
          
        }
    }
}



