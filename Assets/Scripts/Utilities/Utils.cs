
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace kbradu
{
    public enum OriginLike
    {
        /// <summary>
        /// TOP-LEFT
        /// </summary>
        OpenCV,
        /// <summary>
        /// BOTTOM-LEFT
        /// </summary>
        UnityTexture,
    }
    public enum ImageShape
    {
        HWC,
        CHW
    }
    public static class Utils
    {
        public static Texture2D CloneTexture(Texture2D texture)
        {
            // Create a new Texture2D with the same dimensions and format as the source
            Texture2D clonedTexture = new Texture2D(
                texture.width,
                texture.height,
                texture.format,
                texture.mipmapCount > 1);

            // Copy the raw data from the source texture to the new texture
            Graphics.CopyTexture(texture, clonedTexture);

            // Ensure the new texture has the same filter mode, wrap mode, and anisoLevel
            clonedTexture.filterMode = texture.filterMode;
            clonedTexture.wrapMode = texture.wrapMode;
            clonedTexture.anisoLevel = texture.anisoLevel;

            // Apply the changes
            clonedTexture.Apply(false);

            return clonedTexture;
        }
        
        /// <summary>
        /// Returns the hex value of the given r, g & b values in range [0, 255].
        /// </summary>
        public static string HexOf(int r, int g, int b)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b);
        }
        /// <summary>
        /// Returns the hex value of the given r, g & b values in range [0, 1].
        /// </summary>
        public static string HexOf(float r, float g, float b)
        {
            int ri = (int)(r * 255.0f);
            int gi = (int)(g * 255.0f);
            int bi = (int)(b * 255.0f);

            return string.Format("#{0:X2}{1:X2}{2:X2}", ri, gi, bi);
        }

        public static class Math
        {
            public static float Sigmoid(float x)
            {
                return 1f / (1f + Mathf.Exp(-x));
            }
            public static float Tanh(float x)
            {
                float e2x = Mathf.Exp(x);
                return (e2x - 1) / (e2x + 1);
            }
            public static int ArgMax(float[] arr)
            {
                int maxIndex = 0;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] > arr[maxIndex])
                        maxIndex = i;
                }
                return maxIndex;
            }
            public static float[] SoftMax(float[] arr)
            {
                float[] result = new float[arr.Length];
                float max = arr.Max();

                float sum = 0.0f;
                for (int i = 0; i < arr.Length; i++)
                {
                    result[i] = Mathf.Exp(arr[i] - max);
                    sum += result[i];
                }

                for (int i = 0; i < arr.Length; i++)
                {
                    result[i] /= sum;
                }

                return result;
            }
        }

        public static class Benckmark
        {
            static Stopwatch _clock;
            public static void Start()
            {
                _clock = Stopwatch.StartNew();
            }
            public static TimeSpan Stop()
            {
                _clock.Stop();
                UnityEngine.Debug.Log("[TIMER] : " + _clock.Elapsed);
                return _clock.Elapsed;
            }
        }

    }
}