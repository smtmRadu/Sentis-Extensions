using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace kbradu
{
    public class CameraRuntime : MonoBehaviour
    {
        private Camera cam;
        private RenderTexture renderTexture;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public Texture2D GetCamTexture(bool centerCrop = false)
        {
            if (cam == null)
            {
                Debug.LogError("CameraRuntime was attached to a GameObject without CameraComponent.");
                return null;
            }

            if (renderTexture == null)
                renderTexture = new RenderTexture(cam.targetTexture.width, cam.targetTexture.height, 3);

            cam.targetTexture = renderTexture;

            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;

            Texture2D image = null;
            if (!centerCrop)
            {
                image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
                image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
            }
            else
            { 
                int size = Mathf.Min(renderTexture.width, renderTexture.height);
                int startX = (renderTexture.width - size) / 2;
                int startY = (renderTexture.height - size) / 2;
                image = new Texture2D(size, size);
                image.ReadPixels(new Rect(startX, startY, size, size), 0, 0);
            }

            image.Apply();

            RenderTexture.active = activeRT;
            return image;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CameraRuntime), true), CanEditMultipleObjects]
    sealed class CameraRuntimeEditor : Editor
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



