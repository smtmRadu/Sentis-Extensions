using System.Collections.Generic;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace kbradu
{
    public class DisplayRuntime : MonoBehaviour
    {
        [Header("Add this script to an object with an UI Image component.")]
        private Image image;


        private void Start()
        {
            image = GetComponent<Image>();
        }

        public void SetTexture(Texture2D texture)
        {
            Destroy(image.sprite);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
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


