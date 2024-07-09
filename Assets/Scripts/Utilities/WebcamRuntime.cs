using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace kbradu
{
    public class WebcamRuntime : MonoBehaviour
    {
        [SerializeField] private int cameraIndex = 0;
        private WebCamTexture webcamTex;
        private void Awake()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length > 0)
            {
                webcamTex = new WebCamTexture(devices[cameraIndex].name);
                webcamTex.Play();
            }
        }

        public Texture2D GetCamTexture(bool centerCrop = false)
        {
            if (webcamTex == null || !webcamTex.isPlaying)
            {
                Debug.LogError("Webcam texture is not initialized or not playing.");
                return null;
            }

            if(!centerCrop)
            {

                Texture2D tex = new Texture2D(webcamTex.width, webcamTex.height);
                tex.SetPixels(webcamTex.GetPixels());
                tex.Apply();
                return tex;
            }
            else
            {
                int size = Mathf.Min(webcamTex.width, webcamTex.height);
                int startX = (webcamTex.width - size) / 2;
                int startY = (webcamTex.height - size) / 2;

                Texture2D croppedTex = new Texture2D(size, size);
                Color[] pixels = webcamTex.GetPixels(startX, startY, size, size);
                croppedTex.SetPixels(pixels);
                croppedTex.Apply();

                return croppedTex;
            }

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WebcamRuntime), true), CanEditMultipleObjects]
    sealed class WebcamRuntimeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            List<string> dontDrawMe = new List<string> { "m_Script"};
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, dontDrawMe.ToArray());
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}



