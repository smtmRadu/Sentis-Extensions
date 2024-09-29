using System.Collections.Generic;
using Unity.Sentis;
using UnityEditor;
using UnityEngine;

namespace SentisExtensions
{
    public enum Device
    {
        CPU,
        GPUCompute,
        GPUPixel
    }
    public enum ModelAssetType
    {
        ONNX,
        Sentis
    }
    public class ModelRuntime : MonoBehaviour
    {
        [Header("This script runs one auto-disposable worker.")]
        [Tooltip("What extension has the model asset file. Serialize them to .sentis for faster load times.")]
        public ModelAssetType extension = ModelAssetType.ONNX;
        public DefaultAsset modelSentis;
        public ModelAsset modelONNX;
        public BackendType device = BackendType.CPU;

        private Model model_runtime;
        private Worker worker;

        private void Start()
        {
            if (extension == ModelAssetType.ONNX && modelONNX == null)
            {
                Debug.LogError("Please load an onnx model script.");
            }
            else if (extension == ModelAssetType.Sentis && modelSentis == null)
            {
                Debug.LogError("Please load a sentis model script.");
            }

            // Debug.Log(Application.streamingAssetsPath + $"/{modelSentis.name}.sentis");

            if (extension == ModelAssetType.ONNX)
                model_runtime = ModelLoader.Load(modelONNX);
            else if (extension == ModelAssetType.Sentis)
                model_runtime = ModelLoader.Load(Application.streamingAssetsPath + $"/{modelSentis.name}.sentis");
          
            worker = new Worker(model_runtime,device);
        }

        /// <summary>
        /// Note the input and the output will be transfered to GPU if device is set to <see cref="BackendType.GPUCompute"/> or <see cref="BackendType.GPUPixel"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public Tensor Forward(Tensor input)
        {
            if (worker == null)
                throw new System.Exception("Worker was disposed");

            worker.Schedule(input);

            Tensor output = worker.PeekOutput();
            return output;
        }


        private void OnDestroy()
        {
            worker?.Dispose();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ModelRuntime), true), CanEditMultipleObjects]
    sealed class ONNXRuntimeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            List<string> dontDrawMe = new List<string> { "m_Script" };

            ModelRuntime script = target as ModelRuntime;
            if (script.extension == ModelAssetType.ONNX)
                dontDrawMe.Add("modelSentis");
            else
                dontDrawMe.Add("modelONNX");

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, dontDrawMe.ToArray());
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}

