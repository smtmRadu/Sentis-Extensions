using System;
using System.Collections.Generic;
using TMPro;
using Unity.Sentis;
using UnityEngine;


namespace kbradu
{
    public class EffNetClassif : MonoBehaviour
    {
        public ONNXRuntime modelRuntime;
        public DisplayRuntime display;
        public WebcamRuntime webcamRuntime;
        public TMP_Text text;
        public TextAsset labelsFile;
        public int inferenceFreq = 5;

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
            if (Time.frameCount % inferenceFreq != 0)
            {
                TensorFloat f = Utils.TextureToTensor(webcamRuntime.GetCamTexture(true), Origin.TopLeft);
                display.SetTexture(f);
                return;
            }

            var view = webcamRuntime.GetCamTexture(true);
          
            TensorFloat input = Utils.TextureToTensor(view, Origin.TopLeft);
            display.SetTexture(input);
            Utils.Vision.Resize(ref input, 224, 224);
            //display.SetTexture(input);
            Utils.Vision.AffineTransform(input, 2, -1);
           
            TensorFloat output = modelRuntime.Forward(input) as TensorFloat;
            float[] probs = output.ToReadOnlyArray();
            int index =  Utils.Math.ArgMax(probs);
            text.color = Color.Lerp(Color.red, Color.green, probs[index]);
            text.text = $"{labelsMap[index]} ({(int)(probs[index]*100)}%)"; ;

            Destroy(view);
            input.Dispose();
            output.Dispose();
        }
    }
}



