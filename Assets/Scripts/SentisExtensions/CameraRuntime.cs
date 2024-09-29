using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SentisExtensions
{
    public class CameraRuntime : MonoBehaviour
    {
        [Header("This script must be attached to a Camera gameObject.")]
        [HideInInspector] public Camera cam;
        private RenderTexture renderTexture;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("CameraRuntime was attached to a GameObject without CameraComponent.");
            }

        }

        public RenderTexture GetCamRenderTexture(int cullingMask = -1)
        {
            if (cullingMask != -1)
            {
                cam.cullingMask = cullingMask;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 3);
            }
            cam.targetTexture = renderTexture;
            // RenderTexture.active = renderTexture;

            return renderTexture;
        }
        public Texture2D GetCamTexture2D(bool centerCrop = false, int cullingMask = -1)
        {
            int prev_culling_mask = cam.cullingMask;
            if(cullingMask != -1)
            {
                cam.cullingMask = cullingMask;
            }
            if (cam == null)
            {
                Debug.LogError("CameraRuntime was attached to a GameObject without CameraComponent.");
                return null;
            }

            // Create or resize the render texture if necessary
            if (renderTexture == null || renderTexture.width != cam.pixelWidth || renderTexture.height != cam.pixelHeight)
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                }
                renderTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 3);
            }

            cam.targetTexture = renderTexture;
            cam.Render();

            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D image;
            if (!centerCrop)
            {
                image = new Texture2D(renderTexture.width, renderTexture.height);
                image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            }
            else
            {
                int size = Mathf.Min(renderTexture.width, renderTexture.height);
                int startX = (renderTexture.width - size) / 2;
                int startY = (renderTexture.height - size) / 2;
                image = new Texture2D(size, size, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(startX, startY, size, size), 0, 0);
            }

            image.Apply();

            RenderTexture.active = activeRT;
            cam.targetTexture = null;

            cam.cullingMask = prev_culling_mask;

            return image;
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
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



