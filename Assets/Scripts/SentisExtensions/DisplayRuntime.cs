using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SentisExtensions
{
    public class DisplayRuntime : MonoBehaviour
    {
        [Header("Add this script to an object with an UI Image component.")]
        private Image image;
        private Texture2D texture2d;
        private Sprite sprite;

        private void Start()
        {
            image = GetComponent<Image>();
        }

        // This version is faster
        public void SetTexture(RenderTexture texture)
        {
            // Create the Texture2D and Sprite only once
            if (texture2d == null)
            {
                texture2d = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                this.sprite = Sprite.Create(texture2d, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                image.sprite = this.sprite;
            }

            // Set the active RenderTexture and copy its pixels into the Texture2D
            RenderTexture.active = texture;
            texture2d.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture2d.Apply();

            // Optionally clear RenderTexture.active to avoid side effects
            RenderTexture.active = null;
        }



        public void SetTexture(Texture2D texture, bool destroyPrevTexture = true)
        {
            if (destroyPrevTexture && image.sprite != null && image.sprite.texture != null)
            {
                Destroy(image.sprite.texture);
            }

            if (image.sprite != null)
            {
                // Destroy the previous sprite if it exists
                Destroy(image.sprite);
            }

            Destroy(image.sprite);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        [Obsolete("Use SetTexture method instead, the efficiency is the same.")]
        public void SetTexturePixelsFromTensor(Tensor<float> tensor, ImageShape tensor_format)
        {
            int height = -1;
            int width = -1;
            int channels = -1;

            if (tensor_format == ImageShape.HWC)
            {
                int[] shape = tensor.Shape4D();
                height = shape[1];
                width = shape[2];
                channels = shape[3];
            }
            else if (tensor_format == ImageShape.CHW)
            {
                int[] shape = tensor.Shape4D();
                channels = shape[1];
                height = shape[2];
                width = shape[3];
               
            }
            else
                throw new NotImplementedException();

            if(image.sprite == null)
            {
                Texture2D tex = new Texture2D(width, height);
                image.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }

            Color[] colors = image.sprite.texture.GetPixels();

            Parallel.For(0, height, h =>
            {
                for (int w = 0; w < width; w++)
                {
                    Color pixel;

                    if (channels == 3)
                    {
                        pixel = tensor_format == ImageShape.HWC ?
                            new Color(tensor[h, w, 0], tensor[h, w, 1], tensor[h, w, 2]) :
                            new Color(tensor[0, h, w], tensor[1, h, w], tensor[2, h, w]);
                    }
                    else if (channels == 1)
                    {
                        float value = tensor_format == ImageShape.HWC ? tensor[h, w, 0] : tensor[0, h, w];
                        pixel = new Color(value, value, value); // Grayscale
                    }
                    else if (channels == 4)
                    {
                        pixel = tensor_format == ImageShape.HWC ?
                            new Color(tensor[h, w, 0], tensor[h, w, 1], tensor[h, w, 2], tensor[h, w, 3]) :
                            new Color(tensor[0, h, w], tensor[1, h, w], tensor[2, h, w], tensor[3, h, w]);
                    }
                    else
                    {
                        throw new NotImplementedException("Unhandled channels different from 1, 3, and 4");
                    }

                    colors[w + h * width] = pixel;
                }
            });

            image.sprite.texture.SetPixels(colors);
            image.sprite.texture.Apply();
        }
      
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DisplayRuntime), true), CanEditMultipleObjects]
    sealed class UIDisplayEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            List<string> dontDrawMe = new List<string> { "m_Script" };
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, dontDrawMe.ToArray());
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

}


